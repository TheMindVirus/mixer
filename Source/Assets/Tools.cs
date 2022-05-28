using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

public class Tools : MonoBehaviour
{
    [MenuItem("GameObject/Tools/Merge")]
    static void Merge(MenuCommand menuCommand)
    {
        if (menuCommand.context != Selection.activeObject) { return; }
        GameObject go = new GameObject("New Object", typeof(MeshFilter), typeof(MeshRenderer));
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
        mesh = new Mesh(); mesh.name = "Mesh";

        List<long> offsets = new List<long>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvmaps = new List<Vector2>();
        List<List<int>> triangles = new List<List<int>>();
        List<Material> materials = new List<Material>();
        List<SubMeshDescriptor> submeshes = new List<SubMeshDescriptor>();

        offsets.Add(0);
        mesh.subMeshCount = 0;
        int last = Array.IndexOf(Selection.transforms, Selection.activeTransform);
        for (int t = 0; t < Selection.transforms.Length; ++t)
        {
            MeshRenderer subrenderer = Selection.transforms[t].GetComponent<MeshRenderer>();
            MeshFilter subfilter = Selection.transforms[t].GetComponent<MeshFilter>();
            if ((subrenderer == null) || (subfilter == null)) { continue; }
            mesh.subMeshCount += subfilter.sharedMesh.subMeshCount;
            for (int i = 0; i < subfilter.sharedMesh.subMeshCount; ++i)
            {
                submeshes.Add(subfilter.sharedMesh.GetSubMesh(i));
            }
            foreach (Material m in subrenderer.sharedMaterials)
            {
                materials.Add(new Material(m));
            }
            List<Vector3> verts = new List<Vector3>(subfilter.sharedMesh.vertices);
            for (int i = 0; i < verts.Count; ++i)
            {
                verts[i] = Selection.transforms[last].InverseTransformPoint(Selection.transforms[t].TransformPoint(verts[i]));
            }
            vertices.AddRange(verts);
            offsets.Add(vertices.Count);
            uvmaps.AddRange(new List<Vector2>(subfilter.sharedMesh.uv));
            for (int i = 0; i < subfilter.sharedMesh.subMeshCount; ++i)
            {
                List<int> tris = new List<int>();
                subfilter.sharedMesh.GetTriangles(tris, i);
                for (int j = 0; j < tris.Count; ++j) { tris[j] += (int)offsets[t]; }
                triangles.Add(tris);
            }
        }
        renderer.materials = materials.ToArray();
        mesh.vertices = vertices.ToArray();
        mesh.uv  = uvmaps.ToArray();
        for (int i = 0; i < mesh.subMeshCount; ++i)
        {
            mesh.SetTriangles(triangles[i], i);
        }
        mesh.RecalculateNormals();

        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.transform.parent = Selection.transforms[last].parent;
        go.transform.localPosition = Selection.transforms[last].localPosition;
        go.transform.localRotation = Selection.transforms[last].localRotation;
        go.transform.localScale = Selection.transforms[last].localScale;
        Selection.activeObject = go;
    }

    [MenuItem("GameObject/Tools/Trim Recursive")]
    static void TrimRecursive(MenuCommand menuCommand)
    {
        foreach (Transform t in Selection.transforms)
        {
            long count = RecursiveTrim(t);
            Debug.Log("[TRIM]: " + t.name + " | Trimmed " + count.ToString() + " objects");
        }
    }

    static long RecursiveTrim(Transform t, long index = 0, long level = 0)
    {
        long i = 0;
        long count = 0;
        foreach (Transform subt in t)
        {
            count += RecursiveTrim(subt, i, level + 1);
            ++i;
        }
        if ((i == 0) && (index != 0))
        {
            Debug.Log("[TRIM]: " + t.name);
            GameObject.DestroyImmediate(t.gameObject);
            ++count;
        }
        return count;
    }

