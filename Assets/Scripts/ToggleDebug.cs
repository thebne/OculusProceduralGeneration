using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleDebug : MonoBehaviour
{
    public GameObject Target;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            Target.SetActive(!Target.activeSelf);
        }
    }
}
