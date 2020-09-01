using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableMSAABug : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        OVRManager.instance.useRecommendedMSAALevel = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
