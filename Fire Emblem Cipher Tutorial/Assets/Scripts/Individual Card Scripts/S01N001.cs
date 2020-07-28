using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class S01N001 : BasicCard {

    /*
        cardNumber = "S01-001";
        charTitle = "Star and Savior";
        charQuote = "\"I will crush Dolhr. I swear it on this emblem of fire!\"";
        cardIllustrator = "Yusuke Kozaki";
        cardSkills = new string[2];
        cardSkills[0] = "Prince of Light [TRIGGER] [ONCE PER TURN] When you deploy an ally with a Deployment Cost 2 or lower, you may choose 1 enemy in the Back Line, and move them.";
        cardSkills[1] = "Falchion [ALWAYS] If this unit is attacking a <Dragon>, this unit gains +20 attack.";

        charName = "Marth";
        classTitle = "Lodestar";
        depolymentCost = 4;
        promotionCost = 3;
        cardColor = "Red";
        charGender = "Male";
        charWeaponType = "Sword";
        
        baseAttack = 70;
        baseSupport = 20;
    */

    private bool PrinceOfLightUseable = true;


    // NOTE: Because inheriting classes don't call their parents' Start method, setup like references to the animator need to occur in a separate method.
    // (I suppose this might be able to be called in a constructor, but I think that plays with the Unity architecture more than I would like.  XD)
    // Note further that this was assigned as Awake instead of start because these component references need to all be set up before Start functions get called.
    void Awake () {
        SetUp();
               
	}

    //Adds calls to this card's skills when the card enters the field.
    public override void ActivateFieldSkills()
    {
        Owner.deployTriggerTracker.AddListener(this);
        Owner.endTurnEvent.AddListener(ResetHoLOncePerTurn);

        DeclareAttackEvent.AddListener(Falchion);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        Owner.deployTriggerTracker.RemoveListener(this);
        Owner.endTurnEvent.RemoveListener(ResetHoLOncePerTurn);

        if (!PrinceOfLightUseable)
        {
            ResetHoLOncePerTurn();
        }

        DeclareAttackEvent.RemoveListener(Falchion);

        RemoveFromFieldEvent.Invoke(this);
    }

    //Falchion [ALWAYS] If this unit is attacking a <Dragon>, this unit gains +20 attack.
    //Checks to see if the current attack target is a dragon and if so, then adds an attack buff.
    //NOTE: As an [ALWAYS] skill, may need to add more complex tracking if an opponent gains (or loses) Dragon affinity
    //during the battle process. Example Gunter4 (B06-096R)
    //Should add a check to an event on the Defending Card which tracks when that card's affinities and stats are changed. 
    public void Falchion(bool attacking)
    {
        //Only apply if this card is attacking.
        if (attacking)
        {
            if (GameManager.instance.CurrentDefender.UnitTypeArray[(int)CipherData.TypesEnum.Dragon])
            {
                attackModifier += 20;
                CardReader.instance.UpdateGameLog("Marth's Falchion skill provides +20 attack against " 
                    + GameManager.instance.CurrentDefender.CharName + "!");
                AddToSkillChangeTracker("Marth's Falchion skill providing +20 attack.");
                AfterBattleEvent.AddListener(EndFalchionBoost);
            }
        }
    }

    //This method removes the skill modifier information from the card after the battle.
    public void EndFalchionBoost()
    {
        attackModifier -= 20;
        RemoveFromSkillChangeTracker("Marth's Falchion skill providing +20 attack.");
        AfterBattleEvent.RemoveListener(EndFalchionBoost);
    }

    //Prince of Light [TRIGGER] [ONCE PER TURN] When you deploy an ally with a Deployment Cost 2 or lower, you may choose 1 enemy in the Back Line, and move them.
    //Checks to see if a cost 2 or lower ally has been deployed.
    public override bool CheckTriggerSkillCondition(BasicCard triggeringCard)
    {
        //checks that the skill hasn't already been used.
        if (PrinceOfLightUseable)
        {
            //checks if the deployed hero is cost 2 or lower.
            if (triggeringCard.DeploymentCost <= 2)
            {
                //Check if the opponent actually has any cards in the back row to pull.
                if (Owner.Opponent.BackLineStacks.Count > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    //calls the dialogue box for the player to choose to use Prince of Light.
    public override void ActivateTriggerSkill(BasicCard triggeringCard)
    {
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = "Marth's Prince of Light",
            questionText = "Would you like to activate Marth's skill?" +
            "\n\nPrince of Light [TRIGGER] [ONCE PER TURN] When you deploy an ally with a Deployment Cost 2 or lower, you may choose 1 enemy in the Back Line, and move them.",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Yes",
                buttonAction = () => { ChooseHoLTarget(); },
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

    //This method actually identifies a target for the Prince of Light skill by running the card picker.
    //Note that this method contains a soft cancel if no card is chosen.
    private void ChooseHoLTarget()
    {
        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(ActivateHoL);

        //makes the player choose an opponent's back row card for the skill's effect.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = Owner.Opponent.BackLineCards,
            numberOfCardsToPick = 1,
            locationText = Owner.Opponent.playerName + "'s Back Line",
            instructionText = "Please choose one card to move using Marth's Prince of Light skill.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //Actually activates the ability, telling the opponent to move the chosen card from the back line to the front. 
    //Prince of Light [TRIGGER] [ONCE PER TURN] When you deploy an ally with a Deployment Cost 2 or lower, you may choose 1 enemy in the Back Line, and move them.
    private void ActivateHoL(List<BasicCard> target)
    {
        //checks for a soft cancel
        if (target.Count > 0)
        {
            //Ensures this ability is only used once per turn.
            PrinceOfLightUseable = false;
            AddToSkillChangeTracker("Marth's Prince of Light skill has been used this turn.");

            //displays the ability on the Game Log
            CardReader.instance.UpdateGameLog("\n" + Owner.playerName + " activates Marth's Prince of Light skill!");

            Owner.Opponent.MoveCard(target[0]);
        }

        //returns control to the deployTriggerTracker to recheck conditions and activate any remaining abilities.
        Owner.deployTriggerTracker.RecheckTrigger();
    }

    //resets the Once Per Turn flag on Prince of Light
    public void ResetHoLOncePerTurn()
    {
        PrinceOfLightUseable = true;
        RemoveFromSkillChangeTracker("Marth's Prince of Light skill has been used this turn.");
    }
}
