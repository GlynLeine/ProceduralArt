using UnityEngine;
using UnityEditor;

public class MeshData
{
    public Mesh mesh;
    public Material[] materials;

    public MeshData(Mesh mesh, Material[] materials) { this.mesh = mesh; this.materials = materials; }
}

public enum MeshType { wall, roof, facade }

[CreateAssetMenu(fileName = "New Building Theme", menuName = "City Generation/Create Building Theme Object")]
public class BuildingTheme : ScriptableObject
{
    public SectionData[] walls;
    public SectionData[] roofs;
    public SectionData[] roofFacades;

    public MeshData GetRandomMesh(MeshType meshType, SectionType sectionType)
    {
        SectionData[] sections;
        switch (meshType)
        {
            case MeshType.wall:
                sections = walls;
                break;
            case MeshType.roof:
                sections = roofs;
                break;
            case MeshType.facade:
                sections = roofFacades;
                break;
            default:
                return null;
        }

        int index = Random.Range(0, sections.Length);
        Mesh mesh = sections[index].meshes[(int)sectionType];
        Material[] materials = sections[index].materialArrays[(int)sectionType].materials;

        if(mesh == null || materials == null)
            Debug.Log("oh no..");
        return new MeshData(mesh, materials);
    }
}