using System;
using System.Linq;
using UnityEngine;
using System.Threading;

public class TerrainGen : MonoBehaviour
{
    //Public Properties
    #region Inputs
    public bool updateInEditor;
    public int dimension = 10;
    public float uvScale = 2f;
    public Octave[] octaves;
    public int resolution = 1;

    public bool vertexCompute;
    #endregion

    #region Readables

    [Serializable]
    public struct Octave
    {
        public Vector2 offset;
        public Vector2 scale;
        public float height;
    }
    #endregion

    #region Privates
    //Mesh
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    private Vector3[] vertices;

    private float spaceBetweenVertices = 1f;
    private int verticesPerSide = 2;

    private bool setupMesh = true;
    #endregion

    #region Compute Shader
    private ComputeShader computeShader;
    private int terrainKernel;

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer octaveBuffer;
    #endregion

    #region Multi Threading
    //Thread waveUpdateThread = null;

    #endregion

    void Setup()
    {
        spaceBetweenVertices = dimension / (float)(resolution - 1);
        verticesPerSide = Mathf.RoundToInt(dimension / spaceBetweenVertices) + 1;

        #region Mesh Setup

        if (mesh == null || meshFilter == null)
        {
            mesh = new Mesh();
            mesh.name = gameObject.name;

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

            if (meshFilter == null)
                meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        mesh.uv = GenerateUVs();

        #endregion

        #region Compute shader Setup
        computeShader = Resources.Load<ComputeShader>("Compute/TerrainCompute");
        terrainKernel = computeShader.FindKernel("CSTerrain");

        computeShader.SetFloat("dimension", dimension);
        computeShader.SetInt("resolution", resolution);

        if (vertexBuffer == null)
            vertexBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3);

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

        computeShader.SetBuffer(terrainKernel, "vertices", vertexBuffer);
        computeShader.SetBuffer(terrainKernel, "octaves", octaveBuffer);

        vertexBuffer.SetData(mesh.vertices);
        octaveBuffer.SetData(octaves);
        #endregion
    }

    #region In Editor
    private void OnValidate()
    {
        if (resolution > 200)
            resolution = 200;
        resolution = Mathf.RoundToInt(resolution / 8f) * 8;

        if (resolution < 8)
            resolution = 8;

        if (updateInEditor)
            setupMesh = true;
    }

    private void OnDrawGizmos()
    {
        if (setupMesh)
        {
            Setup();
            setupMesh = false;
        }

        if (updateInEditor)
            UpdateMesh();
    }
    #endregion

    #region Mesh Generation
    private Vector3[] GenerateVerts()
    {
        Vector3[] verts = new Vector3[verticesPerSide * verticesPerSide];

        //equaly distributed verts
        for (float x = 0; x <= dimension; x += spaceBetweenVertices)
            for (float z = 0; z <= dimension; z += spaceBetweenVertices)
                verts[Index(x, z)] = new Vector3(x, 0, z);

        return verts;
    }

    private int[] GenerateTries()
    {
        int[] tries = new int[mesh.vertices.Length * 6];

        //two triangles are one tile
        for (float x = 0; x < dimension-spaceBetweenVertices; x += spaceBetweenVertices)
        {
            for (float z = 0; z < dimension-spaceBetweenVertices; z += spaceBetweenVertices)
            {
                tries[Index(x, z) * 6 + 0] = Index(x, z);
                tries[Index(x, z) * 6 + 1] = Index(x + spaceBetweenVertices, z + spaceBetweenVertices);
                tries[Index(x, z) * 6 + 2] = Index(x + spaceBetweenVertices, z);
                tries[Index(x, z) * 6 + 3] = Index(x, z);
                tries[Index(x, z) * 6 + 4] = Index(x, z + spaceBetweenVertices);
                tries[Index(x, z) * 6 + 5] = Index(x + spaceBetweenVertices, z + spaceBetweenVertices);
            }
        }

        return tries;
    }

    private Vector2[] GenerateUVs()
    {
        Vector2[] uvs = new Vector2[mesh.vertices.Length];

        //always set one uv over n tiles than flip the uv and set it again
        for (float x = 0; x <= dimension; x += spaceBetweenVertices)
        {
            for (float z = 0; z <= dimension; z += spaceBetweenVertices)
            {
                Vector2 vec = new Vector2((x / uvScale) % 2, (z / uvScale) % 2);
                uvs[Index(x, z)] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y);
            }
        }

        return uvs;
    }

    private int Index(float x, float z)
    {
        int ret = Mathf.RoundToInt((x / spaceBetweenVertices) * (dimension / spaceBetweenVertices + 1) + (z / spaceBetweenVertices));
        return ret;
    }
    #endregion

    #region Mesh Update
    void UpdateMesh()
    {
        if (computeShader == null || vertexBuffer == null)
            Setup();

        computeShader.SetFloat("time", Time.time);
        computeShader.SetInt("resolution", resolution);

        int vertexDispatchGroupSize = resolution / 8;

        if (vertexCompute)
        {
            computeShader.Dispatch(terrainKernel, vertexDispatchGroupSize, vertexDispatchGroupSize, 1);

            vertices = new Vector3[vertexBuffer.count];
            vertexBuffer.GetData(vertices);
            mesh.vertices = vertices;
        }
        else
        {
            vertices = mesh.vertices;
            for (float x = 0; x <= dimension; x += spaceBetweenVertices)
            {
                for (float z = 0; z <= dimension; z += spaceBetweenVertices)
                {
                    float y = 0f;
                    for (int o = 0; o < octaves.Length; o++)
                    {
                        float perl = Mathf.PerlinNoise((x * octaves[o].scale.x + octaves[o].offset.x) / dimension, (z * octaves[o].scale.y + octaves[o].offset.y) / dimension);
                        y += perl * octaves[o].height;
                    }
                    vertices[Index(x, z)] = new Vector3(x, y, z);
                }
            }
            mesh.vertices = vertices;
        }

        mesh.RecalculateNormals();
    }
    #endregion

    #region Runtime
    // Start is called before the first frame update
    void Start()
    {
        Setup();
        UpdateMesh();
    }

    private void Update()
    {
        Setup();
        UpdateMesh();
    }

    private void OnDestroy()
    {
        if (vertexBuffer != null)
            vertexBuffer.Dispose();
        if (octaveBuffer != null)
            octaveBuffer.Dispose();
    }


    #endregion
}
