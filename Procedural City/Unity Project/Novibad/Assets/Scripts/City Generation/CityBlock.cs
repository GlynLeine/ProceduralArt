using UnityEngine;
using System.Collections.Generic;

public class CityBlock : MonoBehaviour
{
    public Intersection[] intersections;

    public float buildingSpace;

    private StreetGenerator[] streets;

    public void EnforceBoundaries()
    {
        Vector2[][] boundaries = new Vector2[intersections.Length][];

        HashSet<StreetGenerator> uniqueStreets = new HashSet<StreetGenerator>();
        foreach (Intersection intersection in intersections)
            foreach (StreetGenerator street in intersection.connectedStreets)
                uniqueStreets.Add(street);
        streets = new StreetGenerator[uniqueStreets.Count];
        uniqueStreets.CopyTo(streets);
        for (int i = 0; i < streets.Length; i++)
        {
            Vector2[] bounds = new Vector2[] { streets[i].end, streets[i].start };
        }
    }
}
