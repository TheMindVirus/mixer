using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenControl : MonoBehaviour
{
    public GameObject auxiliary = null;

    public void SetPanelData(string data)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.LoadImage(System.Convert.FromBase64String(data));
        GetComponent<MeshRenderer>().material.SetTexture("_EmissionMap", tex);
        auxiliary.GetComponent<MeshRenderer>().material.SetTexture("_EmissionMap", tex);
    }
}
