using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    private float speed;
    private float sensitivity;
    private Vector3 facing;
    private Vector3 direction;

    void Start()
    {
        speed = 0.05f;
        sensitivity = 1.5f;
    }

    void Update()
    {
        if (!Input.GetMouseButton(0))
        {
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x + (Input.GetAxis("Mouse Y") * sensitivity * -1.0f),
                                                     transform.localEulerAngles.y + (Input.GetAxis("Mouse X") * sensitivity),
                                                     transform.localEulerAngles.z);

            direction = new Vector3(0, 0, 0);
            if (Input.GetKey(KeyCode.W)) { direction += new Vector3( 0,  0,  1); }
            if (Input.GetKey(KeyCode.S)) { direction += new Vector3( 0,  0, -1); }
            if (Input.GetKey(KeyCode.A)) { direction += new Vector3(-1,  0,  0); }
            if (Input.GetKey(KeyCode.D)) { direction += new Vector3( 1,  0,  0); }
            transform.Translate(direction * speed);
        }
    }
}