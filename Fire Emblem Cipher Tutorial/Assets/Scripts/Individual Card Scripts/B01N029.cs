using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N029 : BasicCard
{
    /*
    * B01-029N
    * Merric: Wind Mage
    * “Twould be a passing shame if you didn't get to see me flex a little magic muscle. Wait till I show you my latest: Excalibur!”
    * Yoshirou Anbe
    * 
    * Excalibur [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit acquires "Anti-Fliers". (Anti-Fliers [ALWAYS] If this unit is attacking a <Flier>, this unit gains +30 attack.)
    * [ATK] Magic Emblem [SUPP] Draw 1 card. Choose 1 card from your hand, and send it to the Retreat Area.
    * 
    * Mage
    * 1
    * Red
    * Male
    * Tome
    * ATK: 30
    * SUPP: 20
    * Range: 1-2
    */

    private bool excaliburUsed = false;

    // Use this for initialization
    void Awake()
    {
        SetUp();
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
        CardReader.instance.UpdateGameLog(DM.PlayerName + " activates Merric's Excalibur skill! " +
            "Merric: Wind Mage possesses the Anti-Flier's skill until the end of the turn.");

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

    //[ATK] Magic Emblem [SUPP] Draw 1 card. Choose 1 card from your hand, and send it to the Retreat Area.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.MagicEmblem();
    }
}
