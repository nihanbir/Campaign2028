
using System;
using UnityEngine;
using UnityEngine.UI;

public class MainPhaseUIManager : MonoBehaviour
{
    public static MainPhaseUIManager Instance;
    
    [Header("Main Game Phase")] 
    public GameObject mainGamePhase;
    public Button rollDiceButton;
    
    [Header("Player")]
    public GameObject playerActorDisplayPrefab; // Assign prefab in inspector
    public Transform playerUIParent; // Assign a UI container (e.g., a panel) in inspector
    public float spacingBetweenActorCards = 300;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
    }
    
    public void OnRollDiceClicked()
    {
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
    }
}
