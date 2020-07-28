using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N006 : BasicCard
{
    /*
    static B01N006()
    {
        cardNumber = "B01-006";
        charTitle = "Talys's Heart";
        charQuote = "Marth, come quickly!";
        cardIllustrator = "Doji Shiki";
        cardSkills = new string[2];
        cardSkills[0] = "Princess's Charisma [ACT] [TAP, Tap 1 other ally] Choose 1 other ally. Until the end of the turn, that unit gains +10 attack.";
        cardSkills[1] = "[ATK] Elysian Emblem [SUPP] Choose 1 ally other than your attacking unit. You may move that ally.";

        charName = "Caeda";
        classTitle = "Pegasus Knight";
        depolymentCost = 1;
        promotionCost = 0;
        cardColor = "Red";
        charGender = "Female";
        charWeaponType = "Lance";
        affinities[0] = "Flying";
        affinities[1] = "Mounted";
        baseAttack = 30;
        baseSupport = 30;
        baseRange[0] = 1;
    }
    */

    private BasicCard princessCharismaTarget;

    // NOTE: Because inheriting classes don't call their parents' Start method, setup like references to the animator need to occur in a separate method.
    // (I suppose this might be able to be called in a constructor, but I think that plays with the Unity architecture more than I would like.  XD)
    // Note further that this was assigned as Awake instead of start because these component references need to all be set up before Start functions get called.
    void Awake()
    {
        SetUp();
    }

    //Princess's Charisma [ACT] [TAP, Tap 1 other ally] Choose 1 other ally. Until the end of the turn, that unit gains +10 attack.
    protected override bool CheckActionSkillConditions()
    {
        //Verify the card itself is not tapped.
        if (!Tapped)
        {
            //Check that there is at least one other unit on the field.
            if (OtherAllies.Count >= 1)
            {
                //Check that a unit different from this unit is untapped.
                List<BasicCard> otherAllies = OtherAllies;
                for (int i = 0; i < otherAllies.Count; i++)
                {
                    if (!otherAllies[i].Tapped)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    //Can be soft canceled.
    protected override void PayActionSkillCost()
    {
        //choose a second card to be tapped.

        //Identify which cards are possible to be tapped
        List<BasicCard> otherAllies = OtherAllies;
        List<BasicCard> tappableCards = new List<BasicCard>(otherAllies.Count);

        for (int i = 0; i < otherAllies.Count; i++)
        {
            if (!otherAllies[i].Tapped)
            {
                tappableCards.Add(otherAllies[i]);
            }
        }

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(FinishPayCost);

        //makes the player choose a tappable card for the skill's cost.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = tappableCards,
            numberOfCardsToPick = 1,
            locationText = Owner.playerName + "'s Field",
            instructionText = "Please choose one unit to tap to activate " + CharName + "'s Princess's Charisma.\n\n" +
            "Princess's Charisma [ACT] [TAP, Tap 1 other ally] Choose 1 other ally. Until the end of the turn, that unit gains +10 attack.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //This is where the rest of the cost is paid for the effect and the target of the effect will be chosen.
    //Princess's Charisma [ACT] [TAP, Tap 1 other ally] Choose 1 other ally. Until the end of the turn, that unit gains +10 attack.
    private void FinishPayCost(List<BasicCard> list)
    {
        //Can be soft canceled.
        if (list.Count > 0)
        {
            //tap this card
            Tap();

            list[0].Tap();

            //choose another ally unit to buff.

            //This sets up the method to call after the CardPicker finishes.
            MyCardListEvent eventToCall = new MyCardListEvent();
            eventToCall.AddListener(PrincessCharisma);

            //makes the player choose another ally.
            CardPickerDetails details = new CardPickerDetails
            {
                cardsToDisplay = OtherAllies,
                numberOfCardsToPick = 1,
                locationText = Owner.playerName + "'s Field",
                instructionText = "Please choose one other ally to gain +10 attack from Caeda's Princess's Charisma.",
                mayChooseLess = false,
                effectToActivate = eventToCall
            };

            CardPickerWindow cardPicker = CardPickerWindow.Instance();

            cardPicker.ChooseCards(details);
        }
    }

    private void PrincessCharisma(List<BasicCard> list)
    {
        //remember the skill's target for future use.
        princessCharismaTarget = list[0];

        //Adds 10 to a card's attack till the end of the turn.
        princessCharismaTarget.attackModifier += 10;

        //display the boost
        CardReader.instance.UpdateGameLog(Owner.playerName + " activates Caeda's Princess's Charisma skill to give " 
            + princessCharismaTarget.CharName + " +10 attack!");

        princessCharismaTarget.AddToSkillChangeTracker("Caeda's Princess's Charisma skill providing +10 attack.");

        //Removes the +10 modifier to the card's attack at the end of the player's turn or if the card leaves the field.
        Owner.endTurnEvent.AddListener(EndPrincessCharisma);
        princessCharismaTarget.RemoveFromFieldEvent.AddListener(RemoveCaedaBuffFromAlly);
    }

    //This method undoes the Princess Charisma boost at the end of the turn.
    //NOTE: There might be issues if this card is removed from the field or disabled through level up.
    //I'm not sure how Event callbacks work when the listening GameObject is disabled...
    private void EndPrincessCharisma()
    {
        RemoveCaedaBuffFromAlly(princessCharismaTarget);
    }


    //This method undoes the Princess Charisma boost.
    //NOTE: There might be issues if this card is removed from the field or disabled through level up.
    //I'm not sure how Event callbacks work when the listening GameObject is disabled...
    private void RemoveCaedaBuffFromAlly(BasicCard buffedAlly)
    {
        //remove the buff
        buffedAlly.attackModifier -= 10;

        //remove the display
        buffedAlly.RemoveFromSkillChangeTracker("Caeda's Princess's Charisma skill providing +10 attack.");

        //remove the callbacks.
        Owner.endTurnEvent.RemoveListener(EndPrincessCharisma);
        buffedAlly.RemoveFromFieldEvent.RemoveListener(RemoveCaedaBuffFromAlly);

        princessCharismaTarget = null;
    }

    //[ATK] Elysian Emblem [SUPP] Choose 1 ally other than your attacking unit. You may move that ally.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.ElysianEmblem();
    }
}
