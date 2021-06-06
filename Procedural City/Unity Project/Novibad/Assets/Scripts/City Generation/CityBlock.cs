using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[ExecuteInEditMode]
public class CityBlock : MonoBehaviour
{
    public static TerrainGenerator sharedTerrain;

    public List<Intersection> intersections = new List<Intersection>();

    public int seed;

    public TerrainGenerator terrain;

    private IEnumerator TrackProgressCoroutine()
    {
        while (BuildingGenerator.numberOfGeneratingBuildings > 0)
        {
            EditorUtility.DisplayProgressBar("Generating Buildings", BuildingGenerator.numberOfGeneratingBuildings + " buildings still generating", (float)BuildingGenerator.numberOfGeneratingBuildings / BuildingGenerator.numberOfBuildingInitiated);
            yield return null;
        }

        EditorUtility.ClearProgressBar();
    }

    public void RePosition()
    {
        Vector3 averagePos = Vector3.zero;
        int intersectionCount = 0;
        for (int i = 0; i < intersections.Count; i++)
        {
            if (!intersections[i])
                continue;

            intersectionCount++;

            Vector3 pos = intersections[i].transform.position;

            averagePos += pos;
        }
        averagePos /= intersectionCount;

        transform.position = averagePos;

        for (int i = 0; i < intersections.Count; i++)
            if (intersections[i])
                intersections[i].CorrectStreetPositions();

        for (int i = 0; i < intersections.Count; i++)
            if (intersections[i] && intersections[i].connectedStreets.Count > 1)
                intersections[i].CorrectStreetIntersections();
    }

    public void GenerateBuildings()
    {
        System.Random random = new System.Random(seed);

        if (sharedTerrain == null)
        {
            sharedTerrain = terrain;
        }
        else
            terrain = sharedTerrain;

        if (Intersection.sharedTerrain != sharedTerrain)
            Intersection.sharedTerrain = sharedTerrain;

        for (int i = 0; i < intersections.Count; i++)
            if (intersections[i])
                intersections[i].CorrectStreetPositions();

        for (int i = 0; i < intersections.Count; i++)
            if (intersections[i])
                intersections[i].CorrectStreetIntersections();

        foreach (Intersection intersection in intersections)
            if (intersection)
                foreach (StreetGenerator street in intersection.connectedStreets)
                {
                    street.seed = random.Next();
                    street.generatedBuildings = false;
                }

        if (StreetGenerator.sharedTerrain != sharedTerrain)
            StreetGenerator.sharedTerrain = sharedTerrain;

        foreach (Intersection intersection in intersections)
            if (intersection)
                foreach (StreetGenerator street in intersection.connectedStreets)
                    if (!street.generatedBuildings)
                        street.GenerateBuildings();

        StartCoroutine(TrackProgressCoroutine());
    }
}
