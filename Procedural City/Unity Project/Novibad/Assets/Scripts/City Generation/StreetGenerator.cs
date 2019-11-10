using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class StreetGenerator : MonoBehaviour
{
    public BuildingShape[] buildingShapes;
    public BuildingTheme[] buildingThemes;

    public Vector2 start;
    public Vector2 end;
    public float width;
    public float length;

    public float spaceBetweenBuildingsLowerBound;
    public float spaceBetweenBuildingsUpperBound;

    public int seed;

    private System.Random random;

    [System.Serializable]
    public class StreetSide
    {
        public BuildingGenerator[] buildings;
        public Vector2 start;
        public Vector2 end;
        public float length;
        public Vector2[] blockBounds;
    }

    public StreetSide[] sides;

    [HideInInspector]
    public bool generatedBuildings;

    public static TerrainGenerator sharedTerrain;

    public TerrainGenerator terrain;

    public void CalculateBounds(bool useTransform)
    {
        if (sharedTerrain == null)
        {
            sharedTerrain = terrain;
        }
        else
            terrain = sharedTerrain;

        int children = transform.childCount;
        for (int i = 0; i < children; ++i)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        generatedBuildings = false;

        if (width < 0)
            width = 0;

        if (length < 0.001f)
            length = 0.001f;

        Vector2 axis;

        if (useTransform)
        {
            length *= transform.localScale.z;
            width *= transform.localScale.x;
            transform.localScale = Vector3.one;

            Vector2 pos = new Vector2(transform.position.x, transform.position.z);
            Vector3 worldAxis = transform.rotation * Vector3.back;
            axis = new Vector2(worldAxis.x, worldAxis.z);
            start = pos + axis * length / 2f;
            end = pos - axis * length / 2f;
        }
        else
        {
            axis = end - start;
            length = axis.magnitude;
            axis.Normalize();

            Vector2 pos = start + axis * length * 0.5f;

            transform.position = new Vector3(pos.x, sharedTerrain.GetTerrainHeight(pos), pos.y);
            transform.rotation = Quaternion.LookRotation(new Vector3(axis.x, 0, axis.y), Vector3.up);
        }

        Vector2 normal = new Vector2(axis.y, -axis.x);

        sides = new StreetSide[2];

        for (int sideScale = -1; sideScale <= 1; sideScale += 2)
        {
            int sideIndex = Mathf.Max(0, sideScale);
            sides[sideIndex] = new StreetSide();
            sides[sideIndex].start = start + normal * width / 2f * sideScale;
            sides[sideIndex].end = end + normal * width / 2f * sideScale;
            sides[sideIndex].length = length;
        }
    }

    public void GenerateBuildings()
    {
        int children = transform.childCount;
        for (int i = 0; i < children; ++i)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        random = new System.Random(seed);

        for (int sideScale = -1; sideScale <= 1; sideScale += 2)
        {
            int sideIndex = Mathf.Max(0, sideScale);

            List<BuildingGenerator> buildings = new List<BuildingGenerator>();
            int buildingIndex = 0;
            for (float builtSize = 0; builtSize < sides[sideIndex].length;)
            {
                int shapeIndex = random.Next(0, buildingShapes.Length);
                int themeIndex = random.Next(0, buildingThemes.Length);

                if (builtSize + buildingShapes[shapeIndex].width > sides[sideIndex].length)
                    break;

                BuildingGenerator building = new GameObject().AddComponent<BuildingGenerator>();
                building.transform.SetParent(transform);

                if (BuildingGenerator.sharedTerrain != sharedTerrain)
                    BuildingGenerator.sharedTerrain = sharedTerrain;

                building.name = "building " + (sideScale < 0? "left " : "right ") + buildingIndex++;

                if (sideScale < 0)
                {
                    building.allignmentAxisStart = sides[sideIndex].start;
                    building.allignmentAxisEnd = sides[sideIndex].end;
                }
                else
                {
                    building.allignmentAxisStart = sides[sideIndex].end;
                    building.allignmentAxisEnd = sides[sideIndex].start;
                }

                building.axisPosition = (builtSize + buildingShapes[shapeIndex].width * 0.5f) / sides[sideIndex].length;
                builtSize += buildingShapes[shapeIndex].width;
                builtSize += (float)random.NextDouble() * (spaceBetweenBuildingsUpperBound - spaceBetweenBuildingsLowerBound) + spaceBetweenBuildingsLowerBound;

                building.buildingShape = buildingShapes[shapeIndex];
                building.buildingTheme = buildingThemes[themeIndex];
                building.seed = random.Next();
                building.buildingBounds = sides[sideIndex].blockBounds;

                building.CalculateBounds(false);
                building.CalculateSkeleton();
                building.GenerateMeshAsync();
                buildings.Add(building);
            }

            sides[sideIndex].buildings = buildings.ToArray();
        }
        generatedBuildings = true;
    }
}
