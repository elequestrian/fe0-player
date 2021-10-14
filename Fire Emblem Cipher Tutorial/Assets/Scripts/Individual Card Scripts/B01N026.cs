using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N026 : BasicCard
{
    /*
    * B01-026N
    * Lena: Angel to All
    * “You go on ahead. Save yourself!”
    * Fumi
    * 
    * Heal [ACT] [TAP, FLIP 2] Choose 1 non-"Lena" card from your Retreat Area, and add it to your hand.
    * Bond with Julian [ALWAYS] Allied "Julian" gains +10 attack.
    * [DEF] Miracle Emblem [SUPP] Until the end of this combat, your opponent's attacking unit cannot perform a Critical Hit.
    * 
    * Cleric
    * 1
    * Red
    * Female
    * Staff
    * ATK: 20
    * SUPP: 20
    * No Range
    */

    private BasicCard buffedJulian;

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Adds calls to this card's skills when the card enters the field.
    public override void ActivateFieldSkills()
    {
        Owner.FieldChangeEvent.AddListener(CheckForJulian);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        Owner.FieldChangeEvent.RemoveListener(CheckForJulian);

        //removes the buff from any allied Julian when Lena leaves the field.
        if (buffedJulian != null)
            RemoveLenaBuffFromAlly(buffedJulian);
        
        RemoveFromFieldEvent.Invoke(this);
    }

    //Heal [ACT] [TAP, FLIP 2] Choose 1 non-"Lena" card from your Retreat Area, and add it to your hand.
    protected override bool CheckActionSkillConditions()
    {
        //Verify that Heal is usable: Lena is untapped, there are enough bonds, and there are cards in the Retreat.
        if (!Tapped && Owner.FaceUpBonds.Count >= 2 && Owner.Retreat.Count >= 0)
        {
            //Ensure there is at least one non-"Lena" card to target.
            List<BasicCard> theRetreat = Owner.Retreat;

            foreach (BasicCard ally in theRetreat)
            {
                if (!CompareNames(ally))
                {
                    return true;
                }
            }
        }
        return false;
    }

    //This is where Lena is tapped and the bond cards to be flipped are chosen.
    protected override void PayActionSkillCost()
    {
        Tap();
        
        //Choose and flip the bonds to activate this effect.
        Owner.ChooseBondsToFlip(2);

        //adds a callback to activate the skill once the bonds have been flipped.
        Owner.FinishBondFlipEvent.AddListener(ChooseHealTarget);
    }

    //Heal [ACT] [TAP, FLIP 2] Choose 1 non-"Lena" card from your Retreat Area, and add it to your hand.
    private void ChooseHealTarget()
    {
        //removes the callback
        Owner.FinishBondFlipEvent.RemoveListener(ChooseHealTarget);

        //determines the possible targets for heal
        List<BasicCard> theRetreat = Owner.Retreat;
        List<BasicCard> targets = new List<BasicCard>(theRetreat.Count);

        foreach (BasicCard ally in theRetreat)
        {
            //all targets must not share Lena's name.
            if (!CompareNames(ally))
            {
                targets.Add(ally);
            }
        }

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(Heal);

        //makes the player choose a card from their retreat to heal.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = targets,
            numberOfCardsToPick = 1,
            locationText = DM.PlayerName + "'s Retreat",
            instructionText = "Please choose one card to return to your hard using Lena's Heal skill.",
            mayChooseLess = false,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    private void Heal(List<BasicCard> list)
    {
        //ensures only one card was added
        if (list.Count != 1)
        {
            Debug.LogError("ERROR! Lena's Heal was given " + list.Count + " targets instead of the expected 1.  Investigate!");
        }
        
        if (list.Count > 0)
        {
            //moves the card to the player's hand.
            Owner.CardToHand(list[0], Owner.Retreat);

            //updates the game log
            CardReader.instance.UpdateGameLog(DM.PlayerName + " activates Lena's Heal skill to add " + list[0].CharName + ": " +
                list[0].CharTitle + " to their hand!");
        }
    }

    //Bond with Julian [ALWAYS] Allied "Julian" gains +10 attack.
    //This method checks if there is a "Julian" on the Field, and if so, activates the Bond With Julian skill.
    private void CheckForJulian()
    {
        //only check if there is not already a Julian on the field which is buffed.
        if (buffedJulian == null)
        {
            //check other allies for a Julian
            foreach (BasicCard ally in OtherAllies)
            {
                if (ally.CompareNames("Julian"))
                {
                    BondWithJulian(ally);
                }
            }
        }
    }

    private void BondWithJulian(BasicCard julianCard)
    {
        //remember the skill's target for future use.
        buffedJulian = julianCard;

        //Adds 10 to the card's attack.
        buffedJulian.attackModifier += 10;

        //display the boost
        CardReader.instance.UpdateGameLog("Lena's Bond With Julian skill gives " + buffedJulian.CharName +  ": " + buffedJulian.CharTitle
            + " +10 attack!");

        buffedJulian.AddToSkillChangeTracker("Lena's Bond With Julian skill providing +10 attack.");

        //Sets a callback to remove the +10 modifier to the card's attack if the card leaves the field.
        buffedJulian.RemoveFromFieldEvent.AddListener(RemoveLenaBuffFromAlly);
    }

    //This method undoes the Bond With Julian skill's attack boost.
    //NOTE: There might be issues if this card is removed from the field or disabled through level up.
    //I'm not sure how Event callbacks work when the listening GameObject is disabled...
    private void RemoveLenaBuffFromAlly(BasicCard buffedAlly)
    {
        if (buffedJulian == buffedAlly)
        {
            //remove the buff
            buffedAlly.attackModifier -= 10;

            //remove the display
            buffedAlly.RemoveFromSkillChangeTracker("Lena's Bond With Julian skill providing +10 attack.");

            //remove the callbacks.
            buffedAlly.RemoveFromFieldEvent.RemoveListener(RemoveLenaBuffFromAlly);

            buffedJulian = null;
        }
        else
        {
            Debug.LogError("ERROR! RemoveLenaBuffFromAlly was given a different card than what is saved. Investigate!" +
                " The buffed Julian on record is" + buffedJulian.CharName +  ": " + buffedJulian.CharTitle);
        }
    }

    //[DEF] Miracle Emblem [SUPP] Until the end of this combat, your opponent's attacking unit cannot perform a Critical Hit.
    public override void ActivateDefenseSupportSkill()
    {
        AbilitySupport.MiracleEmblem();
    }
}
