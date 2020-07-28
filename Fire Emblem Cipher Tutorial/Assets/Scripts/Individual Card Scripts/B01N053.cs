using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N053 : BasicCard {

    /* B01-053HN
     * Chrom, Crown Prince of Ylisse
     * "I must protect everyone."
     * Kaoru Hagiya
     * 
     * Brand of the Exalt [CONT] For each Class Changed ally, this unit gains +10 attack.
     * [ATK] Hero Emblem [SUPP] If the attacking unit is <Blue>, until the end of this battle, the number of orbs this unit’s attack would destroy becomes 2.
     * 
     * Lord
     * 1
     * Blue
     * Male
     * Sword
     * ATK: 40
     * SUPP: 20
     * Range: 1
     */


    private void Awake()
    {
        SetUp();
    }

    //Brand of the Exalt [CONT] For each Class Changed ally, this unit gains +10 attack.
    public override int CurrentAttackValue
    {
        get
        {
            int extraAtk = 0;

            if (Owner.FieldCards.Contains(this))
            {
                List<CardStack> stacks = Owner.FieldStacks;

                for (int i = 0; i < stacks.Count; i++)
                {
                    if (stacks[i].ClassChanged)
                    {
                        extraAtk += 10;
                    }
                }
            }
            
            int totalAttack = BaseAttack + extraAtk + attackModifier + battleModifier;

            //Only activates (and displays) a critical hit if this unit is attacking
            if (GameManager.instance.CriticalHit && GameManager.instance.CurrentAttacker == this)
            {
                totalAttack = totalAttack * 2;
            }

            return totalAttack;
        } }

    //[ATK] Hero Emblem [SUPP] If the attacking unit is <Blue>, until the end of this battle, the number of orbs this unit’s attack would destroy becomes 2.
    public override void ActivateAttackSupportSkill()
    {
        //Checks that the current attacker is indeed Blue
        if (GameManager.instance.CurrentAttacker.CardColorArray[(int)CipherData.ColorsEnum.Blue])
        {
            //increase the number of orbs to be destroyed to 2.
            GameManager.instance.numOrbsToBreak = 2;
        }

        //return control to the battle command.
        GameManager.instance.ActivateDefenderSupport();
    }
}
