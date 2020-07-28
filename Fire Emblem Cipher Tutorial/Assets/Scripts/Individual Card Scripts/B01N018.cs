using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N018 : BasicCard
{
    /*
     * B01-018HN
     * Ogma: Loyal Blade
     * “The king has bid me and my men join your army.”
     * Meisai
     * 
     * Mercenary Captain [ALWAYS] During your turn, if you have 2 or more other allies with a Deployment Cost of 2 or less, this unit gains +20 attack.
     * [ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
     * 
     * Mercenary
     * 1
     * Red
     * Male
     * Sword
     * ATK: 40
     * SUPP: 10
     * Range: 1
     */

    private bool mercenaryCaptainActive = false;

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //This method activates all skills that can be triggered while the card is on the field.
    //I'm giving the Mercenary Captain skill a higher priority than say deployment triggered abilites.
    //This is because, the card must be deployed for the trigger to activate, but as soon as the card hits the field, the [ALWAYS] skill is in effect, at least in my interpretation.
    //I might have to confirm how this skill overlaps with those if I run into a conflict. 
    public override void ActivateFieldSkills()
    {
        Owner.FieldChangeEvent.AddListener(CheckForMercenaryCaptain);
        Owner.endTurnEvent.AddListener(DisableMercenaryCaptain);
        Owner.BeginTurnEvent.AddListener(CheckForMercenaryCaptain);
    }

    //This method is used to remove skills from event calls when the card is "disabled" (under a stack) and/or removed from the field.
    public override void DeactivateFieldSkills()
    {
        Owner.FieldChangeEvent.RemoveListener(CheckForMercenaryCaptain);
        Owner.endTurnEvent.RemoveListener(DisableMercenaryCaptain);
        Owner.BeginTurnEvent.RemoveListener(CheckForMercenaryCaptain);

        DisableMercenaryCaptain();

        RemoveFromFieldEvent.Invoke(this);
    }


    //Mercenary Captain [ALWAYS] During your turn, if you have 2 or more other allies with a Deployment Cost of 2 or less, this unit gains +20 attack.
    //This method checks to see if we need to activate or deactivate this skill.
    private void CheckForMercenaryCaptain()
    {
        //check if it's the player's turn and there are at least 2 other allies on the field
        if (GameManager.instance.turnPlayer == Owner && OtherAllies.Count >= 2)
        {
            //Count the number of other allies with a Deployment cost of 2 or less.
            int n = 0;
            foreach (BasicCard ally in OtherAllies)
            {
                if (ally.DeploymentCost <= 2)
                {
                    n++;
                }
            }

            //check if there are at least 2 allies with a low deployment cost.
            if (n >= 2)
            {
                ActivateMercenaryCaptain();
            }
            else
            {
                DisableMercenaryCaptain();
            }
        }
        else
        {
            DisableMercenaryCaptain();
        }
    }

    //Mercenary Captain [ALWAYS] During your turn, if you have 2 or more other allies with a Deployment Cost of 2 or less, this unit gains +20 attack.
    //actually activates the Mercenary Captain Effect if needed.
    private void ActivateMercenaryCaptain()
    {
        if (!mercenaryCaptainActive)
        {
            mercenaryCaptainActive = true;
            attackModifier += 20;
            AddToSkillChangeTracker("Ogma's Mercenary Captain skill providing +20 attack.");
            CardReader.instance.UpdateGameLog("Ogma's Mercenary Captain skill raises his attack by +20 until the end of the turn " +
                "as long as he has two low cost allies!");
        }
    }


    //deactivates the MercenaryCaptain Effect if needed.
    private void DisableMercenaryCaptain()
    {
        if (mercenaryCaptainActive)
        {
            mercenaryCaptainActive = false;
            attackModifier -= 20;
            RemoveFromSkillChangeTracker("Ogma's Mercenary Captain skill providing +20 attack.");
        } 
    }

    //[ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.AttackEmblem();
    }
}
