using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseWhenNearby : MonoBehaviour
{
    public Transform Target;
    public float LowestAngle = 0f;
    public float HighestAngle = 130f;
    public float MaxDistance = 1f;
    public float MinDistance = .2f;

    Vector3 initialRot;

    void Start()
    {
        initialRot = transform.localRotation.eulerAngles;
    }
    
    void Update()
    {
        var dist = Vector3.Distance(Target.position, transform.position);
        var distRatio = Mathf.Min(1f, Mathf.Max(0f, dist - MinDistance) / (MaxDistance - MinDistance));

        initialRot.x = Mathf.Lerp(LowestAngle, HighestAngle, distRatio);
        transform.localRotation = Quaternion.Euler(initialRot);
    }
}
