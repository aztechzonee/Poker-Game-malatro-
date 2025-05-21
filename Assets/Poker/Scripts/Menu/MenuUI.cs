using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private Button m_LocalGameButton;
    [SerializeField] private Button m_PhotonGameButton;

    [SerializeField] private bool is2D = false;

    public static string GameScenName;

    public static bool Is2D;

    private void Awake()
    {
        Is2D = is2D;
        GameScenName = is2D ? "Game2D" : "Game";

        Resources.UnloadUnusedAssets();        

#if PHOTON_UNITY_NETWORKING
        m_LocalGameButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("ServerSimulator");
        });

        m_PhotonGameButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Photon");
        });
#else
        SceneManager.LoadScene("ServerSimulator");
#endif
    }
}
