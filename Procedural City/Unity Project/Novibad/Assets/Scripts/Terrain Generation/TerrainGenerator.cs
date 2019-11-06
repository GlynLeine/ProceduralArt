using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;

public class TerrainGenerator : MonoBehaviour
{
    [HideInInspector]
    public RenderTexture heightMap;
    [HideInInspector]
    public Texture2D readTex;

    public int resolution = 1;
    public float dimensions = 100;
    public float uvScale = 100;
    public float meshHeight = 10;

    [Header("Noise Settings")]
    [Range(1, 20)]
    public int layers;
    [Range(0, 1)]
    public float persistence = 0.5f;
    public float baseRoughness = 1;
    public float roughness = 2;
    public int seed = 0;

    [Header("Erosion Settings")]
    public int erosionIterationCount = 50000;
    public int erosionRadius = 3;

    public int maximumDropletLifeTime = 30;
    public float sedimentCapacityFactor = 3;
    [Range(0, 0.1f)]
    public float minimumSedimentCapacity = .01f;
    [Range(0.000001f, 1f)]
    public float depositSpeed = 0.3f;
    [Range(0.000001f, 1)]
    public float erosionSpeed = 0.3f;
    [Range(0, 1)]
    public float evaporationSpeed = .01f;
    [Range(1, 10)]
    public float gravity = 4;
    public float initialSpeed = 1;
    public float initialWaterVolume = 1;
    [Range(0, 1)]
    public float inertia = 0.3f;
    public float waterHeight = 0.365f;
    [Range(0, 1)]
    public float waterDampening = 0.9f;

    [Header("Denoise Settings")]
    public float threshold = 1f;
    [Range(0, 1)]
    public float weight = 0.1f;
    public int denoiseItterations = 1;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private int verticesPerSide;
    private float spaceBetweenVertices;

    private ComputeShader terrainComputeShader;
    private ComputeShader erosionComputeShader;
    private ComputeShader denoiseComputeShader;
    private ComputeBuffer brushIndexBuffer;
    private ComputeBuffer brushWeightBuffer;
    private int terrainKernel;
    private int erosionKernel;
    private int denoiseKernel;

    [HideInInspector]
    public bool eroding = false;
    [HideInInspector]
    public bool autoErode;
    [HideInInspector]
    public bool autoGenMesh;
    [HideInInspector]
    public bool cancelErosion;

    public void OnValidate()
    {
        erosionIterationCount = Mathf.RoundToInt(erosionIterationCount / 1024f) * 1024;

        if (erosionIterationCount < 1024)
            erosionIterationCount = 1024;

        resolution = Mathf.RoundToInt(resolution / 32f) * 32;

        if (resolution < 32)
            resolution = 32;

        if (terrainComputeShader == null)
        {
            terrainComputeShader = Resources.Load<ComputeShader>("Compute/TerrainCompute");
            terrainKernel = terrainComputeShader.FindKernel("CSTerrain");
        }

        if (erosionComputeShader == null)
        {
            erosionComputeShader = Resources.Load<ComputeShader>("Compute/ErosionCompute");
            erosionKernel = erosionComputeShader.FindKernel("CSErosion");
        }

        if (denoiseComputeShader == null)
        {
            denoiseComputeShader = Resources.Load<ComputeShader>("Compute/DenoiseCompute");
            denoiseKernel = denoiseComputeShader.FindKernel("CSDenoise");
        }
    }

    public void SaveMesh()
    {
        Mesh mesh;
        if (meshFilter.sharedMesh != null)
            mesh = meshFilter.sharedMesh;
        else if (meshFilter.mesh != null)
            mesh = meshFilter.mesh;
        else
            return;

        MeshExporter.BindOnFinished(ReCalculateUVs);
        MeshExporter.SaveMesh(this, mesh, mesh.name);
    }

    public void ReCalculateUVs()
    {
        if (meshFilter == null)
            meshFilter = gameObject.GetComponent<MeshFilter>();

        meshFilter.sharedMesh.uv = GenerateUVs();
        EditorUtility.SetDirty(meshFilter.sharedMesh);
    }

