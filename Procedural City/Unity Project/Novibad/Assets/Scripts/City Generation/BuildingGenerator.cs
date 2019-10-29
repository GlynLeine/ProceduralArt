using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    public Vector2[] constraintBounds;
    public Vector2 allignmentAxisStart;
    public Vector2 allignmentAxisEnd;
    [Range(0, 1)]
    public float axisPosition;
    public int width;
    [Range(0.2f, 5)]
    public float prefferedRatio;
    public int height;

    public GameObject wallPrefab;
    public GameObject roofPrefab;

    [ReadOnly]
    public Vector2[] buildingBounds;
    [ReadOnly]
    public int length;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private Mesh wallMesh;
    private Mesh roofMesh;

    public void CalculateBounds()
    {
        length = Mathf.FloorToInt(width / prefferedRatio);
        Vector2 allignmentAxis = (allignmentAxisEnd - allignmentAxisStart).normalized;
        Vector2 position = allignmentAxisStart + axisPosition * (allignmentAxisEnd - allignmentAxisStart);
        buildingBounds = new Vector2[] { position - allignmentAxis * 0.5f * width,
                                         position + allignmentAxis * 0.5f * width,
                                         new Vector2(), new Vector2() };

        Vector2 perpAxis = new Vector2(-allignmentAxis.y, allignmentAxis.x);
        buildingBounds[2] = buildingBounds[1] + perpAxis * length;
        buildingBounds[3] = buildingBounds[0] + perpAxis * length;

        if (constraintBounds != null && constraintBounds.Length > 2)
        {
            transform.position = Vector3.zero;

            allignmentAxisStart = constraintBounds[0];
            allignmentAxisEnd = constraintBounds[1];

            for (int i = 1; i < constraintBounds.Length; i++)
            {
                Vector2 intersection;
                if (LineSegmentsIntersection(constraintBounds[i], constraintBounds[(i + 1) % constraintBounds.Length], buildingBounds[1], buildingBounds[2], out intersection))
                {
                    length = Mathf.FloorToInt(Vector2.Distance(buildingBounds[1], intersection));
                    buildingBounds[2] = buildingBounds[1] + perpAxis * length;
                    buildingBounds[3] = buildingBounds[0] + perpAxis * length;
                }
                if (LineSegmentsIntersection(constraintBounds[i], constraintBounds[(i + 1) % constraintBounds.Length], buildingBounds[0], buildingBounds[3], out intersection))
                {
                    length = Mathf.FloorToInt(Vector2.Distance(buildingBounds[0], intersection));
                    buildingBounds[2] = buildingBounds[1] + perpAxis * length;
                    buildingBounds[3] = buildingBounds[0] + perpAxis * length;

                }
            }
        }

        //        position += perpAxis * 0.5f * length;
        transform.position = new Vector3(position.x, 0, position.y);
    }

    private bool LineSegmentsIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

        if (d == 0.0f)
        {
            return false;
        }

        var u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
        var v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

        if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
        {
            return false;
        }

        intersection.x = p1.x + u * (p2.x - p1.x);
        intersection.y = p1.y + u * (p2.y - p1.y);

        return true;
    }

    public void GenerateMesh()
    {
        int children = transform.childCount;
        for (int i = 0; i < children; ++i)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        if (meshFilter == null)
            meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();


        mesh = new Mesh();
        mesh.name = "Building Mesh";

        if (wallMesh == null)
        {
            GameObject tempObject = Instantiate(wallPrefab);
            wallMesh = tempObject.GetComponentInChildren<MeshFilter>().sharedMesh;
            DestroyImmediate(tempObject);
        }

        if (roofMesh == null)
        {
            GameObject tempObject = Instantiate(roofPrefab);
            roofMesh = tempObject.GetComponentInChildren<MeshFilter>().sharedMesh;
            DestroyImmediate(tempObject);
        }

        for (int floor = 0; floor < height; floor++)
            if (floor < height - 1)
                for (int i = 0; i < buildingBounds.Length; i++)
                {
                    Vector2 wallAxis = buildingBounds[(i + 1) % buildingBounds.Length] - buildingBounds[i];
                    int wallLength = Mathf.RoundToInt(wallAxis.magnitude);
                    wallAxis.Normalize();

                    for (int j = 0; j < wallLength; j++)
                    {
                        Vector2 segmentPos = buildingBounds[i] + (j + 0.5f) * wallAxis;
                        WeldMesh(mesh, wallMesh, new Vector3(segmentPos.x, floor, segmentPos.y), Quaternion.LookRotation(new Vector3(-wallAxis.y, 0, wallAxis.x), Vector3.up), transform);
                    }
                }
            else
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < length; y++)
                    {
                        Vector2 allignmentAxis = (allignmentAxisEnd - allignmentAxisStart).normalized;
                        Vector2 perpAxis = new Vector2(-allignmentAxis.y, allignmentAxis.x);

                        Vector2 position = buildingBounds[0] + (x + 0.5f) * allignmentAxis + (y + 0.5f) * perpAxis;
                        WeldMesh(mesh, roofMesh, new Vector3(position.x, floor-0.5f, position.y), Quaternion.LookRotation(-Vector3.up, new Vector3(-allignmentAxis.y, 0, allignmentAxis.x)), transform);
                    }

        meshFilter.mesh = mesh;
    }

    public void WeldMesh(Mesh target, Mesh source, Vector3 position, Quaternion rotation, Transform parent)
    {
        List<Vector3> vertices = new List<Vector3>(target.vertices);
        List<int> triangles = new List<int>(target.triangles);
        List<Vector2> uvs0 = new List<Vector2>(target.uv);
        List<Vector2> uvs1 = new List<Vector2>(target.uv2);

        for (int i = 0; i < source.vertexCount; i++)
            vertices.Add(parent.worldToLocalMatrix.MultiplyPoint3x4(rotation * source.vertices[i] + position));

        for (int i = 0; i < source.triangles.Length; i++)
            triangles.Add(source.triangles[i] + target.vertexCount);

        uvs0.AddRange(source.uv);
        uvs1.AddRange(source.uv2);

        target.vertices = vertices.ToArray();
        target.triangles = triangles.ToArray();
        target.uv = uvs0.ToArray();
        target.uv2 = uvs1.ToArray();
    }
}
