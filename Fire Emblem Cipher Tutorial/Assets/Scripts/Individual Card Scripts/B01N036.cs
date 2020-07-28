using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N036 : BasicCard
{
    /*
    * B01-036N
    * Linde: Miloah's Child
    * “I want to destroy Gharnef and avenge my father myself!”
    * Tetsu Kurosawa
    * 
    * Thunder [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    * [ATK] Magic Emblem [SUPP] Draw 1 card. Choose 1 card from your hand, and send it to the Retreat Area.
    * 
    * Mage
    * 1
    * Red
    * Female
    * Tome
    * ATK: 30
    * SUPP: 20
    * Range: 1-2
    */

    private bool thunderUsable = true;

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Thunder [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    protected override bool CheckActionSkillConditions()
    {
        //Verify that Thunder is usable and that there are enough bonds to use the skill.
        if (thunderUsable && Owner.FaceUpBonds.Count >= 1)
        {
            return true;
        }
        return false;
    }

    //This is where the bond cards to be flipped will be chosen and the effect activated.
    protected override void PayActionSkillCost()
    {
        //Choose and flip the bonds to activate this effect.
        Owner.ChooseBondsToFlip(1);

        //adds a callback to activate the skill once the bonds have been flipped.
        Owner.FinishBondFlipEvent.AddListener(ActivateThunder);
    }

    //Thunder [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    private void ActivateThunder()
    {
        //removes the callback
        Owner.FinishBondFlipEvent.RemoveListener(ActivateThunder);

        //updates the game log
        CardReader.instance.UpdateGameLog(Owner.playerName + " activates Linde's Thunder skill! " +
            "Linde's attack increases by +10 until the end of the turn.");

        //increases attack
        attackModifier += 10;

        //prevents reuse this turn.
        thunderUsable = false;

        //Displays the effect in the skill tracker.
        AddToSkillChangeTracker("Linde's Thunder providing +10 attack.");

        //set up the cancel for this skill at the end of the turn and if the card is removed from the field.
        Owner.endTurnEvent.AddListener(CancelThunder);
        RemoveFromFieldEvent.AddListener(CancelThunder);
    }

    //This method cancels the effect of Thunder at the end of the player's turn or when this card leaves the field.
    private void CancelThunder()
    {
        //decreases attack.
        attackModifier -= 10;

        //resets the Once Per Turn ability tracker
        thunderUsable = true;

        //removes the skill tracking text and callbacks
        RemoveFromSkillChangeTracker("Linde's Thunder providing +10 attack.");
        Owner.endTurnEvent.RemoveListener(CancelThunder);
        RemoveFromFieldEvent.RemoveListener(CancelThunder);
    }

    //This is an overloaded version of the CancelThunder method which allows it to be called from the RemoveFromFieldEvent.
    private void CancelThunder(BasicCard superfluous)
    {
        CancelThunder();
    }

    //[ATK] Magic Emblem [SUPP] Draw 1 card. Choose 1 card from your hand, and send it to the Retreat Area.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.MagicEmblem();
    }
}
