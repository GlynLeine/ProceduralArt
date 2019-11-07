using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CityBlock))]
public class CityBlockEditor : Editor
{
    CityBlock cityBlock;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Generate buildings"))
        {
            foreach (Intersection intersection in cityBlock.intersections)
                if (intersection)
                    foreach (StreetGenerator street in intersection.connectedStreets)
                        street.generatedBuildings = false;

            foreach (Intersection intersection in cityBlock.intersections)
                if (intersection)
                    foreach (StreetGenerator street in intersection.connectedStreets)
                        if (!street.generatedBuildings)
                            street.GenerateBuildings();
        }

    }

    public void OnSceneGUI()
    {
        if (cityBlock == null)
            cityBlock = target as CityBlock;

        if (cityBlock.intersections != null)
        {
            bool positionsChanged = false;

            Vector3 averagePos = Vector3.zero;
            int intersectionCount = 0;
            for (int i = 0; i < cityBlock.intersections.Length; i++)
            {
                if (!cityBlock.intersections[i])
                    continue;

                intersectionCount++;

                Vector3 pos = cityBlock.intersections[i].transform.position;

                EditorGUI.BeginChangeCheck();
                pos = Handles.DoPositionHandle(pos, Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    positionsChanged = true;
                    cityBlock.intersections[i].transform.position = pos;
                }

                averagePos += pos;
            }
            averagePos /= intersectionCount;

            cityBlock.transform.position = averagePos;

            if (positionsChanged)
            {
                for (int i = 0; i < cityBlock.intersections.Length; i++)
                    if (cityBlock.intersections[i])
                        cityBlock.intersections[i].CorrectStreetPositions();

                for (int i = 0; i < cityBlock.intersections.Length; i++)
                    if (cityBlock.intersections[i])
                        cityBlock.intersections[i].CorrectStreetIntersections();
            }

            for (int i = 0; i < cityBlock.intersections.Length; i++)
                if (cityBlock.intersections[i])
                    (CreateEditor(cityBlock.intersections[i]) as IntersectionEditor).OnSceneGUI();
        }
    }
}