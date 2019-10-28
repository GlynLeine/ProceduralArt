using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    public Vector2[] constraintBounds;
    public Vector2 allignmentAxisStart;
    public Vector2 allignmentAxisEnd;
    public float axisPosition;
    public float width;
    public float ratio;

    [ReadOnly]
    public Vector2[] buildingBounds;
    [ReadOnly]
    public float length;

    public void CalculateBounds()
    {
        length = width / ratio;
        Vector2 allignmentAxis = (allignmentAxisEnd - allignmentAxisStart).normalized;
        Vector2 position = allignmentAxisStart + axisPosition * (allignmentAxisEnd - allignmentAxisStart);
        buildingBounds = new Vector2[] { position - allignmentAxis * 0.5f * width,
                                         position + allignmentAxis * 0.5f * width,
                                         new Vector2(), new Vector2() };

        buildingBounds[3] = buildingBounds[2] + new Vector2(-allignmentAxis.y, allignmentAxis.x) * length;
        buildingBounds[4] = buildingBounds[1] + new Vector2(-allignmentAxis.y, allignmentAxis.x) * length;
    }
}
