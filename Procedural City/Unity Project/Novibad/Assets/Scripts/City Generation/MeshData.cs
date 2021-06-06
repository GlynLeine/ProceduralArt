using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[Serializable]
public class MeshData
{
    [Serializable]
    public struct SubMesh
    {
        public int[] triangles;
    }

    public Vector3[] vertices;
    public Vector2[] uv;
    public SubMesh[] subMeshes;
    public Material[] materials;

    public int subMeshCount
    {
        get => subMeshes.Length;

        set
        {
            SubMesh[] newSubmeshes = new SubMesh[value];
            if (subMeshes != null && subMeshes.Length > 0)
                subMeshes.CopyTo(newSubmeshes, 0);
            subMeshes = newSubmeshes;
        }
    }

    public int vertexCount { get => vertices.Length; }

    public void SetTriangles(int[] triangles, int submesh)
    {
        if (submesh >= 0 && submesh < subMeshCount)
            subMeshes[submesh].triangles = triangles;
    }

    public void SetTriangles(List<int> triangles, int submesh)
    {
        if (submesh >= 0 && submesh < subMeshCount)
            subMeshes[submesh].triangles = triangles.ToArray();
    }

    public int[] GetTriangles(int submesh)
    {
        if (submesh >= 0 && submesh < subMeshCount)
            return subMeshes[submesh].triangles;
        return null;
    }

    public Mesh GetMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uv;

        mesh.subMeshCount = subMeshCount;
        for (int i = 0; i < subMeshCount; i++)
            mesh.SetTriangles(GetTriangles(i), i);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        return mesh;
    }

    public MeshData()
    {
        vertices = new Vector3[0];
        uv = new Vector2[0];
        subMeshes = new SubMesh[0];
        materials = new Material[0];
    }

    public MeshData(Mesh mesh, Material[] materials)
    {
        vertices = mesh.vertices;
        uv = mesh.uv;

        subMeshCount = mesh.subMeshCount;
        for (int i = 0; i < mesh.subMeshCount; i++)
            SetTriangles(mesh.GetTriangles(i), i);


        this.materials = materials;
    }

    public override string ToString()
    {
        int triangleCount = 0;
        for (int i = 0; i < subMeshCount; i++)
            triangleCount += subMeshes[i].triangles.Length;

        return "vertexCount: " + vertexCount + " uvCount: " + uv.Length + " subMeshCount: " + subMeshCount + " triangleCount: " + triangleCount + " materialCount: " + materials.Length;
    }
}