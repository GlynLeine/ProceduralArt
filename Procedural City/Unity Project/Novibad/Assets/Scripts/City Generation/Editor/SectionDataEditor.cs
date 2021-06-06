using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SectionData))]
public class SectionDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        SectionData sectionData = target as SectionData;

        SectionType[] sectionTypes = Enum.GetValues(typeof(SectionType)).Cast<SectionType>() as SectionType[];

        if (sectionData.prefabs == null || sectionData.prefabs.Length != sectionTypes.Length)
            sectionData.prefabs = new GameObject[sectionTypes.Length];

        foreach (SectionType type in sectionTypes)
            sectionData.prefabs[(int)type] = EditorGUILayout.ObjectField(type.ToString(), sectionData.prefabs[(int)type], typeof(GameObject), false) as GameObject;

        if (EditorGUI.EndChangeCheck() | GUILayout.Button("Fetch data"))
        {
            (target as SectionData).FetchData();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
    }
}