using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N028 : BasicCard
{
    /*
    * B01-028R
    * Merric: Gale Sage
    * “Get behind me. I'll face the enemy for us both.”
    * Yoshirou Anbe
    * 
    * Excalibur [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit acquires "Anti-Fliers". (Anti-Fliers [ALWAYS] If this unit is attacking a <Flier>, this unit gains +30 attack.)
    * The Supreme Wind Magic [TRIGGER] When this unit's attack destroys an enemy, if this unit has used "Excalibur" in this turn, draw 1 card.
    * 
    * Sage
    * 4(3)
    * Red
    * Male
    * Tome
    * ATK: 60
    * SUPP: 20
    * Range: 1-2
    */

    private bool excaliburUsed = false; 

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Adds calls to this card's skills when the card enters the field.
    public override void ActivateFieldSkills()
    {
        GameManager.instance.battleDestructionTriggerTracker.AddListener(this);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        GameManager.instance.battleDestructionTriggerTracker.RemoveListener(this);

        RemoveFromFieldEvent.Invoke(this);
    }

    //Excalibur [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit acquires "Anti-Fliers".
    //(Anti-Fliers [ALWAYS] If this unit is attacking a <Flier>, this unit gains +30 attack.)
    protected override bool CheckActionSkillConditions()
    {
        //Verify that Excaliber hasn't already been used this turn and that there are enough bonds to flip.
        if (!excaliburUsed && Owner.FaceUpBonds.Count >= 1)
        {
            return true;
        }
        return false;
    }

    //This is where bond cards are flipped to active Merric's Excalibur.
    protected override void PayActionSkillCost()
    {
        //mark this skill as used.
        excaliburUsed = true;

        //Choose and flip the bonds to activate this effect.
        Owner.ChooseBondsToFlip(1);

        //adds a callback to activate the skill once the bonds have been flipped.
        Owner.FinishBondFlipEvent.AddListener(Excalibur);
    }

    //Excalibur [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit acquires "Anti-Fliers".
    //(Anti-Fliers [ALWAYS] If this unit is attacking a <Flier>, this unit gains +30 attack.)
    private void Excalibur()
    {
        //removes the callback
        Owner.FinishBondFlipEvent.RemoveListener(Excalibur);

        //Adds the Anti-Fliers skill effect to this unit's attacks.
        DeclareAttackEvent.AddListener(AbilitySupport.AntiFliers);

        //display the boost
        CardReader.instance.UpdateGameLog(Owner.playerName + " activates Merric's Excalibur skill! " +
            "Merric: Gale Sage possesses the Anti-Flier's skill until the end of the turn.");
        
        AddToSkillChangeTracker("Excalibur active; Merric possesses the Anti-Fliers skill.");

        //Sets a callback to remove the effect of Excalibur at the end of the turn or if this card is removed from the field.
        Owner.endTurnEvent.AddListener(CancelExcalibur);
        RemoveFromFieldEvent.AddListener(CancelExcalibur);
    }

    //This method cancels the effect of Excalibur at the end of the player's turn or when this card leaves the field.
    private void CancelExcalibur()
    {
        //Remove the Anti-Fliers effect.
        DeclareAttackEvent.RemoveListener(AbilitySupport.AntiFliers);

        //reset the once per turn counter.
        excaliburUsed = false;

        //removes the skill tracking text and callbacks
        RemoveFromSkillChangeTracker("Excalibur active; Merric possesses the Anti-Fliers skill.");
        Owner.endTurnEvent.RemoveListener(CancelExcalibur);
        RemoveFromFieldEvent.RemoveListener(CancelExcalibur);
    }

    //This is an overloaded version of the CancelExcalibur method which allows it to be called from the RemoveFromFieldEvent.
    private void CancelExcalibur(BasicCard superfluous)
    {
        CancelExcalibur();
    }

    //The Supreme Wind Magic [TRIGGER] When this unit's attack destroys an enemy, if this unit has used "Excalibur" in this turn, draw 1 card.
    //Confirms if this unit is the attacker who destroyed the defender and if this unit has used Excalibur this turn.
    public override bool CheckTriggerSkillCondition(BasicCard superfulous)
    {
        //checks that this card is the current attacker and that Excalibur has been used.
        if (GameManager.instance.CurrentAttacker == this && excaliburUsed)
        {
            return true;
        }

        return false;
    }

    //activates the effect of the Supreme Wind Magic skill.
    public override void ActivateTriggerSkill(BasicCard superfluous)
    {
        //Report the skill's effect.
        CardReader.instance.UpdateGameLog(GameManager.instance.CurrentDefender.CharName 
            + "'s destruction activates Merric's The Supreme Wind Magic skill!");

        //Draw 1 card.  Draws are already reported in the CardManager logic.
        Owner.Draw(1);
    }
}
