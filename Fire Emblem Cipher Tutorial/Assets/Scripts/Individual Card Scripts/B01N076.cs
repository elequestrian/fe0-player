using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N076 : BasicCard {


    //Elysian Deliverer [ACT] [TAP] Choose 1 other ally, and move them.
    //[ATK] Elysian Emblem [SUPP] You may choose 1 ally other than your attacking unit, and move them.

    void Awake () {
        SetUp();
	}


    //Elysian Deliverer [ACT] [TAP] Choose 1 other ally, and move them.
    protected override bool CheckActionSkillConditions()
    {
        //Verify the card itself is not tapped.
        if (!Tapped)
        {
            //Check that there is at least one other unit on the field.
            if (Owner.FieldCards.Count > 1)
            {
                return true;
            }
        }
        return false;
    }

    //This is where the target of the effect will be chosen.
    //can be soft canceled.
    protected override void PayActionSkillCost()
    {
        //choose an ally to be moved.

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(ActivateEffect);

        //makes the player choose another ally for the skill's effect.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = OtherAllies,
            numberOfCardsToPick = 1,
            locationText = "Player's Field",
            instructionText = "Please choose one unit to move with " + CharName + "'s Elysian Deliverer.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    private void ActivateEffect(List<BasicCard> list)
    {
        if (list.Count > 0)
        {
            Tap();

            list[0].Owner.MoveCard(list[0]);
        }
    }

    //[ATK] Elysian Emblem [SUPP] You may choose 1 ally other than your attacking unit, and move them.
    public override void ActivateAttackSupportSkill()
    {
        //Checks that there is more than one ally in play.
        if (Owner.FieldCards.Count > 1)
        {
            //check if the player wants to activate Crodelia's skill.  Call a dialogue box.
            DialogueWindowDetails details = new DialogueWindowDetails
            {
                windowTitleText = "Elysian Emblem",
                questionText = "Would you like to activate " + CharName + "'s Elysian Emblem?" +
                "\n\n[ATK] Elysian Emblem [SUPP] You may choose one ally that is not the attacking unit, and move it.",
                button1Details = new DialogueButtonDetails
                {
                    buttonText = "Yes",
                    buttonAction = () => { TargetElysianEmblem(); }
                },
                button2Details = new DialogueButtonDetails
                {
                    buttonText = "No",
                    buttonAction = () => { GameManager.instance.ActivateDefenderSupport(); }
                }
            };

            DialogueWindow dialogueWindow = DialogueWindow.Instance();
            dialogueWindow.MakeChoice(details);
        }
        else
        {
            GameManager.instance.ActivateDefenderSupport();
        }
    }

    //Choose a friendly target for Elysian Emblem.  Can be soft canceled.
    private void TargetElysianEmblem()
    {
        //find the cards on the field besides the attacking unit.
        List<BasicCard> possibleAllies = Owner.FieldCards;
        possibleAllies.Remove(GameManager.instance.CurrentAttacker);

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(ActivateElysianEmblem);

        //makes the player choose another ally for the skill's effect.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = possibleAllies,
            numberOfCardsToPick = 1,
            locationText = "Player's Field",
            instructionText = "Please choose one ally to move using " + CharName + "'s Elysian Emblem.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //Actually moves the chosen unit.  Can be soft canceled.
    //Returns control to the battle logic.
    private void ActivateElysianEmblem(List<BasicCard> list)
    {
        if (list.Count > 0)
        {
            Owner.MoveCard(list[0]);
        }

        GameManager.instance.ActivateDefenderSupport();
    }
}




