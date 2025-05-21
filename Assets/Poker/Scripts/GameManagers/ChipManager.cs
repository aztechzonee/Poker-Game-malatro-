using System.Collections;
using Geometry;
using UnityEngine;

public class ChipManager : IChipManager
{
    [SerializeField] private Transform m_ChipShadow;

    private Transform shadowLight;
    private Transform shadowPlane;

    private int id;
    public override int ID => id;


    public override void Init(int id, Transform chipsSkin, Transform shadowLight, Transform shadowPlane)
    {
        this.id = id;
        Instantiate(chipsSkin, transform.position, transform.rotation, transform);
        this.shadowLight = shadowLight;
        this.shadowPlane = shadowPlane;
        m_ChipShadow.position = GetShadowPosition();
    }

    public override void MoveToPivot(Vector3 endPosition)
    {
        transform.position = endPosition;
        m_ChipShadow.position = GetShadowPosition();
    }

    public override IEnumerator MoveToPivot(float moveTime, Vector3 endPosition, AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve)
    {
        Vector3 startPosition = transform.position;
        float time = 0f;
        WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

        while (time < moveTime)
        {
            float t = time / moveTime;
            Vector3 deltaY = new Vector3(0f, 0.3f * chipsDeltaYCurve.Evaluate(t), 0f);
            transform.position = deltaY + Vector3.Lerp(startPosition, endPosition, chipsMoveCurve.Evaluate(t));
            m_ChipShadow.position = GetShadowPosition();
            time += Time.deltaTime;
            yield return waitForEndOfFrame;
        }
        transform.position = endPosition;
        m_ChipShadow.position = GetShadowPosition();
    }

    private Vector3 GetShadowPosition()
    {
        return 0.005f * Vector3.up + VectorGeometry.GetLightProjectPoint(transform.position, shadowLight.position, shadowPlane);
    }
}
