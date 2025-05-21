using System.Collections;
using Geometry;
using UnityEngine;

public class CardManager : ICardManager
{
    [SerializeField] private CardSurface m_CardSurface1;
    [SerializeField] private CardSurface m_CardSurface2;
    [SerializeField] private CardShadow m_CardShadow;
    [SerializeField] private Transform m_CarPivot;

    [SerializeField] private int m_Subdivisions;
    
    private Transform shadowLight;
    private Transform shadowPlane;

    private AnimationCurve xDeform;
    private AnimationCurve yDeform;

    public override void Init(Material cardMaterial, AnimationCurve xDeform, AnimationCurve yDeform)
    {
        this.xDeform = xDeform;
        this.yDeform = yDeform;
        Quaternion rotation2 = Quaternion.Euler(m_CarPivot.rotation.eulerAngles.x, m_CarPivot.rotation.eulerAngles.y, m_CarPivot.rotation.eulerAngles.z + 180f);
        m_CardSurface2.transform.rotation = rotation2;

        m_CardSurface1.Init(m_Subdivisions, cardMaterial);
        m_CardSurface2.Init(m_Subdivisions, cardMaterial);
        Position = m_CarPivot.position;
        Rotation = m_CarPivot.rotation;
    }

    public override void CreateShadow(Transform shadowLight, Transform shadowPlane)
    {
        this.shadowLight = shadowLight;
        this.shadowPlane = shadowPlane;
        UpdateShadowTransform();
        Vector3[] wordPositions = GetProjectPoints();
        m_CardShadow.CreateMesh(wordPositions, m_CardShadow.transform);
        UpdateShadow();
    }

    private void UpdateShadowTransform()
    {
        m_CardShadow.transform.position = m_CarPivot.position;
        m_CardShadow.transform.LookAt(m_CarPivot.position + Vector3.ProjectOnPlane(m_CarPivot.forward, Vector3.up).normalized, Vector3.up);
    }

    public override void SetRect(Rect rect, Rect backRect, string key)
    {
        Key = key;
        m_CardSurface1.SetRect(backRect);
        m_CardSurface2.SetRect(rect);
    }

    public override void SetSprite(Sprite sprite, string key)
    {

    }

    public override void ForceShowCard()
    {
        
    }

    public override void UpdateCard()
    {
        m_CardSurface1.UpdateCard(m_Subdivisions, CurrentAmplitudeX, CurrentAmplitudeY, xDeform, yDeform);
        m_CardSurface2.UpdateCard(m_Subdivisions, -CurrentAmplitudeX, -CurrentAmplitudeY, xDeform, yDeform);
        UpdateShadow();
    }

    private void UpdateShadow()
    {
        UpdateShadowTransform();

        Vector3[] wordPositions = GetProjectPoints();
        m_CardShadow.UpdateMeshes(wordPositions, m_CardShadow.transform, shadowLight, shadowPlane);
    }

    private Vector3[] GetProjectPoints()
    {
        Vector3[] wordPositions = new Vector3[m_CardSurface1.ExternalsPoints.Length];
        for (int i = 0; i < wordPositions.Length; i++)
        {
            wordPositions[i] = 0.005f * Vector3.up + VectorGeometry.GetLightProjectPoint(m_CardSurface1.ExternalsPoints[i], shadowLight.position, shadowPlane);
        }
        return wordPositions;
    }

    public override void RotateLocal(float angle, Vector3 localAxis)
    {
        m_CarPivot.Rotate(angle * localAxis, Space.Self);
        Position = m_CarPivot.position;
        Rotation = m_CarPivot.rotation;
        UpdateCard();
    }

    public override void MoveCard(float amplitudeX, Vector3 position, Quaternion rotation)
    {
        CurrentAmplitudeX = amplitudeX;
        MoveCard(position, rotation);
    }

    public override void MoveCard(float amplitudeX, float amplitudeY, Vector3 position, Quaternion rotation)
    {
        CurrentAmplitudeX = amplitudeX;
        CurrentAmplitudeY = amplitudeY;
        MoveCard(position, rotation);
    }

    private void MoveCard(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        m_CarPivot.rotation = rotation;

        Quaternion rotation2 = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y, rotation.eulerAngles.z + 180f);
        m_CardSurface2.transform.rotation = rotation2;

        Position = m_CarPivot.position;
        Rotation = m_CarPivot.rotation;
        UpdateCard();
    }

    public override void OpenCard(float openValue)
    {
        CurrentAmplitudeY = openValue;
        UpdateCard();
    }

    public override IEnumerator OpenClose(float openTime, float openValue, bool moveForward, AnimationCurve openCloseCurve)
    {
        float time = 0f;
        WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        float startValueY = CurrentAmplitudeY;
        float endValue = openValue;

        Vector3 startPosition = openValue > 0f ? Position : Position + 0.075f * m_CarPivot.forward;
        Vector3 endPosition = openValue == 0f ? Position : Position + 0.075f * m_CarPivot.forward;

        while (time < openTime)
        {
            float t = time / openTime;
            float tE = openCloseCurve.Evaluate(t);
            float value = Mathf.Lerp(startValueY, endValue, tE);
            CurrentAmplitudeY = value;

            if (moveForward)
            {
                transform.position = Vector3.Lerp(startPosition, endPosition, tE);
            }

            UpdateCard();
            time += Time.deltaTime;
            yield return waitForEndOfFrame;
        }
        CurrentAmplitudeY = endValue;
        UpdateCard();
    }

    public override void Open(float openValue)
    {
        CurrentAmplitudeX = -CurrentAmplitudeX;
        CurrentAmplitudeY = openValue;
        UpdateCard();
    }

    public override IEnumerator Open(float openTime, float openValue, AnimationCurve openCloseCurve)
    {
        CurrentAmplitudeX = -CurrentAmplitudeX;
        float time = 0f;
        WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        float startValueY = CurrentAmplitudeY;
        float endValue = openValue;

        while (time < openTime)
        {
            float t = time / openTime;
            float tE = openCloseCurve.Evaluate(t);
            float value = Mathf.Lerp(startValueY, endValue, tE);
            CurrentAmplitudeY = value;

            UpdateCard();
            time += Time.deltaTime;
            yield return waitForEndOfFrame;
        }
        CurrentAmplitudeY = endValue;
        UpdateCard();
    }
}
