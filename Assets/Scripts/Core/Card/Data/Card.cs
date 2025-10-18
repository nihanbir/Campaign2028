using System;
using System.Collections.Generic;
using UnityEngine;

// Base card class
[Serializable]
public abstract class Card
{
    public string cardName;
    public string description;
    public Sprite artwork;
}
