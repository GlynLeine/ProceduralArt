using System.Collections.Generic;
using UnityEngine;

public class Intersection : MonoBehaviour
{
    public List<StreetGenerator> connectedStreets;

    class SideData
    {
        public bool startSide;
        public StreetGenerator.StreetSide side;
        public SideData other;
        public StreetGenerator street;
        public SideData(bool startSide, StreetGenerator.StreetSide side, SideData other, StreetGenerator street)
        { this.startSide = startSide; this.side = side; this.other = other; this.street = street; }
    }

    public void CorrectStreetPositions()
    {
        if (connectedStreets == null || connectedStreets.Count <= 0)
            return;

        Vector2 position = new Vector2(transform.position.x, transform.position.z);
        transform.position = new Vector3(position.x, 0, position.y);

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

        SortedDictionary<float, SideData> streetSides = new SortedDictionary<float, SideData>();
        Vector2 position = new Vector2(transform.position.x, transform.position.z);
        foreach (StreetGenerator street in connectedStreets)
            foreach (StreetGenerator.StreetSide side in street.sides)
            {
                bool startSide = Vector2.Distance(side.start, position) < Vector2.Distance(side.end, position);

                Vector2 toSide;
                Vector2 axis = (side.end - side.start).normalized;
                if (startSide)
                    toSide = side.start + axis * street.width - position;
                else
                    toSide = side.end - axis * street.width - position;

                float angle = (Mathf.Atan2(toSide.x, toSide.y) + Mathf.PI) * 180 / Mathf.PI;

                streetSides.Add(angle, new SideData(startSide, side, null, street));
            }

        SideData[] sideArray = new SideData[streetSides.Count];
        float[] angles = new float[streetSides.Count];
        streetSides.Values.CopyTo(sideArray, 0);
        streetSides.Keys.CopyTo(angles, 0);
        for (int i = 0; i < sideArray.Length; i++)
        {
            if (i >= sideArray.Length)
                continue;

            int nextIndex = (i + 1) % sideArray.Length;
            if (sideArray[i].street != sideArray[nextIndex].street)
            {
                sideArray[i].other = sideArray[nextIndex];
                streetSides.Remove(angles[nextIndex]);
                i++;
            }
        }

        Vector2 intersection;
        foreach (SideData sideData in streetSides.Values)
        {
            Vector2 firstAxis = (sideData.side.end - sideData.side.start).normalized;
            Vector2 secondAxis = (sideData.other.side.end - sideData.other.side.start).normalized;

            if (LineSegmentsIntersection(sideData.side.start, sideData.side.end, sideData.other.side.start, sideData.other.side.end, out intersection))
            {
                if (sideData.startSide)
                {
                    Vector2 cutAxis = new Vector2(-secondAxis.y, secondAxis.x);
                    float cutLength = sideData.other.street.buildingShapes[0].width / sideData.other.street.buildingShapes[0].prefferedRatioUpperBound;
                    Vector2 newStart = intersection - Vector2.Dot(cutAxis * cutLength, firstAxis) * firstAxis;
                    sideData.side.start = newStart;

                    if (sideData.other.startSide)
                    {
                        sideData.other.side.start = intersection;
                    }
                    else
                    {
                        sideData.other.side.end = intersection;
                    }
                }
                else
                {
                    if (sideData.other.startSide)
                    {
                        Vector2 cutAxis = new Vector2(-firstAxis.y, firstAxis.x);
                        float cutLength = sideData.street.buildingShapes[0].width / sideData.street.buildingShapes[0].prefferedRatioUpperBound;
                        Vector2 newStart = intersection + Vector2.Dot(cutAxis * cutLength, secondAxis) * secondAxis;
                        sideData.other.side.start = newStart;
                    }
                    else
                    {
                        Vector2 cutAxis = new Vector2(-firstAxis.y, firstAxis.x);
                        float cutLength = sideData.street.buildingShapes[0].width / sideData.street.buildingShapes[0].prefferedRatioUpperBound;
                        Vector2 newEnd = intersection + Vector2.Dot(cutAxis * cutLength, secondAxis) * secondAxis;
                        sideData.other.side.end = newEnd;
                    }

                    sideData.side.end = intersection;
                }

                //if (sideData.other.startSide)
                //{
                //    Vector2 cutAxis = new Vector2(-firstAxis.y, firstAxis.x);
                //    float cutLength = sideData.street.buildingShapes[0].width / sideData.street.buildingShapes[0].prefferedRatioUpperBound;
                //    Vector2 newStart = intersection - Vector2.Dot(cutAxis * cutLength, secondAxis) * secondAxis;
                //    sideData.other.side.start = newStart;
                //}
                //else
                //{
                //    sideData.other.side.end = intersection;
                //}
            }
            else if (LineSegmentsIntersection(sideData.side.start - firstAxis * 1000, sideData.side.end + firstAxis * 1000, sideData.other.side.start - secondAxis * 1000, sideData.other.side.end + secondAxis * 1000, out intersection))
            {
                if (sideData.startSide)
                    sideData.side.start = intersection;
                else
                    sideData.side.end = intersection;

                if (sideData.other.startSide)
                    sideData.other.side.start = intersection;
                else
                    sideData.other.side.end = intersection;
            }
        }

        sideArray = new SideData[streetSides.Count];
        streetSides.Values.CopyTo(sideArray, 0);
        for (int i = 0; i < sideArray.Length; i++)
            sideArray[i].side.length = (sideArray[i].side.end - sideArray[i].side.start).magnitude;
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
