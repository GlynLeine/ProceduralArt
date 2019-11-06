using UnityEngine;
using System.Collections.Generic;

public class Intersection : MonoBehaviour
{
    public List<StreetGenerator> connectedStreets;

    [HideInInspector]
    public List<StreetGenerator.StreetSide> streetSides;

    public void CorrectStreetPositions()
    {
        if (connectedStreets == null || connectedStreets.Count <= 0)
            return;

        Vector2 position = new Vector2(transform.position.x, transform.position.z);

        for (int i = 0; i < connectedStreets.Count; i++)
        {
            if ((position - connectedStreets[i].start).sqrMagnitude < (position - connectedStreets[i].end).sqrMagnitude)
                connectedStreets[i].start = position;
            else
                connectedStreets[i].end = position;

            connectedStreets[i].CalculateBounds(false);
        }
    }
    public void CorrectStreetIntersections()
    {
        if (connectedStreets == null || connectedStreets.Count <= 0)
            return;

        streetSides = new List<StreetGenerator.StreetSide>();
        foreach (StreetGenerator street in connectedStreets)
            foreach (StreetGenerator.StreetSide side in street.sides)
                streetSides.Add(side);

        Vector2 intersection;
        for (int i = 0; i < streetSides.Count; i++)
            for (int j = 0; j < streetSides.Count; j++)
            {
                if (i == j)
                    continue;

                if (LineSegmentsIntersection(streetSides[i].start, streetSides[i].end, streetSides[j].start, streetSides[j].end, out intersection))
                {
                    if (Vector2.Distance(streetSides[i].start, intersection) < Vector2.Distance(streetSides[i].end, intersection))
                        streetSides[i].start = intersection;
                    else
                        streetSides[i].end = intersection;

                    if (Vector2.Distance(streetSides[j].start, intersection) < Vector2.Distance(streetSides[j].end, intersection))
                        streetSides[j].start = intersection;
                    else
                        streetSides[j].end = intersection;
                }
            }

        for (int i = 0; i < streetSides.Count; i++)
            streetSides[i].length = (streetSides[i].end - streetSides[i].start).magnitude;
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

}
