using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator2 : MonoBehaviour
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
    [Range(1, 8)]
    public int layers;
    [Range(0, 1)]
    public float persistence = 0.5f;
    public float baseRoughness = 1;
    public float roughness = 2;
    public int seed = 0;

    [Header("Erosion Settings")]
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

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private int verticesPerSide;
    private float spaceBetweenVertices;

    private ComputeShader terrainComputeShader;
    private ComputeShader erosionComputeShader;
    private int terrainKernel;
    private int erosionKernel;

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
            terrainComputeShader = Resources.Load<ComputeShader>("Compute/Terrain2Compute");
            terrainKernel = terrainComputeShader.FindKernel("CSTerrain");
        }

        if (erosionComputeShader == null)
        {
            erosionComputeShader = Resources.Load<ComputeShader>("Compute/Erosion2Compute");
            erosionKernel = erosionComputeShader.FindKernel("CSErosion");
        }
    }

    #region Mesh Generation
    public void GenerateMesh()
    {
        verticesPerSide = resolution-1;
        spaceBetweenVertices = dimensions / verticesPerSide;

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

        mesh.uv = GenerateUVs();
    }

    private Vector3[] GenerateVerts()
    {
        Vector3[] verts = new Vector3[verticesPerSide * verticesPerSide];

        if (readTex == null)
            readTex = new Texture2D(verticesPerSide, verticesPerSide);

        RenderTexture.active = heightMap;
        readTex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        readTex.Apply();

        //equaly distributed verts
        for (int x = 0; x < verticesPerSide; x++)
            for (int z = 0; z < verticesPerSide; z++)
            {
                verts[Index(x, z)] = new Vector3(x * spaceBetweenVertices, readTex.GetPixel(x, z).r * meshHeight, z * spaceBetweenVertices);
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

    public void GenerateHeightMap()
    {
        if (heightMap != null)
            heightMap.Release();
        heightMap = new RenderTexture(resolution, resolution, 24);
        heightMap.enableRandomWrite = true;
        heightMap.Create();

        ApplyNoiseHeight();
    }

    public void ApplyNoiseHeight()
    {
        //Debug.Log("Calculating NoiseHeight...");

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

        int vertexDispatchGroupSize = resolution / 32;
        terrainComputeShader.Dispatch(terrainKernel, vertexDispatchGroupSize, vertexDispatchGroupSize, 1);

        randomOffsetsBuffer.Dispose();

        //Debug.Log("NoiseHeight Done!");
    }

    public void ApplyErosion()
    {
        // Debug.Log("Eroding...");

        List<Vector2Int> brushIndexOffsets = new List<Vector2Int>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -erosionBrushRadius; brushY <= erosionBrushRadius; brushY++)
            for (int brushX = -erosionBrushRadius; brushX <= erosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < erosionBrushRadius * erosionBrushRadius)
                {
                    brushIndexOffsets.Add(new Vector2Int(brushY, brushX));

                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionBrushRadius;
                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }

        for (int i = 0; i < brushWeights.Count; i++)
            brushWeights[i] /= weightSum;

        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int) * 2);
        brushIndexBuffer.SetData(brushIndexOffsets);
        erosionComputeShader.SetBuffer(erosionKernel, "brushIndices", brushIndexBuffer);

        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(float));
        brushWeightBuffer.SetData(brushWeights);
        erosionComputeShader.SetBuffer(erosionKernel, "brushWeights", brushWeightBuffer);


        Vector2Int[] randomIndices = new Vector2Int[erosionIterationCount];
        for (int i = 0; i < erosionIterationCount; i++)
        {
            int randomX = Random.Range(erosionBrushRadius, resolution - erosionBrushRadius);
            int randomY = Random.Range(erosionBrushRadius, resolution - erosionBrushRadius);
            randomIndices[i] = new Vector2Int(randomY, randomX);
        }

        ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int) * 2);
        randomIndexBuffer.SetData(randomIndices);
        erosionComputeShader.SetBuffer(erosionKernel, "randomIndices", randomIndexBuffer);

        erosionComputeShader.SetTexture(erosionKernel, "heightMap", heightMap);

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

        int erosionDispatchGroupSize = erosionIterationCount / 1024;
        erosionComputeShader.Dispatch(erosionKernel, erosionDispatchGroupSize, 1, 1);

        brushIndexBuffer.Dispose();
        brushWeightBuffer.Dispose();
        randomIndexBuffer.Dispose();

        // CPUErosion(0, randomIndices, brushWeights.ToArray(), brushIndexOffsets.ToArray());

        // Debug.Log("Erosion Done!");
    }


    public void CPUErosion(int id, Vector2Int[] randomIndices, float[] brushWeights, Vector2Int[] brushIndices)
    {
        readTex = new Texture2D(resolution, resolution);
        RenderTexture.active = heightMap;
        readTex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        readTex.Apply();
        //RenderTexture.active = null;

        Vector2Int index = randomIndices[id];
        //heightMap[index] += float4(0, 1, 0, 0);
        float posX = index.x + 0.5f;
        float posY = index.y + 0.5f;
        float dirX = 0;
        float dirY = 0;
        float speed = startSpeed;
        float water = startWater;
        float sediment = 0;

        for (int lifetime = 0; lifetime < maximumDropletLifeTime; lifetime++)
        {
            int nodeX = (int)posX;
            int nodeY = (int)posY;
            Vector2Int dropletIndex = new Vector2Int(nodeY, nodeX);

            // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
            float cellOffsetX = posX - nodeX;
            float cellOffsetY = posY - nodeY;

            // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
            Vector3 heightAndGradient = CalculateHeightAndGradient(posX, posY, readTex);

            // Update the droplet's direction and position (move position 1 unit regardless of speed)
            dirX = (dirX * inertia - heightAndGradient.x * (1 - inertia));
            dirY = (dirY * inertia - heightAndGradient.y * (1 - inertia));
            // Normalize direction
            float len = Mathf.Max(0.01f, Mathf.Sqrt(dirX * dirX + dirY * dirY));
            dirX /= len;
            dirY /= len;
            posX += dirX;
            posY += dirY;

            // Stop simulating droplet if it's not moving or has flowed over edge of map
            if ((dirX == 0 && dirY == 0) /*|| posX < borderSize || posX > resolution - borderSize || posY < borderSize || posY > resolution - borderSize*/)
            {
                //break;
            }

            // Find the droplet's new height and calculate the deltaHeight
            float newHeight = CalculateHeightAndGradient(posX, posY, readTex).z;
            float deltaHeight = newHeight - heightAndGradient.z;

            // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
            float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * sedimentCapacityFactor, minSedimentCapacity);

            // If carrying more sediment than capacity, or if flowing uphill:
            if (sediment > sedimentCapacity || deltaHeight > 0)
            {
                // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * depositSpeed;
                sediment -= amountToDeposit;

                // Add the sediment to the four nodes of the current cell using bilinear interpolation
                // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                readTex.SetPixel(dropletIndex.x, dropletIndex.y, readTex.GetPixel(dropletIndex.x, dropletIndex.y) + new Color(0, amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY), 0, 0));
                readTex.SetPixel(dropletIndex.x + 1, dropletIndex.y, readTex.GetPixel(dropletIndex.x + 1, dropletIndex.y) + new Color(0, amountToDeposit * cellOffsetX * (1 - cellOffsetY), 0, 0));
                readTex.SetPixel(dropletIndex.x + 1, dropletIndex.y, readTex.GetPixel(dropletIndex.x + 1, dropletIndex.y) + new Color(0, amountToDeposit * (1 - cellOffsetX) * cellOffsetY, 0, 0));
                readTex.SetPixel(dropletIndex.x + 1, dropletIndex.y, readTex.GetPixel(dropletIndex.x + 1, dropletIndex.y) + new Color(0, amountToDeposit * cellOffsetX * cellOffsetY, 0, 0));
            }
            else
            {
                // Erode a fraction of the droplet's current carry capacity.
                // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                float amountToErode = Mathf.Min((sedimentCapacity - sediment) * erosionSpeed, -deltaHeight);

                for (int i = 0; i < brushIndices.Length; i++)
                {
                    Vector2Int erodeIndex = dropletIndex + brushIndices[i];

                    float weightedErodeAmount = amountToErode * brushWeights[i];
                    float deltaSediment = (readTex.GetPixel(erodeIndex.x, erodeIndex.y).r < weightedErodeAmount) ? readTex.GetPixel(erodeIndex.x, erodeIndex.y).r : weightedErodeAmount;
                    readTex.SetPixel(erodeIndex.x, erodeIndex.y, readTex.GetPixel(erodeIndex.x, erodeIndex.y) - new Color(0, 0, deltaSediment, 0));
                    sediment += deltaSediment;
                }
            }

            // Update droplet's speed and water content
            speed = Mathf.Sqrt(Mathf.Max(0, speed * speed + deltaHeight * gravity));
            water *= (1 - evaporationSpeed);
        }
    }

    Vector3 CalculateHeightAndGradient(float posX, float posY, Texture2D readTex)
    {
        int coordX = (int)posX;
        int coordY = (int)posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        // Calculate heights of the four nodes of the droplet's cell
        int nodeIndexNW = coordY * resolution + coordX;
        float heightNW = readTex.GetPixel(coordX, coordY).r;
        float heightNE = readTex.GetPixel(coordX + 1, coordY).r;
        float heightSW = readTex.GetPixel(coordX, coordY + 1).r;
        float heightSE = readTex.GetPixel(coordX + 1, coordY + 1).r;

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        return new Vector3(gradientX, gradientY, height);
    }
}
