using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "New BuildingShape", menuName = "City Generation/Create Building Shape Object")]
public class BuildingShape : ScriptableObject
{
    [Header("Bounds Settings")]
    public int width;
    [Range(0.2f, 5)]
    public float prefferedRatio;
    public int height;

    [Header("Shape Settings"), Range(0f, 100f)]
    public float splitChance;
    [Range(0.001f, 1f)]
    public float subLimbWidthScaleLowerBound;
    [Range(0.001f, 1f)]
    public float subLimbWidthScaleUpperBound;
    [Range(0.001f, 1f)]
    public float mainLimbWidthScaleLowerBound;
    [Range(0.001f, 1f)]
    public float mainLimbWidthScaleUpperBound;
    [Range(0.001f, 1f)]
    public float subLimbPositionScaleLowerBound;
    [Range(0.001f, 1f)]
    public float subLimbPositionScaleUpperBound;
    [Range(0.001f, 1f)]
    public float subLimbHeightScaleLowerBound;
    [Range(0.001f, 1f)]
    public float subLimbHeigthScaleUpperBound;
}