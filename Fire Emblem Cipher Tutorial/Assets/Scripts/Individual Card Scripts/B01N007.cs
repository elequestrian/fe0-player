using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N007 : BasicCard
{
    /*
     * B01-007R
     * Cain: Knight Hailed as the Bull
     * “Taste my steel!”
     * Aoji
     * 
     * Paladin’s Protection [ALWAYS] Enemies in the Back Line cannot attack this unit or allies with a Deployment Cost of 2 or lower.
     * Red-Green Twin Strike [TRIGGER] [Tap allied "Abel"] When this unit attacks, you may pay the cost and if you do: Until the end of this combat, this unit gains +40 attack.
     * 
     * Paladin
     * 3(2)
     * Red
     * Male
     * Sword
     * Beast
     * ATK: 60
     * SUPP: 10
     * Range: 1
     */

    private bool shouldUseTwinStrike = false;


    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Adds calls to this card's skills when the card enteres the field.
    public override void ActivateFieldSkills()
    {
        Owner.Opponent.AttackTargetHandler.AddListener(this);
        CardReader.instance.UpdateGameLog("Cain's Paladin’s Protection skill prevents Back Line enemies from attacking him or his" +
            " low cost allies!");
        AddToSkillChangeTracker("Cain's Paladin's Protection skill is active.");

        DeclareAttackEvent.AddListener(CheckTwinStrike);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        Owner.Opponent.AttackTargetHandler.RemoveListener(this);
        RemoveFromSkillChangeTracker("Cain's Paladin's Protection skill is active.");

        DeclareAttackEvent.RemoveListener(CheckTwinStrike);

        RemoveFromFieldEvent.Invoke(this);
    }

    //Paladin’s Protection [ALWAYS] Enemies in the Back Line cannot attack this unit or allies with a Deployment Cost of 2 or lower.
    //This virtual method works with the TriggerEventHandler's list editing feature.
    //Overrides the virtual method to remove Cain and low cost allies from the attack targets List.
    public override List<BasicCard> EditList(BasicCard attacker, List<BasicCard> attackTargets)
    {
        //Check if the attacking card is in the opponent's back row.
        if (Owner.Opponent.BackLineCards.Contains(attacker))
        {
            //remove Cain and allies with Deployment Costs of 2 or lower from the list of attack targets.
            attackTargets.RemoveAll(target => target == this || target.DeploymentCost <= 2);
        }

        return attackTargets;
    }

    //Red-Green Twin Strike [TRIGGER] [Tap allied "Abel"] When this unit attacks, you may pay the cost and if you do: Until the end of this combat, this unit gains +40 attack.
    //Have the card AI decide whether or not to use Red-Green Twin Strike.
    public override void Act()
    {
        //Reset the Twin Strike bool.
        shouldUseTwinStrike = false;

        //Confirm that Cain has attack targets
        List<BasicCard> targets = AttackTargets;

        if (!GameManager.instance.FirstTurn && !Tapped && targets.Count > 0)
        {
            //Confirm if we CAN use the ability.
            //Check that there are other allies.
            if (OtherAllies.Count >= 1)
            {
                //check if Abel is deployed
                List<BasicCard> otherAllies = OtherAllies;
                int n = otherAllies.FindIndex(ally => ally.CompareNames("Abel"));

                if (n >= 0)
                {
                    //confirm Abel is untapped.
                    if (!otherAllies[n].Tapped)
                    {
                        //We can use Red-Green Twin Strike.  Now to decide if we should.
                        //First, check that Cain has a high attack (more than +20 above his attack) enemy to target.
                        List<BasicCard> highAttackTargets = targets.FindAll(enemy => enemy.CurrentAttackValue >= CurrentAttackValue + 20);

                        //Second, check that Abel doesn't have good targets for his attack (no targets same or lower attack).
                        List<BasicCard> goodAbelTargets = otherAllies[n].AttackTargets.FindAll(enemy => enemy.CurrentAttackValue <= otherAllies[n].CurrentAttackValue);

                        //NOTE: We could also confirm that Abel isn't likely to crit anything, but since this ability only gives up
                        //a unit's action (a reusable resource), I think it's better to save the Abel card for dodge fodder or deployment.

                        if (highAttackTargets.Count > 0 && goodAbelTargets.Count == 0)
                        {
                            //Let's go!  Confirm we want to activate Red-Green Twin Strike and have the Decision Maker find a good
                            //high attack target!
                            shouldUseTwinStrike = true;
                            DM.ChooseAttackTarget(this, CurrentAttackValue + 40, highAttackTargets);
                            return;
                        }
                    }
                }
            }
        }

        //resume normal turn logic
        base.Act();
    }

    //Red-Green Twin Strike [TRIGGER] [Tap allied "Abel"] When this unit attacks, you may pay the cost and if you do: Until the end of this combat, this unit gains +40 attack.
    //Ensures the conditions are met to make a twin strike and then asks the player if they want to activate that ability.
    private void CheckTwinStrike(bool attacking)
    {
        //check that this unit is attacking.
        if (attacking)
        {
            //Check that there are other allies.
            if (OtherAllies.Count >= 1)
            {
                //check if Abel is deployed
                List<BasicCard> otherAllies = OtherAllies;
                int n = otherAllies.FindIndex(ally => ally.CompareNames("Abel"));

                if (n >= 0)
                {
                    //confirm Abel is untapped.
                    if (!otherAllies[n].Tapped)
                    {
                        //conditions met; Check if the decision maker is an AI or human.
                        if (DM is AIPlayer)
                        {
                            //Confirm whether the AI already decided to use Red-Green Twin Strike 
                            if (shouldUseTwinStrike)
                                RedGreenTwinStrike(otherAllies[n]);
                        }
                        //The decisionMaker is a human.  Call a dialogue box to confirm if the player wants to use Red-Green Twin Strike.
                        else
                        {
                            DialogueWindowDetails details = new DialogueWindowDetails
                            {
                                windowTitleText = "Red-Green Twin Strike",
                                questionText = "Would you like to activate Cain's Red-Green Twin Strike?" +
                                    "\n\nRed-Green Twin Strike [TRIGGER] [Tap allied \"Abel\"] When this unit attacks, " +
                                    "you may pay the cost and if you do: Until the end of this combat, this unit gains +40 attack.",
                                button1Details = new DialogueButtonDetails
                                {
                                    buttonText = "Yes",
                                    buttonAction = () => { RedGreenTwinStrike(otherAllies[n]); }
                                },
                                button2Details = new DialogueButtonDetails
                                {
                                    buttonText = "No",
                                    buttonAction = () => { Debug.Log("No Red-Green Twin Strike. :("); }
                                }
                            };

                            DialogueWindow dialogueWindow = DialogueWindow.Instance();
                            dialogueWindow.MakeChoice(details);
                        }
                    }
                }
            }
        }
    }

    //Red-Green Twin Strike [TRIGGER] [Tap allied "Abel"] When this unit attacks, you may pay the cost and if you do: Until the end of this combat, this unit gains +40 attack.
    //This method actually activates the above skill.
    private void RedGreenTwinStrike(BasicCard abelCard)
    {
        //Tap Abel to pay the cost.
        abelCard.Tap();

        //raise Cain's attack
        attackModifier += 40;

        //display the skill's activation and effect.
        CardReader.instance.UpdateGameLog(DM.PlayerName + " activates Cain's Red-Green Twin Strike skill to tap Abel and give Cain " +
            "+40 attack during this battle!");
        AddToSkillChangeTracker("Cain's Red-Green Twin Strike skill providing +40 attack.");

        //add a call to cancel this skill.
        AfterBattleEvent.AddListener(CancelTwinStrike);
    }

    //This method cancels the Red-Green Twin Strike
    private void CancelTwinStrike()
    {
        //lower Cain's attack
        attackModifier -= 40;

        //remove effect display.
        RemoveFromSkillChangeTracker("Cain's Red-Green Twin Strike skill providing +40 attack.");

        //remove callback.
        AfterBattleEvent.RemoveListener(CancelTwinStrike);
    }
}
