using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CelestialBody))]
public class CelestialBodyInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CelestialBody body = (CelestialBody)target;
        
    }
}
