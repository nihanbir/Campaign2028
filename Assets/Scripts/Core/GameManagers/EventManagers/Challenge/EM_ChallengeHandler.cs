using UnityEngine;

public class EM_ChallengeHandler : BaseEventHandler
{
    private EM_AnyStateHandler _anyStateHandler;
    private EM_ChallengeInstHandler _instHandler;
    
    protected Card chosenCard;
    protected Player defender;
    protected Player attacker;
    protected EventCard currentEventCard;

    public EM_ChallengeHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }

    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        currentEventCard = card;
        attacker = player;
        
        switch (card.eventConditions)
        {
            case EventConditions.Any:
                _anyStateHandler = new EM_AnyStateHandler(_phase, _parent);
                _anyStateHandler.Handle(player, card, effectiveType);
                break;

            case EventConditions.IfInstitutionCaptured:
                _instHandler = new EM_ChallengeInstHandler(_phase, _parent);
                _instHandler.Handle(player, card, effectiveType);
                break;

            default:
                Cancel(card);
                break;
        }
    }

    // Optional: expose unified evaluate and forward to whichever handler was active
    public void EvaluateCapture(int roll)
    {
        bool success = chosenCard switch
        {
            StateCard s => s.IsSuccessfulRoll(roll, attacker.assignedActor.team),
            InstitutionCard i => i.IsSuccessfulRoll(roll, attacker.assignedActor.team),
            _                 => false
        };

        Debug.Log($"rolled: {roll}");
           
        if (success)
        {
            _phase.UpdateCardOwnership(attacker, chosenCard);
        }
        else
        {
            _phase.ReturnCardToDeck(currentEventCard);
            Debug.Log($"Player {attacker.playerID} failed to capture {chosenCard.cardName}");
        }
        
        //TODO: duel completed instead
        _parent.CompleteEvent();
    }
}