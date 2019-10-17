using System.Collections;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGenerator2))]
public class TerrainGenerator2Editor : Editor
{
    TerrainGenerator2 terrainGenerator;

    bool showHeightmap = true;
    float displaySize = .5f;

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

                if(terrainGenerator.readTex != null)
                    EditorGUI.DrawPreviewTexture(displayRect, terrainGenerator.readTex);
                else
                    EditorGUI.DrawPreviewTexture(displayRect, terrainGenerator.heightMap);

                EditorGUILayout.Space();
            }
        }

        EditorGUI.BeginChangeCheck();

        SerializedProperty property = serializedObject.GetIterator();
        while(property.NextVisible(true))
        {
            EditorGUILayout.PropertyField(property);

            if (property.name == "uvScale")
                break;
        }

        if (!autoUpdate && GUILayout.Button("Generate mesh"))
        {
            terrainGenerator.GenerateMesh();
        }

        while (property.NextVisible(true))
        {
            if (property.name == "seed")
            {
                EditorGUILayout.BeginHorizontal();
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 0.000000000001f;
                EditorGUILayout.LabelField("Seed");
                EditorGUIUtility.labelWidth = labelWidth;

                if (GUILayout.Button("Randomize seed", GUILayout.MaxHeight(15f)))
                    terrainGenerator.seed = Random.Range(int.MinValue, int.MaxValue);

                terrainGenerator.seed = EditorGUILayout.IntField(terrainGenerator.seed);

                EditorGUILayout.EndHorizontal();
                break;
            }

            if (property.name != "m_Script")
                EditorGUILayout.PropertyField(property);
        }

        if (!autoUpdate && GUILayout.Button("Generate noise map"))
        {
            terrainGenerator.GenerateHeightMap();
        }

        while (property.NextVisible(true))
        {
            EditorGUILayout.PropertyField(property);
        }

        bool changed = EditorGUI.EndChangeCheck();

        if (!autoUpdate && terrainGenerator.heightMap != null && GUILayout.Button("Apply erosion"))
        {
            terrainGenerator.ApplyErosion();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        if (autoUpdate)
        {
            if (GUILayout.Button("Turn off auto update"))
            {
                autoUpdate = false;
            }

            EditorGUILayout.BeginHorizontal();

            if (!autoUpdateNoise)
            {
                if (GUILayout.Button("Automatically update noise map"))
                    autoUpdateNoise = true;
            }
            else if (GUILayout.Button("Stop automatically updating noise map"))
                autoUpdateNoise = false;

            if (!autoUpdateErosion)
            {
                if (GUILayout.Button("Automatically update erosion"))
                    autoUpdateErosion = true;
            }
            else if (GUILayout.Button("Stop automatically updating erosion"))
                autoUpdateErosion = false;

            EditorGUILayout.EndHorizontal();

            if (changed)
            {
                if (autoUpdateNoise)
                    terrainGenerator.GenerateHeightMap();

                if (terrainGenerator.heightMap != null && autoUpdateErosion)
                    terrainGenerator.ApplyErosion();
            }
        }
        else
        {
            if (GUILayout.Button("Turn on auto update"))
            {
                autoUpdate = true;
            }
        }

        if (terrainGenerator.heightMap != null && GUILayout.Button("Destroy heightmap"))
        {
            terrainGenerator.heightMap = null;
        }

        serializedObject.ApplyModifiedProperties();
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
