using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//This is a static data class for other classes to represent which lays out foundational data about the Cipher card game.
public static class CipherData 
{
    //NOTE: Be sure to change the size of the below arrays in the exisiting card data as well as this information
    //if a size change is needed.  Otherwise, errors may occur.

    public static int NumColors = 8;
    public enum ColorsEnum { Red, Blue, White, Black, Green, Purple, Yellow, Brown }

    public static int NumGenders = 2;
    public enum GendersEnum { Male, Female}

    public static int NumWeapons = 10;
    public enum WeaponsEnum { Sword, Lance, Axe, Bow, Tome, Staff, Brawl, Dragonstone, Knife, Fang}

    public static int NumTypes = 6;
    public enum TypesEnum { Armored, Flier, Beast, Dragon, Mirage, Monster}

    public static int NumRanges = 3;
    public enum RangesEnum { Range1, Range2, Range3}

    //This enum keeps track of the current phase in the game.
    public enum PhaseEnum { Beginning, Bond, Deployment, Action, End}
}

[System.Serializable]
public class MyBasicCardEvent : UnityEvent<BasicCard>
{
}

[System.Serializable]
public class MyBoolEvent : UnityEvent<bool>
{
}