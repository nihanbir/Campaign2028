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

    [HideInInspector] public bool isCaptured = false;
    [HideInInspector] public bool isRevealed = false;
}