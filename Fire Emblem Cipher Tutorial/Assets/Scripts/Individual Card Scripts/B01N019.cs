using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N019 : BasicCard
{
    /*
    * B01-019N
    * Bord: Talysian Warrior
    * “Hoy, I'm not Cord! I'm Bord!”
    * HACCAN
    * 
    * Fighter's Expertise [ALWAYS] During your turn, this unit gains +20 attack.
    * [ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    * 
    * Fighter
    * 1
    * Red
    * Male
    * Axe
    * ATK: 30
    * SUPP: 10
    * Range: 1
    */

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Fighter's Expertise [ALWAYS] During your turn, this unit gains +20 attack.
    public override int ExpectedAttackValue
    {
        get
        {
            //if it is the agent's turn, attack is boosted.
            if (GameManager.instance.turnAgent == DM)
                return BaseAttack + 20;
            else
                return BaseAttack;
        }
    }

    //Adds calls to this card's skills when the card enteres the field.
    public override void ActivateFieldSkills()
    {
        //confirm it's the owner's turn, and if so, then activate FightersExpertise.
        if (GameManager.instance.turnPlayer == Owner)
        {
            CardReader.instance.UpdateGameLog("Bord's Fighter's Expertise skill provides +20 attack during your turn!");
            FightersExpertise();
        }

        //set up the callbacks for Fighter's Expertise.
        Owner.BeginTurnEvent.AddListener(FightersExpertise);
        Owner.endTurnEvent.AddListener(CancelFightersExpertise);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        //confirm it's the owner's turn, and if so, then cancel Fighter's Expertise.
        if (GameManager.instance.turnPlayer == Owner)
        {
            CancelFightersExpertise();
        }

        //removes the callbacks
        Owner.BeginTurnEvent.RemoveListener(FightersExpertise);
        Owner.endTurnEvent.RemoveListener(CancelFightersExpertise);

        RemoveFromFieldEvent.Invoke(this);
    }

    //Fighter's Expertise [ALWAYS] During your turn, this unit gains +20 attack.
    private void FightersExpertise()
    {
        //buff attack
        attackModifier += 20;

        //Report the change in the tracker.
        AddToSkillChangeTracker("Fighter's Expertise skill providing +20 attack.");
    }

    //Removes the boost from Fighter's Expertise.
    private void CancelFightersExpertise()
    {
        //removes the attack buff.
        attackModifier -= 20;

        //Remove the report from the skill tracker.
        RemoveFromSkillChangeTracker("Fighter's Expertise skill providing +20 attack.");
    }

    //[ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.AttackEmblem();
    }
}
