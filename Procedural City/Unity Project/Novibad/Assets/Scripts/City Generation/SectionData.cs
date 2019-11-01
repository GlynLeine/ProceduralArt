using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

[Serializable]
public enum SectionType
{
    straight, inWardCorner, outWardCorner, dot, centeredStraight, centeredCorner, centeredDot
}

[CreateAssetMenu(fileName = "New Section", menuName = "City Generation/Create Section Object")]
public class SectionData : ScriptableObject
{
    public GameObject[] prefabs;

    public MeshData[] meshDataArray;

    public void FetchData()
    {
        SectionType[] sectionTypes = Enum.GetValues(typeof(SectionType)).Cast<SectionType>() as SectionType[];

        if (meshDataArray == null || meshDataArray.Length < sectionTypes.Length)
            meshDataArray = new MeshData[sectionTypes.Length];

        foreach (SectionType type in sectionTypes)
        {
            int index = (int)type;
            if (prefabs[index] == null)
                continue;

            GameObject tempObject = Instantiate(prefabs[index], Vector3.zero, Quaternion.identity);
            MeshFilter meshFilter = tempObject.GetComponentInChildren<MeshFilter>();

            meshDataArray[index] = new MeshData();

            Mesh sourceMesh = meshFilter.sharedMesh;

            Mesh mesh = new Mesh();
            mesh.name = tempObject.name.Substring(0, tempObject.name.Length - 7) + " " + type.ToString() + " mesh";

            mesh.vertices = sourceMesh.vertices;

            mesh.subMeshCount = sourceMesh.subMeshCount;

            mesh.uv = sourceMesh.uv;
            mesh.uv2 = sourceMesh.uv2;

            for (int i = 0; i < sourceMesh.subMeshCount; i++)
                mesh.SetTriangles(sourceMesh.GetTriangles(i), i);

            Matrix4x4 matrix = meshFilter.transform.localToWorldMatrix;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < mesh.vertexCount; i++)
                vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);

            mesh.vertices = vertices;
            meshDataArray[index] = new MeshData(mesh, tempObject.GetComponentInChildren<MeshRenderer>().sharedMaterials);

            DestroyImmediate(tempObject);
        }
    }
}
