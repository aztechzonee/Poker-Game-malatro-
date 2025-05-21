using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class CardProperty
{
    public string Name;
    public Vector2Int Position;
}

[System.Serializable]
public class CardProperty2D
{
    public string Name;
    public Sprite sprite;
}

public class CardsManager : MonoBehaviour
{
    [SerializeField] private Transform m_CardsDeck;
    [SerializeField] private float m_CardThiknes;
    [SerializeField] private Material m_CardMaterial;
    [SerializeField] private Vector2Int m_CardsGrid;
    [SerializeField] private CardProperty[] cardProperties;
    [SerializeField] private CardProperty2D[] cardProperties2D;


    [SerializeField] private float m_CardsShowTime;
    [SerializeField] private float m_CardsMoveTime;
    [SerializeField] private float m_CardsShowHandsTime;
    [SerializeField] private float m_CardsMoveOnTableTime;
    [SerializeField] private AnimationCurve m_CardMoveCurve;
    [SerializeField] private AnimationCurve m_CardMoveOnTableCurve;
    [SerializeField] private AnimationCurve m_CardDeltaYCurve;
    [SerializeField] private AnimationCurve m_CardOnTableDeltaYCurve;

    [SerializeField] private ICardManager m_CardManagerPrefab;
    [SerializeField] private Transform m_DeckPivot;
    [SerializeField] private int m_CardsCount = 52;
    [SerializeField] private Vector2Int m_BackCardPosition;
    [SerializeField] private Vector2Int m_HiddenCardPosition;

    [SerializeField] private Sprite m_HiddenCardSprite;

    [SerializeField] private float m_MaxCardShowHandsDisplacementY;
    [SerializeField] private float m_MaxCardToPlayerDisplacementY;
    [SerializeField] private float m_MaxCardOnTableDisplacementY;

    [SerializeField] private float m_OpenTime;
    [SerializeField] private float m_OpenValue;

    [SerializeField]  private AnimationCurve m_XDeform;
    [SerializeField]  private AnimationCurve m_YDeform;

    [SerializeField] private float m_MaxAmplitudeX = 0.01f;


    public Dictionary<string, CardProperty> CardsDictionary { get; private set; }

    public Dictionary<string, CardProperty2D> CardsDictionary2D { get; private set; }

    public int CardsCount { get; private set; }
    public float CardsShowTime => m_CardsShowTime;

    private int tableCardsCount;
    private float deckHeight;
    private ICardManager nextCardManager;
    private Dictionary<int, List<ICardManager>> playersCardDictionary;
    private Dictionary<string, ICardManager> tableCardDictionary;

    private Dictionary<string, ICardManager> savedCards;
    private Rect backCardRect;
    private Rect hiddenCardRect;
    private Vector2 cardSize;

    public float ShowHandsTime => m_CardsShowHandsTime;

    public ICardManager GetCardManager(string key)
    {
        if(savedCards != null && savedCards.ContainsKey(key))
        {
            return savedCards[key];
        }
        return null;
    }

    public void SetFromHiddenCard(int playerID, List<string> cards)
    {
        int cardIndex = 0;
        foreach (var cardName in cards)
        {
            string hiddenKey = "Hidden_" + playerID + "_" + cardIndex;
            ICardManager cardManager = savedCards[hiddenKey];

            if (!MenuUI.Is2D)
            {
                CardProperty cardProperty = CardsDictionary[cardName];
                cardManager.SetRect(GetCardRect(cardProperty.Position), backCardRect, cardName);
            }
            else
            {
                CardProperty2D cardProperty = CardsDictionary2D[cardName];
                cardManager.SetSprite(cardProperty.sprite, cardName);
            }

            cardManager.name = cardName;
            cardManager.UpdateCard();

            savedCards.Add(cardName, cardManager);
            cardIndex++;
        }
    }

