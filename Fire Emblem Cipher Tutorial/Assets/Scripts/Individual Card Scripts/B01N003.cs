using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N003 : BasicCard {

    /*
     * B01-003
     * Altean Prince, Marth
     * “I know what I must do, now that Medeus has revived and restored Dolhr to its former might.”
     * Yusuke Kozaki
     * 
     * Young Hero [ACT] [TAP, Tap 1 other ally] Choose 1 enemy, and move them. This skill cannot be used unless this unit is in the Front Line.
     * [ATK] Hero Emblem [SUPP] Until the end of this combat, the number of Orbs that your <Red> attacking unit's attack will destroy becomes 2.
     * 
     * Lord
     * 1
     * Red
     * Male
     * Sword
     * ATK: 40
     * SUPP: 20
     * Range: 1
     */



    private void Awake()
    {
        SetUp();
    }

    //Young Hero [ACT] [TAP, Tap 1 other ally] Choose 1 enemy, and move them. This skill cannot be used unless this unit is in the Front Line.
    protected override bool CheckActionSkillConditions()
    {
        //Verify the card itself is not tapped.
        if (!Tapped)
        {
            //Check that there is at least one other unit on the field.
            if (OtherAllies.Count >= 1)
            {
                //Check that this unit is in the Front Line
                if (Owner.FrontLineCards.Contains(this))
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
        }
        return false;
    }

    //Can be soft canceled
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
            instructionText = "Please choose one other unit to tap to activate " + CharName + "'s Young Hero skill.\n\n" +
            "Young Hero [ACT] [TAP, Tap 1 other ally] Choose 1 enemy, and move them. This skill cannot be used unless this unit is in the Front Line.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //Young Hero[ACT] [TAP, Tap 1 other ally] Choose 1 enemy, and move them. This skill cannot be used unless this unit is in the Front Line.
    //This is where the rest of the cost is paid for the effect and the target of the effect will be chosen.
    private void FinishPayCost(List<BasicCard> list)
    {
        if (list.Count > 0)
        {
            //tap this card
            Tap();

            list[0].Tap();

            //choose an enemy card to be moved.

            //This sets up the method to call after the CardPicker finishes.
            MyCardListEvent eventToCall = new MyCardListEvent();
            eventToCall.AddListener(YoungHero);

            //makes the player choose an opponent's card for the skill's effect.
            CardPickerDetails details = new CardPickerDetails
            {
                cardsToDisplay = Owner.Opponent.FieldCards,
                numberOfCardsToPick = 1,
                locationText = Owner.Opponent.playerName + "'s Field",
                instructionText = "Please choose one unit to move with Marth's Young Hero.",
                mayChooseLess = false,
                effectToActivate = eventToCall
            };

            CardPickerWindow cardPicker = CardPickerWindow.Instance();

            cardPicker.ChooseCards(details);
        }
    }

    private void YoungHero(List<BasicCard> list)
    {
        CardReader.instance.UpdateGameLog(Owner.playerName + " activates Marth's Young Hero skill to move " + Owner.Opponent.playerName
                    + "'s " + list[0].CharName + "!");
        list[0].Owner.MoveCard(list[0]);
    }

    //[ATK] Hero Emblem [SUPP] Until the end of this combat, the number of Orbs that your <Red> attacking unit's attack will destroy becomes 2.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.HeroEmblem(CipherData.ColorsEnum.Red);
    }

}

