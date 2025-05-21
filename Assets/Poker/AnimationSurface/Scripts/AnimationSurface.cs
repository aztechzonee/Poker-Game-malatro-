using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GridWrapMode
{
    Repeat = 0,
    Clamp
}

public class AnimationSurface
{
    public AnimationCurve3[] curves;
    private int verticesCount = 0;
    public float maxTime { get; private set; }

    public AnimationSurface(Vector3[][] values)
    {
        SetValues(values);
    }

    public void SetValues(Vector3[][] values)
    {
        this.maxTime = 0.0f;
        if (curves == null || curves.Length != values.Length)
        {
            curves = new AnimationCurve3[values.Length];
        }
        for (int i = 0; i < values.Length; i++)
        {
            if (curves[i] == null || curves[i].Length != values[i].Length)
            {
                curves[i] = new AnimationCurve3(values[i]);
            }
            else
            {
                curves[i].SetValues(values[i]);
            }

            if (i != 0)
            {
                float deltaTime = Vector3.Distance(values[i - 1][0], values[i][0]);
                this.maxTime += deltaTime;
            }
        }
    }

    public Vector3 Evaluate(float x, float y)
    {
        Vector3[] xNodes = new Vector3[curves.Length];
        for (int i = 0; i < curves.Length; i++)
        {
            xNodes[i] = curves[i].Evaluate(y);
            ccc++;
        }
        ccc++;
        return new AnimationCurve3(xNodes).Evaluate(x);
    }

