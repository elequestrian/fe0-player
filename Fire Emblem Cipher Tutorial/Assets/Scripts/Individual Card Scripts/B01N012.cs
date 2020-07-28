using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N012 : BasicCard
{
    /*
    * B01-012N
    * Draug: Guardian Knight
    * “I will protect you... With my very life, if need be.”
    * Homakura
    * 
    * Armor Expertise [ALWAYS] If this unit is being attacked by a non-<Tome> unit, this unit gains +20 attack.
    * [DEF] Defense Emblem [SUPP] Until the end of this combat, your defending unit gains +20 attack.
    * 
    * Knight
    * 1
    * Red
    * Male
    * Lance
    * Armor
    * ATK: 30
    * SUPP: 10
    * Range: 1
    */

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Adds calls to this card's skills when the card enteres the field.
    public override void ActivateFieldSkills()
    {
        DeclareAttackEvent.AddListener(ArmorExpertise);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        DeclareAttackEvent.RemoveListener(ArmorExpertise);

        RemoveFromFieldEvent.Invoke(this);
    }

    //Armor Expertise [ALWAYS] If this unit is being attacked by a non-<Tome> unit, this unit gains +20 attack.
    //NOTE: Consider putting this ability in the AbilitySupport Class.
    private void ArmorExpertise(bool attacking)
    {
        //check that the calling unit is being attacked.
        if (!attacking)
        {
            //check that the attacker doesn't have a <Tome> affinity.
            if (!GameManager.instance.CurrentAttacker.CharWeaponArray[(int)CipherData.WeaponsEnum.Tome])
            {
                //raises attack
                GameManager.instance.CurrentDefender.attackModifier += 20;

                //reports the boost
                CardReader.instance.UpdateGameLog(GameManager.instance.CurrentDefender.CharName + "'s Armor Expertise skill provides" +
                    " +20 attack against non-<Tome> attacks!");
                GameManager.instance.CurrentDefender.AddToSkillChangeTracker("Armor Expertise providing +20 attack.");
                GameManager.instance.CurrentDefender.AfterBattleEvent.AddListener(EndArmorExpertise);
            }
        }
    }

    //Removes the boost and callbacks/display of Armor Expertise after the battle is over.
    private void EndArmorExpertise()
    {
        //lowers attack
        GameManager.instance.CurrentDefender.attackModifier -= 20;

        //removes display and callback
        GameManager.instance.CurrentDefender.RemoveFromSkillChangeTracker("Armor Expertise providing +20 attack.");
        GameManager.instance.CurrentDefender.AfterBattleEvent.RemoveListener(EndArmorExpertise);
    }

    //[DEF] Defense Emblem [SUPP] Until the end of this combat, your defending unit gains +20 attack.
    public override void ActivateDefenseSupportSkill()
    {
        AbilitySupport.DefenseEmblem();
    }
}