    [MenuItem("GameObject/Tools/Ripple Reparent")]
    static void RippleReparent(MenuCommand menuCommand)
    {
        Transform grandParent = Selection.transforms[0].parent.parent;
        List<Transform> rippleSelect = new List<Transform>();
        foreach (Transform parent in grandParent)
        {
            rippleSelect.Add(parent.GetChild(0));
        }
        foreach (Transform child in rippleSelect)
        {
            Transform tmp = child.parent;
            child.parent = grandParent;
            GameObject.DestroyImmediate(tmp.gameObject);
        }
    }

    [MenuItem("GameObject/Tools/Get Bounds")]
    static void GetBounds(MenuCommand menuCommand)
    {
        Debug.Log(Selection.activeGameObject.GetComponent<MeshFilter>().sharedMesh.bounds.size);
    }

    [MenuItem("GameObject/Tools/Get Center")]
    static void GetCenter(MenuCommand menuCommand)
    {
        Debug.Log(Selection.activeGameObject.GetComponent<MeshFilter>().sharedMesh.bounds.center);
    }

    [MenuItem("GameObject/Tools/Join Both")]
    static void JoinBoth(MenuCommand menuCommand)
    {
        if (menuCommand.context != Selection.activeObject) { return; }
        GameObject newObject = new GameObject("New Object", typeof(MeshFilter), typeof(MeshRenderer));
        MeshRenderer renderer = newObject.transform.GetComponent<MeshRenderer>();
        MeshFilter filter = newObject.transform.GetComponent<MeshFilter>();
        List<MeshFilter> filters = new List<MeshFilter>();

        foreach (Transform t in Selection.transforms)
        {
            filters.Add(t.GetComponent<MeshFilter>());
        }
 
        CombineInstance[] combine = new CombineInstance[filters.Count];
        for (int i = 0; i < filters.Count; ++i)
        {
            combine[i].mesh = filters[i].sharedMesh;
            combine[i].transform = Matrix4x4.identity;
            combine[0].transform = Matrix4x4.Translate(Vector3.Scale(filters[0].sharedMesh.bounds.center, new Vector3(1.00f, 1.00f, 1.00f))); //Why is it off 
            combine[1].transform = Matrix4x4.Translate(Vector3.Scale(filters[0].sharedMesh.bounds.center, new Vector3(0.25f, 0.25f, 0.25f))); //by this much?
        }
 
        filter.sharedMesh = new Mesh();
        filter.sharedMesh.CombineMeshes(combine, true, true, true);
        renderer.material = Selection.transforms[0].GetComponent<Renderer>().sharedMaterial;
        newObject.transform.parent = Selection.transforms[0].parent;
        newObject.transform.localPosition = Selection.transforms[0].localPosition - filters[0].sharedMesh.bounds.center;
        newObject.transform.localRotation = Selection.transforms[0].localRotation;
        newObject.transform.localScale = Selection.transforms[0].localScale;
        Selection.activeObject = newObject;
    }

    [MenuItem("GameObject/Tools/Join Both (Inverted Selection)")]
    static void JoinBothInvertedSelection(MenuCommand menuCommand)
    {
        if (menuCommand.context != Selection.activeObject) { return; }
        GameObject newObject = new GameObject("New Object", typeof(MeshFilter), typeof(MeshRenderer));
        MeshRenderer renderer = newObject.transform.GetComponent<MeshRenderer>();
        MeshFilter filter = newObject.transform.GetComponent<MeshFilter>();
        List<MeshFilter> filters = new List<MeshFilter>();

        foreach (Transform t in Selection.transforms)
        {
            filters.Add(t.GetComponent<MeshFilter>());
        }
 
        CombineInstance[] combine = new CombineInstance[filters.Count];
        for (int i = 0; i < filters.Count; ++i)
        {
            combine[i].mesh = filters[i].sharedMesh;
            combine[i].transform = Matrix4x4.identity;
            combine[1].transform = Matrix4x4.Translate(Vector3.Scale(filters[1].sharedMesh.bounds.center, new Vector3(1.00f, 1.00f, 1.00f))); //Why is it off
            combine[0].transform = Matrix4x4.Translate(Vector3.Scale(filters[1].sharedMesh.bounds.center, new Vector3(0.12f, 0.12f, 0.12f))); //by this much?
        }
 
        filter.sharedMesh = new Mesh();
        filter.sharedMesh.CombineMeshes(combine, true, true, true);
        renderer.material = Selection.transforms[0].GetComponent<Renderer>().sharedMaterial;
        newObject.transform.parent = Selection.transforms[0].parent;
        newObject.transform.localPosition = Selection.transforms[0].localPosition - Vector3.Scale(filters[1].sharedMesh.bounds.center, new Vector3(0.12f, 0.12f, 0.12f));
        newObject.transform.localRotation = Selection.transforms[0].localRotation;
        newObject.transform.localScale = Selection.transforms[0].localScale;
        Selection.activeObject = newObject;
    }

