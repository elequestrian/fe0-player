using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class S01N004 : BasicCard
{

    /*
     * S01-004ST
     * Ogma: Wielder of a Heavy Blade
     * “I figured we'd have to settle the score sooner or later...”
     * Akira Fujikawa
     * 
     * Captain of the Royal Talysian Army [TRIGGER] Each time an ally with a Deployment Cost of 2 or lower is deployed, until the end of the turn, this unit and that ally gain +10 attack.
     * Levin Sword [ACT] [FLIP 1] Until the end of the turn, this unit loses -10 attack and acquires <Tome> affinity and range 1-2.
     * 
     * Hero
     * 3(2)
     * Red
     * Male
     * Sword
     * ATK: 60
     * SUPP: 10
     * Range: 1
     */

    private List<BasicCard> buffedAllies = new List<BasicCard>(10);
    private int timesBuffed = 0;
    private bool levinSwordActive = false;

    //Levin Sword [ACT] [FLIP 1] Until the end of the turn, this unit loses -10 attack and acquires <Tome> affinity and range 1-2.
    public override bool[] CharWeaponArray
    {
        get
        {
            //if the Levin Sword skill is active, then this card acquires <Tome> affinity in addition to its existing weapon types.
            if (levinSwordActive)
            {
                bool[] weaponArray = (bool[])base.CharWeaponArray.Clone();

                weaponArray[(int)CipherData.WeaponsEnum.Tome] = true;

                return weaponArray;
            }
            else
            {
                return base.CharWeaponArray;
            }
        }
    }

    public override bool[] BaseRangeArray
    {
        get
        {
            //if the Levin Sword skill is active, then this card acquires range 1-2 in addition to its exisiting range.
            if (levinSwordActive)
            {
                bool[] rangeArray = (bool[])base.BaseRangeArray.Clone();

                rangeArray[(int)CipherData.RangesEnum.Range1] = true;
                rangeArray[(int)CipherData.RangesEnum.Range2] = true;

                return rangeArray;
            }
            else
            {
                return base.BaseRangeArray;
            }
        }
    }


    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Adds calls to this card's skills when the card enteres the field.
    public override void ActivateFieldSkills()
    {
        Owner.deployTriggerTracker.AddListener(this);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        Owner.deployTriggerTracker.RemoveListener(this);

        RemoveOgmaBuffFromHimself();

        RemoveFromFieldEvent.Invoke(this);
    }


    //Captain of the Royal Talysian Army [TRIGGER] Each time an ally with a Deployment Cost of 2 or lower is deployed, until the end of the turn, this unit and that ally gain +10 attack.
    //Checks to see if a cost 2 or lower ally has been deployed 
    public override bool CheckTriggerSkillCondition(BasicCard triggeringCard)
    {
        if (triggeringCard.DeploymentCost <= 2)
        {
            return true;
        }

        return false;
    }

    //Activates Ogma's Captain of the Royal Talysian Army buffing skill. (No choice)
    public override void ResolveTriggerSkillLP(BasicCard triggeringCard)
    {
        //displays the ability on the Game Log
        CardReader.instance.UpdateGameLog("\nOgma's Captain of the Royal Talysian Army skill raises the attack of himself and " 
            + triggeringCard.CharName + " by +10 until the end of the turn!");

        //remember the skill's target for future use.
        buffedAllies.Add(triggeringCard);

        //Adds 10 to the triggering card's attack and Ogma's till the end of the turn.
        triggeringCard.attackModifier += 10;
        attackModifier += 10;

        //display the boost on the target.
        triggeringCard.AddToSkillChangeTracker("Ogma's Captain of the Royal Talysian Army skill providing +10 attack.");

        //update Ogma's skill display.
        RemoveFromSkillChangeTracker("Ogma's Captain of the Royal Talysian Army skill providing +" + timesBuffed + "0 attack.");
        timesBuffed++;
        AddToSkillChangeTracker("Ogma's Captain of the Royal Talysian Army skill providing +" + timesBuffed + "0 attack.");
        
        //Sets up the removal methods at the end of the player's turn or if a buffed card leaves the field.
        Owner.endTurnEvent.AddListener(RemoveAllOgmaBuffs);
        triggeringCard.RemoveFromFieldEvent.AddListener(RemoveOgmaBuffFromAnother);
        
        //returns control to the deployTriggerTracker to recheck conditions and activate any remaining abilities.
        Owner.deployTriggerTracker.RecheckTrigger();
    }

    //This method undoes the Captain of the Royal Talysian Army skill buff for all impacted units still on the field at the end of the turn.
    //NOTE: There might be issues if this card is removed from the field or disabled through level up.
    //I'm not sure how Event callbacks work when the listening GameObject is disabled...
    private void RemoveAllOgmaBuffs()
    {
        //check if Ogma is still on the field before removing his buff.
        if (Owner.FieldCards.Contains(this))
        {
            RemoveOgmaBuffFromHimself();
        }
        
        //cycle through all the buffedAllies to remove their buffs one by one.
        while (buffedAllies.Count > 0)
        {
            RemoveOgmaBuffFromAnother(buffedAllies[0]);
        }

        //remove the callback.
        Owner.endTurnEvent.RemoveListener(RemoveAllOgmaBuffs);       
    }

    //This helper method removes the Captain of the Royal Talysian Army skill buff from Ogma himself
    //either at the end of the turn or when this card is removed from the field.
    private void RemoveOgmaBuffFromHimself()
    {
        //remove the buff
        int buff = timesBuffed * 10;
        attackModifier -= buff;

        //remove the display
        RemoveFromSkillChangeTracker("Ogma's Captain of the Royal Talysian Army skill providing +" + timesBuffed + "0 attack.");

        //resets the timesBuffed counter.
        timesBuffed = 0;
    }

    //This helper method removes the Captain of the Royal Talysian Army skill buff from a particular ally
    //either at the end of the turn or when that card is removed from the field.
    private void RemoveOgmaBuffFromAnother(BasicCard buffedAlly)
    {
        //removes the buff and skill display
        buffedAlly.attackModifier -= 10;
        buffedAlly.RemoveFromSkillChangeTracker("Ogma's Captain of the Royal Talysian Army skill providing +10 attack.");

        //remove the callback
        buffedAlly.RemoveFromFieldEvent.RemoveListener(RemoveOgmaBuffFromAnother);

        //remove the ally from the list of buffedAllies
        buffedAllies.Remove(buffedAlly);
    }

    //Levin Sword [ACT] [FLIP 1] Until the end of the turn, this unit loses -10 attack and acquires <Tome> affinity and range 1-2.
    protected override bool CheckActionSkillConditions()
    {
        //Verify there is at least one available bond.
        if (Owner.FaceUpBonds.Count >= 1)
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
        Owner.ChooseBondsToFlip(1);

        //adds a callback to activate the skill once the bonds have been flipped.
        Owner.FinishBondFlipEvent.AddListener(ActivateLevinSword);
    }

    //Levin Sword [ACT] [FLIP 1] Until the end of the turn, this unit loses -10 attack and acquires <Tome> affinity and range 1-2.
    //This is the method that gets called once the bond flip is finished.
    private void ActivateLevinSword()
    {
        //removes the callback
        Owner.FinishBondFlipEvent.RemoveListener(ActivateLevinSword);

        //updates the game log
        CardReader.instance.UpdateGameLog(DM.PlayerName + " activates Ogma's Levin Sword skill! " +
            "Ogma gains <Tome> affinity, 1-2 range, and -10 attack.");

        //reduces attack by 10, adds the affinity and Range bonus, and displays effect in the skill tracker.
        attackModifier -= 10;
        levinSwordActive = true;
        AddToSkillChangeTracker("Ogma's Levin Sword providing -10 attack, <Tome> affinity, and 1-2 range.");

        //set up the cancel for this skill at the end of the turn and if the card is removed from the field.
        Owner.endTurnEvent.AddListener(CancelLevinSword);
        RemoveFromFieldEvent.AddListener(CancelLevinSword);
    }

    //This method cancels the effect of Levin Sword at the end of the player's turn or when this card leaves the field.
    private void CancelLevinSword()
    {
        //removes the affinity, range boost, and attack penalty.
        levinSwordActive = false;
        attackModifier += 10;

        //removes the skill tracking text and callbacks
        RemoveFromSkillChangeTracker("Ogma's Levin Sword providing -10 attack, <Tome> affinity, and 1-2 range.");
        Owner.endTurnEvent.RemoveListener(CancelLevinSword);
        RemoveFromFieldEvent.RemoveListener(CancelLevinSword);
    }

    //This is an overloaded version of the CancelLevinSword method which is used to allow CancelLevinSword to be called from
    //the RemoveFromFieldEvent.  This structure means that if a person activates Levin Sword multiple times (dumb, but possible),
    //the removals will be handled correctly.
    private void CancelLevinSword(BasicCard superfluous)
    {
        CancelLevinSword();
    }

}
