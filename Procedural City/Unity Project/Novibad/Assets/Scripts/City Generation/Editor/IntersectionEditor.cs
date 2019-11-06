using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Intersection))]
public class IntersectionEditor : Editor
{
    Intersection intersection;
    public override void OnInspectorGUI()
    {
        if (intersection == null)
            intersection = target as Intersection;

        EditorGUI.BeginChangeCheck();

        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck() | GUILayout.Button("Correct streets") || intersection.transform.hasChanged)
        {
            if (intersection.transform.hasChanged)
                intersection.CorrectStreetPositions();
            intersection.CorrectStreetIntersections();
            intersection.transform.hasChanged = false;
        }

        if (GUILayout.Button("Generate buildings"))
            foreach (var street in intersection.connectedStreets)
                if (!street.generatedBuildings)
                    street.GenerateBuildings();
    }

    public void OnSceneGUI()
    {
        if (intersection == null)
            intersection = target as Intersection;

        if (intersection.connectedStreets != null)
            foreach (var street in intersection.connectedStreets)
                (CreateEditor(street) as StreetGeneratorEditor).DrawLines();
    }
}