    public void CreateDeck(Transform shadowLight, Transform shadowPlane)
    {
        cardSize = new Vector2(1f / (float)m_CardsGrid.x, 1f / (float)m_CardsGrid.y);
        backCardRect = new Rect(cardSize.x * m_BackCardPosition.x, cardSize.y * m_BackCardPosition.y, cardSize.x, cardSize.y);
        hiddenCardRect = new Rect(cardSize.x * m_HiddenCardPosition.x, cardSize.y * m_HiddenCardPosition.y, cardSize.x, cardSize.y);


        savedCards = new Dictionary<string, ICardManager>(0);
        playersCardDictionary = new Dictionary<int, List<ICardManager>>(0);
        tableCardDictionary = new Dictionary<string, ICardManager>(0);

        CardsCount = m_CardsCount;
        tableCardsCount = 0;

        if (deckHeight > 0)
        {
            m_DeckPivot.localPosition = new Vector3(0f, deckHeight, 0f);
        }

        deckHeight = m_DeckPivot.localPosition.y;

        if (!MenuUI.Is2D)
        {
            CardsDictionary = new Dictionary<string, CardProperty>();

            for (int i = 0; i < cardProperties.Length; i++)
            {
                CardProperty cardProperty = cardProperties[i];
                CardsDictionary.Add(cardProperty.Name, cardProperty);
            }
        }
        else
        {
            CardsDictionary2D = new Dictionary<string, CardProperty2D>();

            for (int i = 0; i < cardProperties.Length; i++)
            {
                CardProperty2D cardProperty = cardProperties2D[i];
                CardsDictionary2D.Add(cardProperty.Name, cardProperty);
            }
        }

        nextCardManager = Instantiate(m_CardManagerPrefab, m_CardsDeck.position + (m_CardThiknes * CardsCount + m_CardThiknes) * Vector3.up, Quaternion.identity, m_CardsDeck);
        nextCardManager.Init(m_CardMaterial, m_XDeform, m_YDeform);
        nextCardManager.CreateShadow(shadowLight, shadowPlane);
        nextCardManager.SetRect(hiddenCardRect, backCardRect, "Next Card");
        nextCardManager.name = "Next Card";
        nextCardManager.UpdateCard();

        savedCards.Add("Next Card", nextCardManager);
    }

    public void RessetCards(GameStateData gameStateData, Transform shadowLight, Transform shadowPlane)
    {
        foreach (var cardManager in savedCards)
        {
            Destroy(cardManager.Value.gameObject);
        }

        CreateDeck(shadowLight, shadowPlane);
    }

    private Rect GetCardRect(Vector2Int position)
    {
        return new Rect(cardSize.x * position.x, cardSize.y * position.y, cardSize.x, cardSize.y);
    }

    public void GiveCardsToPlayer(int playerID, bool isMainPlayer, List<string> cards, Transform[] pivots,
        Transform shadowLight, Transform shadowPlane)
    {
        if (playersCardDictionary.ContainsKey(playerID))
        {
            return;
        }

        List<ICardManager> cardManagers = GetCardsManagers(playerID, cards, shadowLight, shadowPlane);

        MoveCardsToPlayer(isMainPlayer, cardManagers, pivots);
    }
    
    public IEnumerator GiveAndAnimateCardsToPlayer(int playerID, bool isMainPlayer, List<string> cards, Transform[] pivots,
        Transform shadowLight, Transform shadowPlane)
    {
        if (playersCardDictionary.ContainsKey(playerID))
        {
            yield break;
        }

        List<ICardManager> cardManagers = GetCardsManagers(playerID, cards, shadowLight, shadowPlane);

        yield return StartCoroutine(MoveAndAnimateCardsToPlayer(isMainPlayer, cardManagers, pivots));
    }

