using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N021 : BasicCard
{
    /*
    * B01-021HN
    * Barst: The Hatchet
    * “Bord! Cord! Let's do this!”
    * HACCAN
    * 
    * [Formation Skill] Bord, Cord, and Barst [TRIGGER] [Tap allied "Bord" and "Cord"] When this unit attacks, you may pay the cost, and if you do: Until the end of this combat, this unit gains +50 attack and the number of Orbs that this unit's attack will destroy becomes 2.
    * Fighter's Expertise [ALWAYS] During your turn, this unit gains +20 attack.
    * 
    * Fighter
    * 2
    * Red
    * Male
    * Axe
    * ATK: 40
    * SUPP: 10
    * Range: 1
    */

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Adds calls to this card's skills when the card enteres the field.
    public override void ActivateFieldSkills()
    {
        //confirm it's the owner's turn, and if so, then activate FightersExpertise.
        if (GameManager.instance.turnPlayer == Owner)
        {
            CardReader.instance.UpdateGameLog("Barst's Fighter's Expertise skill provides +20 attack during your turn!");
            FightersExpertise();
        }

        //set up the callbacks for Fighter's Expertise.
        Owner.BeginTurnEvent.AddListener(FightersExpertise);
        Owner.endTurnEvent.AddListener(CancelFightersExpertise);

        //Sets up Formation Skill
        DeclareAttackEvent.AddListener(CheckFormationSkill);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        //confirm it's the owner's turn, and if so, then cancel Fighter's Expertise.
        if (GameManager.instance.turnPlayer == Owner)
        {
            CancelFightersExpertise();
        }

        //removes the callbacks
        Owner.BeginTurnEvent.RemoveListener(FightersExpertise);
        Owner.endTurnEvent.RemoveListener(CancelFightersExpertise);
        DeclareAttackEvent.RemoveListener(CheckFormationSkill);

        RemoveFromFieldEvent.Invoke(this);
    }

    //[Formation Skill] Bord, Cord, and Barst [TRIGGER] [Tap allied "Bord" and "Cord"] 
    //When this unit attacks, you may pay the cost, and if you do: 
    //Until the end of this combat, this unit gains +50 attack and the number of Orbs that this unit's attack will destroy becomes 2.

    //Ensures the conditions are met to activate the formation skill and then asks the player if they want to activate that ability.
    private void CheckFormationSkill(bool attacking)
    {
        //check that this unit is attacking and that there are other allies.
        if (attacking && OtherAllies.Count >= 1)
        {
            //check if Bord is deployed
            List<BasicCard> otherAllies = OtherAllies;
            int b = otherAllies.FindIndex(ally => ally.CompareNames("Bord"));

            if (b >= 0)
            {
                //check if Cord is deployed
                int c = otherAllies.FindIndex(ally => ally.CompareNames("Cord"));

                if (c >= 0)
                {
                    //confirm Bord and Cord are both untapped.
                    if (!otherAllies[b].Tapped && !otherAllies[c].Tapped)
                    {
                        //conditions met; call a dialogue box to confirm if the player wants to use Formation Skill.
                        DialogueWindowDetails details = new DialogueWindowDetails
                        {
                            windowTitleText = "Formation Skill",
                            questionText = "Would you like to activate Barst's Formation Skill?" +
                            "\n\n[Formation Skill] Bord, Cord, and Barst [TRIGGER] [Tap allied \"Bord\" and \"Cord\"] When this unit attacks, you may pay the cost, and if you do: Until the end of this combat, this unit gains +50 attack and the number of Orbs that this unit's attack will destroy becomes 2.",
                            button1Details = new DialogueButtonDetails
                            {
                                buttonText = "Yes",
                                buttonAction = () => { FormationSkill(otherAllies[b], otherAllies[c]); }
                            },
                            button2Details = new DialogueButtonDetails
                            {
                                buttonText = "No",
                                buttonAction = () => { Debug.Log("No Formation Skill. :("); }
                            }
                        };

                        DialogueWindow dialogueWindow = DialogueWindow.Instance();
                        dialogueWindow.MakeChoice(details);
                    }
                }
                
            }
        }
    }

    //[Formation Skill] Bord, Cord, and Barst [TRIGGER] [Tap allied "Bord" and "Cord"] 
    //When this unit attacks, you may pay the cost, and if you do: 
    //Until the end of this combat, this unit gains +50 attack and the number of Orbs that this unit's attack will destroy becomes 2.

    //This method actually activates the above skill.
    private void FormationSkill(BasicCard bordCard, BasicCard cordCard)
    {
        //Tap Bord and Cord to pay the cost.
        bordCard.Tap();
        cordCard.Tap();

        //raise Barst's attack
        attackModifier += 50;

        //increase the number of orbs to be destroyed to 2.
        GameManager.instance.numOrbsToBreak = 2;

        //display the skill's activation and effect.
        CardReader.instance.UpdateGameLog(Owner.playerName + " activates Barst's Formation Skill: Bord, Cord, and Barst! Tapping Bord" +
            " and Cord gives Barst +50 attack and the ability to destroy 2 orbs during this battle!");
        AddToSkillChangeTracker("Barst's Formation Skill providing +50 attack and the ability to destroy 2 orbs.");

        //add a call to cancel this skill.
        AfterBattleEvent.AddListener(CancelFormationSkill);
    }

    //This method cancels the Formation Skill
    private void CancelFormationSkill()
    {
        //lower Barst's attack
        attackModifier -= 50;

        //remove effect display.
        RemoveFromSkillChangeTracker("Barst's Formation Skill providing +50 attack and the ability to destroy 2 orbs.");

        //remove callback.
        AfterBattleEvent.RemoveListener(CancelFormationSkill);
    }

    //Fighter's Expertise [ALWAYS] During your turn, this unit gains +20 attack.
    private void FightersExpertise()
    {
        //buff attack
        attackModifier += 20;

        //Report the change in the tracker.
        AddToSkillChangeTracker("Fighter's Expertise skill providing +20 attack.");
    }

    //Removes the boost from Fighter's Expertise.
    private void CancelFightersExpertise()
    {
        //removes the attack buff.
        attackModifier -= 20;

        //Remove the report from the skill tracker.
        RemoveFromSkillChangeTracker("Fighter's Expertise skill providing +20 attack.");
    }
}
