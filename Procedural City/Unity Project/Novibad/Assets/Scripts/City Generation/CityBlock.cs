using UnityEngine;
using System.Collections;
using UnityEditor;

[ExecuteInEditMode]
public class CityBlock : MonoBehaviour
{
    public static TerrainGenerator sharedTerrain;

    public Intersection[] intersections;

    public int seed;

    public TerrainGenerator terrain;

    private IEnumerator TrackProgressCoroutine()
    {
        while (BuildingGenerator.numberOfGeneratingBuildings > 0)
        {
            EditorUtility.DisplayProgressBar("Generating Buildings", BuildingGenerator.numberOfGeneratingBuildings + " buildings still generating", 1);
            yield return null;
        }

        EditorUtility.ClearProgressBar();
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

        for (int i = 0; i < intersections.Length; i++)
            if (intersections[i])
                intersections[i].CorrectStreetPositions();

        for (int i = 0; i < intersections.Length; i++)
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
