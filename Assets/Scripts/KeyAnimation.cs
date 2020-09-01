using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyAnimation : MonoBehaviour
{
    public float MovementSpeed = 1f;
    public float DistanceVariance = 0.5f;
    public AnimationCurve DistanceCurve;
    public float RotationSpeed = 1f;
    public Vector3 RotationVariance = new Vector3(5f, 10f, 5f);
    public Vector3 RotationPhase = new Vector3(0f, 1f, 2f);
    void Start()
    {
        StartCoroutine(nameof(ShowEffect));
    }

    IEnumerator ShowEffect()
    {
        var t = 0f;
        while (true)
        {
            t += Time.fixedDeltaTime;
            transform.localPosition = Vector3.up * DistanceVariance * DistanceCurve.Evaluate((t * MovementSpeed) % 1);
            var x = RotationVariance.x * DistanceCurve.Evaluate(((t + RotationPhase.x) * RotationSpeed) % 1);
            var y = RotationVariance.y * DistanceCurve.Evaluate(((t + RotationPhase.y) * RotationSpeed) % 1);
            var z = RotationVariance.z * DistanceCurve.Evaluate(((t + RotationPhase.z) * RotationSpeed) % 1);
            transform.localRotation = Quaternion.Euler(x, y, z);
            yield return new WaitForFixedUpdate();
        }
    }
}