    [MenuItem("GameObject/Tools/Join SubMesh")]
    static void JoinSubMesh(MenuCommand menuCommand)
    {
        if (menuCommand.context != Selection.activeObject) { return; }
        GameObject newObject = new GameObject("New Object", typeof(MeshFilter), typeof(MeshRenderer));
        MeshRenderer renderer = newObject.transform.GetComponent<MeshRenderer>();
        MeshFilter filter = newObject.transform.GetComponent<MeshFilter>();
        List<long> offsets = new List<long>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvmaps = new List<Vector2>();
        List<List<int>> triangleSet = new List<List<int>>();
        List<Material> materials = new List<Material>();
        filter.sharedMesh = new Mesh();

        int index = 0;
        offsets.Add(0);
        foreach (Transform t in Selection.transforms)
        {
            MeshFilter subfilter = t.GetComponent<MeshFilter>();
            if (subfilter != null)
            {
                List<Vector3> tmpverts = new List<Vector3>();
                List<Vector2> tmpuvs = new List<Vector2>();
                List<int> tmptris = new List<int>();

                subfilter.sharedMesh.GetVertices(tmpverts);
                offsets.Add(tmpverts.Count);
                
                for (int i = 0; i < tmpverts.Count; ++i)
                {
                    //tmpverts[i] = Selection.transforms[0].localToWorldMatrix.MultiplyPoint3x4(tmpverts[i]);
                    //tmpverts[i] = Selection.transforms[0].worldToLocalMatrix.MultiplyPoint3x4(tmpverts[i]);
                    //tmpverts[i] = Matrix4x4.Rotate(Quaternion.Euler(0.00f, 90.00f, 0.00f)).MultiplyPoint3x4(tmpverts[i]);
                    //tmpverts[i] = Matrix4x4.Rotate(t.rotation).MultiplyPoint3x4(tmpverts[i]);
                    //tmpverts[i] = Matrix4x4.Rotate(Quaternion.Inverse(Selection.transforms[0].rotation)).MultiplyPoint3x4(tmpverts[i]);
                    //tmpverts[i] = Matrix4x4.Rotate(Quaternion.Inverse(t.rotation)).MultiplyPoint3x4(tmpverts[i]);
                    //if (index > 0) { tmpverts[i] = Matrix4x4.Rotate(Quaternion.Inverse(Selection.transforms[index - 1].rotation)).MultiplyPoint3x4(tmpverts[i]); }
                    //if (index > 0) { tmpverts[i] = Matrix4x4.Rotate(Selection.transforms[index - 1].rotation).MultiplyPoint3x4(tmpverts[i]); }
                    //if (index > 0) { tmpverts[i] = Matrix4x4.Rotate(Quaternion.Euler(0.0f, 180.0f, 0.0f)).MultiplyPoint3x4(tmpverts[i]); }
                    Transform p = t;
                    while (p.parent != null) { p = p.parent; }
                    //Debug.Log(p.rotation);
                    //Debug.Log(subfilter.sharedMesh.bounds.center);
                    if (index > 0)
                    {
                        //tmpverts[i] = Matrix4x4.Rotate(Quaternion.Inverse(p.rotation)).MultiplyPoint3x4(tmpverts[i]);
tmpverts[i] = Matrix4x4.Rotate(Quaternion.Inverse(Selection.transforms[0].rotation)).MultiplyPoint3x4(tmpverts[i]);
tmpverts[i] = Matrix4x4.Rotate(Selection.transforms[index].rotation).MultiplyPoint3x4(tmpverts[i]);
                        tmpverts[i] = Matrix4x4.Translate(Vector3.Scale(p.position, new Vector3(0.0f, 0.0f, 1.0f))).MultiplyPoint3x4(tmpverts[i]);
                        tmpverts[i] = Matrix4x4.Translate(Vector3.Scale(subfilter.sharedMesh.bounds.center, new Vector3(0.0f, 0.0f, -1.0f))).MultiplyPoint3x4(tmpverts[i]);
                    }   //What on Earth is going on here!?
                    vertices.Add(tmpverts[i]);
                }

                for (int i = 0; i < 8; ++i) //FIXED 8-UV-CHANNEL LIMIT
                {
                    subfilter.sharedMesh.GetUVs(i, tmpuvs);
                    uvmaps.AddRange(tmpuvs);
                }
                
                for (int i = 0; i < subfilter.sharedMesh.subMeshCount; ++i)
                {
                    subfilter.sharedMesh.GetTriangles(tmptris, i, false);
                    triangleSet.Add(tmptris);
                }
            }
            MeshRenderer subrenderer = t.GetComponent<MeshRenderer>();
            if (subrenderer != null)
            {
                foreach (Material m in subrenderer.sharedMaterials)
                {
                    materials.Add(new Material(m));
                }
            }
            ++index;
        }

        filter.sharedMesh.subMeshCount = 2; //triangleSet.Count;
        filter.sharedMesh.SetVertices(vertices.ToArray());
        filter.sharedMesh.SetUVs(0, uvmaps.ToArray());
        for (int i = 0; i < filter.sharedMesh.subMeshCount; ++i)
        {
            if (i > 0)
            {
                for (int j = 0; j < triangleSet[i].Count; ++j)
                {
                    triangleSet[i][j] += (int)(offsets[i]); //64-bit -> 32-bit loss of data
                }
            }
            filter.sharedMesh.SetTriangles(triangleSet[i].ToArray(), i);
        }
        filter.sharedMesh.RecalculateNormals();

        renderer.sharedMaterials = materials.ToArray();
        newObject.transform.parent = Selection.transforms[0].parent;
        newObject.transform.localPosition = Selection.transforms[0].localPosition;
        newObject.transform.localRotation = Selection.transforms[0].localRotation;
        newObject.transform.localScale = Selection.transforms[0].localScale;
        Selection.activeObject = newObject;
    }

