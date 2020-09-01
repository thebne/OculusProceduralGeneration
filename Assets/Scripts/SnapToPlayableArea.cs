using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SnapToPlayableArea : MonoBehaviour
{
    public GuardianBoundaryEnforcer m_enforcer;
    public enum AttachType
    {
        Point0,
        Point1,
        Point2,
        Point3,
        Center,
    }
    public AttachType AttachTo = AttachType.Center;
    public float YOffset = 0f;
    public float ProximityToCenter = 0f;
    public bool LookTowardsCenter = false;
    

    void Start()
    {
        m_enforcer.TrackingChanged += RefreshDisplay;
        RefreshDisplay();
    }

    void RefreshDisplay()
    {
#if !UNITY_EDITOR
        bool configured = OVRManager.boundary.GetConfigured();
        if (!configured)
        {
            Debug.LogWarning("boundary not configured");
            // TODO show error here / someplace else?
            return;
        }
#endif

#if !UNITY_EDITOR
        var boundary = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
#else
        var boundary = GameObject.Find("/GuardianDebug/Loader").GetComponent<GuardianJsonLoader>().PlayAreaPositions.ToArray();
#endif
        
        Vector3 pos;
        var center = (boundary[0] + boundary[1] + boundary[2] + boundary[3]) / 4;
        switch (AttachTo)
        {
            case AttachType.Point0:
                pos = boundary[0];
                break;
            case AttachType.Point1:
                pos = boundary[1];
                break;
            case AttachType.Point2:
                pos = boundary[2];
                break;
            case AttachType.Point3:
                pos = boundary[3];
                break;
            case AttachType.Center:
            default:
                pos = center;
                break;
        }
        pos += (center - pos) * ProximityToCenter;
        var origY = transform.position.y;
        transform.position = pos;
        if (LookTowardsCenter)
        {
            transform.LookAt(center, Vector3.up);
        }

        pos.y = origY + YOffset;
        transform.position = pos;
    }

}
