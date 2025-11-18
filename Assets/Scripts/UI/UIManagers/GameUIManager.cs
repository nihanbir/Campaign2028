using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Dice & Actions")]
    [SerializeField] public Sprite[] diceFaces;
    [SerializeField] private TextMeshProUGUI phaseText;
    
    [Header("Phase UIs")]
    [SerializeField] public UM_SetupPhase setupUI;
    [SerializeField] public UM_MainPhase mainUI;

    public UM_BasePhase previouslyActiveUI = null;
    // Add others (CivilWarUI, GameOverUI) as needed
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}