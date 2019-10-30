using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshData))]
public class MeshDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if(GUILayout.Button("Fetch Data"))
            (target as MeshData).FetchData();
    }
}