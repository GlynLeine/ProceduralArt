using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class TerrainGen : MonoBehaviour
{
    //Public Properties
    #region Inputs
    public bool updateInEditor;

    [Header("Mesh Settings")]
    public int dimension = 10;
    public float uvScale = 2f;
    public Octave[] octaves;
    public int resolution = 1;
    public int seed = 0;

    [Header("Erosion Settings")]
    public bool erodeInEditor;
    public int erosionIterationCount = 50000;
    public int erosionBrushRadius = 3;

    public int maximumDropletLifeTime = 30;
    public float sedimentCapacityFactor = 3;
    public float minSedimentCapacity = .01f;
    public float depositSpeed = 0.3f;
    public float erosionSpeed = 0.3f;

    public float evaporationSpeed = .01f;
    public float gravity = 4;
    public float startSpeed = 1;
    public float startWater = 1;
    [Range(0, 1)]
    public float inertia = 0.3f;
    #endregion

    #region Readables
    [Serializable]
    public struct Octave
    {
        public Vector2 offset;
        public Vector2 scale;
        public float height;
    }

    public float[,] HeightMap { get => heightMap; }
    #endregion

    #region Privates
    //Mesh
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    [HideInInspector]
    public Mesh mesh;

    private Vector3[] vertices;
    private float[,] heightMap;

    private float spaceBetweenVertices = 1f;
    private int verticesPerSide = 2;

    private bool updateMesh = true;
    #endregion

    #region Compute Shader
    private ComputeShader terrainComputeShader;
    private ComputeShader erosionComputeShader;
    private int terrainKernel;
    private int erosionKernel;

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer octaveBuffer;
    private ComputeBuffer brushIndexBuffer;
    private ComputeBuffer brushWeightBuffer;
    private ComputeBuffer randomIndexBuffer;
    #endregion

    #region Multi Threading
    //Thread waveUpdateThread = null;

    #endregion

    void Setup()
    {
        spaceBetweenVertices = dimension / (float)(resolution);
        verticesPerSide = resolution;

        #region Mesh Setup
        if (mesh == null || meshFilter == null)
        {
            mesh = new Mesh();
            mesh.name = gameObject.name;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.vertices = GenerateVerts();
            if (mesh.vertexCount <= 0)
                return;

            mesh.triangles = GenerateTries();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (meshFilter == null)
                meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();

            meshFilter.mesh = mesh;

            if (meshRenderer == null)
                meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        mesh.uv = GenerateUVs();

        #endregion

        #region Compute shader Setup     
        #region Terrain Compute
        terrainComputeShader = Resources.Load<ComputeShader>("Compute/TerrainCompute");
        terrainKernel = terrainComputeShader.FindKernel("CSTerrain");

        terrainComputeShader.SetFloat("dimension", dimension);
        terrainComputeShader.SetInt("resolution", resolution);

        if (vertexBuffer == null)
        {
            vertexBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3);
        }
        else if (vertexBuffer.count != mesh.vertexCount)
        {
            vertexBuffer.Dispose();
            vertexBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3);
        }

        if (octaveBuffer == null)
        {
            int hypotheticalStride = System.Runtime.InteropServices.Marshal.SizeOf<Octave>();
            octaveBuffer = new ComputeBuffer(octaves.Length, hypotheticalStride);
        }
        else if (octaveBuffer.count != octaves.Length)
        {
            int hypotheticalStride = System.Runtime.InteropServices.Marshal.SizeOf<Octave>();
            octaveBuffer.Dispose();
            octaveBuffer = new ComputeBuffer(octaves.Length, hypotheticalStride);
        }

        terrainComputeShader.SetBuffer(terrainKernel, "vertices", vertexBuffer);
        terrainComputeShader.SetBuffer(terrainKernel, "octaves", octaveBuffer);

        vertexBuffer.SetData(mesh.vertices);

        UnityEngine.Random.InitState(seed);
        Octave[] seededOctaves = octaves.Clone() as Octave[];
        for (int i = 0; i < seededOctaves.Length; i++)
        {
            seededOctaves[i].offset += new Vector2(UnityEngine.Random.value * seed * 4868, UnityEngine.Random.value * seed * 4868);
        }

        octaveBuffer.SetData(seededOctaves);
        #endregion

        #region Erosion Compute
        if (erodeInEditor)
        {
            erosionComputeShader = Resources.Load<ComputeShader>("Compute/ErosionCompute");
            erosionKernel = erosionComputeShader.FindKernel("CSErosion");

            List<int> brushIndexOffsets = new List<int>();
            List<float> brushWeights = new List<float>();

            float weightSum = 0;
            for (int brushY = -erosionBrushRadius; brushY <= erosionBrushRadius; brushY++)
            {
                for (int brushX = -erosionBrushRadius; brushX <= erosionBrushRadius; brushX++)
                {
                    float sqrDst = brushX * brushX + brushY * brushY;
                    if (sqrDst < erosionBrushRadius * erosionBrushRadius)
                    {
                        brushIndexOffsets.Add(brushY * resolution + brushX);
                        float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionBrushRadius;
                        weightSum += brushWeight;
                        brushWeights.Add(brushWeight);
                    }
                }
            }
            for (int i = 0; i < brushWeights.Count; i++)
            {
                brushWeights[i] /= weightSum;
            }

            brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
            brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(float));
            brushIndexBuffer.SetData(brushIndexOffsets);
            brushWeightBuffer.SetData(brushWeights);
            erosionComputeShader.SetBuffer(erosionKernel, "brushIndices", brushIndexBuffer);
            erosionComputeShader.SetBuffer(erosionKernel, "brushWeights", brushWeightBuffer);

            int[] randomIndices = new int[erosionIterationCount];
            for (int i = 0; i < erosionIterationCount; i++)
            {
                int randomX = UnityEngine.Random.Range(erosionBrushRadius, resolution + erosionBrushRadius);
                int randomY = UnityEngine.Random.Range(erosionBrushRadius, resolution + erosionBrushRadius);
                randomIndices[i] = randomY * resolution + randomX;
            }

            randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
            randomIndexBuffer.SetData(randomIndices);
            erosionComputeShader.SetBuffer(erosionKernel, "randomIndices", randomIndexBuffer);

            erosionComputeShader.SetBuffer(erosionKernel, "vertices", vertexBuffer);


            // Erosion Settings
            erosionComputeShader.SetInt("borderSize", erosionBrushRadius);
            erosionComputeShader.SetInt("mapSize", resolution);
            erosionComputeShader.SetInt("brushLength", brushIndexOffsets.Count);
            erosionComputeShader.SetInt("maxLifetime", maximumDropletLifeTime);
            erosionComputeShader.SetFloat("inertia", inertia);
            erosionComputeShader.SetFloat("sedimentCapacityFactor", sedimentCapacityFactor);
            erosionComputeShader.SetFloat("minSedimentCapacity", minSedimentCapacity);
            erosionComputeShader.SetFloat("depositSpeed", depositSpeed);
            erosionComputeShader.SetFloat("erodeSpeed", erosionSpeed);
            erosionComputeShader.SetFloat("evaporateSpeed", evaporationSpeed);
            erosionComputeShader.SetFloat("gravity", gravity);
            erosionComputeShader.SetFloat("startSpeed", startSpeed);
            erosionComputeShader.SetFloat("startWater", startWater);
        }
        #endregion

        #endregion
    }

    #region In Editor
    private void OnValidate()
    {
        erosionIterationCount = Mathf.RoundToInt(erosionIterationCount / 1024f) * 1024;

        if (erosionIterationCount < 1024)
            erosionIterationCount = 1024;

        resolution = Mathf.RoundToInt(resolution / 32f) * 32;

        if (resolution < 32)
            resolution = 32;

        if (updateInEditor)
            updateMesh = true;
    }

    private void OnDrawGizmos()
    {
        if (!updateInEditor)
            return;

        if (updateMesh)
        {
            UpdateMesh();
            updateMesh = false;
        }
    }
    #endregion

    #region Mesh Generation
    private Vector3[] GenerateVerts()
    {
        Vector3[] verts = new Vector3[verticesPerSide * verticesPerSide];

        //equaly distributed verts
        for (int x = 0; x < verticesPerSide; x++)
            for (int z = 0; z < verticesPerSide; z++)
            {
                verts[Index(x, z)] = new Vector3(x * spaceBetweenVertices, 0, z * spaceBetweenVertices);
            }

        return verts;
    }

    private int[] GenerateTries()
    {
        int[] tries = new int[(verticesPerSide - 1) * (verticesPerSide - 1) * 6];

        //two triangles are one tile
        for (int x = 0; x < verticesPerSide - 1; x++)
        {
            for (int z = 0; z < verticesPerSide - 1; z++)
            {
                int index = (x * (verticesPerSide - 1) + z) * 6;
                int vertIndex = Index(x, z);

                tries[index + 0] = vertIndex;
                tries[index + 1] = vertIndex + verticesPerSide + 1;
                tries[index + 2] = vertIndex + verticesPerSide;

                tries[index + 3] = vertIndex;
                tries[index + 4] = vertIndex + 1;
                tries[index + 5] = vertIndex + verticesPerSide + 1;
            }
        }

        return tries;
    }

    private Vector2[] GenerateUVs()
    {
        Vector2[] uvs = new Vector2[verticesPerSide * verticesPerSide];

        //always set one uv over n tiles than flip the uv and set it again
        for (int x = 0; x < verticesPerSide; x++)
        {
            for (int z = 0; z < verticesPerSide; z++)
            {
                Vector2 vec = new Vector2((x * spaceBetweenVertices / uvScale) % 2, (z * spaceBetweenVertices / uvScale) % 2);
                uvs[Index(x, z)] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y);
            }
        }

        return uvs;
    }

    private int Index(int x, int z)
    {
        int ret = x * verticesPerSide + z;
        return ret;
    }
    #endregion

    #region Mesh Update
    public void UpdateMesh()
    {
        Setup();
        UpdateMeshHeight();

        if (erodeInEditor)
            Erode();

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        UpdateHeightMap();

        ReleaseBuffers();
    }

    void UpdateHeightMap()
    {
        Debug.Log("Updating HeightMap...");

        int vertexCount = vertexBuffer.count;
        vertices = new Vector3[vertexCount];
        vertexBuffer.GetData(vertices);

        heightMap = new float[resolution, resolution];
        for (int x = 0; x < resolution; x++)
            for (int y = 0; y < resolution; y++)
                heightMap[x, y] = vertices[x*resolution + y].y;

        Debug.Log("HeightMap Done!");
    }

    void Erode()
    {
        Debug.Log("Eroding...");

        int erosionDispatchGroupSize = erosionIterationCount / 1024;
        erosionComputeShader.Dispatch(erosionKernel, erosionDispatchGroupSize, 1, 1);

        vertices = new Vector3[vertexBuffer.count];
        vertexBuffer.GetData(vertices);
        mesh.vertices = vertices;

        Debug.Log("Erosion Done!");
    }

    void UpdateMeshHeight()
    {
        Debug.Log("Calculating MeshHeight...");

        int vertexDispatchGroupSize = resolution / 32;
        terrainComputeShader.Dispatch(terrainKernel, vertexDispatchGroupSize, vertexDispatchGroupSize, 1);

        vertices = new Vector3[vertexBuffer.count];
        vertexBuffer.GetData(vertices);
        mesh.vertices = vertices;


        Debug.Log("MeshHeight Done!");
    }

    void ReleaseBuffers()
    {
        vertexBuffer.Release();
        vertexBuffer = null;
        octaveBuffer.Release();
        octaveBuffer = null;

        brushIndexBuffer?.Release();
        brushIndexBuffer = null;
        brushWeightBuffer?.Release();
        brushWeightBuffer = null;
        randomIndexBuffer?.Release();
        randomIndexBuffer = null;
    }
    #endregion

    #region Runtime
    void Start()
    {
        erodeInEditor = true;
        UpdateMesh();
    }

    private void Update()
    {
        /// Uncomment for editing mesh in play mode (HIGHLY DISCOURAGED!!!)
        //Setup();
        //UpdateMesh();
    }

    private void OnDestroy()
    {
    }


    #endregion
}
