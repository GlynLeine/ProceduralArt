using UnityEngine;
using System.Collections.Generic;

public class VoronoiTest : MonoBehaviour
{
    Voronoi diagram;

    List<Vector2> points;

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

    public void AddRandomPoints(int count)
    {
        if (points == null)
            points = new List<Vector2>();

        for (int i = 0; i < count; i++)
            points.Add(new Vector2(Random.value, Random.value));
    }

    public void Generate()
    {
        diagram = new Voronoi(points);
    }
}