    private List<ICardManager> GetCardsManagers(int playerID, List<string> cards,
        Transform shadowLight, Transform shadowPlane)
    {
        List<ICardManager> cardManagers = new List<ICardManager>(0);
        foreach (var cardName in cards)
        {
            if (CardsCount == 0)
            {
                break;
            }

            if (!MenuUI.Is2D)
            {
                if (!CardsDictionary.ContainsKey(cardName))
                {
                    CardsDictionary.Add(cardName, new CardProperty { Name = cardName, Position = m_HiddenCardPosition });
                }
            }
            else
            {
                if (!CardsDictionary2D.ContainsKey(cardName))
                {
                    CardsDictionary2D.Add(cardName, new CardProperty2D { Name = cardName, sprite = m_HiddenCardSprite });
                }
            }
            
            ICardManager cardManager = Instantiate(m_CardManagerPrefab, m_CardsDeck.position +
                (m_CardThiknes * CardsCount + m_CardThiknes) * Vector3.up, Quaternion.identity, m_CardsDeck);
            savedCards.Add(cardName, cardManager);


            cardManagers.Add(cardManager);

            cardManager.Init(m_CardMaterial, m_XDeform, m_YDeform);
            cardManager.CreateShadow(shadowLight, shadowPlane);

            if (!MenuUI.Is2D)
            {
                CardProperty cardProperty = CardsDictionary[cardName];
                cardManager.SetRect(GetCardRect(cardProperty.Position), backCardRect, cardName);
                cardManager.name = "Card_" + cardProperty.Name;

                if (CardsCount == 0 || !CardsDictionary.ContainsKey(cardName))
                {
                    nextCardManager.gameObject.SetActive(false);
                    break;
                }
            }
            else
            {
                CardProperty2D cardProperty = CardsDictionary2D[cardName];
                cardManager.SetSprite(cardProperty.sprite, cardName);
                cardManager.name = "Card_" + cardProperty.Name;

                if (CardsCount == 0 || !CardsDictionary2D.ContainsKey(cardName))
                {
                    nextCardManager.gameObject.SetActive(false);
                    break;
                }
            }
            
            CardsCount--;

            m_DeckPivot.localPosition = new Vector3(0f, deckHeight * (float)CardsCount / (float)m_CardsCount, 0f);
            nextCardManager.MoveCard(0f, m_DeckPivot.position, nextCardManager.Rotation);
        }
        playersCardDictionary.Add(playerID, cardManagers);

        return cardManagers;
    }

    public void ShowCards(int playerIndex, Transform[] pivots)
    {
        if (!playersCardDictionary.ContainsKey(playerIndex))
        {
            return;
        }
        List<ICardManager> cardManagers = playersCardDictionary[playerIndex];

        if (MenuUI.Is2D)
        {
            for (int i = 0; i < cardManagers.Count; i++)
            {
                cardManagers[i].ForceShowCard();
            }
            return;
        }

        for (int i = 0; i < cardManagers.Count; i++)
        {
            cardManagers[i].transform.SetParent(pivots[i]);
            cardManagers[i].Open(-0.3f * m_OpenValue);
        }

        for (int i = 0; i < cardManagers.Count; i++)
        {
            cardManagers[i].MoveCard(m_MaxAmplitudeX, pivots[i].position, pivots[i].rotation);
        }
    }

    public IEnumerator AnimateShowCards(int playerIndex, Transform[] pivots)
    {
        if(!playersCardDictionary.ContainsKey(playerIndex))
        {
            yield break;
        }
        List<ICardManager> cardManagers = playersCardDictionary[playerIndex];

        if(MenuUI.Is2D)
        {
            for (int i = 0; i < cardManagers.Count; i++)
            {
                cardManagers[i].ForceShowCard();
            }
            yield break;
        }
       

        float time = 0;

        WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        List<Vector3> cardPositions = new List<Vector3>(0);
        List<Quaternion> cardRotations = new List<Quaternion>(0);
        foreach (var cardManager in cardManagers)
        {
            cardPositions.Add(cardManager.Position);
            cardRotations.Add(cardManager.Rotation);
        }
        for (int i = 0; i < cardManagers.Count; i++)
        {
            cardManagers[i].transform.SetParent(pivots[i]);
            StartCoroutine(cardManagers[i].Open(m_CardsShowTime, -0.3f * m_OpenValue, m_CardMoveCurve));
        }

        while (time < m_CardsShowTime)
        {
            float t = time / m_CardsShowTime;
            float tE = m_CardMoveCurve.Evaluate(t);
            Vector3 displacementY = new Vector3(0f, m_MaxCardToPlayerDisplacementY * m_CardDeltaYCurve.Evaluate(t), 0f);

            for (int i = 0; i < cardManagers.Count; i++)
            {
                cardManagers[i].transform.SetParent(pivots[i]);
                cardManagers[i].MoveCard(m_MaxAmplitudeX * t, displacementY + Vector3.Lerp(cardPositions[i], pivots[i].position, tE),
                    Quaternion.Lerp(cardRotations[i], pivots[i].rotation, tE));
            }
            time += Time.deltaTime;
            yield return waitForEndOfFrame;
        }
        for (int i = 0; i < cardManagers.Count; i++)
        {
            cardManagers[i].MoveCard(m_MaxAmplitudeX, pivots[i].position, pivots[i].rotation);
        }
    }

