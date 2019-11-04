using UnityEngine;
using System;

public enum MeshType { wall, roof, facade }

[CreateAssetMenu(fileName = "New Building Theme", menuName = "City Generation/Create Building Theme Object")]
public class BuildingTheme : ScriptableObject
{
    public SectionData[] walls;
    public SectionData[] roofs;
    public SectionData[] roofFacades;

    [ThreadStatic]
    private System.Random random;
    [ThreadStatic]
    private int seed;

    /// <summary>
    /// Sets randomisation seed (thread specific).
    /// </summary>
    public void SetSeed(int seed)
    {
        this.seed = seed;
        random = new System.Random(seed);
    }

    /// <summary>
    /// Retrieves random mesh of certain type and section type (thread safe).
    /// </summary>
    /// <param name="meshType">Requested mesh type, eg. roof or wall.</param>
    /// <param name="sectionType">Requested section type, eg. straight or corner.</param>
    public MeshData GetRandomMesh(MeshType meshType, SectionType sectionType)
    {
        if (random == null)
            random = new System.Random(seed);

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

        int index = random.Next(sections.Length);
        return sections[index].meshDataArray[(int)sectionType];
    }
}