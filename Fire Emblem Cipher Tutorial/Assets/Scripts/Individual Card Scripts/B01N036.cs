using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N036 : BasicCard
{
    /*
    * B01-036N
    * Linde: Miloah's Child
    * “I want to destroy Gharnef and avenge my father myself!”
    * Tetsu Kurosawa
    * 
    * Thunder [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    * [ATK] Magic Emblem [SUPP] Draw 1 card. Choose 1 card from your hand, and send it to the Retreat Area.
    * 
    * Mage
    * 1
    * Red
    * Female
    * Tome
    * ATK: 30
    * SUPP: 20
    * Range: 1-2
    */

    private bool thunderUsable = true;

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Thunder [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    //Have the card AI decide whether to use Thunder.
    public override void Act()
    {
        //Confirm if Linde can/should use her ability to attack.
        List<BasicCard> targets = AttackTargets;

        if (!GameManager.instance.FirstTurn && !Tapped && targets.Count > 0)
        {
            //Decide whether Linde should use her Thunder ability to raise her attack.
            //First, check if it's even possible to use the skills
            //and if we have enough active bonds based on this deck's strategy to spare one.
            if (CheckActionSkillConditions() && DM.ShouldFlipBonds(this, 1))
            {
                //Confirm if there is a good target to make use of the extra attack.
                List<BasicCard> thunderTargets = new List<BasicCard>(AttackTargets.Count);

                foreach (BasicCard enemy in targets)
                {
                    //Is the enemy the MC or a higher cost unit?
                    //If so, and the extra attack makes us more likely to kill, then add to the target list.
                    if (enemy.CurrentAttackValue == CurrentAttackValue + 10 
                        && (enemy == Owner.Opponent.MCCard || enemy.DeploymentCost > DeploymentCost))
                    {
                        thunderTargets.Add(enemy);
                    }
                }

                //Confirm if we have any good targets for the Thunder skill.
                if (thunderTargets.Count > 0)
                {
                    //Activate Thunder and target the enemies.
                    PayActionSkillCost();
                    DM.ChooseAttackTarget(this, CurrentAttackValue + 10, thunderTargets);
                    return;
                }


            }
        }

        //resume normal turn logic if we don't decide to attack a flier or active the Steel Bow.
        base.Act();
    }

    //Thunder [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    protected override bool CheckActionSkillConditions()
    {
        //Verify that Thunder is usable and that there are enough bonds to use the skill.
        if (thunderUsable && Owner.FaceUpBonds.Count >= 1)
        {
            return true;
        }
        return false;
    }

    //This is where the bond cards to be flipped will be chosen and the effect activated.
    protected override void PayActionSkillCost()
    {
        //adds a callback to activate the skill once the bonds have been flipped.
        Owner.FinishBondFlipEvent.AddListener(ActivateThunder);

        //Choose and flip the bonds to activate this effect.
        DM.ChooseBondsToFlip(this, 1, CardSkills[0]);
    }

    //Thunder [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    private void ActivateThunder()
    {
        //removes the callback
        Owner.FinishBondFlipEvent.RemoveListener(ActivateThunder);

        //updates the game log
        CardReader.instance.UpdateGameLog(DM.PlayerName + " activates Linde's Thunder skill! " +
            "Linde's attack increases by +10 until the end of the turn.");

        //increases attack
        attackModifier += 10;

        //prevents reuse this turn.
        thunderUsable = false;

        //Displays the effect in the skill tracker.
        AddToSkillChangeTracker("Linde's Thunder providing +10 attack.");

        //set up the cancel for this skill at the end of the turn and if the card is removed from the field.
        Owner.endTurnEvent.AddListener(CancelThunder);
        RemoveFromFieldEvent.AddListener(CancelThunder);
    }

    //This method cancels the effect of Thunder at the end of the player's turn or when this card leaves the field.
    private void CancelThunder()
    {
        //decreases attack.
        attackModifier -= 10;

        //resets the Once Per Turn ability tracker
        thunderUsable = true;

        //removes the skill tracking text and callbacks
        RemoveFromSkillChangeTracker("Linde's Thunder providing +10 attack.");
        Owner.endTurnEvent.RemoveListener(CancelThunder);
        RemoveFromFieldEvent.RemoveListener(CancelThunder);
    }

    //This is an overloaded version of the CancelThunder method which allows it to be called from the RemoveFromFieldEvent.
    private void CancelThunder(BasicCard superfluous)
    {
        CancelThunder();
    }

    //[ATK] Magic Emblem [SUPP] Draw 1 card. Choose 1 card from your hand, and send it to the Retreat Area.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.MagicEmblem();
    }
}