    public void Smooth(int subdivisions)
    {
        if (subdivisions <= 1)
        {
            return;
        }
        Vector3[][] nodes = new Vector3[curves.Length + (subdivisions - 1) * (curves.Length - 1)][];

        float iValue = 0.0f;
        float jValue = 0.0f;

        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i] = new Vector3[curves[0].Length + (subdivisions - 1) * (curves[0].Length - 1)];
            iValue = (float)i * maxTime / (float)(nodes.Length - 1);
            for (int j = 0; j < nodes[i].Length; j++)
            {
                jValue = (float)j * curves[0].maxTime / (float)(nodes[i].Length - 1);
                nodes[i][j] = Evaluate(iValue, jValue);
            }
        }
        SetValues(nodes);
    }
    public void Smooth()
    {
        foreach (AnimationCurve3 curve in curves)
        {
            curve.Smooth();
        }
    }

    public Vector3 Normal(float x, float y)
    {
        float x1 = x + 0.0001f;
        float x2 = x;
        float y1 = y + 0.0001f;
        float y2 = y;
        Vector3 xTangent = (1.0f / 0.0001f) * (Evaluate(x1, y) - Evaluate(x2, y));
        Vector3 yTangent = (1.0f / 0.0001f) * (Evaluate(x, y1) - Evaluate(x, y2));

        return -Vector3.Cross(xTangent, yTangent).normalized;
    }

    public void DrawSurface(LineRenderer[] lines)
    {
        for (int i = 0; i < curves.Length && i < lines.Length; i++)
        {
            LineRenderer line = lines[i];
            line.positionCount = curves[i].values.Count;
            foreach (Vector3 value in curves[i].values)
            {
                line.SetPositions(curves[i].values.ToArray());
            }
        }
    }

    public void DrawMesh(Mesh mesh, Rect rect)
    {
        List<Vector3> vertices = new List<Vector3>(0);
        List<int> triangles = new List<int>(0);
        List<Vector2> uv = new List<Vector2>(0);
        List<Vector3> normals = new List<Vector3>(0);

        if (mesh == null || verticesCount != mesh.vertices.Length)
        {
            mesh = new Mesh();
        }

        int i = 0;
        int k = 0;
        verticesCount = 0;
        foreach (AnimationCurve3 curve in curves)
        {
            float delta = curve.maxTime / (float)(curves.Length - 1);
            int j = 0;
            int nCount = curves.Length;

            for (float f = 0.0f; j < nCount; f += delta)
            {
                vertices.Add(curve.Evaluate(f));
                float deltaX = (float)i / (float)(nCount - 1);
                float deltaY = (float)j / (float)(nCount - 1);
                uv.Add(new Vector2(rect.x + rect.width * deltaX, rect.y + rect.height * deltaY));
                normals.Add(Vector3.up);
                verticesCount++;
                if (j < nCount - 1 && i < nCount - 1)
                {
                    triangles.Add(k);
                    triangles.Add(k + 1);
                    triangles.Add(k + nCount);

                    triangles.Add(k + nCount);
                    triangles.Add(k + 1);
                    triangles.Add(k + nCount + 1);
                }
                k++;
                j++;
            }
            i++;
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.normals = normals.ToArray();
        mesh.RecalculateNormals();
    }
    public int ccc = 0;
    public void DrawMesh(GridWrapMode wrapMode, int subdivisions, float scaleX, float scaleY, Mesh rMesh, Transform parent, List<Mesh> meshes)
    {
        int count = 2 * (subdivisions + 1);
        CombineInstance[] combine = new CombineInstance[count * count + 4 * count];

        Vector3[] vertices = new Vector3[4];
        int[] triangles = new int[6];
        Vector3[] normals = new Vector3[4];
        Vector2[] uv = new Vector2[4];

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 1;
        triangles[4] = 0;
        triangles[5] = 3;

        uv[2] = new Vector2(0.0f, 1.0f);
        uv[3] = new Vector2(1.0f, 0.0f);
        uv[0] = new Vector2(1.0f, 1.0f);
        uv[1] = new Vector2(0.0f, 0.0f);

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = new Vector3(0.0f, 1.0f, 0.0f);
        }

        int k = 0;
        float xSmooth = scaleX / (float)count;
        float ySmooth = scaleY / (float)count;

        Vector3 v0 = Vector3.zero;
        Vector3 v2 = Vector3.zero;

        for (float x = 0.0f; x <= scaleX + xSmooth; x += xSmooth)
        {
            for (float y = 0.0f; y <= scaleY + ySmooth; y += ySmooth)
            {
                if (k >= combine.Length)
                {
                    break;
                }
                if (y == 0.0f)
                {
                    vertices[3] = Evaluate(x + xSmooth, y);
                    vertices[1] = Evaluate(x, y);

                }
                else
                {
                    vertices[3] = v0;
                    vertices[1] = v2;
                }

                v0 = Evaluate(x + xSmooth, y + ySmooth);
                v2 = Evaluate(x, y + ySmooth);

                vertices[2] = v2;
                vertices[0] = v0;

                Vector3 position = 0.5f * (vertices[2] + vertices[3]);

                vertices[0] = vertices[0].ToLocalPosition(position, parent);
                vertices[1] = vertices[1].ToLocalPosition(position, parent);
                vertices[2] = vertices[2].ToLocalPosition(position, parent);
                vertices[3] = vertices[3].ToLocalPosition(position, parent);


                if (wrapMode == GridWrapMode.Clamp)
                {
                    uv[0] = new Vector2(1.0f - x, y + ySmooth);
                    uv[1] = new Vector2(1.0f - xSmooth - x, y);
                    uv[2] = new Vector2(1.0f - xSmooth - x, y + ySmooth);
                    uv[3] = new Vector2(1.0f - x, y);
                }

                Mesh newMesh = new Mesh();
                meshes.Add(newMesh);
                newMesh.vertices = vertices;
                newMesh.triangles = triangles;
                newMesh.normals = normals;

                newMesh.uv = uv;

                combine[k].mesh = newMesh;
                position = position.ToLocalPosition(parent);
                Matrix4x4 m44 = new Matrix4x4();
                m44.SetTRS(position, Quaternion.identity, Vector3.one);
                combine[k].transform = m44;
                k++;
            }
        }
        rMesh.Clear();
        rMesh.CombineMeshes(combine, true, true);
        rMesh.RecalculateNormals();
        combine = null;
    }
}
