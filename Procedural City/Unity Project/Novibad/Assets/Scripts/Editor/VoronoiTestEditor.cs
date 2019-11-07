using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoronoiTest))]
public class VoronoiTestEditor : Editor
{
    VoronoiTest voronoi;

    public override void OnInspectorGUI()
    {
        if(voronoi == null)
            voronoi = target as VoronoiTest;



        if (GUILayout.Button("Generate"))
        {
            voronoi.Generate();
        }
    }
}
