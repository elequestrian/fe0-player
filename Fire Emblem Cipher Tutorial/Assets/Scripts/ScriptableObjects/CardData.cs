using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu (menuName = "CardData")]
public class CardData : ScriptableObject {

    //unchangeable attributes of each card.
    //These are loaded from a card specific ScriptableObject to save memory as they are shared among one card type.
    //These fields are protected and only accessible via public properties to allow for alterations if need be (alternate names for instance).
    //NOTE: Remeber to update the CardDataEditor if you add anything here so it's displayed in the Inspector.
    public string cardNumber;
    public string charTitle;
    public string charQuote;
    public string cardIllustrator;
    [Multiline]
    public string[] cardSkills;

    public string charName;
    public string classTitle;
    public int deploymentCost;
    public bool canPromote;
    public int promotionCost;
    public bool[] cardColor = new bool[CipherData.NumColors];
    public bool[] charGender = new bool[CipherData.NumGenders];
    public bool[] charWeaponType = new bool[CipherData.NumWeapons];
    public bool[] unitTypes = new bool [CipherData.NumTypes];
    public int baseAttack;
    public int baseSupport;
    public bool[] baseRange = new bool [CipherData.NumRanges];               

   
}

