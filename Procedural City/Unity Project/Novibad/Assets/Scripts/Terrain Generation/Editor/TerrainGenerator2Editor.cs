using System.Collections;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGenerator2))]
public class TerrainGenerator2Editor : Editor
{
    TerrainGenerator2 terrainGenerator;

    bool showHeightmap;
    float displaySize = 100f;

    bool autoUpdate = false;
    bool autoUpdateNoise = true;
    bool autoUpdateErosion = false;

    public override void OnInspectorGUI()
    {
        if (terrainGenerator.heightMap != null)
        {
            EditorGUILayout.BeginHorizontal();

            GUIStyle style = new GUIStyle(EditorStyles.foldout);
            style.fontStyle = FontStyle.Bold;
            showHeightmap = EditorGUILayout.Foldout(showHeightmap, "Heightmap", style);

            if (showHeightmap)
            {
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent("size")).x;
                displaySize = EditorGUILayout.Slider("size", displaySize, 0, 1);
                EditorGUIUtility.labelWidth = labelWidth;
            }

            EditorGUILayout.EndHorizontal();

            if (showHeightmap)
            {
                Rect displayRect;
                if (displaySize < 1)
                {
                    float layoutWidth = EditorGUIUtility.currentViewWidth - (EditorGUI.indentLevel + 2) * 10f;
                    displayRect = GUILayoutUtility.GetAspectRect(1, GUILayout.Width(displaySize * layoutWidth));
                }
                else
                    displayRect = GUILayoutUtility.GetAspectRect(1);

                if (terrainGenerator.readTex != null)
                    EditorGUI.DrawPreviewTexture(displayRect, terrainGenerator.readTex);
                else
                    EditorGUI.DrawPreviewTexture(displayRect, terrainGenerator.heightMap);

                EditorGUILayout.Space();
            }
        }

        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();


        if (autoUpdate)
        {
            if (GUILayout.Button("Turn off auto update"))
            {
                autoUpdate = false;
            }

            if (!autoUpdateNoise)
            {
                if (GUILayout.Button("Automatically update heightmap"))
                    autoUpdateNoise = true;
            }
            else if (GUILayout.Button("Stop automatically updating heightmap"))
                autoUpdateNoise = false;

            if (!autoUpdateErosion)
            {
                if (GUILayout.Button("Automatically update erosion"))
                    autoUpdateErosion = true;
            }
            else if (GUILayout.Button("Stop automatically updating erosion"))
                autoUpdateErosion = false;

            if (autoUpdateNoise)
                terrainGenerator.ApplyNoiseHeight();

            if (terrainGenerator.heightMap != null && autoUpdateErosion)
                terrainGenerator.ApplyErosion();
        }
        else
        {
            if (GUILayout.Button("Turn on auto update"))
            {
                autoUpdate = true;
            }

            if (GUILayout.Button("Generate heightmap"))
            {
                terrainGenerator.GenerateHeightMap();
            }

            if (terrainGenerator.heightMap != null && GUILayout.Button("Apply erosion"))
            {
                terrainGenerator.ApplyErosion();
            }
        }

        if (terrainGenerator.heightMap != null && GUILayout.Button("Destroy heightmap"))
        {
            terrainGenerator.heightMap = null;
        }
    }

    void OnEnable()
    {
        terrainGenerator = (TerrainGenerator2)target;
        Tools.hidden = true;
    }

    void OnDisable()
    {
        Tools.hidden = false;
    }
}
