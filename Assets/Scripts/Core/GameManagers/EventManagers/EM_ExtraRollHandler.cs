public class EM_ExtraRollHandler : BaseEventHandler
{
    public EM_ExtraRollHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }

    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        bool canApply = card.eventConditions switch
        {
            EventConditions.IfOwnsInstitution => player.HasInstitution(card.requiredInstitution),
            EventConditions.None => true,
            _ => false
        };

        if (canApply)
            player.AddExtraRoll();
        
        else if (card.canReturnToDeck)
            _phase.ReturnCardToDeck(card);

        Complete(player, card);
    }
}