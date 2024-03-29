﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This static class is meant to do the work for many common abilities.  The idea is that individual cards can provide some set inputs
//and then just call this class to do all the heavy lifting.
public static class AbilitySupport
{
    //[ATK] Hero Emblem [SUPP] Until the end of this combat, the number of Orbs that your <Color> attacking unit's attack will destroy becomes 2.
    //called in the ActivateAttackSupportSkill() method of certain cards.
    public static void HeroEmblem(CipherData.ColorsEnum color)
    {
        //Checks that the current attacker is the given color
        if (GameManager.instance.CurrentAttacker.CardColorArray[(int)color])
        {
            //increase the number of orbs to be destroyed to 2.
            GameManager.instance.numOrbsToBreak = 2;
            
            //updates the card reader.
            CardReader.instance.UpdateGameLog(GameManager.instance.CurrentAttacker.Owner.playerName + "'s supported " 
                + GameManager.instance.CurrentAttacker.Owner.SupportCard.CharName + " activates Hero Emblem. " 
                + GameManager.instance.CurrentAttacker.CharName + "'s attack will destroy 2 orbs!");

            //tracks the boost.
            GameManager.instance.CurrentAttacker.AddToSkillChangeTracker("Hero Emblem granting the ability to destroy 2 orbs.");
            GameManager.instance.CurrentAttacker.AfterBattleEvent.AddListener(EndHeroEmblem);
        }

        //return control to the battle command.
        GameManager.instance.ActivateDefenderSupport();
    }

    //This method removes the display and callback for Hero Emblem after the attack has concluded.
    private static void EndHeroEmblem()
    {
        GameManager.instance.CurrentAttacker.RemoveFromSkillChangeTracker("Hero Emblem granting the ability to destroy 2 orbs.");
        GameManager.instance.CurrentAttacker.AfterBattleEvent.RemoveListener(EndHeroEmblem);
    }

