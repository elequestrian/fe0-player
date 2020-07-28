using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N014 : BasicCard
{
    /*
    * B01-014N
    * Gordin: Archer of the Liberators
    * “I won't miss... Not at this range!”
    * Kokon Konfuzi
    * 
    * Steel Bow [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    * Anti-Fliers [ALWAYS] If this unit is attacking a <Flier>, this unit gains +30 attack.
    * [ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    * 
    * Archer
    * 1
    * Red
    * Male
    * Bow
    * ATK: 30
    * SUPP: 20
    * Range: 2
    */

    private bool steelBowUsable = true;

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Adds calls to this card's skills when the card enters the field.
    public override void ActivateFieldSkills()
    {
        DeclareAttackEvent.AddListener(AbilitySupport.AntiFliers);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        DeclareAttackEvent.RemoveListener(AbilitySupport.AntiFliers);

        RemoveFromFieldEvent.Invoke(this);
    }

    //Steel Bow [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    protected override bool CheckActionSkillConditions()
    {
        //Verify that Steel Bow is usable.
        if (steelBowUsable)
        {
            //Check there there are enough bonds to use the skill.
            if (Owner.FaceUpBonds.Count >= 1)
            {
                return true;
            }
        }
        return false;
    }

    //This is where the bond cards to be flipped will be chosen and the effect activated.
    protected override void PayActionSkillCost()
    {
        //Choose and flip the bonds to activate this effect.
        Owner.ChooseBondsToFlip(1);

        //adds a callback to activate the skill once the bonds have been flipped.
        Owner.FinishBondFlipEvent.AddListener(ActivateSteelBow);
    }

    //Steel Bow [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    private void ActivateSteelBow()
    {
        //removes the callback
        Owner.FinishBondFlipEvent.RemoveListener(ActivateSteelBow);

        //updates the game log
        CardReader.instance.UpdateGameLog(Owner.playerName + " activates Gordin's Steel Bow skill! " +
            "Gordin's attack increases by +10 until the end of the turn.");

        //increases attack
        attackModifier += 10;

        //prevents reuse this turn.
        steelBowUsable = false;

        //Displays the effect in the skill tracker.
        AddToSkillChangeTracker("Gordin's Steel Bow providing +10 attack.");

        //set up the cancel for this skill at the end of the turn and if the card is removed from the field.
        Owner.endTurnEvent.AddListener(CancelSteelBow);
        RemoveFromFieldEvent.AddListener(CancelSteelBow);
    }

    //This method cancels the effect of Steel Bow at the end of the player's turn or when this card leaves the field.
    private void CancelSteelBow()
    {
        //decreases attack.
        attackModifier -= 10;

        //resets the Once Per Turn ability
        steelBowUsable = true;

        //removes the skill tracking text and callbacks
        RemoveFromSkillChangeTracker("Gordin's Steel Bow providing +10 attack.");
        Owner.endTurnEvent.RemoveListener(CancelSteelBow);
        RemoveFromFieldEvent.RemoveListener(CancelSteelBow);
    }

    //This is an overloaded version of the CancelSteelBow method which allows it to be called from the RemoveFromFieldEvent.
    private void CancelSteelBow(BasicCard superfluous)
    {
        CancelSteelBow();
    }

    //[ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.AttackEmblem();
    }
}
