using UnityEngine;

public class EM_NoImpactHandler : BaseEventHandler
{
    public EM_NoImpactHandler(GM_MainPhase phase, EventManager parent)
        : base(phase, parent) { }

    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        Debug.Log($"No impact event triggered: {card.cardName}");

        // Still raise bus events for consistency
        EventCardBus.Instance.Raise(
            new EventCardEvent(EventStage.EventStarted, new EventStartedData(effectiveType, player, card))
        );

        // Immediately mark as completed
        _parent.EndEventImmediate(card, player);
    }
}