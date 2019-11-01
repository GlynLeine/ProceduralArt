using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    private const float MINUTE_VALUE = 0.0001f;

    [HideInInspector]
    public Vector2[] constraintBounds;
    [HideInInspector]
    public Vector2 allignmentAxisStart;
    [HideInInspector]
    public Vector2 allignmentAxisEnd;
    [HideInInspector]
    public float axisPosition;

    [Header("Bounds Settings")]
    public int seed;
    public int width;
    [Range(0.2f, 5)]
    public float prefferedRatio;
    public int height;
    [ReadOnly]
    public int length;

    [Header("Shape Settings"), Range(0f, 100f)]
    public float splitChance;
    [Range(0.001f, 1f)]
    public float widthScaleLowerBound;
    [Range(0.001f, 1f)]
    public float widthScaleUpperBound;
    [Range(0.001f, 1f)]
    public float lengthScaleLowerBound;
    [Range(0.001f, 1f)]
    public float lengthScaleUpperBound;
    [Range(0.001f, 1f)]
    public float heightScaleLowerBound;
    [Range(0.001f, 1f)]
    public float heigthScaleUpperBound;

    [Header("Visuals Settings")]
    public BuildingTheme buildingTheme;

    [HideInInspector]
    public Vector2[] buildingBounds;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private MeshData meshData;
    private Mesh mesh;

    private bool cancelGeneration = false;
    private bool restartGeneration = false;
    private Thread generationThread;
    private Matrix4x4 baseTransformMatrix;

    private struct Limb
    {
        public Vector2 axis;
        public int length;
        public int width;
    }
    private Limb[] skeleton;

    private System.Random random;

    public void CalculateBounds(bool useTransformPosition)
    {
        Vector2 allignmentAxis = (allignmentAxisEnd - allignmentAxisStart);
        float maxWidth = allignmentAxis.magnitude - MINUTE_VALUE;
        allignmentAxis.Normalize();

        if (width > (int)maxWidth)
            width = (int)maxWidth;
        if (width < 1)
            width = 1;

        length = Mathf.Max(1, Mathf.FloorToInt(width / prefferedRatio));

        if (useTransformPosition)
            axisPosition = Vector2.Dot(new Vector2(transform.position.x, transform.position.z) - allignmentAxisStart, allignmentAxis) / Vector2.Distance(allignmentAxisStart, allignmentAxisEnd);

        axisPosition = Mathf.Min(1f - width / 2f / maxWidth, Mathf.Max(width / 2f / maxWidth, axisPosition));

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
                    length = Mathf.Max(1, Mathf.FloorToInt(Vector2.Distance(buildingBounds[1], intersection)));
                    buildingBounds[2] = buildingBounds[1] + perpAxis * length;
                    buildingBounds[3] = buildingBounds[0] + perpAxis * length;
                }
                if (LineSegmentsIntersection(constraintBounds[i], constraintBounds[(i + 1) % constraintBounds.Length], buildingBounds[0], buildingBounds[3], out intersection))
                {
                    length = Mathf.Max(1, Mathf.FloorToInt(Vector2.Distance(buildingBounds[0], intersection)));
                    buildingBounds[2] = buildingBounds[1] + perpAxis * length;
                    buildingBounds[3] = buildingBounds[0] + perpAxis * length;

                }
            }
        }

        transform.rotation = Quaternion.LookRotation(new Vector3(perpAxis.x, 0, perpAxis.y), Vector3.up);
        transform.position = new Vector3(position.x, 0, position.y);
    }

    public void CalculateSkeleton()
    {
        random = new System.Random(seed);        
        if(((float)random.NextDouble()) * 100f <= splitChance)
        {

        }
        else
        {
            Vector2 limbAxis = (allignmentAxisEnd - allignmentAxisStart).normalized;
            limbAxis = new Vector2(-limbAxis.y, limbAxis.x);
            skeleton = new Limb[] { new Limb() { axis = limbAxis, length = length, width = width} };
        }
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

    private IEnumerator TrackProgressCoroutine()
    {
        while (generationThread.IsAlive)
        {
            if (cancelGeneration)
            {
                generationThread.Abort();
            }
            yield return null;
        }

        if (!cancelGeneration)
        {
            mesh = meshData.GetMesh();
            mesh.name = gameObject.name + " Mesh";
            meshFilter.mesh = mesh;
            meshRenderer.materials = meshData.materials;
        }

        if(restartGeneration)
        {
            restartGeneration = false;
            cancelGeneration = false;
            generationThread = new Thread(new ThreadStart(MeshGenerationThread));

            meshData = new MeshData();
            baseTransformMatrix = transform.worldToLocalMatrix;

            generationThread.Start();

            StartCoroutine(TrackProgressCoroutine());
        }
    }

    public void GenerateMesh()
    {
        if (meshFilter == null)
            meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (generationThread != null && generationThread.IsAlive)
        {
            cancelGeneration = true;
            restartGeneration = true;
            return;
        }

        restartGeneration = false;
        cancelGeneration = false;
        generationThread = new Thread(new ThreadStart(MeshGenerationThread));

        meshData = new MeshData();
        baseTransformMatrix = transform.worldToLocalMatrix;

        generationThread.Start();

        StartCoroutine(TrackProgressCoroutine());
    }

    private void MeshGenerationThread()
    {
        buildingTheme.SetSeed(seed);

        for (int floor = 0; floor < height; floor++)
            for (int i = 0; i < buildingBounds.Length; i++)
            {
                Vector2 wallAxis = buildingBounds[(i + 1) % buildingBounds.Length] - buildingBounds[i];
                int wallLength = Mathf.RoundToInt(wallAxis.magnitude);
                wallAxis.Normalize();

                for (int j = 0; j < wallLength; j++)
                {
                    Vector2 segmentPos = buildingBounds[i] + j * wallAxis;
                    MeshData wallData = buildingTheme.GetRandomMesh(MeshType.wall, SectionType.straight);
                    WeldMesh(meshData, wallData, new Vector3(segmentPos.x, floor, segmentPos.y), Quaternion.LookRotation(new Vector3(-wallAxis.y, 0, wallAxis.x), Vector3.up), baseTransformMatrix);
                }
            }

        for (int i = 0; i < buildingBounds.Length; i++)
        {
            Vector2 axis = buildingBounds[(i + 1) % buildingBounds.Length] - buildingBounds[i];
            Vector2 origin = buildingBounds[i];
            int roofLength = Mathf.RoundToInt(axis.magnitude);
            axis.Normalize();

            Vector2 inwardAxis = new Vector2(-axis.y, axis.x);

            if (i == 0 || i == 2)
            {
                for (int perimeter = 0; perimeter < width / 2; perimeter++)
                    for (int j = perimeter; j < roofLength - perimeter; j++)
                    {
                        Vector2 segmentPos = origin + j * axis - inwardAxis * MINUTE_VALUE;
                        MeshData roofFacadeData = buildingTheme.GetRandomMesh(MeshType.facade, SectionType.straight);
                        WeldMesh(meshData, roofFacadeData, new Vector3(segmentPos.x, height + perimeter, segmentPos.y), Quaternion.LookRotation(new Vector3(-axis.y, 0, axis.x), Vector3.up), baseTransformMatrix);
                    }

                if (width % 2 == 1)
                {
                    int index = width / 2;
                    Vector2 segmentPos = origin + index * axis - inwardAxis * MINUTE_VALUE;
                    MeshData roofFacadeData = buildingTheme.GetRandomMesh(MeshType.facade, SectionType.centeredStraight);
                    WeldMesh(meshData, roofFacadeData, new Vector3(segmentPos.x, height + index, segmentPos.y), Quaternion.LookRotation(new Vector3(-axis.y, 0, axis.x), Vector3.up), baseTransformMatrix);
                }
            }
            else
            {
                for (int perimeter = 0; perimeter < width / 2; perimeter++)
                    for (int j = 0; j < roofLength; j++)
                    {
                        Vector2 segmentPos = origin + j * axis + inwardAxis * perimeter - inwardAxis * MINUTE_VALUE;
                        MeshData roofData = buildingTheme.GetRandomMesh(MeshType.roof, SectionType.straight);
                        WeldMesh(meshData, roofData, new Vector3(segmentPos.x, height + perimeter, segmentPos.y), Quaternion.LookRotation(new Vector3(-axis.y, 0, axis.x), Vector3.up), baseTransformMatrix);
                    }
            }
        }

        List<(Vector2 origin, Vector2 axis, int length)> buildingSkeleton = new List<(Vector2 origin, Vector2 axis, int length)>();
        List<Vector2> uniques = new List<Vector2>();
        for (int i = 0; i < buildingBounds.Length; i++)
        {
            Vector2 normal = buildingBounds[(i + 1) % buildingBounds.Length] - buildingBounds[i];
            int length = Mathf.RoundToInt(normal.magnitude);
            normal.Normalize();

            if (!uniques.Contains(normal) && !uniques.Contains(-normal))
            {
                uniques.Add(normal);
                buildingSkeleton.Add((buildingBounds[i], normal, length));
            }
        }

        if (width % 2 == 1)
        {
            for (int i = 0; i < buildingSkeleton.Count; i++)
            {
                Vector2 axis = buildingSkeleton[i].axis;
                Vector2 origin = buildingSkeleton[i].origin;
                int length = buildingSkeleton[i].length;

                int index = width / 2;
                if (i != 0 && i != 2)
                {
                    Vector2 inwardAxis = new Vector2(-axis.y, axis.x);

                    for (int j = 0; j < length; j++)
                    {
                        Vector2 segmentPos = buildingBounds[i] + j * axis + inwardAxis * index;
                        MeshData roofData = buildingTheme.GetRandomMesh(MeshType.roof, SectionType.centeredStraight);
                        WeldMesh(meshData, roofData, new Vector3(segmentPos.x, height + index, segmentPos.y), Quaternion.LookRotation(new Vector3(-axis.y, 0, axis.x), Vector3.up), baseTransformMatrix);
                    }
                }
            }
        }
    }

    public void WeldMesh(MeshData target, MeshData source, Vector3 position, Quaternion rotation, Matrix4x4 worldToLocalMatrix)
    {
        List<Material> materials = new List<Material>();

        if (target.vertexCount > 0)
        {
            materials = new List<Material>(target.materials);
        }
        else
            target.subMeshCount = 0;

        int vertexOffset = target.vertexCount;

        List<Vector3> vertices = new List<Vector3>(target.vertices);

        for (int i = 0; i < source.vertexCount; i++)
            vertices.Add(worldToLocalMatrix.MultiplyPoint3x4(rotation * source.vertices[i] + position));

        List<Vector2> uvs = new List<Vector2>(target.uv);

        uvs.AddRange(source.uv);

        target.vertices = vertices.ToArray();

        if (uvs.Count == target.vertexCount)
            target.uv = uvs.ToArray();

        for (int i = 0; i < source.subMeshCount; i++)
        {
            List<int> triangles = new List<int>(source.GetTriangles(i));

            for (int j = 0; j < triangles.Count; j++)
                triangles[j] += vertexOffset;

            Material material = source.materials[i];

            int submeshIndex = materials.IndexOf(material);
            if (submeshIndex >= 0)
            {
                triangles.AddRange(target.GetTriangles(submeshIndex));
                target.SetTriangles(triangles, submeshIndex);
            }
            else
            {
                target.subMeshCount++;
                target.SetTriangles(triangles, target.subMeshCount - 1);
                materials.Add(material);
            }
        }

        target.materials = materials.ToArray();
    }
}
