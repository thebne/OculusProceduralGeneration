using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GuardianJsonRecorder : MonoBehaviour
{
    public GuardianBoundaryEnforcer m_enforcer;

    // Start is called before the first frame update
    void Update()
    {
        if (!OVRInput.GetDown(OVRInput.Button.Two))
        {
            return;
        }

        bool configured = OVRManager.boundary.GetConfigured();
        if (!configured)
        {
            return;
        }

        Debug.Log("Dumping Guardian data to json");

        var outerBoundary = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.OuterBoundary);
        var outerBoundaryDim = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.OuterBoundary);
        var playArea = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
        var playAreaDim = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);

        List<object> boundaryObjects = new List<object>();
        foreach (var pt in outerBoundary)
        {
            var testRes = OVRManager.boundary.TestPoint(pt, OVRBoundary.BoundaryType.OuterBoundary);
            boundaryObjects.Add(new
            {
                Point = pt,
                Normal = testRes.ClosestPointNormal
            });
        }
        List<object> paObjects = new List<object>();
        foreach (var pt in playArea)
        {
            var testRes = OVRManager.boundary.TestPoint(pt, OVRBoundary.BoundaryType.PlayArea);
            paObjects.Add(new
            {
                Point = pt,
                Normal = testRes.ClosestPointNormal
            });
        }

        string json = JsonConvert.SerializeObject(new
        {
            OuterBoundary = new
            {
                Dimensions = outerBoundaryDim,
                Data = boundaryObjects
            },
            PlayArea = new
            {
                Dimensions = playAreaDim,
                Data = paObjects
            }
        });

        var path = Application.persistentDataPath +
            $"/GuardianDumps_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.json";
        using (StreamWriter writer = new StreamWriter(path, false))
        {
            writer.Write(json);
        }
        Debug.Log($"Guardian Json dumped to {path}");
    }
}
