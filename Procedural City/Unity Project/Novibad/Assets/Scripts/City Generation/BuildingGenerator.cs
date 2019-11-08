using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class BuildingGenerator : MonoBehaviour
{
    private const float MINUTE_VALUE = 0.0001f;
    public static int numberOfGeneratingBuildings = 0;

    public static TerrainGenerator sharedTerrain;

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

    public TerrainGenerator terrain;

    [Header("Visuals Settings")]
    public BuildingShape buildingShape;
    public BuildingTheme buildingTheme;
    public int seed;

    #region Generation Values
    private int width;
    private int length;
    private int height;

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
    private float heightOffset;

    [System.Serializable]
    public struct WallInterrupt
    {
        public int height;
        public int sideScale;
        public int start;
        public int end;
    }

    [System.Serializable]
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
        random = new System.Random(seed);

        if (constraintBounds != null && constraintBounds.Length >= 2)
        {
            allignmentAxisStart = constraintBounds[0];
            allignmentAxisEnd = constraintBounds[1];
        }

        Vector2 allignmentAxis = (allignmentAxisEnd - allignmentAxisStart);
        float maxWidth = allignmentAxis.magnitude - MINUTE_VALUE;
        allignmentAxis.Normalize();
        width = buildingShape.width;
        if (width > (int)maxWidth)
            width = (int)maxWidth;
        if (width < 1)
            width = 1;

        height = random.Next(buildingShape.heightLowerBound, buildingShape.heightUpperBound + 1);

        length = Mathf.Max(1, Mathf.FloorToInt(width / ((float)random.NextDouble() * (buildingShape.prefferedRatioUpperBound - buildingShape.prefferedRatioLowerBound) + buildingShape.prefferedRatioLowerBound)));

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

        if (sharedTerrain == null)
        {
            sharedTerrain = terrain;
        }
        else
            terrain = sharedTerrain;

        float lowestHeight = float.MaxValue;
        float highestHeight = float.MinValue;
        if (sharedTerrain != null)
            for (int i = 0; i < buildingBounds.Length; i++)
            {
                float terrainHeight = sharedTerrain.GetTerrainHeight(buildingBounds[i]);

                if (terrainHeight < lowestHeight)
                    lowestHeight = terrainHeight;

                if (terrainHeight > highestHeight)
                    highestHeight = terrainHeight;
            }

        if (lowestHeight > float.MaxValue * 0.9)
            heightOffset = transform.position.y;
        else
        {
            heightOffset = lowestHeight;
            height += Mathf.RoundToInt(highestHeight - lowestHeight);
        }

        transform.rotation = Quaternion.LookRotation(new Vector3(perpAxis.x, 0, perpAxis.y), Vector3.up);
        transform.position = new Vector3(position.x, heightOffset, position.y);
    }

    public void CalculateSkeleton()
    {
        if ((float)random.NextDouble() * 100f <= buildingShape.splitChance)
        {
            Vector2 subLimbAxis = (allignmentAxisEnd - allignmentAxisStart).normalized;
            Vector2 mainLimbAxis = new Vector2(-subLimbAxis.y, subLimbAxis.x);
            int sideScale = random.Next(2) > 0.5 ? 1 : -1;
            subLimbAxis *= sideScale;

            float widthScale = (float)random.NextDouble() * (buildingShape.mainLimbWidthScaleUpperBound - buildingShape.mainLimbWidthScaleLowerBound) + buildingShape.mainLimbWidthScaleLowerBound;
            int mainLimbWidth = Mathf.RoundToInt(width * widthScale);
            int subLimbLength = width - mainLimbWidth + mainLimbWidth / 2;
            int subLimbHeight = Mathf.RoundToInt(height * ((float)random.NextDouble() * (buildingShape.subLimbHeigthScaleUpperBound - buildingShape.subLimbHeightScaleLowerBound) + buildingShape.subLimbHeightScaleLowerBound));

            Vector2 mainLimbBase = new Vector2(transform.position.x, transform.position.z) - (subLimbLength - width / 2f) * subLimbAxis;

            float splitScale = (float)random.NextDouble() * (buildingShape.subLimbPositionScaleUpperBound - buildingShape.subLimbPositionScaleLowerBound) + buildingShape.subLimbPositionScaleLowerBound;
            int splitIndex = (int)(splitScale * length);
            Vector2 subLimbBase = mainLimbBase + mainLimbAxis * splitIndex;

            int subLimbWidth = Mathf.RoundToInt(length * ((float)random.NextDouble() * (buildingShape.subLimbWidthScaleUpperBound - buildingShape.subLimbWidthScaleLowerBound) + buildingShape.subLimbWidthScaleLowerBound));

            WallInterrupt mainWallInterrupt = new WallInterrupt() { sideScale = sideScale, start = splitIndex - subLimbWidth / 2, end = splitIndex + Mathf.FloorToInt(subLimbWidth / 2f), height = subLimbHeight };
            WallInterrupt subLimbInterruptLeft = new WallInterrupt() { sideScale = -1, start = -1, end = Mathf.RoundToInt(mainLimbWidth / 2f), height = height };
            WallInterrupt subLimbInterruptRight = new WallInterrupt() { sideScale = 1, start = -1, end = Mathf.RoundToInt(mainLimbWidth / 2f), height = height };

            Limb mainLimb = new Limb() { limbBase = mainLimbBase, axis = mainLimbAxis, length = length, width = mainLimbWidth, height = height, doubleSided = true, interrupts = new WallInterrupt[] { mainWallInterrupt } };
            Limb subLimb = new Limb() { limbBase = subLimbBase, axis = subLimbAxis, length = subLimbLength, width = subLimbWidth, height = subLimbHeight, doubleSided = false, interrupts = new WallInterrupt[] { subLimbInterruptLeft, subLimbInterruptRight } };
            skeleton = new Limb[] { mainLimb, subLimb };
        }
        else
        {
            Vector2 limbAxis = (allignmentAxisEnd - allignmentAxisStart).normalized;
            limbAxis = new Vector2(-limbAxis.y, limbAxis.x);
            skeleton = new Limb[] { new Limb() { limbBase = new Vector2(transform.position.x, transform.position.z), axis = limbAxis, length = length, width = width, height = height, doubleSided = true } };
        }
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

    private IEnumerator TrackProgressCoroutine()
    {
        while (generationThread.IsAlive)
        {
            if (cancelGeneration)
            {
                generationThread.Abort();
                numberOfGeneratingBuildings--;
            }
            yield return null;
        }

        if (!cancelGeneration)
        {
            mesh = meshData.GetMesh();
            mesh.name = gameObject.name + " Mesh";
            Unwrapping.GenerateSecondaryUVSet(mesh);
            meshFilter.mesh = mesh;
            meshRenderer.materials = meshData.materials;
            numberOfGeneratingBuildings--;
        }

        if (restartGeneration)
        {
            Debug.Log("restart thread");
            restartGeneration = false;
            cancelGeneration = false;
            generationThread = new Thread(new ThreadStart(MeshGenerationThread));

            meshData = new MeshData();
            baseTransformMatrix = transform.worldToLocalMatrix;

            generationThread.Start();
            numberOfGeneratingBuildings++;
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
        heightOffset = transform.position.y;

        generationThread.Start();
        numberOfGeneratingBuildings++;
        StartCoroutine(TrackProgressCoroutine());
    }

    private void GenerateWalls()
    {
        foreach (Limb limb in skeleton)
        {
            Vector2 yAxis = limb.axis;
            Vector2 xAxis = new Vector2(yAxis.y, -yAxis.x);

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
                        Vector3 position = new Vector3(segmentPos.x, floor + heightOffset, segmentPos.y);
                        Quaternion rotation = Quaternion.LookRotation(new Vector3(xAxis.x, 0, xAxis.y) * sideScale * -1, Vector3.up);
                        WeldMesh(meshData, wallData, position, rotation, baseTransformMatrix, new Vector2(y, floor));
                    }

                    if (!limb.doubleSided && sideScale < 0)
                        continue;

                    for (int x = 0; x < limb.width; x++)
                    {
                        Vector2 segmentPos = limb.limbBase + xAxis * (x - limb.width / 2f + 0.5f) + yAxis * limb.length * Mathf.Max(0, sideScale);
                        MeshData wallData = buildingTheme.GetRandomMesh(MeshType.wall, SectionType.straight);
                        Vector3 position = new Vector3(segmentPos.x, floor + heightOffset, segmentPos.y);
                        Quaternion rotation = Quaternion.LookRotation(new Vector3(yAxis.x, 0, yAxis.y) * sideScale * -1, Vector3.up);
                        WeldMesh(meshData, wallData, position, rotation, baseTransformMatrix, new Vector2(x - limb.width / 2f, floor));
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
                        Vector3 position = new Vector3(segmentPos.x, limb.height + x + heightOffset, segmentPos.y);
                        Quaternion rotation = Quaternion.LookRotation(new Vector3(xAxis.x, 0, xAxis.y) * sideScale * -1, Vector3.up);
                        WeldMesh(meshData, roofData, position, rotation, baseTransformMatrix, new Vector2(x, y));
                    }

                    for (int frontScale = -1; frontScale <= 1; frontScale += 2)
                        if (limb.doubleSided || frontScale > 0)
                            for (int floor = 0; floor <= x; floor++)
                            {
                                Vector2 segmentPos = limb.limbBase + xAxis * (limb.width / 2f - x - 0.5f) * sideScale + yAxis * (limb.length * Mathf.Max(0, frontScale) + MINUTE_VALUE * frontScale);
                                MeshData roofFacadeData = buildingTheme.GetRandomMesh(MeshType.facade, SectionType.centeredStraight);
                                Vector3 position = new Vector3(segmentPos.x, limb.height + floor + heightOffset, segmentPos.y);
                                Quaternion rotation = Quaternion.LookRotation(new Vector3(yAxis.x, 0, yAxis.y) * frontScale * -1, Vector3.up);
                                WeldMesh(meshData, roofFacadeData, position, rotation, baseTransformMatrix, new Vector2(x, floor));
                            }

                }

            if (limb.width % 2 == 1)
            {
                int roofHeight = limb.width / 2;

                for (int y = 0; y < limb.length; y++)
                {
                    Vector2 segmentPos = limb.limbBase + (y + 0.5f) * yAxis;
                    MeshData roofData = buildingTheme.GetRandomMesh(MeshType.roof, SectionType.centeredStraight);
                    Vector3 position = new Vector3(segmentPos.x, limb.height + roofHeight + heightOffset, segmentPos.y);
                    Quaternion rotation = Quaternion.LookRotation(new Vector3(xAxis.x, 0, xAxis.y), Vector3.up);
                    WeldMesh(meshData, roofData, position, rotation, baseTransformMatrix, new Vector2(0, y));
                }

                for (int y = 0; y <= roofHeight; y++)
                    for (int frontScale = -1; frontScale <= 1; frontScale += 2)
                        if (limb.doubleSided || frontScale > 0)
                        {
                            Vector2 segmentPos = limb.limbBase + yAxis * limb.length * Mathf.Max(0, frontScale) + yAxis * MINUTE_VALUE * frontScale;
                            MeshData roofFacadeData = buildingTheme.GetRandomMesh(MeshType.facade, SectionType.centeredStraight);
                            Vector3 position = new Vector3(segmentPos.x, limb.height + y + heightOffset, segmentPos.y);
                            Quaternion rotation = Quaternion.LookRotation(new Vector3(yAxis.x, 0, yAxis.y) * frontScale * -1, Vector3.up);
                            WeldMesh(meshData, roofFacadeData, position, rotation, baseTransformMatrix, new Vector2(0, y));
                        }
            }
        }
    }

    private void MeshGenerationThread()
    {
        buildingTheme.SetSeed(seed);

        GenerateWalls();

        GenerateRoofs();
    }

    public void WeldMesh(MeshData target, MeshData source, Vector3 position, Quaternion rotation, Matrix4x4 worldToLocalMatrix, Vector2 uvOffset)
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
        int uvIndex = uvs.Count;
        uvs.AddRange(source.uv);

        for (int i = uvIndex; i < uvs.Count; i++)
            uvs[i] += uvOffset;

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
