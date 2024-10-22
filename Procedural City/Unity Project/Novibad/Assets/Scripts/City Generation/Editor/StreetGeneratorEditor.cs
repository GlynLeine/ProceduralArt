﻿using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StreetGenerator))]
public class StreetGeneratorEditor : Editor
{
    StreetGenerator street;

    public override void OnInspectorGUI()
    {
        if (street == null)
            street = target as StreetGenerator;

        EditorGUI.BeginChangeCheck();
        float length = street.length;
        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck() || street.transform.hasChanged)
        {
            street.CalculateBounds(street.transform.hasChanged || length != street.length);
            street.transform.hasChanged = false;
        }

        if (GUILayout.Button("Generate Buildings"))
        {
            street.GenerateBuildings();
        }
    }

    private void OnEnable()
    {
        if (street == null)
            street = target as StreetGenerator;

        street.transform.hasChanged = false;
    }

    public void OnSceneGUI()
    {
        if (street == null)
            street = target as StreetGenerator;

        float positionHeight = street.transform.position.y;

        EditorGUI.BeginChangeCheck();

        Vector3 streetStartWorld = new Vector3(street.start.x, positionHeight, street.start.y);
        Vector3 streetEndWorld = new Vector3(street.end.x, positionHeight, street.end.y);

        Vector3 streetAxis = (streetEndWorld - streetStartWorld).normalized;

        if (StreetGenerator.sharedTerrain != null)
        {
            streetStartWorld.y = StreetGenerator.sharedTerrain.GetTerrainHeight(street.start);
            streetEndWorld.y = StreetGenerator.sharedTerrain.GetTerrainHeight(street.end);
        }

        streetStartWorld = Handles.DoPositionHandle(streetStartWorld, Quaternion.LookRotation(streetAxis, Vector3.up));
        streetEndWorld = Handles.DoPositionHandle(streetEndWorld, Quaternion.LookRotation(streetAxis, Vector3.up));

        Handles.color = Color.white;
        Handles.DrawLine(streetStartWorld, streetEndWorld);

        if (street.sides != null)
            foreach (var side in street.sides)
            {
                Handles.DrawLine(new Vector3(side.start.x, streetStartWorld.y, side.start.y), new Vector3(side.end.x, streetEndWorld.y, side.end.y));

                if (side.buildings != null)
                    foreach (var building in side.buildings)
                        if (building)
                            (CreateEditor(building) as BuildingGeneratorEditor).DrawLines();
            }

        street.start = new Vector2(streetStartWorld.x, streetStartWorld.z);
        street.end = new Vector2(streetEndWorld.x, streetEndWorld.z);

        if (EditorGUI.EndChangeCheck() || street.transform.hasChanged)
        {
            street.CalculateBounds(street.transform.hasChanged);
            street.transform.hasChanged = false;
        }
    }

    public void DrawLines()
    {
        if (street == null)
            street = target as StreetGenerator;

        float positionHeight = street.transform.position.y;

        Vector3 streetStartWorld = new Vector3(street.start.x, positionHeight, street.start.y);
        Vector3 streetEndWorld = new Vector3(street.end.x, positionHeight, street.end.y);

        if (StreetGenerator.sharedTerrain != null)
        {
            streetStartWorld.y = StreetGenerator.sharedTerrain.GetTerrainHeight(street.start);
            streetEndWorld.y = StreetGenerator.sharedTerrain.GetTerrainHeight(street.end);
        }

        Handles.color = Color.white;
        Handles.DrawLine(streetStartWorld, streetEndWorld);

        if (street.sides != null)
            foreach (var side in street.sides)
            {
                Handles.DrawLine(new Vector3(side.start.x, streetStartWorld.y, side.start.y), new Vector3(side.end.x, streetEndWorld.y, side.end.y));

                if (side.buildings != null)
                    foreach (var building in side.buildings)
                        if (building)
                            (CreateEditor(building) as BuildingGeneratorEditor).DrawLines();
            }
    }
}