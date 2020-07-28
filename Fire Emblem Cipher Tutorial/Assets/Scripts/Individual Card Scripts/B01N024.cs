using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B01N024 : BasicCard
{
    /*
     * B01-024HN
     * Navarre: Scarlet Sword
     * “...Nay, I'll turn no blade of mine on a woman.”
     * Enomoto
     * 
     * Sword of the Cutthroats [ALWAYS] During your turn, if you have no allies other than this unit and your Main Character, this unit gains +20 attack.
     * [ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
     * 
     * Myrmidon
     * 1
     * Red
     * Male
     * Sword
     * ATK: 40
     * SUPP: 10
     * Range: 1
     */

    private bool cutthroatSwordActive = false;

    // Use this for initialization
    void Awake()
    {
        SetUp();
    }

    //This method activates all skills that can be triggered while the card is on the field.
    public override void ActivateFieldSkills()
    {
        Owner.FieldChangeEvent.AddListener(CheckForCutthroatSword);
        Owner.endTurnEvent.AddListener(DisableCutthroatSword);
        Owner.BeginTurnEvent.AddListener(CheckForCutthroatSword);
    }

    //This method is used to remove skills from event calls when the card is "disabled" (under a stack) and/or removed from the field.
    public override void DeactivateFieldSkills()
    {
        Owner.FieldChangeEvent.RemoveListener(CheckForCutthroatSword);
        Owner.endTurnEvent.RemoveListener(DisableCutthroatSword);
        Owner.BeginTurnEvent.RemoveListener(CheckForCutthroatSword);

        DisableCutthroatSword();

        RemoveFromFieldEvent.Invoke(this);
    }

    //Sword of the Cutthroats [ALWAYS] During your turn, if you have no allies other than this unit and your Main Character, this unit gains +20 attack.
    //This method checks to see if we need to activate or deactivate this skill.
    private void CheckForCutthroatSword()
    {
        //check if it's the player's turn and there is no more than 1 other ally on the field
        if (GameManager.instance.turnPlayer == Owner && OtherAllies.Count <= 1)
        {
            //If there is another ally confirm that it is the MC.
            if (OtherAllies.Count == 1)
            {
                if (OtherAllies[0] != Owner.MCCard)
                {
                    DisableCutthroatSword();
                    return;
                }
            }

            //should only reach this point if all allies are either the MC or Navarre.
            ActivateCutthroatSword();
        }
        else
        {
            DisableCutthroatSword();
        }
    }

    //Sword of the Cutthroats [ALWAYS] During your turn, if you have no allies other than this unit and your Main Character, this unit gains +20 attack.
    //actually activates the Sword of the Cutthroats effect if needed.
    private void ActivateCutthroatSword()
    {
        if (!cutthroatSwordActive)
        {
            cutthroatSwordActive = true;
            attackModifier += 20;
            AddToSkillChangeTracker("Navarre's Sword of the Cutthroats skill providing +20 attack.");
            CardReader.instance.UpdateGameLog("Navarre's Sword of the Cutthroats skill raises his attack by +20 until the end of the turn" +
                " as long as he and the MC are the only cards on the field!");
        }
    }


    //deactivates the Sword of the Cutthroats effect if needed.
    private void DisableCutthroatSword()
    {
        if (cutthroatSwordActive)
        {
            cutthroatSwordActive = false;
            attackModifier -= 20;
            RemoveFromSkillChangeTracker("Navarre's Sword of the Cutthroats skill providing +20 attack.");
        }
    }

    //[ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    public override void ActivateAttackSupportSkill()
    {
        AbilitySupport.AttackEmblem();
    }
}
