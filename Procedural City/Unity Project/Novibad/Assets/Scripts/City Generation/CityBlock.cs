using UnityEngine;
using System.Collections.Generic;

public class CityBlock : MonoBehaviour
{
    public Intersection[] intersections;

    public float buildingSpace;

    private HashSet<StreetGenerator> streets;

    public void EnforceBoundaries()
    {
        Vector2[][] boundaries = new Vector2[intersections.Length][];

        foreach (Intersection intersection in intersections)
            foreach (StreetGenerator street in intersection.connectedStreets)
                streets.Add(street);

        for (int i = 0; i < streets.Count; i++)
        {

            //Vector2[] bounds = new Vector2[] { intersections };
        }
    }
}
