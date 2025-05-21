using UnityEngine;
using System.Collections;

public class GizmosQuad : MonoBehaviour
{
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        DrawGizmosQuad(transform);
    }

    public static void DrawGizmosQuad(Transform quad)
    {
        Vector3 pos = quad.position;

        Vector3 localPos1 = 0.5f * quad.lossyScale.x * quad.right + 0.5f * quad.lossyScale.z * quad.forward;
        Vector3 localPos2 = 0.5f * quad.lossyScale.x * quad.right - 0.5f * quad.lossyScale.z * quad.forward;
        Vector3 localPos3 = -0.5f * quad.lossyScale.x * quad.right - 0.5f * quad.lossyScale.z * quad.forward;
        Vector3 localPos4 = -0.5f * quad.lossyScale.x * quad.right + 0.5f * quad.lossyScale.z * quad.forward;

        Gizmos.DrawLine(pos + localPos1, pos + localPos2);
        Gizmos.DrawLine(pos + localPos2, pos + localPos3);
        Gizmos.DrawLine(pos + localPos3, pos + localPos4);
        Gizmos.DrawLine(pos + localPos4, pos + localPos1);
    }
}
