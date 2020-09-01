using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GuardianJsonLoader : MonoBehaviour
{
    public TextAsset JsonFile;

    public List<Vector3> OuterBoundaryPositions { get; private set; } = new List<Vector3>();
    public List<Vector3> OuterBoundaryNormals { get; private set; } = new List<Vector3>();
    public object OuterBoundaryDimensions { get; private set; }
    public List<Vector3> PlayAreaPositions { get; private set; } = new List<Vector3>();
    public List<Vector3> PlayAreaNormals { get; private set; } = new List<Vector3>();
    public object PlayAreaDimensions { get; private set; }

    void Awake()
    {
        dynamic obj = JsonConvert.DeserializeObject(JsonFile.text);
        foreach (var pt in obj["OuterBoundary"]["Data"])
        {
            OuterBoundaryPositions.Add(new Vector3((float)pt["Point"]["x"], (float)pt["Point"]["y"], (float)pt["Point"]["z"]));
            OuterBoundaryNormals.Add(new Vector3((float)pt["Normal"]["z"], (float)pt["Normal"]["z"], (float)pt["Normal"]["z"]));
        }
        foreach (var pt in obj["PlayArea"]["Data"])
        {
            PlayAreaPositions.Add(new Vector3((float)pt["Point"]["x"], (float)pt["Point"]["y"], (float)pt["Point"]["z"]));
            PlayAreaNormals.Add(new Vector3((float)pt["Normal"]["z"], (float)pt["Normal"]["z"], (float)pt["Normal"]["z"]));
        }
    }
}
