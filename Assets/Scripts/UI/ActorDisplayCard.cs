using TMPro;
using UnityEngine;

public class ActorDisplayCard : DisplayCard
{
    private ActorCard _actor;
    public TextMeshProUGUI EVScoreText;
    public TextMeshProUGUI InstScoreText;

    public void SetActor(ActorCard actor)
    {
        _actor = actor;
        UpdateUI();
    }

    private void UpdateUI()
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
}
