using UnityEngine;

public class EM_ChallengeHandler : BaseEventHandler
{
    private EM_AnyStateHandler _anyStateHandler;
    private EM_ChallengeInstHandler _instHandler;
    
    private Card _chosenCard;
    protected EventCard currentEventCard;
    
    private readonly EM_ChallengeHandler _parentHandler;

    public EM_ChallengeHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent)
    {
        _parentHandler = this;
    }

    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        currentEventCard = card;
        
        switch (card.eventConditions)
        {
            case EventConditions.Any:
                _anyStateHandler = new EM_AnyStateHandler(phase, parent, this);
                _anyStateHandler.Handle(player, card, effectiveType);
                break;

            case EventConditions.IfInstitutionCaptured:
                _instHandler = new EM_ChallengeInstHandler(phase, parent, this);
                _instHandler.Handle(player, card, effectiveType);
                break;

            default:
                Cancel(card);
                break;
        }
    }

    public void SetChosenCard(Card chosenCard) => _chosenCard = chosenCard;

    public override void EvaluateRoll(Player player, int roll)
    {
        bool success = _chosenCard switch
        {
            StateCard s => s.IsSuccessfulRoll(roll, player.assignedActor.team),
            InstitutionCard i => i.IsSuccessfulRoll(roll, player.assignedActor.team),
            _                 => false
        };

        Debug.Log($"rolled: {roll}");
           
        if (success)
        {
            phase.UpdateCardOwnership(player, _chosenCard);
        }
        else
        {
            phase.ReturnCardToDeck(currentEventCard);
            Debug.Log($"Player {player.playerID} failed to capture {_chosenCard.cardName}");
        }
        
        _chosenCard = null;
        parent.CompleteDuel();
    }
}