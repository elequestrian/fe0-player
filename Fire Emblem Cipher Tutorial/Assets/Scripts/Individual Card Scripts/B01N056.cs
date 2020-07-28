using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N056 : BasicCard
{
    /*
     * S01-056HN
     * Lucina: Swordswoman Calling Herself Marth
     * "You may call me Marth."
     * Yusuke Kozaki
     * 
     * Lord
     * 1
     * Blue
     * Female
     * Sword
     * 
     * ATK: 40
     * SUP: 20
     * Range: 1
     * 
     * The Name of the Hero-King [SPECIAL] Treat this card as if its Unit Name is also "Marth".
     * 
     * Parallel Falchion [ALWAYS] If this unit is attacking a <Dragon> unit, this unit gains +20 attack.
     * 
     * [ATK] Hero Emblem [SUPP] Until the end of this combat, the number of Orbs that your <Blue> attacking unit's attack will destroy becomes 2.
     * 
     */

    void Awake()
    {
        SetUp();

    }

    //The Name of the Hero-King [SPECIAL] Treat this card as if its Unit Name is also "Marth".
    //Since all overloads refer to the string parameter CompareNames method, I'll just update that one. 
    public override bool CompareNames(string name)
    {
        return name.Equals(CharName) || name.Equals("Marth");
    }

    //NEED TO ADD: Parallel Falchion and Hero Emblem
}
