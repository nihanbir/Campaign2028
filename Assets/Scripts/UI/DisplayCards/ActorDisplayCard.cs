using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActorDisplayCard : DisplayCard
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
}
