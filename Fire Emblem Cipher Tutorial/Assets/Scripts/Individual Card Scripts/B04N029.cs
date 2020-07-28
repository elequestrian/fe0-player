using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B04N029 : BasicCard
{
    /*
    * B04-029R
    * Julian: Angel-Searching Thief
    * “If I could see Lena's smiling face just one more time... then I could die happy.”
    * пNekoR
    * 
    * Pass [ACT] [FLIP 1] Until the end of the turn, if this unit is in the Front Line, this unit can attack enemies in the Back Line regardless of range. This skill is only usable if there are 2 or less enemies in the Front Line.
    * The Just Thief's Treasure [TRIGGER] When this unit's attack destroys an enemy and if this unit has used "Pass" this turn, your opponent may choose 1 card from their hand and send it to the Retreat Area. If they do not, you draw 1 card.
    * 
    * Master Thief
    * 3(2)
    * Red
    * Male
    * Sword
    * ATK: 60
    * SUPP: 10
    * Range: 1
    */

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }
}