    private void MoveCardsToPlayer(bool isMainPlayer, List<ICardManager> cardManagers, Transform[] pivots)
    {
        for (int i = 0; i < cardManagers.Count; i++)
        {
            cardManagers[i].transform.SetParent(pivots[i]);
            cardManagers[i].MoveCard(m_MaxAmplitudeX, pivots[i].position, pivots[i].rotation);
        }

        if (isMainPlayer)
        {
            foreach (var cardManager in cardManagers)
            {
                cardManager.OpenCard(m_OpenValue);
                cardManager.ForceShowCard();
            }
        }
    }

    private IEnumerator MoveAndAnimateCardsToPlayer(bool isMainPlayer, List<ICardManager> cardManagers, Transform[] pivots)
    {
        float time = 0;

        WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        List<Vector3> cardPositions = new List<Vector3>(0);
        List<Quaternion> cardRotations = new List<Quaternion>(0);
        foreach (var cardManager in cardManagers)
        {
            cardPositions.Add(cardManager.Position);
            cardRotations.Add(cardManager.Rotation);
        }
        for (int i = 0; i < cardManagers.Count; i++)
        {
            cardManagers[i].transform.SetParent(pivots[i]);
        }
        while (time < m_CardsMoveTime)
        {
            float t = time / m_CardsMoveTime;
            float tE = m_CardMoveCurve.Evaluate(t);
            Vector3 displacementY = new Vector3(0f, m_MaxCardToPlayerDisplacementY * m_CardDeltaYCurve.Evaluate(t), 0f);

            for (int i = 0; i < cardManagers.Count; i++)
            {
                cardManagers[i].transform.SetParent(pivots[i]);
                cardManagers[i].MoveCard(m_MaxAmplitudeX * t, displacementY + Vector3.Lerp(cardPositions[i], pivots[i].position, tE),
                    Quaternion.Lerp(cardRotations[i], pivots[i].rotation, tE));
            }
            time += Time.deltaTime;
            yield return waitForEndOfFrame;
        }
        for (int i = 0; i < cardManagers.Count; i++)
        {
            cardManagers[i].MoveCard(m_MaxAmplitudeX, pivots[i].position, pivots[i].rotation);
        }

        StartCoroutine(OpenClose(isMainPlayer, cardManagers));
    }

    public IEnumerator OpenClose(bool isMainPlayer, List<ICardManager> cardManagers)
    {
        float openValue = isMainPlayer ? m_OpenValue : 0.5f * m_OpenValue;

        foreach (var cardManager in cardManagers)
        {
            StartCoroutine(cardManager.OpenClose(m_OpenTime, openValue, !isMainPlayer, m_CardMoveCurve));
        }
        if (!isMainPlayer)
        {
            yield return new WaitForSeconds(3f * m_OpenTime);
            foreach (var cardManager in cardManagers)
            {
                StartCoroutine(cardManager.OpenClose(m_OpenTime, 0f, true, m_CardMoveCurve));
            }
        }
        else
        {
            foreach (var cardManager in cardManagers)
            {
                cardManager.ForceShowCard();
            }
        }
    }

    public IEnumerator OpenClose(int playerIndex, bool isMainPlayer)
    {
        if(playersCardDictionary.Count == 0 || !playersCardDictionary.ContainsKey(playerIndex))
        {
            yield break;
        }
        
        float openValue = isMainPlayer ? m_OpenValue : 0.5f * m_OpenValue;

        foreach (var cardManager in playersCardDictionary[playerIndex])
        {
            StartCoroutine(cardManager.OpenClose(m_OpenTime, openValue, !isMainPlayer, m_CardMoveCurve));
        }
        if (!isMainPlayer)
        {
            yield return new WaitForSeconds(1f);
            foreach (var cardManager in playersCardDictionary[playerIndex])
            {
                StartCoroutine(cardManager.OpenClose(m_OpenTime, 0f, true, m_CardMoveCurve));
            }
        }
    }

    public void MoveCardsToTable(GameStateData gameStateData, Transform shadowLight, Transform shadowPlane, Transform[] cardPivots)
    {
        if (tableCardDictionary.Count >= 5)
        {
            return;
        }

        List<ICardManager> cardManagers = GetCardsForMoveToTable(gameStateData, shadowLight, shadowPlane, cardPivots);

        if (cardManagers.Count != 0)
        {
            int startTableCardsCount = tableCardsCount;
            foreach (var cardManager in cardManagers)
            {
                MoveCardsToTable(startTableCardsCount, cardManager, cardPivots);
                startTableCardsCount++;
            }
        }
    }

