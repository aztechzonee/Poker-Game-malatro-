using UnityEngine;

namespace Geometry
{
    public static class VectorGeometry
    {
        public static bool GetPlaneHitPoint(Ray ray, Vector3 pointOnPlane, Vector3 planeNormal, out Vector3 hitPoint)
        {
            return GetPlaneHitPoint(ray.origin, ray.direction, pointOnPlane, planeNormal, out hitPoint);
        }
        public static bool GetPlaneHitPoint(Vector3 origin, Vector3 direction, Vector3 pointOnPlane, Vector3 planeNormal, out Vector3 hitPoint)
        {
            float dot = Vector3.Dot(direction, -planeNormal);
            if(dot <= 0f)
            {
                hitPoint = Vector3.zero;
                return false;
            }
            Vector3 projectPointOnPlane = ProjectPointOnPlane(origin, pointOnPlane, planeNormal);
            float h = Vector3.Distance(projectPointOnPlane, origin);
            Vector3 VectorH = h * planeNormal;

            hitPoint = projectPointOnPlane + VectorH + (direction * h / dot);
            return true;
        }

        public static Vector3 ProjectPointOnPlane(Vector3 point, Vector3 pointOnPlane, Vector3 planeNormal)
        {
            return pointOnPlane - Vector3.ProjectOnPlane(pointOnPlane - point, planeNormal);
        }

        public static Vector3 ProjectPointOnLine(Vector3 point, Vector3 pointOnLine, Vector3 lineNormal)
        {
            return pointOnLine + Vector3.ProjectOnPlane(point - pointOnLine, lineNormal);
        }

        public static Vector3 ProjectPointOnRay(Vector3 point, Vector3 startPoint, Vector3 direction)
        {
            return startPoint + Vector3.Project(point - startPoint, direction);
        }

        public static Vector3 ClampPositionInCube(Vector3 position, float radius, Transform cube)
        {
            if (SphereInCube(position, radius, cube))
            {
                return position;
            }
            Vector3 localPosition = GetLocalPosition(position, cube);
            float x = Mathf.Clamp(localPosition.x, -0.5f * cube.lossyScale.x + radius, 0.5f * cube.lossyScale.x - radius);
            float y = Mathf.Clamp(localPosition.y, -0.5f * cube.lossyScale.y + radius, 0.5f * cube.lossyScale.y - radius);
            float z = Mathf.Clamp(localPosition.z, -0.5f * cube.lossyScale.z + radius, 0.5f * cube.lossyScale.z - radius);
            Vector3 clampedLocalPosition = new Vector3(x, y, z);
            return GetWordPosition(clampedLocalPosition, cube);
        }

        public static bool SphereInCube(Vector3 position, float radius, Transform cube)
        {
            Vector3 localPos = GetLocalPosition(position, cube);
            Vector3 cubeScale = cube.lossyScale;
            return 
            Mathf.Abs(localPos.x) + radius - 0.5f * cubeScale.x <= 0.0f &&
            Mathf.Abs(localPos.y) + radius - 0.5f * cubeScale.y <= 0.0f &&
            Mathf.Abs(localPos.z) + radius - 0.5f * cubeScale.z <= 0.0f;
        }

        public static Vector3 GetLocalPosition(Vector3 wordPosition, Transform shape)
        {
            Vector3 deltaPos = wordPosition - shape.position;
            return new Vector3(Vector3.Dot(deltaPos, shape.right), Vector3.Dot(deltaPos, shape.up), Vector3.Dot(deltaPos, shape.forward));
        }

        public static Vector3 GetWordPosition(Vector3 localPosition, Transform shape)
        {
            return shape.position + localPosition.x * shape.right + localPosition.y * shape.up + localPosition.z * shape.forward;
        }
        public static Vector3 GetPerpendicularToVector(Vector3 vector, Vector3 origin)
        {
            return origin - Vector3.Project(origin, vector);
        }

        public static Vector3 GetLightProjectPoint(Vector3 point, Vector3 lightPoint, Transform plane)
        {
            Vector3 direction = (point - lightPoint).normalized;
            Vector3 projectPointOnPlane = ProjectPointOnPlane(point, plane.position, plane.up);
            float pointOnPlaneDistance = Vector3.Distance(point, projectPointOnPlane);
            Vector3 projectOnPlane = Vector3.ProjectOnPlane(direction, plane.up);
            float angle = Mathf.Acos(Vector3.Dot(direction, -plane.up));
            float tang = Mathf.Tan(angle);
            return projectPointOnPlane + tang * pointOnPlaneDistance * projectOnPlane.normalized;
        }
    }
}
