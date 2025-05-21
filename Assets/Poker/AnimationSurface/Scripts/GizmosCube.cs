using UnityEngine;
using System.Collections;

public class GizmosCube : MonoBehaviour
{
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        DrawGizmosCube(transform);
    }

    public static void DrawGizmosCube(Transform cube)
    {
        Vector3 pos = cube.position;
		
        Vector3 localPos1 = 0.5f * cube.lossyScale.x * cube.right + 0.5f * cube.lossyScale.y * cube.up + 0.5f * cube.lossyScale.z * cube.forward;
        Vector3 localPos2 = -0.5f * cube.lossyScale.x * cube.right + 0.5f * cube.lossyScale.y * cube.up + 0.5f * cube.lossyScale.z * cube.forward;
        Vector3 localPos3 = -0.5f * cube.lossyScale.x * cube.right - 0.5f * cube.lossyScale.y * cube.up + 0.5f * cube.lossyScale.z * cube.forward;
        Vector3 localPos4 = 0.5f * cube.lossyScale.x * cube.right - 0.5f * cube.lossyScale.y * cube.up + 0.5f * cube.lossyScale.z * cube.forward;
		
        Vector3 localPos5 = 0.5f * cube.lossyScale.x * cube.right + 0.5f * cube.lossyScale.y * cube.up - 0.5f * cube.lossyScale.z * cube.forward;
        Vector3 localPos6 = -0.5f * cube.lossyScale.x * cube.right + 0.5f * cube.lossyScale.y * cube.up - 0.5f * cube.lossyScale.z * cube.forward;
        Vector3 localPos7 = -0.5f * cube.lossyScale.x * cube.right - 0.5f * cube.lossyScale.y * cube.up - 0.5f * cube.lossyScale.z * cube.forward;
        Vector3 localPos8 = 0.5f * cube.lossyScale.x * cube.right - 0.5f * cube.lossyScale.y * cube.up - 0.5f * cube.lossyScale.z * cube.forward;
		
		
        Gizmos.DrawLine(pos + localPos1, pos + localPos2);
        Gizmos.DrawLine(pos + localPos2, pos + localPos3);
        Gizmos.DrawLine(pos + localPos3, pos + localPos4);
        Gizmos.DrawLine(pos + localPos4, pos + localPos1);
		
        Gizmos.DrawLine(pos + localPos5, pos + localPos6);
        Gizmos.DrawLine(pos + localPos6, pos + localPos7);
        Gizmos.DrawLine(pos + localPos7, pos + localPos8);
        Gizmos.DrawLine(pos + localPos8, pos + localPos5);
		
        Gizmos.DrawLine(pos + localPos1, pos + localPos5);
        Gizmos.DrawLine(pos + localPos2, pos + localPos6);
        Gizmos.DrawLine(pos + localPos3, pos + localPos7);
        Gizmos.DrawLine(pos + localPos4, pos + localPos8);
    }
}
