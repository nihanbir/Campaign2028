using TMPro;
using UnityEngine;

public class ActorDisplayCard : DisplayCard
{
    private ActorCard _actor;
    public TextMeshProUGUI EVScoreText;
    public TextMeshProUGUI InstScoreText;

    public override void InitializeCard()
    {
        base.InitializeCard();
        Player firstPlayer = GameManager.Instance.players[0];
        _actor = firstPlayer.assignedActor;

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
}
