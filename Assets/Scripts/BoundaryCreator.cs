using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundaryCreator : MonoBehaviour
{
    // XXX Depending on the demo manager only for reorient notifications.
    public GuardianBoundaryEnforcer m_enforcer;
    public GuardianBoundaryMesh m_prefab;
    
    void Start()
    {
        m_enforcer.TrackingChanged += RefreshDisplay;
        RefreshDisplay();
    }

    void RefreshDisplay()
    {
        if (transform.childCount > 0)
        {
            Destroy(transform.GetChild(0).gameObject);
        }

        var newBoundary = Instantiate(m_prefab);
        newBoundary.transform.SetParent(transform);
    }
}
