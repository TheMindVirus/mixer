#define CONTROL_TEST_MODE
#define CONTROL_EXTERNAL_MODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
public class ChromaData
{
    public string name;
    public int r;
    public int g;
    public int b;
    public int a;
    public int emission;
    public int er;
    public int eg;
    public int eb;
    public int ei;
}

[Serializable]
public class FaderData
{
    public string name;
    public int level;
}

public class ControlControl : MonoBehaviour
{
    private Ray ray;
    private RaycastHit hit;
    private float delta;
    private float sensitivity;
    private bool selected;
    private bool firstChance;
    private UnityEngine.UI.Text HUD;
    private GameObject control;
    private GameObject controlLast;
    private Vector3 origin;
    private Dictionary<string, Vector3> origins;
    private ChromaData chroma;
    private GameObject chromaGo;
    private FaderData fader;
    private GameObject faderGo;
    private float faderDistance;
    private string fullName;

#if CONTROL_EXTERNAL_MODE
    [DllImport("__Internal")]
    private static extern void ControlChange(string name, int value);
#else
    static void ControlChange(string name, int value) { Debug.Log("[MIDI]: " + name + ": " + value.ToString()); }
#endif

    private static string GetControlFullName(GameObject go)
    {
        string path = "/" + go.name;
        while (go.transform.parent != null)
        {
            go = go.transform.parent.gameObject;
            path = "/" + go.name + path;
        }
        return path;
    }

    public void SetChroma(string json)
    {
        chroma = JsonUtility.FromJson<ChromaData>(json);
        chromaGo = GameObject.Find(chroma.name);
        if (chromaGo != null)
        {
            Renderer renderer = chromaGo.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.SetVector("_Color", new Vector4(chroma.r, chroma.g, chroma.b, chroma.a));
                if (chroma.emission != 0)
                {
                    renderer.material.SetVector("_EmissionColor", new Color(chroma.er * chroma.ei,
                                                                            chroma.eg * chroma.ei,
                                                                            chroma.eb * chroma.ei));
                }
            }
        }
    }

    public void SetFader(string json) //!!!EXPERIMENTAL!!!
    {
        fader = JsonUtility.FromJson<FaderData>(json);
        faderGo = GameObject.Find(fader.name);
        if (faderGo != null)
        {
            faderGo.transform.localPosition = new Vector3(fader.level,
                                                          faderGo.transform.localPosition.y,
                                                          faderGo.transform.localPosition.z);
        }
    }

    void Start()
    {
        hit = new RaycastHit();
        ray = new Ray();
        sensitivity = 0.5f;
        selected = false;
        firstChance = true;
        HUD = GameObject.Find("/Player/HUD/Text").GetComponent<UnityEngine.UI.Text>();
        control = null;
        controlLast = null;
        origin = new Vector3(0, 0, 0);
        origins = new Dictionary<string, Vector3>();
        chroma = new ChromaData();
        chromaGo = null;
        fader = new FaderData();
        faderGo = null;
        faderDistance = 170.0f;
        fullName = "";
#if CONTROL_TEST_MODE
        Test();
#endif
    }

#if CONTROL_TEST_MODE
    void Test()
    {
        int missing = 0;
        string missinglabels = "";
        string[] sublabels =
        {
            "InputLevelUpperLeft", "InputLevelUpperRight", "InputLevelLowerLeft", "InputLevelLowerRight",
            "InputLevelMonitor", "InputLevelPhones", "InputAuxLeft", "InputAuxRight",
            "LEDPeakMasterLeft", "LEDPeakMasterRight", "LEDSignalMasterLeft", "LEDSignalMasterRight",
            "MenuScene", "MenuSetup", "MenuMIDI", "MenuUtility", "MenuInsert", "MenuRouting", "MenuGroup", "MenuPatch", "MenuDynamics",
            "MenuEQ", "MenuEffect", "MenuView", "MenuAux1", "MenuAux2", "MenuAux3", "MenuAux4", "MenuAux5", "MenuAux6", "MenuAux7", "MenuAux8",
            "MenuLayer1", "MenuLayer2", "MenuLayer3", "MenuLayer4", "ScreenF0", "ScreenF1", "ScreenF2", "ScreenF3", "ScreenF4", "ScreenF5",
            "ScreenLevelPan", "ScreenLevelQ", "ScreenLevelFrequency", "ScreenLevelGain", "ScreenHigh", "ScreenHighMid", "ScreenLowMid", "ScreenLow",
            "ControlStore", "ControlDown", "ControlUp", "ControlRecall", "ControlLEDSolo", "ControlClearSolo", "ControlDecrease", "ControlIncrease",
            "ControlArrowUp", "ControlArrowDown", "ControlArrowLeft", "ControlArrowRight", "FaderLevelMasterLeft", "FaderLevelMasterRight",
            "FaderSelectMasterLeft", "FaderSelectMasterRight", "FaderEnableMasterLeft", "FaderEnableMasterRight", "UserPage",
            "UserSelectLeft", "UserSelectRight", "UserSoloLeft", "UserSoloRight", "UserEnableLeft", "UserEnableRight",
            "UserGainLeft", "UserGainRight", "UserLayer1", "UserLayer2", "UserLayer3", "UserLayer4", "UserCue",
            "UserLayer5", "UserLayer6", "UserLayer7", "UserLayer8", "UserGo", "ControlLevelWheel", "ControlEnter"
        };
        string[] subsublabels =
        {
            "InputPadCh", "InputLevelCh", "LEDPeakCh", "LEDSignalCh",
            "FaderSelectCh", "FaderSoloCh", "FaderEnableCh", "FaderLevelCh"
        };
        for (int i = 1; i <= 9; ++i)
        {
            foreach (string k in sublabels)
            {
                if ((null == transform.Find(i.ToString() + k))
                &&  (null == transform.Find(" FaderStub").Find(i.ToString() + k)))
                {
                    missing += 1; missinglabels += i.ToString() + k + " | ";
                }
            }
            for (int j = 1; j <= 24; ++j)
            {
                foreach (string k in subsublabels)
                {
                    if ((null == transform.Find(i.ToString() + k + j.ToString()))
                    &&  (null == transform.Find(" FaderStub").Find(i.ToString() + k + j.ToString())))
                    {
                        missing += 1; missinglabels += i.ToString() + k + j.ToString() + " | ";
                    }
                }
            }
        }
        Debug.Log(missing.ToString() + " missing controls detected | " + missinglabels);
    }
