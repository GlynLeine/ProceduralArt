using UnityEngine;
using System.Collections;

public class StreetGenerator : MonoBehaviour
{
    BuildingShape[] buildingShapes;
    BuildingTheme[] buildingThemes;

    public Vector2 start;
    public Vector2 end;
    public float width;

    BuildingGenerator[] leftSide;
    BuildingGenerator[] rightSide;
}
