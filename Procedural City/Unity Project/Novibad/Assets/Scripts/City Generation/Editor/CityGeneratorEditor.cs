using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CityGenerator))]
public class CityGeneratorEditor : Editor
{
    CityGenerator city;

    public override void OnInspectorGUI()
    {
        if (city == null)
            city = target as CityGenerator;

        DrawDefaultInspector();

        GUI.enabled = false;
        EditorGUILayout.IntField("Amount of points", city.points.Count);
        Vector2[] vertices;
        if ((vertices = city.GetVertices()) != null)
            EditorGUILayout.IntField("Amount of vertices", vertices.Length);
        GUI.enabled = true;

        if (GUILayout.Button("Add city points"))
            city.PlotPoints();

        if (GUILayout.Button("Generate map"))
            city.GenerateMap();

        if(city.diagram != null)
        if (GUILayout.Button("Generate streets"))
            city.GenerateStreets();

        if(city.cityBlocks != null && city.cityBlocks.Count > 0 && BuildingGenerator.numberOfGeneratingBuildings <= 0)
            if(GUILayout.Button("Generate Buildings"))
                city.GenerateBuildings();

        if (city.points.Count > 0)
            if (GUILayout.Button("Clear"))
                city.Clear();
    }

    private void OnSceneGUI()
    {
        if (city == null)
            city = target as CityGenerator;

        if (city.terrain != null)
        {
            Handles.color = Color.blue;
            Vector3 cityPos = new Vector3(city.terrain.cityCenter.x, 0, city.terrain.cityCenter.y) + city.transform.position;

            cityPos = Handles.DoPositionHandle(cityPos, Quaternion.identity);

            Handles.DrawWireArc(cityPos, Vector3.up, Vector3.forward, 360, city.terrain.cityRadius);

            cityPos -= city.terrain.transform.position;
            city.terrain.cityCenter = new Vector2Int(Mathf.RoundToInt(cityPos.x), Mathf.RoundToInt(cityPos.z));
        }

        Vector2 position = new Vector2(city.transform.position.x, city.transform.position.z);

        if (city.cityBlocks != null && city.cityBlocks.Count > 0)
        {
            foreach (CityBlock cityBlock in city.cityBlocks)
                foreach (Intersection intersection in cityBlock.intersections)
                {
                    Handles.color = Color.green;
                    Handles.SphereHandleCap(0, intersection.transform.position, Quaternion.identity, 3, EventType.Repaint);
                    foreach (StreetGenerator street in intersection.connectedStreets)
                    {
                        Vector3 start = new Vector3(street.start.x, city.terrain.GetTerrainHeight(street.start), street.start.y);
                        Vector3 end = new Vector3(street.end.x, city.terrain.GetTerrainHeight(street.end), street.end.y);

                        Handles.color = Color.yellow;
                        Handles.DrawLine(start, end);
                    }
                }
        }
        else
        {
            Handles.color = Color.white;
            Vector2[] sites = city.points.ToArray();
            if (sites != null && sites.Length > 0)
                foreach (Vector2 site in sites)
                {
                    Handles.SphereHandleCap(0, new Vector3(site.x, city.transform.position.y, site.y), Quaternion.identity, 1, EventType.Repaint);
                }

            Vector2[] vertexArray = city.GetVertices();

            if (vertexArray == null || vertexArray.Length <= 0)
                return;

            Handles.color = Color.white;
            for (int i = 0; i < vertexArray.Length; i += 2)
            {
                Vector3 start = new Vector3(vertexArray[i].x, city.transform.position.y, vertexArray[i].y);

                Vector3 end = new Vector3(vertexArray[i + 1].x, city.transform.position.y, vertexArray[i + 1].y);

                Handles.DrawLine(start, end);
            }
        }

        Vector3 nw = new Vector3(city.minBounds.x, city.transform.position.y, city.minBounds.y);
        Vector3 ne = new Vector3(city.maxBounds.x, city.transform.position.y, city.minBounds.y);
        Vector3 sw = new Vector3(city.minBounds.x, city.transform.position.y, city.maxBounds.y);
        Vector3 se = new Vector3(city.maxBounds.x, city.transform.position.y, city.maxBounds.y);

        Handles.color = Color.blue;
        Handles.DrawLine(nw, ne);
        Handles.DrawLine(ne, se);
        Handles.DrawLine(se, sw);
        Handles.DrawLine(sw, nw);
    }
}
