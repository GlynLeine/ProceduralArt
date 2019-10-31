using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

static class MeshExporter
{
    static private MonoBehaviour meshOwner;
    static private bool applySharedMesh = true;
    static private string meshName;
    static private string filePath;
    static private string extension;

    static private Vector3[] vertices;
    static private Vector2[] uv;
    static private Vector3[] normals;
    static private int[] triangles;

    static private List<string> fileContents;

    static private float progress;
    static private bool cancelSave = false;

    private enum Task { vertex, uv, normal, triangle, write, done }
    static private Task currentTask;

    static public void SaveMesh(MonoBehaviour owner, Mesh mesh, string fileName, bool reapplySharedMesh = true, string folder = "Assets/Resources/Meshes/Exporter/", bool overwrite = true)
    {
        meshOwner = owner;
        applySharedMesh = reapplySharedMesh;
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        if (overwrite)
        {
            filePath = folder + fileName;
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        else
        {
            int index = 1;
            while (File.Exists(fileName + index))
            {
                index++;
            }
            filePath = folder + fileName + index;
        }

        vertices = mesh.vertices;
        uv = mesh.uv;
        normals = mesh.normals;
        triangles = mesh.triangles;

        meshName = mesh.name;

        Debug.Log("Saving mesh " + meshName);

        cancelSave = false;
        currentTask = Task.vertex;
        owner.StartCoroutine(TrackProgress());
        new Thread(new ThreadStart(SaveMeshToObj)).Start();
    }

    static private IEnumerator TrackProgress()
    {
        while (currentTask != Task.done)
        {
            string progressInfo = "";

            switch (currentTask)
            {
                case Task.vertex:
                    progressInfo = "Saving vertices " + progress + "% done";
                    break;
                case Task.uv:
                    progressInfo = "Saving uvs " + progress + "% done";
                    break;
                case Task.normal:
                    progressInfo = "Saving normals " + progress + "% done";
                    break;
                case Task.triangle:
                    progressInfo = "Saving triangles " + progress + "% done";
                    break;
                case Task.write:
                    progressInfo = "Writing File " + progress + "% done";
                    break;
            }

            if (currentTask != Task.write)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Saving Mesh " + meshName, progressInfo, progress / 100f))
                {
                    cancelSave = true;
                }
            }
            else
                EditorUtility.DisplayProgressBar("Saving Mesh ", progressInfo, progress / 100f);

            yield return null;
        }
        yield return new WaitForSeconds(1f);

        AssetDatabase.ImportAsset(filePath + extension, ImportAssetOptions.ForceUpdate);

        if (applySharedMesh)
            meshOwner.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(filePath + extension);

        EditorUtility.ClearProgressBar();
        Debug.Log("Saved mesh to " + filePath);
    }

    /// <summary>
	/// Saves the mesh as an OBJ file.
	/// </summary>
	/// <param name="mesh">Mesh.</param>
	/// <param name="filename">Filename. Automatically sets the extension.</param>
	static private void SaveMeshToObj()
    {
        extension = ".obj";
        fileContents = new List<string>();

        //object name
        fileContents.Add("#" + meshName);
        fileContents.Add("g " + meshName);

        //vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            float x = vertices[i].x;
            float y = vertices[i].y;
            float z = vertices[i].z;
            fileContents.Add(String.Format("v {0:F3} {1:F3} {2:F3}", -x, y, z));

            progress = (float)i / vertices.Length * 100f;
            currentTask = Task.vertex;
            if (cancelSave)
            {
                currentTask = Task.done;
                Debug.Log("Cancelled saving mesh " + meshName);
                return;
            }
        }
        fileContents.Add("");

        //uv-set
        for (int i = 0; i < uv.Length; i++)
        {
            float u = uv[i].x;
            float v = uv[i].y;
            fileContents.Add(String.Format("vt {0:F3} {1:F3}", u, v));

            progress = (float)i / uv.Length * 100f;
            currentTask = Task.uv;
        }
        fileContents.Add("");

        //normals
        for (int i = 0; i < normals.Length; i++)
        {
            float x = normals[i].x;
            float y = normals[i].y;
            float z = normals[i].z;
            fileContents.Add(String.Format("vn {0:F3} {1:F3} {2:F3}", x, y, z));

            progress = (float)i / normals.Length * 100f;
            currentTask = Task.normal;
            if (cancelSave)
            {
                currentTask = Task.done;
                Debug.Log("Cancelled saving mesh " + meshName);
                return;
            }
        }
        fileContents.Add("");

        //triangles
        for (int i = 0; i < triangles.Length / 3; i++)
        {
            int v0 = triangles[i * 3 + 0] + 1;
            int v1 = triangles[i * 3 + 1] + 1;
            int v2 = triangles[i * 3 + 2] + 1;
            fileContents.Add(String.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", v2, v1, v0));

            progress = (float)i / (triangles.Length / 3) * 100f;
            currentTask = Task.triangle;
            if (cancelSave)
            {
                currentTask = Task.done;
                Debug.Log("Cancelled saving mesh " + meshName);
                return;
            }
        }

        StreamWriter writer = new StreamWriter(filePath + extension);

        for (int i = 0; i < fileContents.Count; i++)
        {
            writer.WriteLine(fileContents[i]);
            progress = (float)i / fileContents.Count * 100f;
            currentTask = Task.write;
        }
        writer.Close();

        currentTask = Task.done;
    }
}

