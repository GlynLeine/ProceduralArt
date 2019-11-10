using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;

public class CityGenerator : MonoBehaviour
{
    [HideInInspector]
    public Voronoi diagram;
    [HideInInspector]
    public List<Vector2> points;
    [HideInInspector]
    public Vector2 minBounds;
    [HideInInspector]
    public Vector2 maxBounds;

    public TerrainGenerator terrain;

    public BuildingShape[] buildingShapes;
    public BuildingTheme[] buildingThemes;

    public float spaceBetweenBuildingsLowerBound;
    public float spaceBetweenBuildingsUpperBound;

    public float streetWidth;
    public int seed;

    [HideInInspector]
    public List<CityBlock> cityBlocks;

    private static IEnumerator generationCoRoutine;
    private static IEnumerator trackProgressCoRoutine;

    static CityGenerator()
    {
        EditorApplication.update += Update;
    }

    static void Update()
    {
        if (generationCoRoutine != null)
        {
            generationCoRoutine.MoveNext();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        if (trackProgressCoRoutine != null)
        {
            trackProgressCoRoutine.MoveNext();
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }


    public void GenerateBuildings()
    {
        System.Random random = new System.Random(seed);
        BuildingGenerator.numberOfBuildingInitiated = 0;

        foreach (CityBlock block in cityBlocks)
            foreach (Intersection intersection in block.intersections)
                foreach (StreetGenerator street in intersection.connectedStreets)
                    street.generatedBuildings = false;

        foreach (CityBlock block in cityBlocks)
            foreach (Intersection intersection in block.intersections)
                foreach (StreetGenerator street in intersection.connectedStreets)
                    if (!street.generatedBuildings)
                    {
                        float distance = (new Vector2(street.transform.position.x, street.transform.position.z) - (terrain.cityCenter + new Vector2(terrain.transform.position.x, terrain.transform.position.z))).magnitude;
                        if (distance <= terrain.cityRadius)
                        {
                            street.seed = random.Next();
                            street.terrain = terrain;
                            street.GenerateBuildings();
                        }
                    }

        trackProgressCoRoutine = TrackProgressCoroutine();
    }

    private IEnumerator TrackProgressCoroutine()
    {
        while (BuildingGenerator.numberOfGeneratingBuildings > 0)
        {
            float progress = 1f - (float)BuildingGenerator.numberOfGeneratingBuildings / BuildingGenerator.numberOfBuildingInitiated;
            EditorUtility.DisplayProgressBar("Generating Buildings", BuildingGenerator.numberOfGeneratingBuildings + " buildings generating \n"
                + BuildingGenerator.numberOfBuildingInitiated + " buildings intiated \n" + (progress*100f) + "% done", progress);
            yield return null;
        }

        EditorUtility.ClearProgressBar();
    }

    public void GenerateStreets()
    {
        int children = transform.childCount;
        for (int i = 0; i < children; ++i)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        System.Random random = new System.Random(seed);

        Edge[] edges = GetEdges();

        Dictionary<Point, Intersection> intersections = new Dictionary<Point, Intersection>();

        Dictionary<Point, CityBlock> generatedBlocks = new Dictionary<Point, CityBlock>();

        CityBlock.sharedTerrain = terrain;
        Intersection.sharedTerrain = terrain;

        int intersectionCount = 0;
        int blockCount = 0;
        for (int i = 0; i < edges.Length; i++)
        {
            Edge edge = edges[i];

            Intersection end;
            Intersection start;
            CityBlock leftBlock;
            CityBlock rightBlock;

            if (intersections.ContainsKey(edge.start))
                start = intersections[edge.start];
            else
            {
                GameObject gameObject = new GameObject();
                gameObject.transform.parent = transform;
                start = gameObject.AddComponent<Intersection>();
                start.gameObject.name = "intersection " + intersectionCount;
                intersectionCount++;

                start.transform.position = new Vector3(edge.start.position.x, terrain.GetTerrainHeight(edge.start.position), edge.start.position.y);

                intersections.Add(edge.start, start);
            }

            if (intersections.ContainsKey(edge.end))
                end = intersections[edge.end];
            else
            {
                GameObject gameObject = new GameObject();
                gameObject.transform.parent = transform;
                end = gameObject.AddComponent<Intersection>();
                end.gameObject.name = "intersection " + intersectionCount;
                intersectionCount++;

                end.transform.position = new Vector3(edge.end.position.x, terrain.GetTerrainHeight(edge.end.position), edge.end.position.y);

                intersections.Add(edge.end, end);
            }

            if (generatedBlocks.ContainsKey(edge.site_left))
                leftBlock = generatedBlocks[edge.site_left];
            else
            {
                GameObject gameObject = new GameObject();
                gameObject.transform.parent = transform;
                leftBlock = gameObject.AddComponent<CityBlock>();
                leftBlock.gameObject.name = "city block " + blockCount;
                blockCount++;

                leftBlock.seed = random.Next();

                generatedBlocks.Add(edge.site_left, leftBlock);
            }

            if (generatedBlocks.ContainsKey(edge.site_right))
                rightBlock = generatedBlocks[edge.site_right];
            else
            {
                GameObject gameObject = new GameObject();
                gameObject.transform.parent = transform;
                rightBlock = gameObject.AddComponent<CityBlock>();
                rightBlock.gameObject.name = "city block " + blockCount;
                blockCount++;

                rightBlock.seed = random.Next();

                generatedBlocks.Add(edge.site_right, rightBlock);
            }

            if (!leftBlock.intersections.Contains(start))
                leftBlock.intersections.Add(start);
            if (!leftBlock.intersections.Contains(end))
                leftBlock.intersections.Add(end);

            if (!rightBlock.intersections.Contains(start))
                rightBlock.intersections.Add(start);
            if (!rightBlock.intersections.Contains(end))
                rightBlock.intersections.Add(end);

            GameObject parentObject = new GameObject();
            parentObject.transform.parent = transform;
            StreetGenerator street = parentObject.AddComponent<StreetGenerator>();
            street.gameObject.name = "street " + i;
            street.width = streetWidth;
            street.buildingShapes = buildingShapes;
            street.buildingThemes = buildingThemes;
            street.spaceBetweenBuildingsLowerBound = spaceBetweenBuildingsLowerBound;
            street.spaceBetweenBuildingsUpperBound = spaceBetweenBuildingsUpperBound;

            street.CalculateBounds(true);

            end.connectedStreets.Add(street);
            start.connectedStreets.Add(street);

            street.end = new Vector2(end.transform.position.x, end.transform.position.z);
            street.start = new Vector2(start.transform.position.x, start.transform.position.z);

            leftBlock.RePosition();
            rightBlock.RePosition();
        }

        cityBlocks = new List<CityBlock>();
        cityBlocks.AddRange(generatedBlocks.Values);

    }

    public void PlotPoints()
    {
        if (terrain == null)
            return;

        System.Random random = new System.Random(seed);

        Vector2 pos = new Vector2(transform.position.x, transform.position.z);
        minBounds = pos;
        maxBounds = new Vector2(terrain.dimensions + pos.x, terrain.dimensions + pos.y);

        int[] yCount = new int[terrain.resolution];
        for (int x = 5; x < terrain.resolution - 4; x++)
        {
            for (int y = 5; y < terrain.resolution - 4; y++)
            {
                if ((float)random.NextDouble() * 100f < terrain.readTex.GetPixelBilinear((float)x / terrain.resolution, (float)y / terrain.resolution).g)
                {
                    points.Add(new Vector2(x + pos.x, y + pos.y + yCount[y] * 0.1f));
                    yCount[y]++;
                }
            }
        }
    }

    public Edge[] GetEdges()
    {
        if (diagram == null || diagram.edges == null)
            return null;

        List<Edge> edgeList = new List<Edge>();
        foreach (Edge edge in diagram.edges)
        {
            bool startOut = edge.start.x < minBounds.x || edge.start.x > maxBounds.x || edge.start.y < minBounds.y || edge.start.y > maxBounds.y;
            bool endOut = edge.end.x < minBounds.x || edge.end.x > maxBounds.x || edge.end.y < minBounds.y || edge.end.y > maxBounds.y;

            if (startOut && endOut)
                continue;

            if (startOut)
                edge.start.position = Restrict(edge.start.position, edge.end.position, minBounds, maxBounds);

            if (endOut)
                edge.end.position = Restrict(edge.end.position, edge.start.position, minBounds, maxBounds);

            if (terrain.GetTerrainHeight(edge.start.position) < terrain.meshHeight * terrain.waterHeight + terrain.transform.position.y ||
               terrain.GetTerrainHeight(edge.end.position) < terrain.meshHeight * terrain.waterHeight + terrain.transform.position.y)
                continue;

            edgeList.Add(edge);
        }
        return edgeList.ToArray();
    }

    public Vector2[] GetVertices()
    {
        if (diagram == null || diagram.edges == null)
            return null;

        List<Vector2> vertexList = new List<Vector2>();
        foreach (Edge edge in diagram.edges)
        {
            Vector2 start = edge.start.position;
            Vector2 end = edge.end.position;

            bool startOut = start.x < minBounds.x || start.x > maxBounds.x || start.y < minBounds.y || start.y > maxBounds.y;
            bool endOut = end.x < minBounds.x || end.x > maxBounds.x || end.y < minBounds.y || end.y > maxBounds.y;

            if (startOut && endOut)
                continue;

            if (startOut)
                start = Restrict(start, edge.end.position, minBounds, maxBounds);

            if (endOut)
                end = Restrict(end, edge.start.position, minBounds, maxBounds);

            if (terrain.GetTerrainHeight(start) < terrain.meshHeight * terrain.waterHeight + terrain.transform.position.y ||
               terrain.GetTerrainHeight(end) < terrain.meshHeight * terrain.waterHeight + terrain.transform.position.y)
                continue;

            vertexList.Add(start);
            vertexList.Add(end);
        }
        return vertexList.ToArray();
    }

    private Vector2 Restrict(Vector2 point, Vector2 other, Vector2 min, Vector2 max)
    {
        Vector2 nw = new Vector2(min.x, min.y);
        Vector2 ne = new Vector2(max.x, min.y);
        Vector2 sw = new Vector2(min.x, max.y);
        Vector2 se = new Vector2(max.x, max.y);

        Vector2 intersection;

        if (LineSegmentsIntersection(point, other, nw, ne, out intersection))
            return intersection;


        if (LineSegmentsIntersection(point, other, ne, se, out intersection))
            return intersection;


        if (LineSegmentsIntersection(point, other, se, sw, out intersection))
            return intersection;


        if (LineSegmentsIntersection(point, other, sw, nw, out intersection))
            return intersection;

        return point;
    }

    private bool LineSegmentsIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        float d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

        if (d == 0.0f)
        {
            return false;
        }

        float u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
        float v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

        if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
        {
            return false;
        }

        intersection.x = p1.x + u * (p2.x - p1.x);
        intersection.y = p1.y + u * (p2.y - p1.y);

        return true;
    }

    public void Clear()
    {
        points = new List<Vector2>();
        cityBlocks = new List<CityBlock>();
        diagram = null;
    }

    public void AddPoint(Vector2 point)
    {
        if (points == null)
            points = new List<Vector2>();

        points.Add(point);
    }

    public void RemovePoint(Vector2 point)
    {
        if (points == null)
            return;
        points.Remove(point);
    }

    public void AddRandomPoints(int count, float scale)
    {
        if (points == null)
            points = new List<Vector2>();

        Vector2 position = new Vector2(transform.position.x, transform.position.z);

        for (int i = 0; i < count; i++)
            points.Add(position + new Vector2((Random.value - 0.5f) * scale, (Random.value - 0.5f) * scale));
    }

    public void GenerateMap()
    {
        if (points == null || points.Count <= 0)
            return;

        cityBlocks = new List<CityBlock>();
        int children = transform.childCount;
        for (int i = 0; i < children; ++i)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        diagram = new Voronoi(points, this);
        generationCoRoutine = diagram.generateVoronoi();
    }
}
