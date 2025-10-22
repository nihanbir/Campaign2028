using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ActorDisplayCard : DisplayCard, IPointerClickHandler
{
    private ActorCard _actor;
    public TextMeshProUGUI EVScoreText;
    public TextMeshProUGUI InstScoreText;
    public TextMeshProUGUI nameText;
    public Image image;

    public void SetActor(ActorCard actor)
    {
        _actor = actor;
        UpdateUI();
    }

    public override void UpdateUI()
    {
        if (_actor == null)
        {
            Debug.Log("Player has no assigned actor.");
            return;
        }
        
        nameText.text = _actor.cardName;
        image.sprite = _actor.artwork;

        EVScoreText.text = _actor.evScore.ToString();
        InstScoreText.text = _actor.instScore.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Notify the SetupPhaseUIManager that this actor card was selected
        SetupPhaseUIManager.Instance.SelectActorCard(this);
    }

    public ActorCard GetActorCard()
    {
        return _actor;
    }
}
