using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "New BuildingShape", menuName = "City Generation/Create Building Shape Object")]
public class BuildingShape : ScriptableObject
{
    [Header("Bounds Settings")]
    public int width;
    [Range(0.2f, 5)]
    public float prefferedRatioLowerBound = 1;
    [Range(0.2f, 5)]
    public float prefferedRatioUpperBound = 1;
    public int heightLowerBound;
    public int heightUpperBound;

    [Header("Shape Settings"), Range(0f, 100f)]
    public float splitChance;
    [Range(0.001f, 1f)]
    public float subLimbWidthScaleLowerBound = 0.001f;
    [Range(0.001f, 1f)]
    public float subLimbWidthScaleUpperBound = 0.001f;
    [Range(0.001f, 1f)]
    public float mainLimbWidthScaleLowerBound = 0.001f;
    [Range(0.001f, 1f)]
    public float mainLimbWidthScaleUpperBound = 0.001f;
    [Range(0.001f, 1f)]
    public float subLimbPositionScaleLowerBound = 0.001f;
    [Range(0.001f, 1f)]
    public float subLimbPositionScaleUpperBound = 0.001f;
    [Range(0.001f, 1f)]
    public float subLimbHeightScaleLowerBound = 0.001f;
    [Range(0.001f, 1f)]
    public float subLimbHeigthScaleUpperBound = 0.001f;
}