    public IEnumerator AnimateMoveCardsToTable(GameStateData gameStateData, Transform shadowLight, Transform shadowPlane, Transform[] cardPivots)
    {
        if (tableCardDictionary.Count >= 5)
        {
            yield break;
        }

        List<ICardManager> cardManagers = GetCardsForMoveToTable(gameStateData, shadowLight, shadowPlane, cardPivots);

        if (cardManagers.Count != 0)
        {
            int startTableCardsCount = tableCardsCount;
            foreach (var cardManager in cardManagers)
            {
                yield return StartCoroutine(AnimateMoveCardsToTable(startTableCardsCount, cardManager, cardPivots));
                startTableCardsCount++;
            }
        }
    }

    private List<ICardManager> GetCardsForMoveToTable(GameStateData gameStateData, Transform shadowLight, Transform shadowPlane, Transform[] cardPivots)
    {
        List<ICardManager> cardManagers = new List<ICardManager>(0);
        tableCardsCount = tableCardDictionary.Count;
        foreach (var cardName in gameStateData.tableCards)
        {
            if (tableCardDictionary.Count >= 5)
            {
                break;
            }

            if (tableCardDictionary.ContainsKey(cardName))
            {
                continue;
            }

            if (!MenuUI.Is2D)
            {
                if (CardsCount == 0 || !CardsDictionary.ContainsKey(cardName))
                {
                    break;
                }
            }
            else
            {
                if (CardsCount == 0 || !CardsDictionary2D.ContainsKey(cardName))
                {
                    break;
                }
            }

           
            ICardManager cardManager = Instantiate(m_CardManagerPrefab, m_CardsDeck.position + (m_CardThiknes * CardsCount + m_CardThiknes) * Vector3.up, Quaternion.identity, m_CardsDeck);
            savedCards.Add(cardName, cardManager);
            cardManagers.Add(cardManager);
            tableCardDictionary.Add(cardName, cardManager);

            cardManager.Init(m_CardMaterial, m_XDeform, m_YDeform);
            cardManager.CreateShadow(shadowLight, shadowPlane);

            if (!MenuUI.Is2D)
            {
                CardProperty cardProperty = CardsDictionary[cardName];
                cardManager.SetRect(GetCardRect(cardProperty.Position), backCardRect, cardName);
                cardManager.name = "Card_" + cardProperty.Name;

                if (CardsCount == 0 || !CardsDictionary.ContainsKey(cardName))
                {
                    nextCardManager.gameObject.SetActive(false);
                    break;
                }

            }
            else
            {
                CardProperty2D cardProperty = CardsDictionary2D[cardName];
                cardManager.SetSprite(cardProperty.sprite, cardName);
                cardManager.name = "Card_" + cardProperty.Name;

                if (CardsCount == 0 || !CardsDictionary2D.ContainsKey(cardName))
                {
                    nextCardManager.gameObject.SetActive(false);
                    break;
                }
            }

            CardsCount--;

            m_DeckPivot.localPosition = new Vector3(0f, deckHeight * (float)CardsCount / (float)m_CardsCount, 0f);
            nextCardManager.MoveCard(0f, m_DeckPivot.position, nextCardManager.Rotation);
        }

        return cardManagers;
    }

    private void MoveCardsToTable(int cardsCount, ICardManager cardManager, Transform[] cardPivots)
    {
        cardManager.MoveCard(-m_MaxAmplitudeX, 0f, cardPivots[cardsCount].position, cardPivots[cardsCount].rotation);
        cardManager.transform.SetParent(cardPivots[cardsCount]);

        cardManager.ForceShowCard();
    }

