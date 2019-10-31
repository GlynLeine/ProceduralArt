using System.Collections;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    TerrainGenerator terrainGenerator;

    bool showHeightmap = true;
    float displaySize = .5f;

    bool autoUpdate = false;
    bool autoUpdateNoise = true;
    bool autoUpdateErosion = false;
    bool autoUpdateMesh = false;

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
                EditorGUI.DrawPreviewTexture(displayRect, terrainGenerator.heightMap);

                EditorGUILayout.Space();
            }
        }

        EditorGUI.BeginChangeCheck();

        SerializedProperty property = serializedObject.GetIterator();
        while (property.NextVisible(true))
        {
            EditorGUILayout.PropertyField(property);

            if (property.name == "meshHeight")
                break;
        }

        bool changed = EditorGUI.EndChangeCheck();

        if (terrainGenerator.heightMap != null && GUILayout.Button("Generate mesh"))
        {
            terrainGenerator.GenerateMesh();
        }

        EditorGUI.BeginChangeCheck();

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

        changed = EditorGUI.EndChangeCheck() || changed;

        if (GUILayout.Button("Generate noise map"))
        {
            terrainGenerator.GenerateHeightMap();
        }

        EditorGUI.BeginChangeCheck();

        while (property.NextVisible(true))
        {
            EditorGUILayout.PropertyField(property);
        }

        changed = EditorGUI.EndChangeCheck() || changed;

        if (terrainGenerator.eroding)
        {
            GUI.enabled = false;
            GUILayout.Button("Apply erosion");
            GUI.enabled = true;
            if (GUILayout.Button("Cancel erosion"))
                terrainGenerator.cancelErosion = true;
        }
        else if (terrainGenerator.heightMap != null && GUILayout.Button("Apply erosion"))
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
                terrainGenerator.autoErode = false;
                terrainGenerator.autoGenMesh = false;
            }

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
                {
                    autoUpdateErosion = true;
                    terrainGenerator.autoErode = true;
                }
            }
            else if (GUILayout.Button("Stop automatically updating erosion"))
            {
                autoUpdateErosion = false;
                terrainGenerator.autoErode = false;
            }

            if (!autoUpdateMesh)
            {
                if (GUILayout.Button("Automatically update mesh"))
                {
                    autoUpdateMesh = true;
                    terrainGenerator.autoGenMesh = true;
                }
            }
            else if (GUILayout.Button("Stop automatically updating mesh"))
            {
                autoUpdateMesh = false;
                terrainGenerator.autoGenMesh = false;
            }

            if (changed)
            {
                if (autoUpdateNoise)
                    terrainGenerator.GenerateHeightMap();

                if (terrainGenerator.heightMap != null && autoUpdateErosion && !terrainGenerator.eroding)
                    terrainGenerator.ApplyErosion();

                if (terrainGenerator.heightMap != null && autoUpdateMesh && !autoUpdateErosion)
                    terrainGenerator.GenerateMesh();
            }
        }
        else
        {
            if (GUILayout.Button("Turn on auto update"))
            {
                autoUpdate = true;
                if (autoUpdateErosion)
                    terrainGenerator.autoErode = true;
                if(autoUpdateMesh)
                    terrainGenerator.autoGenMesh = true;
            }
        }

        if (terrainGenerator.heightMap != null && GUILayout.Button("Destroy heightmap"))
        {
            terrainGenerator.heightMap = null;
        }

        if (GUILayout.Button(new GUIContent("Save mesh", "Saves mesh to an OBJ file and points shared mesh to that file instead of storing the mesh in the scene. (greatly improves scene file size.)")))
            terrainGenerator.SaveMesh();

        serializedObject.ApplyModifiedProperties();
    }

    void OnEnable()
    {
        terrainGenerator = (TerrainGenerator)target;
        //Tools.hidden = true;
    }

    void OnDisable()
    {
        //Tools.hidden = false;
    }
}
