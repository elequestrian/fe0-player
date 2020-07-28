using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class S01N002 : BasicCard {

    /*
     * cardNumber = "S01-002";
        charTitle = "Battlefield-Soaring Princess";
        charQuote = “I wish nothing more than to put an end to this dreadful war, and swiftly.”;
        cardIllustrator = "Douji Shiki";
        cardSkills = new string[2];
        cardSkills[0] = "Wyvern Whip [TRIGGER] Each time an ally with a Deployment Cost of 2 or lower is deployed, you may choose as many allies as you wish, and move them.";
        cardSkills[1] = "Javelin [ACT] [FLIP 1] Until the end of the turn, this unit acquires range 1-2.";

        charName = "Caeda";
        classTitle = "Dracoknight";
        depolymentCost = 3;
        promotionCost = 2;
        cardColor = "Red";
        charGender = "Female";
        charWeaponType = "Lance";
        affinities = new string[2] "Flying" "Beast"
        baseAttack = 50;
        baseSupport = 30;
        
        */

    private bool javelinActive = false;

    //Javelin [ACT] [FLIP 1] Until the end of the turn, this unit acquires range 1-2.
    public override bool[] BaseRangeArray
    {
        get
        {
            //if the Javelin skill is active, then this card acquires range 1-2 in addition to its exisiting range.
            if (javelinActive)
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
    void Awake () {
        SetUp();
    }

    //Javelin [ACT] [FLIP 1] Until the end of the turn, this unit acquires range 1-2.
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
        Owner.FinishBondFlipEvent.AddListener(ActivateJavelin);
    }

    //This is the method that gets called once the bond flip is finished.
    private void ActivateJavelin()
    {
        //removes the callback
        Owner.FinishBondFlipEvent.RemoveListener(ActivateJavelin);

        CardReader.instance.UpdateGameLog(Owner.playerName + " activates Caeda's Javelin skill!");

        javelinActive = true;
        AddToSkillChangeTracker("Caeda's Javelin providing 1-2 range.");

        //set up the cancel for this skill at the end of the turn.
        Owner.endTurnEvent.AddListener(CancelJavelin);
    }

    //This method cancels the effect of Javelin at the end of the player's turn or when this card leaves the field.
    private void CancelJavelin()
    {
        javelinActive = false;
        RemoveFromSkillChangeTracker("Caeda's Javelin providing 1-2 range.");
        Owner.endTurnEvent.RemoveListener(CancelJavelin);
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
        CancelJavelin();

        RemoveFromFieldEvent.Invoke(this);
    }


    //Wyvern Whip [TRIGGER] Each time an ally with a Deployment Cost of 2 or lower is deployed, you may choose as many allies as you wish, and move them.
    //Checks to see if a cost 2 or lower ally has been deployed 
    public override bool CheckTriggerSkillCondition(BasicCard triggeringCard)
    {
        if (triggeringCard.DeploymentCost <= 2)
        {
            return true;
        }

        return false;
    }

    //Calls the dialogue box for the player to choose to use this skill
    public override void ActivateTriggerSkill(BasicCard triggeringCard)
    {
        //lets the player choose whether to activate the skill or not.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = "Caeda's Wyvern Whip",
            questionText = "Would you like to activate Caeda's skill?" +
            "\n\nWyvern Whip [TRIGGER] Each time an ally with a Deployment Cost of 2 or lower is deployed, you may choose as many allies as you wish, and move them.",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Yes",
                buttonAction = () => { ChooseWWTarget(); },
            },
            button2Details = new DialogueButtonDetails
            {
                buttonText = "No",
                buttonAction = () => { Owner.deployTriggerTracker.RecheckTrigger(); },
            }
        };

        DialogueWindow dialogueWindow = DialogueWindow.Instance();
        dialogueWindow.MakeChoice(details);
    }

    //This method actually identifies a target for the Wyvern Whip skill by running the card picker.
    //Note that this method contains a soft cancel if no card is chosen.
    private void ChooseWWTarget()
    {
        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(ActivateWW);

        //makes the player choose among their own allies for the skill's effect.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = Owner.FieldCards,
            numberOfCardsToPick = Owner.FieldCards.Count,
            locationText = Owner.playerName + "'s Field",
            instructionText = "Please choose the allies to move using Caeda's Wyvern Whip skill.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //Actually activates the ability, moving the chosen cards on the player's field. 
    //Wyvern Whip [TRIGGER] Each time an ally with a Deployment Cost of 2 or lower is deployed, you may choose as many allies as you wish, and move them.
    //NOTE: watch for multiple movement skills activating...
    private void ActivateWW(List<BasicCard> targets)
    {
        //checks for a soft cancel
        if (targets.Count > 0)
        {
            //displays the ability on the Game Log
            CardReader.instance.UpdateGameLog("\n" + Owner.playerName + " activates Caeda's Wyvern Whip skill!");

            //Moves each chosen card
            for (int i = 0; i < targets.Count; i++)
            {
                Owner.MoveCard(targets[i]);
            }
        }

        //returns control to the deployTriggerTracker to recheck conditions and activate any remaining abilities.
        Owner.deployTriggerTracker.RecheckTrigger();
    }
}