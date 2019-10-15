using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGen))]
public class TerrainGenEditor : Editor
{
    TerrainGen terrainGenerator;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Update Mesh"))
        {
            terrainGenerator.UpdateMesh();
        }

        if (GUILayout.Button("Reconstruct Mesh"))
        {
            terrainGenerator.mesh = null;
            terrainGenerator.UpdateMesh();
        }
    }

    void OnEnable()
    {
        terrainGenerator = (TerrainGen)target;
        Tools.hidden = true;
    }

    void OnDisable()
    {
        Tools.hidden = false;
    }
}
