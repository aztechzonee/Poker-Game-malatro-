using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public delegate void UserChooseHandler(PlayerChoose choos, int cost);

public class GameUI : MonoBehaviour
{
    [SerializeField] private Image m_LoadinPanel;
    [SerializeField] private Image m_PlayerChoose;
    [Space(10), Header("Fold")]
    [SerializeField] private Button m_UserFoldButton;
    [SerializeField] private Image m_UserFoldImage;
    [Space(10), Header("CheckOrCall")]
    [SerializeField] private Button m_UserCheckOrCallButton;
    [SerializeField] private Image m_UserCheckImage;
    [SerializeField] private Image m_UserCallImage;
    [SerializeField] private Text m_CallText;
    [Space(10), Header("Bet")]
    [SerializeField] private Button m_UserBetButton;
    [SerializeField] private Button m_UserBetAllButton;
    [SerializeField] private Image m_UserBetImage;
    [SerializeField] private Text m_MaxBetText;
    [SerializeField] private Text m_BetText;
    [SerializeField] private Image m_MaxBet;
    [Space(10), Header("PlusMinus")]
    [SerializeField] private Button m_UserChoosePlusButton;
    [SerializeField] private Button m_UserChooseMinusButton;
    [SerializeField] private Button m_UserChooseLittlePlusButton;
    [SerializeField] private Button m_UserChooseLittleMinusButton;
    [Space(10), Header("Settings")]
    [SerializeField] private int littleCost = 10;
    [SerializeField] private int bigCost = 100;

    private int callValue;
    private int betValue;
    private int maxCost;

    public event UserChooseHandler OnPlayerChoose;

    private void Awake()
    {
        m_MaxBet.gameObject.SetActive(false);

        m_UserFoldButton.onClick.AddListener(() =>
        {
            TriggerChooseButtons(false);
            m_UserFoldImage.gameObject.SetActive(true);
            StartCoroutine(WaitAndPlayerChoose(PlayerChoose.Fold, 0));
        });

        m_UserCheckOrCallButton.onClick.AddListener(() =>
        {
            TriggerChooseButtons(false);
            if (callValue > 0)
            {
                m_UserCallImage.gameObject.SetActive(true);
                StartCoroutine(WaitAndPlayerChoose(PlayerChoose.Call, callValue));
            }
            else
            {
                m_UserCheckImage.gameObject.SetActive(true);
                StartCoroutine(WaitAndPlayerChoose(PlayerChoose.Check, callValue));
            }
        });

        m_UserBetButton.onClick.AddListener(() =>
        {
            TriggerChooseButtons(false);
            m_UserBetImage.gameObject.SetActive(true);
            StartCoroutine(WaitAndPlayerChoose(PlayerChoose.Bet, betValue));
        });

        m_UserBetAllButton.onClick.AddListener(() =>
        {
            TriggerChooseButtons(false);
            m_UserBetImage.gameObject.SetActive(true);
            StartCoroutine(WaitAndPlayerChoose(PlayerChoose.Bet, maxCost));
        });

        m_UserChoosePlusButton.onClick.AddListener(() =>
        {
            OnUserChooseUpdate(bigCost);
        });

        m_UserChooseMinusButton.onClick.AddListener(() =>
        {
            OnUserChooseUpdate(-bigCost);
        });

        m_UserChooseLittlePlusButton.onClick.AddListener(() =>
        {
            OnUserChooseUpdate(littleCost);
        });

        m_UserChooseLittleMinusButton.onClick.AddListener(() =>
        {
            OnUserChooseUpdate(-littleCost);
        });
    }

    public void HideLoadingPanel()
    {
        m_LoadinPanel.gameObject.SetActive(false);
    }

    public void RessetMaxBetText(GameStateData gameStateData)
    {
        m_MaxBetText.text = "";
        m_MaxBet.gameObject.SetActive(false);
    }

    public void SetMaxBetText(GameStateData gameStateData)
    {
        if (gameStateData.bet == 0)
        {
            m_MaxBet.gameObject.SetActive(false);
            return;
        }
        m_MaxBetText.text = "All bet: " + gameStateData.bet +
            (gameStateData.playersMaxBet == 0 ? "" : "\n\nMax bet: " + gameStateData.playersMaxBet);
        m_MaxBet.gameObject.SetActive(true);
    }

    private IEnumerator WaitAndPlayerChoose(PlayerChoose choos, int cost)
    {
        yield return new WaitForSeconds(0.15f);
        HideUserChoose();
        OnPlayerChoose?.Invoke(choos, cost);
    }

    public void SetCheckAndBetCost(int cValue, int bValue)
    {
        callValue = cValue;
        callValue = Mathf.Clamp(callValue, 0, maxCost);
        m_CallText.text = "$" + callValue;
        betValue = bValue;
        m_BetText.text = "$" + betValue;
    }

    public void SetMaxCost(int value)
    {
        maxCost = value;
    }

    private void OnUserChooseUpdate(int value)
    {
        betValue += value;
        betValue = Mathf.Clamp(betValue, 20, maxCost);
        m_BetText.text = "$" + betValue;
    }

    public void ShowPlayerChoose()
    {
        TriggerChooseButtons(true);

        m_UserFoldImage.gameObject.SetActive(false);
        m_UserCheckImage.gameObject.SetActive(false);
        m_UserCallImage.gameObject.SetActive(false);
        m_UserBetImage.gameObject.SetActive(false);

        m_PlayerChoose.gameObject.SetActive(true);
    }

    public IEnumerator AnimateWrongBet()
    {
        m_UserBetButton.image.color = Color.red;
        yield return new WaitForSeconds(1f);
        m_UserBetButton.image.color = Color.white;
    }

    public void HideUserChoose()
    {
        m_PlayerChoose.gameObject.SetActive(false);
    }

    private void TriggerChooseButtons(bool isOn)
    {
        m_UserFoldButton.interactable = isOn;
        m_UserCheckOrCallButton.interactable = isOn;
        m_UserBetButton.interactable = isOn;
        m_UserBetAllButton.interactable = isOn;
    }
}
