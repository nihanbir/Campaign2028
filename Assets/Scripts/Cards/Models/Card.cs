using System;
using System.Collections.Generic;
using UnityEngine;

// Base card class
[Serializable]
public abstract class Card
{
    public string cardName;
    public Sprite artwork;
    public Sprite backSide;
    public abstract CardType CardType { get; }

    [HideInInspector] public bool isCaptured = false;
    [HideInInspector] public bool isRevealed = false;
}

public enum CardType
{
    Actor,
    Allegiance,
    Event,
    Institution,
    State,
}