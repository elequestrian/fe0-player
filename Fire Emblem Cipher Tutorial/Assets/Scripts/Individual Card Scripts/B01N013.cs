using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N013 : BasicCard
{
    /*
    * B01-013HN
    * Gordin: Bow Knight of the League
    * “I was dead anyway till you came, sir... If I'm to die again, at least let me die for you.”
    * Kokon Konfuzi
    * 
    * Warning Shot [TRIGGER] When you deploy an ally with a Deployment Cost 2 or less, you may choose 1 <Flier> enemy, and move them.
    * Anti-Fliers [ALWAYS] If this unit is attacking a <Flier>, this unit gains +30 attack.
    * 
    * Sniper
    * 3(2)
    * Red
    * Male
    * Bow
    * ATK: 50
    * SUPP: 20
    * Range: 2
    */

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Adds calls to this card's skills when the card enters the field.
    public override void ActivateFieldSkills()
    {
        Owner.deployTriggerTracker.AddListener(this);
        DeclareAttackEvent.AddListener(AbilitySupport.AntiFliers);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        Owner.deployTriggerTracker.RemoveListener(this);
        DeclareAttackEvent.RemoveListener(AbilitySupport.AntiFliers);

        RemoveFromFieldEvent.Invoke(this);
    }

    //Anti-Fliers [ALWAYS] If this unit is attacking a <Flier>, this unit gains +30 attack.
    //Have the card AI decide who to attack based on Anti-Fliers.
    public override void Act()
    {
        //Confirm if Gordin can/should use either of his abilities to attack.
        List<BasicCard> targets = AttackTargets;

        if (!GameManager.instance.FirstTurn && !Tapped && targets.Count > 0)
        {
            //Aim attack based on the abilty to easily take down flier enemies.
            //Check that Gordin has Flier targets with no more than +10 attack compared to himself with the +30 buff.
            List<BasicCard> flierTargets = targets.FindAll(enemy => enemy.UnitTypeArray[(int)CipherData.TypesEnum.Flier]
            && enemy.CurrentAttackValue <= CurrentAttackValue + 40);

            if (flierTargets.Count > 0)
            {
                //NOTE: We should also confirm if Gordin is likely to crit.

                DM.ChooseAttackTarget(this, CurrentAttackValue + 30, flierTargets);
                return;
            }
        }

        //resume normal turn logic if we don't decide to attack a flier.
        base.Act();
    }

    //Warning Shot [TRIGGER] When you deploy an ally with a Deployment Cost 2 or less, you may choose 1 <Flier> enemy, and move them.
    //Checks to see if the ability can be used.
    public override bool CheckTriggerSkillCondition(BasicCard triggeringCard)
    {
        //checks if the deployed hero is cost 2 or lower.
        if (triggeringCard.DeploymentCost <= 2)
        {
            //Check if the opponent actually has any <Flier> cards to move.
            List<BasicCard> enemies = Owner.Opponent.FieldCards;

            foreach (BasicCard enemy in enemies)
            {
                if (enemy.UnitTypeArray[(int)CipherData.TypesEnum.Flier])
                {
                    return true;
                }
            }
        }

        return false;
    }

    //allows a Local Player to decide whether to activate Gordon's Warning shot.
    //calls the dialogue box for the player to choose to use Warning Shot.
    public override void ResolveTriggerSkillLP(BasicCard triggeringCard)
    {
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = "Gordin's Warning Shot",
            questionText = "Would you like to activate Gordin's skill?" +
            "\n\nWarning Shot [TRIGGER] When you deploy an ally with a Deployment Cost 2 or less, you may choose 1 <Flier> enemy, and move them.",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Yes",
                buttonAction = () => { ChooseWSTarget(); },
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

    //This method actually identifies a target for the Warning Shot skill by running the card picker.
    //Note that this method contains a soft cancel if no card is chosen.
    private void ChooseWSTarget()
    {
        //Find the opponent's <Flier> cards.
        List<BasicCard> enemies = Owner.Opponent.FieldCards;
        List<BasicCard> targets = new List<BasicCard>(enemies.Count);

        foreach (BasicCard enemy in enemies)
        {
            if (enemy.UnitTypeArray[(int)CipherData.TypesEnum.Flier])
            {
                targets.Add(enemy);
            }
        }

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(WarningShot);

        //makes the player choose an opponent's Flier card to move with the skill's effect.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = targets,
            numberOfCardsToPick = 1,
            locationText = DM.Opponent.PlayerName + "'s Field",
            instructionText = "Please choose one card to move using Gordin's Warning Shot skill.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //Warning Shot [TRIGGER] When you deploy an ally with a Deployment Cost 2 or less, you may choose 1 <Flier> enemy, and move them.
    //Actually activates the ability, telling the opponent to move the chosen card. 
    private void WarningShot(List<BasicCard> target)
    {
        //checks for a soft cancel
        if (target.Count > 0)
        {
            //displays the ability on the Game Log
            CardReader.instance.UpdateGameLog("\n" + DM.PlayerName + " activates Gordin's Warning Shot skill!");

            Owner.Opponent.MoveCard(target[0]);
        }

        //returns control to the deployTriggerTracker to recheck conditions and activate any remaining abilities.
        Owner.deployTriggerTracker.RecheckTrigger();
    }

    //allows an AI Player to decide whether to activate Gordon's Warning shot.
    //For now, the AI will always activate the ability if possible.
    //EDIT: add a strategy call to the DM(?) for more sophisticated field analysis of whether to use the ability and which to move. 
    //Warning Shot [TRIGGER] When you deploy an ally with a Deployment Cost 2 or less, you may choose 1 <Flier> enemy, and move them.
    public override void ResolveTriggerSkillAI(BasicCard triggeringCard)
    {
        //Find the opponent's <Flier> cards.  Per the condition check above, there should be at least one.
        List<BasicCard> enemies = Owner.Opponent.FieldCards;
        List<BasicCard> targets = new List<BasicCard>(enemies.Count);

        foreach (BasicCard enemy in enemies)
        {
            if (enemy.UnitTypeArray[(int)CipherData.TypesEnum.Flier])
            {
                targets.Add(enemy);
            }
        }

        //displays the ability on the Game Log
        CardReader.instance.UpdateGameLog("\n" + DM.PlayerName + " activates Gordin's Warning Shot skill!");

        Owner.Opponent.MoveCard(targets[Random.Range(0, targets.Count)]);

        //returns control to the deployTriggerTracker to recheck conditions and activate any remaining abilities.
        Owner.deployTriggerTracker.RecheckTrigger();
    }
}