#endif

    void Update()
    {
        selected = Input.GetMouseButton(0);
        ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
        if (Physics.Raycast(ray, out hit) && (!selected))
        {
            control = hit.collider.transform.gameObject;
            HUD.text = control.name;
        }
        else if (!selected) { HUD.text = "SpaceShip"; }

        if ((control != null)
        && (!control.name.StartsWith(" "))
        && (!control.name.Equals("Stack"))
        && (!control.name.Equals("Spaceship"))
        && (!control.name.Equals("BigChungusMixer"))) //!!!BUG: Default Select for Buttons includes GameObjects outside of the parent BigChungusMixer
        {
            if (selected)
            {
                if (firstChance) { fullName = GetControlFullName(control); }
                if (control.name.Contains("FaderLevel")) //Faders
                {
                    if (firstChance)
                    {
                        if (!(origins.TryGetValue(fullName, out origin))) //!!!BUG: Using a fader once prevents buttons from working
                        {
                            origin = control.transform.localPosition;
                            origins.Add(fullName, origin);
                        }
                        else { delta = control.transform.localPosition.x - origin.x; }
                    }
                    var previous = control.transform.localPosition;
                    delta += (Input.GetAxis("Mouse Y") * sensitivity * 100.0f);
                    control.transform.localPosition = new Vector3(Mathf.Clamp(origin.x + delta, origin.x - faderDistance, origin.x), origin.y, origin.z);
                    if (control.transform.localPosition != previous)
                    {
                        int value = (int)((((0.5f + (control.transform.localPosition.x - origin.x)) / faderDistance) * 127.0f) + 127.0f);
                        ControlChange(fullName, value);
                    }
                }
                else if (control.name.Contains("Level")) //Knobs
                {
                    if (firstChance)
                    {
                        if (!(origins.TryGetValue(fullName, out origin)))
                        {
                            origin = control.transform.localEulerAngles;
                            origins.Add(fullName, origin);
                        }
                        else { delta = control.transform.localEulerAngles.y + 90.0f; if (delta > 180.0f) { delta -= 360.0f; } }
                    }
                    var prev = control.transform.localEulerAngles;
                    delta += (Input.GetAxis("Mouse Y") * sensitivity * 120.0f);
                    control.transform.localEulerAngles = new Vector3(origin.x, Mathf.Clamp(origin.y + delta, origin.y - 160.0f, origin.y + 160.0f), origin.z);
                    if (control.transform.localEulerAngles != prev)
                    {
                        int value = (int)(((160.0f + (control.transform.localEulerAngles.y - origin.y)) / 320.0f) * 127.0f);
                        if (value < 0) { value += 142; }
                        ControlChange(fullName, value);
                    }
                }
                else if (!control.name.Contains("LED")) //Buttons
                {
                    if (firstChance)
                    {
                        if (!(origins.TryGetValue(fullName, out origin)))
                        {
                            origin = control.transform.localPosition;
                            origins.Add(fullName, origin);
                        }
                        control.transform.localPosition = new Vector3(origin.x, origin.y + 5.0f, origin.z);
                        ControlChange(fullName, 1);
                    }
                }
                controlLast = control; //!!!BUG: control GameObject changes to another GameObject on button release
                firstChance = false;
            }
        }

        selected = Input.GetMouseButton(0);
        if ((controlLast != null)
        && (!controlLast.name.StartsWith(" "))
        && (!controlLast.name.Equals("Stack"))
        && (!controlLast.name.Equals("Spaceship"))
        && (!controlLast.name.Equals("BigChungusMixer"))) //!!!BUG: Default Select for Buttons includes GameObjects outside of the parent BigChungusMixer
        {
            if (!selected)
            {
                fullName = GetControlFullName(controlLast); //!!!BUG: control GameObject changes to another GameObject on first chance button release
                if (controlLast.name.Contains("FaderLevel")) //Faders
                {
                    //No Action
                }
                else if (controlLast.name.Contains("Level")) //Knobs
                {
                    //No Action
                }
                else if (!controlLast.name.Contains("LED")) //Buttons
                {
                    var prev = controlLast.transform.localPosition;
                    if (origins.TryGetValue(fullName, out origin)) { controlLast.transform.localPosition = origin; }
                    if (controlLast.transform.localPosition != prev) { ControlChange(fullName, 0); }
                }
                controlLast = null;
                fullName = "";
                delta = 0.0f;
                firstChance = true;
            }
        }
    }
}
