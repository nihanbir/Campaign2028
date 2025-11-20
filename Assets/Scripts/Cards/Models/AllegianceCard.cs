using System;

[Serializable]
public class AllegianceCard : Card
{
    public override CardType CardType => CardType.Allegiance;
    public AllegianceType allegiance;
}

public enum AllegianceType
{
    USA,
    China
}