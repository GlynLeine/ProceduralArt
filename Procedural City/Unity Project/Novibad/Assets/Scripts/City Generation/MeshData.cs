using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "New Mesh Data", menuName = "City Generation/Create Mesh Data Object")]
public class MeshData : ScriptableObject
{
    public GameObject prefab;
    public Mesh mesh;
    public Material[] materials;

    public void FetchData()
    {
        GameObject tempObject = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        MeshFilter meshFilter = tempObject.GetComponentInChildren<MeshFilter>();

        Mesh sourceMesh = meshFilter.sharedMesh;
        mesh = new Mesh();
        mesh.name = tempObject.name.Substring(0, tempObject.name.Length - 7) + " mesh";
        mesh.vertices = sourceMesh.vertices;
        mesh.subMeshCount = sourceMesh.subMeshCount;
        mesh.uv = sourceMesh.uv;
        mesh.uv2 = sourceMesh.uv2;

        for (int i = 0; i < sourceMesh.subMeshCount; i++)
            mesh.SetTriangles(sourceMesh.GetTriangles(i), i);

        Matrix4x4 matrix = meshFilter.transform.localToWorldMatrix;
        materials = tempObject.GetComponentInChildren<MeshRenderer>().sharedMaterials;

        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < mesh.vertexCount; i++)
            vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);

        mesh.vertices = vertices;

        DestroyImmediate(tempObject);


        if (!AssetDatabase.IsValidFolder("Assets/Resources/Meshes"))
            AssetDatabase.CreateFolder("Assets/Resources", "Meshes");

        AssetDatabase.CreateAsset(mesh, "Assets/Resources/Meshes/" + mesh.name + ".obj");
    }
}
