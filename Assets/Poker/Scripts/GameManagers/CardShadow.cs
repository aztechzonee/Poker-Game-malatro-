using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class CardShadow : MonoBehaviour
{
    private Mesh mesh;
    [SerializeField] private MeshFilter filter;

    public void CreateMesh(Vector3[] wordPositions, Transform parent)
    {
        GetMeshData(wordPositions, parent,
            out Vector3[] vertices, out Vector2[] uvs, out Vector4[] tangents, out int[] triangles, out Vector3[] normals);
        mesh = new Mesh
        {
            vertices = vertices,
            uv = uvs,
            tangents = tangents,
            triangles = triangles,
            normals = normals
        };
        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void UpdateMeshes(Vector3[] wordPositions, Transform parent, Transform light, Transform plane)
    {
        GetMeshVertices(wordPositions, parent, out Vector3[] vertices);
        mesh.SetVertices(vertices);

    }

    private void GetMeshVertices(Vector3[] wordPositions, Transform parent, out Vector3[] vertices)
    {
        vertices = new Vector3[wordPositions.Length];
        if (Vector3.Dot(transform.up, Vector3.up) > 0f)
        {
            for (int i = 0; i < wordPositions.Length; i++)
            {
                vertices[i] = wordPositions[i].ToLocalPosition(parent);
            }
        }
        else
        {
            for (int i = wordPositions.Length - 1; i >= 0; i--)
            {
                vertices[i] = wordPositions[i].ToLocalPosition(parent);
            }
        }
    }

    private void GetMeshData(Vector3[] wordPositions, Transform parent,
        out Vector3[] vertices, out Vector2[] uvs, out Vector4[] tangents, out int[] triangles, out Vector3[] normals)
    {
        GetMeshVertices(wordPositions, parent, out vertices);

        uvs = new Vector2[]
        {
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 1.0f),
        };

        tangents = new Vector4[]
        {
            new Vector4(1.0f, 0.0f, 0.0f, -1.0f),
            new Vector4(1.0f, 0.0f, 0.0f, -1.0f),
            new Vector4(1.0f, 0.0f, 0.0f, -1.0f),
            new Vector4(1.0f, 0.0f, 0.0f, -1.0f)
        };

        triangles = new int[]
        {
            0,3,1,3,0,2
        };

        Vector3 normal = Vector3.up;

        normals = new Vector3[]
        {
            normal,
            normal,
            normal,
            normal
        };
    }
}