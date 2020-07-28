using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class S01N005 : BasicCard
{
    /*
     * S01-005ST
     * Navarre: Wielder of a Killing Edge
     * “Will it be tears or blood you weep first?”
     * yuma
     * 
     * Killing Edge [ACT] [FLIP 3] Until the end of the turn, this card's attacks cannot be evaded.
     * Aloof Swordsman [ALWAYS] If you have no allies other than this unit and your Main Character, this unit gains +10 attack.
     * 
     * Swordmaster
     * 3(2)
     * Red
     * Male
     * Sword
     * ATK: 60
     * SUPP: 10
     * Range: 1
     */

    private bool aloofSwordsmanActive = false;

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //This method activates all skills that can be triggered while the card is on the field.
    //Always skills like Aloof Swordsman have a higher priority than say deployment trigger abilites.
    public override void ActivateFieldSkills()
    {
        Owner.FieldChangeEvent.AddListener(CheckForAloofSwordsman);
    }

    //This method is used to remove skills from event calls when the card is "disabled" (under a stack) and/or removed from the field.
    public override void DeactivateFieldSkills()
    {
        Owner.FieldChangeEvent.RemoveListener(CheckForAloofSwordsman);

        DisableAloofSwordsman();

        RemoveFromFieldEvent.Invoke(this);
    }

    //Killing Edge [ACT] [FLIP 3] Until the end of the turn, this card's attacks cannot be evaded.
    protected override bool CheckActionSkillConditions()
    {
        //Verify there are at least three available bonds.
        if (Owner.FaceUpBonds.Count >= 3)
        {
            return true;
        }
        else
            return false;
    }

    //This is where the bond cards to be flipped will be chosen and the effect activated.
    protected override void PayActionSkillCost()
    {
        //Choose and flip the bonds to activate this effect.
        Owner.ChooseBondsToFlip(3);

        //adds a callback to activate the skill once the bonds have been flipped.
        Owner.FinishBondFlipEvent.AddListener(ActivateKillingEdge);
    }

    //Killing Edge [ACT] [FLIP 3] Until the end of the turn, this card's attacks cannot be evaded.
    //This is the method that gets called once the bond flip is finished.
    private void ActivateKillingEdge()
    {
        //removes the callback
        Owner.FinishBondFlipEvent.RemoveListener(ActivateKillingEdge);

        //updates the game log
        CardReader.instance.UpdateGameLog(Owner.playerName + " activates Navarre's Killing Edge skill! " +
            "Navarre's attacks cannot be avoided this turn.");

        //set up the call to ensure that Navarre's attacks cannot be evaded.
        DeclareAttackEvent.AddListener(KillingEdge);

        //Displays the effect in the skill tracker.
        AddToSkillChangeTracker("Navarre's Killing Edge active. The opponent cannot evade his attacks.");

        //set up the cancel for this skill at the end of the turn and if the card is removed from the field.
        Owner.endTurnEvent.AddListener(CancelKillingEdge);
        RemoveFromFieldEvent.AddListener(CancelKillingEdge);
    }

    //This method removes the defender's ability to evade if Navarre is attacking.
    private void KillingEdge(bool attacking)
    {
        //Only activates when this unit is attacking.
        if (attacking)
        {
            GameManager.instance.PreventEvade();
        }
    }

    //This method cancels the effect of Killing Edge at the end of the player's turn or when this card leaves the field.
    private void CancelKillingEdge()
    {
        //removes the cannot be evaded clause from Navarre's attacks.
        DeclareAttackEvent.RemoveListener(KillingEdge);

        //removes the skill tracking text and callbacks
        RemoveFromSkillChangeTracker("Navarre's Killing Edge active. The opponent cannot evade his attacks.");
        Owner.endTurnEvent.RemoveListener(CancelKillingEdge);
        RemoveFromFieldEvent.RemoveListener(CancelKillingEdge);
    }

    //This is an overloaded version of the CancelKillingEdge method which allows it to be called from
    //the RemoveFromFieldEvent.  This structure means that if a person activates Killing Edge multiple times (dumb, but possible),
    //the removals will be handled correctly.
    private void CancelKillingEdge(BasicCard superfluous)
    {
        CancelKillingEdge();
    }

    //Aloof Swordsman [ALWAYS] If you have no allies other than this unit and your Main Character, this unit gains +10 attack.
    //This method checks to see if we need to activate or deactivate this skill.
    private void CheckForAloofSwordsman()
    {
        //check if there is no more than 1 other ally on the field
        if (OtherAllies.Count <= 1)
        {
            //If there is another ally confirm that it is the MC.
            if (OtherAllies.Count == 1)
            {
                if (OtherAllies[0] != Owner.MCCard)
                {
                    DisableAloofSwordsman();
                    return;
                }
            }

            //should only reach this point if all allies are either the MC or Navarre.
            ActivateAloofSwordsman();
        }
        else
        {
            DisableAloofSwordsman();
        }
    }

    //Aloof Swordsman [ALWAYS] If you have no allies other than this unit and your Main Character, this unit gains +10 attack.
    //actually activates the Aloof Swordsman effect if needed.
    private void ActivateAloofSwordsman()
    {
        if (!aloofSwordsmanActive)
        {
            aloofSwordsmanActive = true;
            attackModifier += 10;
            AddToSkillChangeTracker("Navarre's Aloof Swordsman skill providing +10 attack.");
            CardReader.instance.UpdateGameLog("Navarre's Aloof Swordsman skill raises his attack by +10 as long as he " +
                "and the MC are the only cards on the field!");
        }
    }


    //deactivates the Aloof Swordsman effect if needed.
    private void DisableAloofSwordsman()
    {
        if (aloofSwordsmanActive)
        {
            aloofSwordsmanActive = false;
            attackModifier -= 10;
            RemoveFromSkillChangeTracker("Navarre's Aloof Swordsman skill providing +10 attack.");
        }
    }
}