    //[ATK] Elysian Emblem [SUPP] Choose 1 ally other than your attacking unit. You may move that ally.
    public static void ElysianEmblem()
    {
        //Checks that there is more than one other ally besides the attacking card in play.
        if (GameManager.instance.CurrentAttacker.OtherAllies.Count > 0)
        {
            //check if the player wants to activate this skill.  Call a dialogue box.
            DialogueWindowDetails details = new DialogueWindowDetails
            {
                windowTitleText = "Elysian Emblem",
                questionText = "Would you like to activate the supported " 
                + GameManager.instance.CurrentAttacker.Owner.SupportCard.CharName + "'s Elysian Emblem?" +
                "\n\n[ATK] Elysian Emblem [SUPP] Choose 1 ally other than your attacking unit. You may move that ally.",
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
    private static void TargetElysianEmblem()
    {
        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(ActivateElysianEmblem);

        //makes the player choose another ally for the skill's effect.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = GameManager.instance.CurrentAttacker.OtherAllies,
            numberOfCardsToPick = 1,
            locationText = GameManager.instance.CurrentAttacker.Owner.playerName + "'s Field",
            instructionText = "Please choose one ally to move using Elysian Emblem.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //Actually moves the chosen unit.  Can be soft canceled.
    private static void ActivateElysianEmblem(List<BasicCard> list)
    {
        if (list.Count > 0)
        {
            //updates the card reader.
            CardReader.instance.UpdateGameLog(GameManager.instance.CurrentAttacker.Owner.playerName + "'s supported "
                + GameManager.instance.CurrentAttacker.Owner.SupportCard.CharName + " activates Elysian Emblem.");

            GameManager.instance.CurrentAttacker.Owner.MoveCard(list[0]);
        }

        //Returns control to the battle logic.
        GameManager.instance.ActivateDefenderSupport();
    }

    //[ATK] Attack Emblem [SUPP] Until the end of this combat, your attacking unit gains +20 attack.
    public static void AttackEmblem()
    {
        GameManager.instance.CurrentAttacker.attackModifier += 20;

        //reports the boost
        CardReader.instance.UpdateGameLog(GameManager.instance.CurrentAttacker.Owner.playerName + "'s supported "
                + GameManager.instance.CurrentAttacker.Owner.SupportCard.CharName + " activates Attack Emblem. "
                + GameManager.instance.CurrentAttacker.CharName + "'s attack is boosted by +20 for this battle!");
        GameManager.instance.CurrentAttacker.AddToSkillChangeTracker("Attack Emblem providing +20 attack.");
        GameManager.instance.CurrentAttacker.AfterBattleEvent.AddListener(EndAttackEmblem);

        //continues the battle logic
        GameManager.instance.ActivateDefenderSupport();
    }

    //This method removes the boost and assorted displays and callbacks for Attack Emblem after the attack has concluded.
    private static void EndAttackEmblem()
    {
        GameManager.instance.CurrentAttacker.attackModifier -= 20;
        GameManager.instance.CurrentAttacker.RemoveFromSkillChangeTracker("Attack Emblem providing +20 attack.");
        GameManager.instance.CurrentAttacker.AfterBattleEvent.RemoveListener(EndAttackEmblem);
    }

    //[DEF] Defense Emblem [SUPP] Until the end of this combat, your defending unit gains +20 attack.
    public static void DefenseEmblem()
    {
        GameManager.instance.CurrentDefender.attackModifier += 20;

        //reports the boost
        CardReader.instance.UpdateGameLog(GameManager.instance.CurrentDefender.Owner.playerName + "'s supported "
                + GameManager.instance.CurrentDefender.Owner.SupportCard.CharName + " activates Defense Emblem. "
                + GameManager.instance.CurrentDefender.CharName + "'s attack is boosted by +20 for this battle!");
        GameManager.instance.CurrentDefender.AddToSkillChangeTracker("Defense Emblem providing +20 attack.");
        GameManager.instance.CurrentDefender.AfterBattleEvent.AddListener(EndDefenseEmblem);

        //continues the battle logic
        GameManager.instance.AddSupportValues();
    }

    //This method removes the boost and assorted displays and callbacks for Defense Emblem after the attack has concluded.
    private static void EndDefenseEmblem()
    {
        GameManager.instance.CurrentDefender.attackModifier -= 20;
        GameManager.instance.CurrentDefender.RemoveFromSkillChangeTracker("Defense Emblem providing +20 attack.");
        GameManager.instance.CurrentDefender.AfterBattleEvent.RemoveListener(EndDefenseEmblem);
    }

    //Anti-Fliers [ALWAYS] If this unit is attacking a <Flier>, this unit gains +30 attack.
    public static void AntiFliers(bool attacking)
    {
        //Only apply if the calling card is attacking.
        if (attacking)
        {
            //check if the defender is a flier.
            if (GameManager.instance.CurrentDefender.UnitTypeArray[(int)CipherData.TypesEnum.Flier])
            {
                //boost attack
                GameManager.instance.CurrentAttacker.attackModifier += 30;

                //report the boost
                CardReader.instance.UpdateGameLog(GameManager.instance.CurrentAttacker.CharName + "'s Anti-Fliers skill provides " +
                    "+30 attack against " + GameManager.instance.CurrentDefender.CharName + "!");
                GameManager.instance.CurrentAttacker.AddToSkillChangeTracker("Anti-Fliers skill providing +30 attack.");

                //add a callback to remove the boost.
                GameManager.instance.CurrentAttacker.AfterBattleEvent.AddListener(EndAntiFliersBoost);
            }
        }
    }

    //This method removes the skill modifier information from the card after the battle.
    private static void EndAntiFliersBoost()
    {
        //remove the boost
        GameManager.instance.CurrentAttacker.attackModifier -= 30;

        //remove the notification from the SkillChangeTracker.
        GameManager.instance.CurrentAttacker.RemoveFromSkillChangeTracker("Anti-Fliers skill providing +30 attack.");

        //remove the callback.
        GameManager.instance.CurrentAttacker.AfterBattleEvent.RemoveListener(EndAntiFliersBoost);
    }

    //[DEF] Miracle Emblem [SUPP] Until the end of this combat, your opponent's attacking unit cannot perform a Critical Hit.
    public static void MiracleEmblem()
    {
        //Removes the attacker's ability to crit.
        GameManager.instance.PreventCritical();

        //reports the effect
        CardReader.instance.UpdateGameLog(GameManager.instance.CurrentDefender.Owner.playerName + "'s supported "
                + GameManager.instance.CurrentDefender.Owner.SupportCard.CharName + " activates Miracle Emblem. "
                + GameManager.instance.CurrentAttacker.Owner.playerName + "'s attacking " +
                GameManager.instance.CurrentAttacker.CharName + " cannot perform a Critical Hit in this battle!");

        GameManager.instance.CurrentAttacker.AddToSkillChangeTracker("Miracle Emblem prevents Critical Hits in this battle.");

        //sets up the cancel callback.
        GameManager.instance.CurrentDefender.AfterBattleEvent.AddListener(EndMiracleEmblem);

        //continues the battle logic
        GameManager.instance.AddSupportValues();
    }

    //This method removes the displays and callbacks for Miracle Emblem after the attack has concluded.
    //Note that the ability to crit is automatically re-enabled at the end of each battle by the GameManager.
    private static void EndMiracleEmblem()
    {
        GameManager.instance.CurrentAttacker.RemoveFromSkillChangeTracker("Miracle Emblem prevents Critical Hits in this battle.");
        GameManager.instance.CurrentDefender.AfterBattleEvent.RemoveListener(EndMiracleEmblem);
    }

    //[ATK] Magic Emblem [SUPP] Draw 1 card. Choose 1 card from your hand, and send it to the Retreat Area.
    public static void MagicEmblem()
    {
        //reports the boost
        CardReader.instance.UpdateGameLog(GameManager.instance.CurrentAttacker.Owner.playerName + "'s supported "
                + GameManager.instance.CurrentAttacker.Owner.SupportCard.CharName + " activates Magic Emblem.");

        //Draw.
        GameManager.instance.CurrentAttacker.Owner.Draw(1);

        //This sets up the method to call after the CardPicker finishes setting up the discard choice.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(FinishMagicEmblem);

        //makes the player choose one card to discard from their hand.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = GameManager.instance.CurrentAttacker.Owner.Hand,
            numberOfCardsToPick = 1,
            locationText = GameManager.instance.CurrentAttacker.Owner.playerName + "'s Hand",
            instructionText = "Please choose one card to discard for Magic Emblem. (The final card in the list was just drawn.)",
            mayChooseLess = false,
            effectToActivate = eventToCall
        };

        CardPickerWindow.Instance().ChooseCards(details);
    }

    //This method receives the list containing the chosen card to discard and actually discards it, completing the Magic Emblem effect.
    private static void FinishMagicEmblem(List<BasicCard> list)
    {
        if (list.Count != 1)
        {
            Debug.LogError("FinishMagicEmblem() was sent " + list.Count + " cards in a list instead of the assumed one. Investigate!");
        }
        else
        {
            GameManager.instance.CurrentAttacker.Owner.DiscardCardFromHand(list[0]);
        }

        //continues the battle logic
        GameManager.instance.ActivateDefenderSupport();
    }
}
