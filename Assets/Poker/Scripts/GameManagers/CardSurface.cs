using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSurface : MonoBehaviour
{
    private GameObject card;
    private Mesh mesh;
    private Vector3[][] nodes;

    private MeshRenderer render;
    public AnimationSurface surface;

    public Rect rect;

    public Vector3 Axis => transform.forward;
    public Vector3 Normal => -transform.up;
    public Vector3 Pivot => transform.position;

    public void SetRect(Rect rect)
    {
        this.rect = rect;
    }

    public Vector3[] ExternalsPoints { get; private set; }

    public void Init(int subdivisions, Material cardMaterial)
    {
        card = gameObject;
        
        mesh = card.AddComponent<MeshFilter>().sharedMesh = new Mesh();
        render = card.AddComponent<MeshRenderer>();
        render.sharedMaterial = cardMaterial;
        render.receiveShadows = false;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        render.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        render.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        render.allowOcclusionWhenDynamic = false;

        subdivisions = Mathf.Clamp(subdivisions, 2, subdivisions);
        nodes = new Vector3[subdivisions + 1][];

        ExternalsPoints = new Vector3[] { transform.position, transform.position, transform.position, transform.position };

        CalculateNodes(subdivisions, 0f, 0f, null, null);
        surface = new AnimationSurface(nodes);

        UpdateCard(subdivisions, 0f, 0f, null, null);
    }

    public void UpdateCard(int subdivisions, float xAmplitude, float yAmplitude, AnimationCurve xDeform, AnimationCurve yDeform)
    {
        CalculateNodes(subdivisions, xAmplitude, yAmplitude, xDeform, yDeform);
        surface.SetValues(nodes);
        surface.Smooth();
        surface.DrawMesh(mesh, rect);
    }
    private void CalculateNodes(int subdivisions, float xAmplitude, float yAmplitude, AnimationCurve xDeform, AnimationCurve yDeform)
    {
        float ZDeform05 = 1f;// yAmplitude != 0f ? 0.7f : 1f;

        for (int i = 0; i <= subdivisions; i++)
        {
            float ti = (float)i / (float)subdivisions;
            float xDeform01 = xDeform != null ? xDeform.Evaluate(ti) : 0f;
            nodes[i] = new Vector3[subdivisions + 1];
            for (int j = 0; j <= subdivisions; j++)
            {
                float tj = (float)j / (float)subdivisions;
                float yDeform01 = yDeform != null ? yDeform.Evaluate(tj) : 0f;
                
                nodes[i][j] = (-0.5f + ti) * Vector3.right + (-0.5f + ZDeform05 * tj) * Vector3.forward + (xAmplitude * xDeform01 + yAmplitude * yDeform01) * Vector3.up;
            }
        }

        ExternalsPoints[0] = nodes[0][0].ToWordPosition(transform.parent);
        ExternalsPoints[1] = nodes[subdivisions][0].ToWordPosition(transform.parent);
        ExternalsPoints[2] = nodes[0][subdivisions].ToWordPosition(transform.parent);
        ExternalsPoints[3] = nodes[subdivisions][subdivisions].ToWordPosition(transform.parent);
    }
}
