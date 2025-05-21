using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class BackUI : MonoBehaviour
{
    [SerializeField] private Button m_BackButton;
    public Action OnBackButton;

    private void Awake()
    {
        m_BackButton.onClick.AddListener(() =>
        {
            OnBackButton?.Invoke();
            SceneManager.LoadScene("Menu");
        });
    }
}
