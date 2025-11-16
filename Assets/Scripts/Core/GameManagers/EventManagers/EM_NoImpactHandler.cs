using UnityEngine;

public class EM_NoImpactHandler : BaseEventHandler
{
    public EM_NoImpactHandler(GM_MainPhase phase, EventManager parent)
        : base(phase, parent) { }

    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        Debug.Log($"No impact event triggered: {card.cardName}");

        // Immediately mark as completed
        _parent.CompleteEvent();
    }
}