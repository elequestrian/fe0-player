using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class S01N003 : BasicCard {

    /*
     * S01-003ST
     * Jagen: Faithful Veteran
     * “Sire, it gladdens me to see you in one piece.”
     * Azusa
     * 
     * Paladin
     * 3(2)
     * Red
     * Male
     * Lance
     * Affinity: Beast
     * ATK: 70
     * SUP: 20
     * Range: 1
     * 
     * Battlefield Mentor [SPECIAL] This card is unable to be played in the Bond Area.
     * 
     */

    void Awake()
    {
        SetUp();
    }


    //Battle-worn Veteran [SPECIAL] This card cannot be placed in the Bond Area.
    public override bool Bondable
    {
        get
        {
            //CardReader.instance.UpdateGameLog(CharName + " cannot be placed in the Bond Area.");
            return false;
        }
    }
}
