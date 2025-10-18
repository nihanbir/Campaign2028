using System;

[Serializable]
public class AllegianceCard : Card
{
    public AllegianceType allegiance;
}

public enum AllegianceType
{
    USA_Democrats,
    USA_Republicans,
    China
}