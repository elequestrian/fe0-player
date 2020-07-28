using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N008 : BasicCard
{
    /*
     * B01-008N
     * Cain: The Red Knight
     * “Glory to Altea, land of my birth!”
     * Aoji
     * 
     * Red and Green Bond [ALWAYS] If this unit is being supported by "Abel", this unit gains +30 attack.
     * [ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
     * 
     * Cavalier
     * 1
     * Red
     * Male
     * Sword
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
        BattleSupportEvent.AddListener(RedGreenBond);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        BattleSupportEvent.RemoveListener(RedGreenBond);

        RemoveFromFieldEvent.Invoke(this);
    }

    //Red and Green Bond [ALWAYS] If this unit is being supported by "Abel", this unit gains +30 attack.
    //called by the BattleSupportEvent, this method checks if there is a supporting card and if it is named "Abel", and if so activates the ability.
    private void RedGreenBond()
    {
        if (Owner.SupportCard != null && Owner.SupportCard.CompareNames("Abel"))
        {
            //Display the ability
            CardReader.instance.UpdateGameLog("Cain's Red and Green Bond skill raises his attack by +30 when supported by Abel!");
            AddToSkillChangeTracker("Cain's Red and Green Bond skill providing +30 attack for this battle.");

            //raise Cain's attack
            attackModifier += 30;

            //set up the removal callback
            AfterBattleEvent.AddListener(CancelRedGreenBond);
        }
    }

    //Cancels the effect of the Red and Green Bond skill
    private void CancelRedGreenBond()
    {
        //remove the skill display
        RemoveFromSkillChangeTracker("Cain's Red and Green Bond skill providing +30 attack for this battle.");

        //restore Cain's attack
        attackModifier -= 30;

        //removes the callback
        AfterBattleEvent.RemoveListener(CancelRedGreenBond);
    }

    //[ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.AttackEmblem();
    }
}
