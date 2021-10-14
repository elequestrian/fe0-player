using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N014 : BasicCard
{
    /*
    * B01-014N
    * Gordin: Archer of the Liberators
    * “I won't miss... Not at this range!”
    * Kokon Konfuzi
    * 
    * Steel Bow [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    * Anti-Fliers [ALWAYS] If this unit is attacking a <Flier>, this unit gains +30 attack.
    * [ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    * 
    * Archer
    * 1
    * Red
    * Male
    * Bow
    * ATK: 30
    * SUPP: 20
    * Range: 2
    */

    private bool steelBowUsable = true;

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //Adds calls to this card's skills when the card enters the field.
    public override void ActivateFieldSkills()
    {
        DeclareAttackEvent.AddListener(AbilitySupport.AntiFliers);
    }

    //Removes the call to this card's skills when the card leaves the field.
    public override void DeactivateFieldSkills()
    {
        DeclareAttackEvent.RemoveListener(AbilitySupport.AntiFliers);

        RemoveFromFieldEvent.Invoke(this);
    }

    //Steel Bow [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    //Anti-Fliers [ALWAYS] If this unit is attacking a <Flier>, this unit gains +30 attack.
    //Have the card AI decide who to attack based on Anti-Fliers.
    public override void Act()
    {
        //Confirm if Gordin can/should use either of his abilities to attack.
        List<BasicCard> targets = AttackTargets;

        if (!GameManager.instance.FirstTurn && !Tapped && targets.Count > 0)
        {
            //Decide whether Gordin should use his Steel Bow ability to raise his attack.
            //First, check if it's even possible to use the skills
            //and if we have enough active bonds based on this deck's strategy to spare one.
            if (CheckActionSkillConditions() && DM.ShouldFlipBonds(this, 1))
            {
                //Confirm if there is a good target to make use of the extra attack.
                List<BasicCard> steelBowTargets = new List<BasicCard>(AttackTargets.Count);

                foreach (BasicCard enemy in targets)
                {
                    //Is the enemy a high attack flier, or the MC, or a higher cost unit?
                    //If so, and the extra attack makes us more likely to kill, then add to the target list.
                    if ((enemy.UnitTypeArray[(int)CipherData.TypesEnum.Flier] && enemy.CurrentAttackValue == CurrentAttackValue + 40)
                        || (enemy.CurrentAttackValue == CurrentAttackValue + 10
                        && (enemy == Owner.Opponent.MCCard || enemy.DeploymentCost > DeploymentCost)))
                    {
                        steelBowTargets.Add(enemy);
                    }
                }

                //Confirm if we have any good targets for the Steel Bow skill.
                if (steelBowTargets.Count > 0)
                {
                    //Activate Steel Bow and target the enemies.
                    PayActionSkillCost();

                    //Prioritize attacking the fliers.
                    if (steelBowTargets.Exists(enemy => enemy.UnitTypeArray[(int)CipherData.TypesEnum.Flier]))
                        DM.ChooseAttackTarget(this, CurrentAttackValue + 40, steelBowTargets.FindAll(enemy => enemy.UnitTypeArray[(int)CipherData.TypesEnum.Flier]));
                    else
                        DM.ChooseAttackTarget(this, CurrentAttackValue + 10, steelBowTargets);

                    return;
                }

  
            }

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
        
        //resume normal turn logic if we don't decide to attack a flier or active the Steel Bow.
        base.Act();
    }

    //Steel Bow [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    protected override bool CheckActionSkillConditions()
    {
        //Verify that Steel Bow is usable.
        if (steelBowUsable)
        {
            //Check there there are enough bonds to use the skill.
            if (Owner.FaceUpBonds.Count >= 1)
            {
                return true;
            }
        }
        return false;
    }

    //This is where the bond cards to be flipped will be chosen and the effect activated.
    protected override void PayActionSkillCost()
    {
        //adds a callback to activate the skill once the bonds have been flipped.
        Owner.FinishBondFlipEvent.AddListener(ActivateSteelBow);

        //Choose and flip the bonds to activate this effect.
        DM.ChooseBondsToFlip(this, 1, CardSkills[0]);
    }

    //Steel Bow [ACT] [ONCE PER TURN] [FLIP 1] Until the end of the turn, this unit gains +10 attack.
    private void ActivateSteelBow()
    {
        //removes the callback
        Owner.FinishBondFlipEvent.RemoveListener(ActivateSteelBow);

        //updates the game log
        CardReader.instance.UpdateGameLog(DM.PlayerName + " activates Gordin's Steel Bow skill! " +
            "Gordin's attack increases by +10 until the end of the turn.");

        //increases attack
        attackModifier += 10;

        //prevents reuse this turn.
        steelBowUsable = false;

        //Displays the effect in the skill tracker.
        AddToSkillChangeTracker("Gordin's Steel Bow providing +10 attack.");

        //set up the cancel for this skill at the end of the turn and if the card is removed from the field.
        Owner.endTurnEvent.AddListener(CancelSteelBow);
        RemoveFromFieldEvent.AddListener(CancelSteelBow);
    }

    //This method cancels the effect of Steel Bow at the end of the player's turn or when this card leaves the field.
    private void CancelSteelBow()
    {
        //decreases attack.
        attackModifier -= 10;

        //resets the Once Per Turn ability
        steelBowUsable = true;

        //removes the skill tracking text and callbacks
        RemoveFromSkillChangeTracker("Gordin's Steel Bow providing +10 attack.");
        Owner.endTurnEvent.RemoveListener(CancelSteelBow);
        RemoveFromFieldEvent.RemoveListener(CancelSteelBow);
    }

    //This is an overloaded version of the CancelSteelBow method which allows it to be called from the RemoveFromFieldEvent.
    private void CancelSteelBow(BasicCard superfluous)
    {
        CancelSteelBow();
    }

    //[ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.AttackEmblem();
    }
}
