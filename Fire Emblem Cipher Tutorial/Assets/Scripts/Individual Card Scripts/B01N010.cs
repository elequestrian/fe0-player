using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N010 : BasicCard
{
    /*
    * B01-010N
    * Abel: The Green Knight
    * “I will protect my prince... I gave Princess Elice my solemn word that I would.”
    * Raita Kazama
    * 
    * Green and Red Bond [ALWAYS] If this unit is being supported by "Cain", this unit gains +30 attack.
    * [ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    * 
    * Cavalier
    * 1
    * Red
    * Male
    * Lance
    * Beast
    * ATK: 40
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
        BattleSupportEvent.AddListener(GreenRedBond);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        BattleSupportEvent.RemoveListener(GreenRedBond);

        RemoveFromFieldEvent.Invoke(this);
    }

    //Green and Red Bond [ALWAYS] If this unit is being supported by "Cain", this unit gains +30 attack.
    //called by the BattleSupportEvent, this method checks if there is a supporting card and if it is named "Cain", and if so activates the ability.
    private void GreenRedBond()
    {
        if (Owner.SupportCard != null && Owner.SupportCard.CompareNames("Cain"))
        {
            //Display the ability
            CardReader.instance.UpdateGameLog("Abel's Green and Red Bond skill raises his attack by +30 when supported by Cain!");
            AddToSkillChangeTracker("Abel's Green and Red Bond skill providing +30 attack for this battle.");

            //raise Abel's attack
            attackModifier += 30;

            //set up the removal callback
            AfterBattleEvent.AddListener(CancelGreenRedBond);
        }
    }

    //Cancels the effect of the Green and Red Bond skill
    private void CancelGreenRedBond()
    {
        //remove the skill display
        RemoveFromSkillChangeTracker("Abel's Green and Red Bond skill providing +30 attack for this battle.");

        //restore Abel's attack
        attackModifier -= 30;

        //removes the callback
        AfterBattleEvent.RemoveListener(CancelGreenRedBond);
    }

    //[ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.AttackEmblem();
    }
}
