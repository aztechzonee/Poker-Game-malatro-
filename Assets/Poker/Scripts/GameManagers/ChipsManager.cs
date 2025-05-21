using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChipsManager : MonoBehaviour
{
    [SerializeField] private Transform[] m_StartChipsPivots;
    [SerializeField] private Transform[] m_ChipsSkins;
    [SerializeField] private IChipManager m_ChipPrefab;

    [Space(10), Header("Settings")]
    [SerializeField] private float m_ChipsThicknes;
    [SerializeField] private float m_ChipsMoveTime;
    [SerializeField] private AnimationCurve m_ChipsMoveCurve;
    [SerializeField] private AnimationCurve m_ChipsDeltaYCurve;

    public float GetChipsThicknes => m_ChipsThicknes;
    public float GetChipsMoveTime => m_ChipsMoveTime;
    public Transform[] StartChipsPivots => m_StartChipsPivots;

    public AnimationCurve GetChipsMoveCurve => m_ChipsMoveCurve;
    public AnimationCurve GetChipsDeltaYCurve => m_ChipsDeltaYCurve;

    private Stack<IChipManager>[] chipsManagers;

    private void Awake()
    {
        foreach (var startChipsPivots in m_StartChipsPivots)
        {
            Destroy(startChipsPivots.GetChild(0).gameObject);
        }
    }

    public void MoveChipsToTable(Stack<IChipManager>[] chipsManagers, Transform[] chipsPivots)
    {
        if(chipsManagers == null || chipsManagers.Length != chipsPivots.Length)
        {
            return;
        }
        for (int i = 0; i < chipsPivots.Length; i++)
        {
            foreach (var chipManager in chipsManagers[i])
            {
                chipManager.transform.SetParent(chipsPivots[i]);
               
                Vector3 randomDisplacement = new Vector3(Random.Range(
                    0f, 2f * m_ChipsThicknes), 0f, Random.Range(0f, 2f * m_ChipsThicknes));

                chipManager.MoveToPivot(randomDisplacement +
                    chipsPivots[i].position + (float)chipsPivots[i].childCount * m_ChipsThicknes * Vector3.up);
            }
        }
    }

    public void CreateChipsManagers(ChipsColumn[] chips, Transform shadowLight, Transform shadowPlane)
    {
        CreateChipsManagers(chips, shadowLight, shadowPlane, out chipsManagers);
    }

    public Stack<IChipManager>[] CreateNewChipsManagers(ChipsColumn[] chips, Transform shadowLight, Transform shadowPlane)
    {
        CreateChipsManagers(chips, shadowLight, shadowPlane, out Stack<IChipManager>[] chipsManagers);
        return GiveChipsManagers(chips, chipsManagers, shadowLight, shadowPlane);
    }


    public Stack<IChipManager>[] GiveChipsManagers(ChipsColumn[] chips, Transform shadowLight, Transform shadowPlane)
    {
        return GiveChipsManagers(chips, chipsManagers, shadowLight, shadowPlane);
    }

    private void CreateChipsManagers(ChipsColumn[] chips, Transform shadowLight, Transform shadowPlane,
        out Stack<IChipManager>[] newChipsManagers)
    {
        newChipsManagers = new Stack<IChipManager>[chips.Length];
        float chipsThicknes = m_ChipsThicknes;

        for (int i = 0; i < newChipsManagers.Length && i < m_ChipsSkins.Length; i++)
        {
            Stack<IChipManager> chipsStack = new Stack<IChipManager>(0);

            Vector3 randomDisplacement = new Vector3(Random.Range(0f, 2f * chipsThicknes), 0f, Random.Range(0f, 2f * chipsThicknes));
            int randomIndex = 0;
            int randomMaxCount = Random.Range(5, 10);
            for (int j = 0; j < chips[i].count; j++)
            {
                IChipManager chipManager = Instantiate(m_ChipPrefab, m_StartChipsPivots[i]);
                if (randomIndex > randomMaxCount)
                {
                    randomDisplacement = new Vector3(Random.Range(0f, 2f * chipsThicknes), 0f, Random.Range(0f, 2f * chipsThicknes));
                    randomIndex = 0;
                }
                else
                {
                    randomDisplacement = new Vector3(Random.Range(0f, 0.5f * chipsThicknes), 0f, Random.Range(0f, 0.5f * chipsThicknes));
                }
                randomIndex++;
                chipManager.transform.position = randomDisplacement + m_StartChipsPivots[i].position + (float)j * chipsThicknes * Vector3.up;

                chipManager.Init(i, m_ChipsSkins[i], shadowLight, shadowPlane);
                chipsStack.Push(chipManager);
            }

            newChipsManagers[i] = chipsStack;
        }        
    }

    public void PushChip(int index, IChipManager chipManager)
    {
        chipsManagers[index].Push(chipManager);
    }

    private Stack<IChipManager>[] GiveChipsManagers(ChipsColumn[] chips, Stack<IChipManager>[] chipsManagers,
        Transform shadowLight, Transform shadowPlane)
    {
        Stack<IChipManager>[] getChipsManagers = new Stack<IChipManager>[chips.Length];
        for (int i = 0; i < getChipsManagers.Length; i++)
        {
            getChipsManagers[i] = new Stack<IChipManager>(0);
        }
        for (int i = 0; i < chips.Length; i++)
        {
            Stack<IChipManager> chipsStack = new Stack<IChipManager>(0);
            int chipsCount = chips[i].count;
            int chipsStartIndex = 0;
            for (int j = 0; j < chipsCount; j++)
            {
                if (chipsManagers[i].Count > 0)
                {
                    chipsStack.Push(chipsManagers[i].Pop());
                }
                else
                {
                    IChipManager chipManager = Instantiate(m_ChipPrefab, m_StartChipsPivots[i]);
                    chipManager.transform.position = m_StartChipsPivots[i].position + (float)chipsStartIndex * m_ChipsThicknes * Vector3.up;

                    chipManager.Init(i, m_ChipsSkins[i], shadowLight, shadowPlane);
                    chipsStack.Push(chipManager);
                    chipsStartIndex++;
                }
            }
            getChipsManagers[i] = chipsStack;
        }
        return getChipsManagers;
    }
}