    [MenuItem("GameObject/Tools/Mesh Recenter")]
    static void MeshRecenter(MenuCommand menuCommand)
    {
        Mesh mesh = Selection.activeGameObject.GetComponent<MeshFilter>().sharedMesh;
        List<Vector3> verts = new List<Vector3>();
        mesh.GetVertices(verts);
        Debug.Log("[MESH]: Previous Location: " + Selection.activeGameObject.transform.localPosition.ToString());
        for (int i = 0; i < verts.Count; ++i)
        {
            //verts[i] = new Vector3(verts[i].x - Selection.activeGameObject.transform.localPosition.x,
            //                       verts[i].y - Selection.activeGameObject.transform.localPosition.y,
            //                       verts[i].z - Selection.activeGameObject.transform.localPosition.z);
            verts[i] = new Vector3(verts[i].x - 0.5f,
                                   verts[i].y - 0.5f,
                                   verts[i].z - 0.5f);
        }
        mesh.SetVertices(verts);
        Selection.activeGameObject.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
    }

    [MenuItem("GameObject/Tools/Flip Normals")]
    static void FlipNormals(MenuCommand menuCommand)
    {
        foreach (Transform t in Selection.transforms)
        {
            MeshFilter filter = t.GetComponent<MeshFilter>();
            if (filter != null)
            {  
                List<Vector3> normals = new List<Vector3>();
                filter.sharedMesh.GetNormals(normals);
                for (int i = 0; i < normals.Count; ++i)
                {
                    Debug.Log(normals[i]);
                    normals[i] = new Vector3(-1.0f * normals[i].x,
                                             -1.0f * normals[i].y,
                                             -1.0f * normals[i].z);
                }
                filter.sharedMesh.SetNormals(normals, 0, normals.Count);
            }
        }
    }
}

#endif