    public void ReCalculateNormals()
    {
        if (meshFilter == null)
            meshFilter = gameObject.GetComponent<MeshFilter>();

        meshFilter.sharedMesh.RecalculateNormals();
        meshFilter.sharedMesh.RecalculateBounds();
        meshFilter.sharedMesh.RecalculateTangents();

        EditorUtility.SetDirty(meshFilter.sharedMesh);
    }

    #region Mesh Generation
    public void GenerateMesh()
    {
        verticesPerSide = resolution;
        spaceBetweenVertices = dimensions / verticesPerSide;

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = gameObject.name;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.vertices = GenerateVerts();

        if (mesh.vertexCount <= 0)
            return;

        mesh.uv = GenerateUVs();

        mesh.triangles = GenerateTries();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

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

    private Vector3[] GenerateVerts()
    {
        Vector3[] verts = new Vector3[verticesPerSide * verticesPerSide];

        if (readTex == null || readTex.width != resolution)
            readTex = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false, true);

        RenderTexture.active = heightMap;
        readTex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0, false);
        readTex.Apply(false);

        //equaly distributed verts
        for (int x = 0; x < verticesPerSide; x++)
            for (int z = 0; z < verticesPerSide; z++)
            {
                verts[Index(x, z)] = new Vector3(x * spaceBetweenVertices, readTex.GetPixelBilinear((float)x / (float)verticesPerSide, (float)z / (float)verticesPerSide).r * meshHeight, z * spaceBetweenVertices);
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

                tries[index + 0] = (int)vertIndex;
                tries[index + 1] = (int)(vertIndex + verticesPerSide + 1);
                tries[index + 2] = (int)(vertIndex + verticesPerSide);

                tries[index + 3] = (int)vertIndex;
                tries[index + 4] = (int)(vertIndex + 1);
                tries[index + 5] = (int)(vertIndex + verticesPerSide + 1);
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
                Vector2 vec = new Vector2((x * spaceBetweenVertices / uvScale) % (2 - spaceBetweenVertices / uvScale), (z * spaceBetweenVertices / uvScale) % (2 - spaceBetweenVertices / uvScale));
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

    public void GenerateHeightMap()
    {
        if (heightMap != null)
            heightMap.Release();
        heightMap = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        heightMap.useDynamicScale = false;
        heightMap.useMipMap = false;
        heightMap.enableRandomWrite = true;
        heightMap.Create();

        ApplyNoiseHeight();
    }

    public void ApplyNoiseHeight()
    {
        terrainComputeShader.SetInt("resolution", resolution);
        terrainComputeShader.SetInt("layers", layers);
        terrainComputeShader.SetFloat("persistence", persistence);
        terrainComputeShader.SetFloat("baseRoughness", baseRoughness);
        terrainComputeShader.SetFloat("roughness", roughness);

        ComputeBuffer randomOffsetsBuffer = new ComputeBuffer(layers, sizeof(float) * 2);

        terrainComputeShader.SetTexture(terrainKernel, "heightMap", heightMap);
        terrainComputeShader.SetBuffer(terrainKernel, "randomOffsets", randomOffsetsBuffer);

        Random.InitState(seed);
        Vector2[] randomOffsets = new Vector2[layers];
        for (int i = 0; i < randomOffsets.Length; i++)
        {
            randomOffsets[i] = new Vector2(Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));
        }

        randomOffsetsBuffer.SetData(randomOffsets);

        int dispatchGroupSize = resolution / 32;
        terrainComputeShader.Dispatch(terrainKernel, dispatchGroupSize, dispatchGroupSize, 1);

        randomOffsetsBuffer.Dispose();
    }

    public void ApplyErosion()
    {
        List<Vector2Int> brushIndexOffsets = new List<Vector2Int>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -erosionRadius; brushY <= erosionRadius; brushY++)
            for (int brushX = -erosionRadius; brushX <= erosionRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < erosionRadius * erosionRadius)
                {
                    brushIndexOffsets.Add(new Vector2Int(brushY, brushX));

                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionRadius;
                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }

        for (int i = 0; i < brushWeights.Count; i++)
            brushWeights[i] /= weightSum;

        brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int) * 2);
        brushIndexBuffer.SetData(brushIndexOffsets);
        erosionComputeShader.SetBuffer(erosionKernel, "brushIndices", brushIndexBuffer);

        brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(float));
        brushWeightBuffer.SetData(brushWeights);
        erosionComputeShader.SetBuffer(erosionKernel, "brushWeights", brushWeightBuffer);

        erosionComputeShader.SetTexture(erosionKernel, "heightMap", heightMap);

        // Erosion Settings
        erosionComputeShader.SetInt("borderSize", erosionRadius);
        erosionComputeShader.SetInt("resolution", resolution);
        erosionComputeShader.SetInt("brushLength", brushIndexOffsets.Count);
        erosionComputeShader.SetInt("maxLifetime", maximumDropletLifeTime);
        erosionComputeShader.SetFloat("inertia", inertia);
        erosionComputeShader.SetFloat("sedimentCapacityFactor", sedimentCapacityFactor);
        erosionComputeShader.SetFloat("minSedimentCapacity", minimumSedimentCapacity);
        erosionComputeShader.SetFloat("depositSpeed", depositSpeed);
        erosionComputeShader.SetFloat("erodeSpeed", erosionSpeed);
        erosionComputeShader.SetFloat("evaporateSpeed", evaporationSpeed);
        erosionComputeShader.SetFloat("gravity", gravity);
        erosionComputeShader.SetFloat("startSpeed", initialSpeed);
        erosionComputeShader.SetFloat("startWater", initialWaterVolume);
        erosionComputeShader.SetFloat("terrainHeight", 1f);
        erosionComputeShader.SetFloat("waterHeight", waterHeight);
        erosionComputeShader.SetFloat("waterDampening", waterDampening);

        Debug.Log("Starting erosion...");
        StartCoroutine(Erode());
    }

    private IEnumerator Erode()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        eroding = true;

        int maximumDispatchCount = erosionIterationCount / 1024;
        int dispatchGroupSize = 1;

        int dispatchCount = 0;

        while (dispatchCount < maximumDispatchCount)
        {
            dispatchCount += dispatchGroupSize;

            Vector2Int[] randomIndices = new Vector2Int[dispatchGroupSize * 1024];
            for (int j = 0; j < dispatchGroupSize * 1024; j++)
            {
                int randomX = Random.Range(0, resolution - 1);
                int randomY = Random.Range(0, resolution - 1);
                randomIndices[j] = new Vector2Int(randomY, randomX);
            }

            ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int) * 2);
            randomIndexBuffer.SetData(randomIndices);
            erosionComputeShader.SetBuffer(erosionKernel, "randomIndices", randomIndexBuffer);

            erosionComputeShader.Dispatch(erosionKernel, dispatchGroupSize, 1, 1);
            stopwatch.Stop();
            long time = stopwatch.ElapsedMilliseconds;

            stopwatch.Reset();
            stopwatch.Start();
            if (time < 150)
                dispatchGroupSize++;
            else
                dispatchGroupSize--;

            if (dispatchCount + dispatchGroupSize > maximumDispatchCount)
                dispatchGroupSize = maximumDispatchCount - dispatchCount;

            randomIndexBuffer.Dispose();

            float progress = (float)dispatchCount / maximumDispatchCount * 100f;
            if (EditorUtility.DisplayCancelableProgressBar("Eroding Terrain", progress + "% done", progress / 100f))
                cancelErosion = true;

            if (cancelErosion)
                break;

            yield return null;
        }

        brushIndexBuffer.Dispose();
        brushWeightBuffer.Dispose();

        eroding = false;

        EditorUtility.ClearProgressBar();

        if (cancelErosion)
            Debug.Log("Erosion cancelled.");
        else
            Debug.Log("Erosion done!");

        cancelErosion = false;

        ApplyDenoise();

        if (autoGenMesh)
            GenerateMesh();
    }

    public void ApplyDenoise()
    {
        denoiseComputeShader.SetTexture(denoiseKernel, "heightMap", heightMap);
        denoiseComputeShader.SetFloat("threshold", threshold);
        denoiseComputeShader.SetInt("resolution", resolution);
        denoiseComputeShader.SetFloat("weight", weight);

        int dispatchGroupSize = resolution / 32;
        for (int i = 0; i < denoiseItterations; i++)
            denoiseComputeShader.Dispatch(denoiseKernel, dispatchGroupSize, dispatchGroupSize, 1);
    }
}
