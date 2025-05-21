using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Camera m_Camera;

    [SerializeField] private Vector3 m_Position1;
    [SerializeField] private Vector3 m_Position2;

    [SerializeField] private int m_ScreenWidth1;
    [SerializeField] private int m_ScreenHeight1;

    [SerializeField] private int m_ScreenWidth2;
    [SerializeField] private int m_ScreenHeight2;

    private int screenWidth;
    private int screenHeight;

    private void Start()
    {
        MoveCamera();
    }
#if UNITY_EDITOR
    private void Update()
    {
        if(screenWidth != Screen.width || screenHeight != Screen.height)
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            MoveCamera();
        }
    }
#endif

    private void MoveCamera()
    {
        float k1 = (float)m_ScreenWidth1 / (float)m_ScreenHeight1;
        float k2 = (float)m_ScreenWidth2 / (float)m_ScreenHeight2;
        float k = (float)screenWidth / (float)screenHeight;
        float t = (k - k1) / (k2 - k1);
        if (!float.IsNaN(t))
        {
            m_Camera.transform.position = Vector3.LerpUnclamped(m_Position1, m_Position2, t);
        }
    }
}
