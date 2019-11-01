using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(BuildingGenerator))]
public class BuildingGeneratorEditor : Editor
{
    BuildingGenerator building;

    public override void OnInspectorGUI()
    {
        if (building == null)
            building = target as BuildingGenerator;

        EditorGUI.BeginChangeCheck();

        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck() | GUILayout.Button("Generate"))
        {
            building.CalculateBounds(building.transform.hasChanged);
            building.GenerateMesh();
        }
    }

    void OnSceneGUI()
    {
        if (building == null)
            building = target as BuildingGenerator;

        EditorGUI.BeginChangeCheck();
        Vector3 axisStartWorld = new Vector3(building.allignmentAxisStart.x, 0, building.allignmentAxisStart.y);
        Vector3 axisEndWorld = new Vector3(building.allignmentAxisEnd.x, 0, building.allignmentAxisEnd.y);

        Vector3 axisNormal = (axisEndWorld - axisStartWorld).normalized;
        axisNormal = new Vector3(-axisNormal.z, axisNormal.y, axisNormal.x);

        Handles.color = Color.white;
        Handles.DrawLine(axisStartWorld, axisEndWorld);

        for (int i = 0; i < 4; i++)
            Handles.DrawLine(new Vector3(building.buildingBounds[i].x, 0, building.buildingBounds[i].y), new Vector3(building.buildingBounds[(i + 1) % 4].x, 0, building.buildingBounds[(i + 1) % 4].y));


        if (building.constraintBounds != null && building.constraintBounds.Length > 2)
        {
            int constraintVertexCount = building.constraintBounds.Length;
            for (int i = 0; i < constraintVertexCount; i++)
            {
                Vector3 pos = new Vector3(building.constraintBounds[i].x, 0, building.constraintBounds[i].y);

                Vector3 leftPos = new Vector3(building.constraintBounds[(i - 1 + constraintVertexCount) % constraintVertexCount].x, 0, building.constraintBounds[(i - 1 + constraintVertexCount) % constraintVertexCount].y);
                Vector3 rightPos = new Vector3(building.constraintBounds[(i + 1) % constraintVertexCount].x, 0, building.constraintBounds[(i + 1) % constraintVertexCount].y);
                Vector3 vertexNormal = ((leftPos - pos).normalized +  (rightPos - pos).normalized).normalized;

                pos = Handles.DoPositionHandle(pos, Quaternion.LookRotation(vertexNormal, Vector3.up));
                building.constraintBounds[i] = new Vector2(pos.x, pos.z);
                Handles.DrawLine(new Vector3(building.constraintBounds[i].x, 0, building.constraintBounds[i].y), new Vector3(building.constraintBounds[(i + 1) % constraintVertexCount].x, 0, building.constraintBounds[(i + 1) % constraintVertexCount].y));
            }
        }
        else
        {
            axisStartWorld = Handles.DoPositionHandle(axisStartWorld, Quaternion.LookRotation(axisNormal, Vector3.up));
            axisEndWorld = Handles.DoPositionHandle(axisEndWorld, Quaternion.LookRotation(axisNormal, Vector3.up));

            building.allignmentAxisStart = new Vector2(axisStartWorld.x, axisStartWorld.z);
            building.allignmentAxisEnd = new Vector2(axisEndWorld.x, axisEndWorld.z);
        }

        if (EditorGUI.EndChangeCheck() || building.transform.hasChanged)
        {
            building.CalculateBounds(building.transform.hasChanged);
            building.GenerateMesh();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            building.transform.hasChanged = false;
        }
    }
}