    private IEnumerator AnimateMoveCardsToTable(int cardsCount,  ICardManager cardManager, Transform[] cardPivots)
    {
        float time = 0;

        WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

        Vector3 cardPosition = cardManager.Position;
        Quaternion cardRotation = cardManager.Rotation;
        float openTime = 0.25f * m_CardsMoveOnTableTime;
        float closeDeltaTime = 0.5f * m_CardsMoveOnTableTime;
        StartCoroutine(cardManager.OpenClose(openTime, 0.75f * m_OpenValue, false, m_CardMoveOnTableCurve));

        bool startOpen = false;

        while (time < m_CardsMoveOnTableTime)
        {
            float t = time / m_CardsMoveOnTableTime;
            float tE = m_CardMoveOnTableCurve.Evaluate(t);
            Vector3 displacementY = new Vector3(0f, m_MaxCardOnTableDisplacementY * m_CardOnTableDeltaYCurve.Evaluate(t), 0f);

            if (!startOpen && time > openTime)
            {
                startOpen = true;
                StartCoroutine(cardManager.OpenClose(closeDeltaTime, 0f, false, m_CardMoveOnTableCurve));
            }

            Quaternion needRotation = Quaternion.Lerp(cardRotation, cardPivots[cardsCount].rotation, tE);

            cardManager.MoveCard(-m_MaxAmplitudeX * t, displacementY + Vector3.Lerp(cardPosition, cardPivots[cardsCount].position, tE), needRotation);

            time += Time.deltaTime;
            yield return waitForEndOfFrame;
        }

        cardManager.MoveCard(-m_MaxAmplitudeX, 0f, cardPivots[cardsCount].position, cardPivots[cardsCount].rotation);
        cardManager.transform.SetParent(cardPivots[cardsCount]);

        cardManager.ForceShowCard();
    }

    public void ShowCardOnTable(ICardManager cardManager, Transform cardPivot)
    {
        if (cardManager == null || cardManager.transform.parent == cardPivot)
        {
            return;
        }
        cardManager.transform.SetParent(cardPivot);

        cardManager.ForceShowCard();
    }

    public void ResetCardsInPivot(Transform cardPivot, Vector3 displacement)
    {
        CardManager[] cardManagers = cardPivot.GetComponentsInChildren<CardManager>();
        if (cardManagers != null && cardManagers.Length != 0)
        {
            for (int i = 0; i < cardManagers.Length; i++)
            {
                Vector3 childCountFactor = displacement.z * i * cardPivot.forward -
                        0.5f * displacement.x * i * cardPivot.right + 0.2f * displacement.y * i * Vector3.up;
                cardManagers[i].MoveCard(-m_MaxAmplitudeX, 0f, cardPivot.position + childCountFactor, cardPivot.rotation);
            }
        }
    }

    public IEnumerator AnimateAndShowCardOnTable(ICardManager cardManager, Transform cardPivot, Vector3 displacement, int cardIndex)
    {
        if(cardManager == null || cardManager.transform.parent == cardPivot)
        {
            yield break;
        }
        float time = 0;

        WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        Vector3 cardPosition = cardManager.Position;
        Quaternion cardRotation = cardManager.Rotation;

        Vector3 childCountFactor = Vector3.zero;
        float childCount = -1f;
        void RessetCountFactor()
        {
            if (childCount != (float)cardPivot.childCount)
            {
                childCount = (float)cardPivot.childCount;
                childCountFactor = -displacement.z * childCount * cardPivot.forward +
                    0.5f * displacement.x * childCount * cardPivot.right + 0.2f * displacement.y * childCount * Vector3.up;
            }
        }

        RessetCountFactor();

        float currentAmplitudeX = cardManager.CurrentAmplitudeX;
        float currentAmplitudeY = cardManager.CurrentAmplitudeY;

        while (time < m_CardsShowHandsTime)
        {
            RessetCountFactor();
            float t = time / m_CardsShowHandsTime;
            Vector3 displacementY = new Vector3(0f, (m_MaxCardShowHandsDisplacementY + 0.2f * displacement.y * (float)cardIndex) * m_CardOnTableDeltaYCurve.Evaluate(t), 0f);

            Quaternion needRotation = Quaternion.Lerp(cardRotation, cardPivot.rotation, t);
            Vector3 needPosition = displacementY + Vector3.Lerp(cardPosition, cardPivot.position + childCountFactor, t);

            cardManager.MoveCard(Mathf.Lerp(currentAmplitudeX, - m_MaxAmplitudeX, 1.2f * t), Mathf.Lerp(currentAmplitudeY, 0f, 1.2f * t), needPosition, needRotation);
            time += Time.deltaTime;
            yield return waitForEndOfFrame;
        }
        RessetCountFactor();
        cardManager.MoveCard(-m_MaxAmplitudeX, 0f, cardPivot.position + childCountFactor, cardPivot.rotation);
        cardManager.transform.SetParent(cardPivot);

        cardManager.ForceShowCard();
    }
}
