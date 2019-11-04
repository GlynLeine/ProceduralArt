﻿using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    private const float MINUTE_VALUE = 0.0001f;

    #region Position & Constraint Inputs
    [HideInInspector]
    public Vector2[] constraintBounds;
    [HideInInspector]
    public Vector2 allignmentAxisStart;
    [HideInInspector]
    public Vector2 allignmentAxisEnd;
    [HideInInspector]
    public float axisPosition;
    #endregion

    [Header("Visuals Settings")]
    public BuildingShape buildingShape;
    public BuildingTheme buildingTheme;
    public int seed;

    #region Generation Values
    private int width;
    private int length;

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

    public struct WallInterrupt
    {
        public int height;
        public int sideScale;
        public int start;
        public int end;
    }
    public struct Limb
    {
        public bool doubleSided;
        public Vector2 limbBase;
        public Vector2 axis;
        public int length;
        public int height;
        public int width;

        public WallInterrupt[] interrupts;
    }
    public Limb[] skeleton;

    private System.Random random;
    #endregion

    public void CalculateBounds(bool useTransformPosition)
    {
        Vector2 allignmentAxis = (allignmentAxisEnd - allignmentAxisStart);
        float maxWidth = allignmentAxis.magnitude - MINUTE_VALUE;
        allignmentAxis.Normalize();
        width = buildingShape.width;
        if (width > (int)maxWidth)
            width = (int)maxWidth;
        if (width < 1)
            width = 1;

        length = Mathf.Max(1, Mathf.FloorToInt(width / buildingShape.prefferedRatio));

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

        CalculateSkeleton();
    }

    public void CalculateSkeleton()
    {
        random = new System.Random(seed);
        if ((float)random.NextDouble() * 100f <= buildingShape.splitChance)
        {
            Vector2 subLimbAxis = (allignmentAxisEnd - allignmentAxisStart).normalized;
            Vector2 mainLimbAxis = new Vector2(-subLimbAxis.y, subLimbAxis.x);
            int sideScale = random.Next(2) > 0.5 ? 1 : -1;
            subLimbAxis *= sideScale;

            float widthScale = (float)random.NextDouble() * (buildingShape.mainLimbWidthScaleUpperBound - buildingShape.mainLimbWidthScaleLowerBound) + buildingShape.mainLimbWidthScaleLowerBound;
            int mainLimbWidth = Mathf.RoundToInt(width * widthScale);
            int subLimbLength = width - mainLimbWidth + mainLimbWidth / 2;
            int subLimbHeight = Mathf.RoundToInt(buildingShape.height * ((float)random.NextDouble() * (buildingShape.subLimbHeigthScaleUpperBound - buildingShape.subLimbHeightScaleLowerBound) + buildingShape.subLimbHeightScaleLowerBound));

            Vector2 mainLimbBase = new Vector2(transform.position.x, transform.position.z) - (subLimbLength - width / 2f) * subLimbAxis;

            float splitScale = (float)random.NextDouble() * (buildingShape.subLimbPositionScaleUpperBound - buildingShape.subLimbPositionScaleLowerBound) + buildingShape.subLimbPositionScaleLowerBound;
            int splitIndex = (int)(splitScale * length);
            Vector2 subLimbBase = mainLimbBase + mainLimbAxis * splitIndex;

            int subLimbWidth = Mathf.RoundToInt(length * ((float)random.NextDouble() * (buildingShape.subLimbWidthScaleUpperBound - buildingShape.subLimbWidthScaleLowerBound) + buildingShape.subLimbWidthScaleLowerBound));

            WallInterrupt mainWallInterrupt = new WallInterrupt() { sideScale = sideScale, start = splitIndex - subLimbWidth / 2, end = splitIndex + Mathf.FloorToInt(subLimbWidth / 2f), height = subLimbHeight };
            WallInterrupt subLimbInterruptLeft = new WallInterrupt() { sideScale = -1, start = -1, end = Mathf.RoundToInt(mainLimbWidth / 2f), height = buildingShape.height };
            WallInterrupt subLimbInterruptRight = new WallInterrupt() { sideScale = 1, start = -1, end = Mathf.RoundToInt(mainLimbWidth / 2f), height = buildingShape.height };

            Limb mainLimb = new Limb() { limbBase = mainLimbBase, axis = mainLimbAxis, length = length, width = mainLimbWidth, height = buildingShape.height, doubleSided = true, interrupts = new WallInterrupt[] { mainWallInterrupt } };
            Limb subLimb = new Limb() { limbBase = subLimbBase, axis = subLimbAxis, length = subLimbLength, width = subLimbWidth, height = subLimbHeight, doubleSided = false, interrupts = new WallInterrupt[] { subLimbInterruptLeft, subLimbInterruptRight } };
            skeleton = new Limb[] { mainLimb, subLimb };

            pos = new Vector2(transform.position.x, transform.position.z);
        }
        else
        {
            Vector2 limbAxis = (allignmentAxisEnd - allignmentAxisStart).normalized;
            limbAxis = new Vector2(-limbAxis.y, limbAxis.x);
            skeleton = new Limb[] { new Limb() { limbBase = new Vector2(transform.position.x, transform.position.z), axis = limbAxis, length = length, width = width, height = buildingShape.height, doubleSided = true } };
        }
    }

    Vector2 pos;

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

        if (restartGeneration)
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

    private void GenerateWalls()
    {
        foreach (Limb limb in skeleton)
        {
            Vector2 yAxis = limb.axis;
            Vector2 xAxis = new Vector2(yAxis.y, -yAxis.x);

            Debug.Log((limb.limbBase - pos) + " " + limb.width + " " + limb.length);

            for (int floor = 0; floor < limb.height; floor++)
                for (int sideScale = -1; sideScale <= 1; sideScale += 2)
                {
                    for (int y = 0; y < limb.length; y++)
                    {

                        if (limb.interrupts != null)
                        {
                            bool interrupt = false;
                            foreach (WallInterrupt wallInterrupt in limb.interrupts)
                                if (sideScale == wallInterrupt.sideScale)
                                    if (y >= wallInterrupt.start + Mathf.Max(0, floor - wallInterrupt.height + 1) && y < wallInterrupt.end - Mathf.Max(0, floor - wallInterrupt.height + 1))
                                        interrupt = true;
                            if (interrupt)
                                continue;
                        }

                        Vector2 segmentPos = limb.limbBase + xAxis * (limb.width * 0.5f) * sideScale + yAxis * (y + 0.5f);
                        MeshData wallData = buildingTheme.GetRandomMesh(MeshType.wall, SectionType.straight);
                        WeldMesh(meshData, wallData, new Vector3(segmentPos.x, floor, segmentPos.y), Quaternion.LookRotation(new Vector3(xAxis.x, 0, xAxis.y) * sideScale * -1, Vector3.up), baseTransformMatrix);
                    }

                    if (!limb.doubleSided && sideScale < 0)
                        continue;

                    for (int x = 0; x < limb.width; x++)
                    {
                        Vector2 segmentPos = limb.limbBase + xAxis * (x - limb.width / 2f + 0.5f) + yAxis * limb.length * Mathf.Max(0, sideScale);
                        MeshData wallData = buildingTheme.GetRandomMesh(MeshType.wall, SectionType.straight);
                        WeldMesh(meshData, wallData, new Vector3(segmentPos.x, floor, segmentPos.y), Quaternion.LookRotation(new Vector3(yAxis.x, 0, yAxis.y) * sideScale * -1, Vector3.up), baseTransformMatrix);
                    }
                }
        }
    }

    private void GenerateRoofs()
    {
        foreach (Limb limb in skeleton)
        {
            Vector2 yAxis = limb.axis;
            Vector2 xAxis = new Vector2(yAxis.y, -yAxis.x);

            for (int x = 0; x < limb.width / 2; x++)
                for (int sideScale = -1; sideScale <= 1; sideScale += 2)
                {
                    for (int y = 0; y < limb.length; y++)
                    {
                        if (limb.interrupts != null)
                        {
                            bool interrupt = false;
                            foreach (WallInterrupt wallInterrupt in limb.interrupts)
                                if (sideScale == wallInterrupt.sideScale)
                                    if (y >= wallInterrupt.start + Mathf.Max(0, limb.height + x - wallInterrupt.height + 1) && y < wallInterrupt.end - Mathf.Max(0, limb.height + x - wallInterrupt.height + 1))
                                        interrupt = true;
                            if (interrupt)
                                continue;
                        }

                        Vector2 segmentPos = limb.limbBase + xAxis * (limb.width / 2f - x) * sideScale + yAxis * (y + 0.5f);
                        MeshData roofData = buildingTheme.GetRandomMesh(MeshType.roof, SectionType.straight);
                        WeldMesh(meshData, roofData, new Vector3(segmentPos.x, limb.height + x, segmentPos.y), Quaternion.LookRotation(new Vector3(xAxis.x, 0, xAxis.y) * sideScale * -1, Vector3.up), baseTransformMatrix);
                    }

                    for (int frontScale = -1; frontScale <= 1; frontScale += 2)
                        if (limb.doubleSided || frontScale > 0)
                            for (int floor = 0; floor <= x; floor++)
                            {
                                Vector2 segmentPos = limb.limbBase + xAxis * (limb.width / 2f - x - 0.5f) * sideScale + yAxis * (limb.length * Mathf.Max(0, frontScale) + MINUTE_VALUE * frontScale);
                                MeshData roofFacadeData = buildingTheme.GetRandomMesh(MeshType.facade, SectionType.centeredStraight);
                                WeldMesh(meshData, roofFacadeData, new Vector3(segmentPos.x, limb.height + floor, segmentPos.y), Quaternion.LookRotation(new Vector3(yAxis.x, 0, yAxis.y) * frontScale * -1, Vector3.up), baseTransformMatrix);
                            }

                }

            if (limb.width % 2 == 1)
            {
                int roofHeight = limb.width / 2;

                for (int y = 0; y < limb.length; y++)
                {
                    Vector2 segmentPos = limb.limbBase + (y + 0.5f) * yAxis;
                    MeshData roofData = buildingTheme.GetRandomMesh(MeshType.roof, SectionType.centeredStraight);
                    WeldMesh(meshData, roofData, new Vector3(segmentPos.x, limb.height + roofHeight, segmentPos.y), Quaternion.LookRotation(new Vector3(xAxis.x, 0, xAxis.y), Vector3.up), baseTransformMatrix);
                }

                for (int y = 0; y <= roofHeight; y++)
                    for (int frontScale = -1; frontScale <= 1; frontScale += 2)
                        if (limb.doubleSided || frontScale > 0)
                        {
                            Vector2 segmentPos = limb.limbBase + yAxis * limb.length * Mathf.Max(0, frontScale) + yAxis * MINUTE_VALUE * frontScale;
                            MeshData roofFacadeData = buildingTheme.GetRandomMesh(MeshType.facade, SectionType.centeredStraight);
                            WeldMesh(meshData, roofFacadeData, new Vector3(segmentPos.x, limb.height + y, segmentPos.y), Quaternion.LookRotation(new Vector3(yAxis.x, 0, yAxis.y) * frontScale * -1, Vector3.up), baseTransformMatrix);
                        }
            }
        }
    }

    private void MeshGenerationThread()
    {
        buildingTheme.SetSeed(seed);

        GenerateWalls();

        GenerateRoofs();

        //for (int i = 0; i < buildingBounds.Length; i++)
        //{
        //    Vector2 axis = buildingBounds[(i + 1) % buildingBounds.Length] - buildingBounds[i];
        //    Vector2 origin = buildingBounds[i];
        //    int roofLength = Mathf.RoundToInt(axis.magnitude);
        //    axis.Normalize();

        //    Vector2 inwardAxis = new Vector2(-axis.y, axis.x);

        //    if (i == 0 || i == 2)
        //    {
        //        for (int perimeter = 0; perimeter < width / 2; perimeter++)
        //            for (int j = perimeter; j < roofLength - perimeter; j++)
        //            {
        //                Vector2 segmentPos = origin + j * axis - inwardAxis * MINUTE_VALUE;
        //                MeshData roofFacadeData = buildingTheme.GetRandomMesh(MeshType.facade, SectionType.straight);
        //                WeldMesh(meshData, roofFacadeData, new Vector3(segmentPos.x, buildingShape.height + perimeter, segmentPos.y), Quaternion.LookRotation(new Vector3(-axis.y, 0, axis.x), Vector3.up), baseTransformMatrix);
        //            }

        //        if (width % 2 == 1)
        //        {
        //            int index = width / 2;
        //            Vector2 segmentPos = origin + index * axis - inwardAxis * MINUTE_VALUE;
        //            MeshData roofFacadeData = buildingTheme.GetRandomMesh(MeshType.facade, SectionType.centeredStraight);
        //            WeldMesh(meshData, roofFacadeData, new Vector3(segmentPos.x, buildingShape.height + index, segmentPos.y), Quaternion.LookRotation(new Vector3(-axis.y, 0, axis.x), Vector3.up), baseTransformMatrix);
        //        }
        //    }
        //    else
        //    {
        //        for (int perimeter = 0; perimeter < width / 2; perimeter++)
        //            for (int j = 0; j < roofLength; j++)
        //            {
        //                Vector2 segmentPos = origin + j * axis + inwardAxis * perimeter - inwardAxis * MINUTE_VALUE;
        //                MeshData roofData = buildingTheme.GetRandomMesh(MeshType.roof, SectionType.straight);
        //                WeldMesh(meshData, roofData, new Vector3(segmentPos.x, buildingShape.height + perimeter, segmentPos.y), Quaternion.LookRotation(new Vector3(-axis.y, 0, axis.x), Vector3.up), baseTransformMatrix);
        //            }
        //    }
        //}

        //List<(Vector2 origin, Vector2 axis, int length)> buildingSkeleton = new List<(Vector2 origin, Vector2 axis, int length)>();
        //List<Vector2> uniques = new List<Vector2>();
        //for (int i = 0; i < buildingBounds.Length; i++)
        //{
        //    Vector2 normal = buildingBounds[(i + 1) % buildingBounds.Length] - buildingBounds[i];
        //    int length = Mathf.RoundToInt(normal.magnitude);
        //    normal.Normalize();

        //    if (!uniques.Contains(normal) && !uniques.Contains(-normal))
        //    {
        //        uniques.Add(normal);
        //        buildingSkeleton.Add((buildingBounds[i], normal, length));
        //    }
        //}

        //if (width % 2 == 1)
        //{
        //    for (int i = 0; i < buildingSkeleton.Count; i++)
        //    {
        //        Vector2 axis = buildingSkeleton[i].axis;
        //        Vector2 origin = buildingSkeleton[i].origin;
        //        int length = buildingSkeleton[i].length;

        //        int index = width / 2;
        //        if (i != 0 && i != 2)
        //        {
        //            Vector2 inwardAxis = new Vector2(-axis.y, axis.x);

        //            for (int j = 0; j < length; j++)
        //            {
        //                Vector2 segmentPos = buildingBounds[i] + j * axis + inwardAxis * index;
        //                MeshData roofData = buildingTheme.GetRandomMesh(MeshType.roof, SectionType.centeredStraight);
        //                WeldMesh(meshData, roofData, new Vector3(segmentPos.x, buildingShape.height + index, segmentPos.y), Quaternion.LookRotation(new Vector3(-axis.y, 0, axis.x), Vector3.up), baseTransformMatrix);
        //            }
        //        }
        //    }
        //}
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
