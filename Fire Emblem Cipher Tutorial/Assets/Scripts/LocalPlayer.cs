using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//This class represents a local human player in the game.

public class LocalPlayer : DecisionMaker
{
    public BasicCard attacker;

    /*
    //A constructor requiring the cardManager
    public LocalPlayer(CardManager manager)
    {
        cardManager = manager;
    }
    

    //A constructor which inherits from the DecisionMaker class constructor.
    public LocalPlayer(DeckList decklist, CardManager cm, string name) : base(decklist, cm, name)
    {
        
    }
    */

    //This method allows a local human player to choose their MC and decide whether to mulligan.
    public override void PlayerSetup()
    {
        GameManager.instance.StartCoroutine(GameManager.instance.HumanPlayerSetup(this));
    }

    //This method checks if the player wants to use the default MC.
    public void CheckDefaultMC()
    {
        //First check if there is a default MC.
        if (decklist.DefaultMC != null)
        {
            //Find the MC card in the deck.  -1 if not found.
            int MCIndex = CardManager.Deck.FindIndex(x => x.CardNumber.Equals(decklist.DefaultMC));

            if (MCIndex >= 0)
            {
                List<BasicCard> defaultMCList = new List<BasicCard>(1);
                defaultMCList.Add(CardManager.Deck[MCIndex]);

                //display the defaultMC in the Card Reader
                CardReader.instance.DisplayCard(defaultMCList[0].gameObject);

                //Ask if the player wants to use the Default Lord
                DialogueWindowDetails details = new DialogueWindowDetails
                {
                    windowTitleText = PlayerName + "'s Main Character Choice",
                    questionText = "Would you like to use " + defaultMCList[0].CharName
                        + " as your Main Character or choose a different one?",
                    button1Details = new DialogueButtonDetails
                    {
                        buttonText = "Use " + defaultMCList[0].CharName,
                        buttonAction = () => { SetMC(defaultMCList); },
                    },
                    button2Details = new DialogueButtonDetails
                    {
                        buttonText = "See All Choices",
                        buttonAction = () => { ChooseMC(); },
                    }
                };

                DialogueWindow dialogueWindow = DialogueWindow.Instance();
                dialogueWindow.MakeChoice(details);
            }
            //Could not find the MC in the deck; have the player choose one.
            else
            {
                Debug.LogWarning("Method CheckDefaultMC could not find the DeckList's DefaultMC: " + decklist.DefaultMC
                    + " in " + PlayerName + "'s Deck.");
                ChooseMC();
            }
        }
        //If no default MC, then choose one.
        else
        {
            Debug.LogError("No default MC assigned for " + PlayerName + "'s DeckList.");
            ChooseMC();
        }
    }

    //This method lets the human player choose their MC from all possible 1 cost cards in the deck.
    public void ChooseMC()
    {
        //Identify which cards are possible lords
        List<BasicCard> potentialMCs = new List<BasicCard>(CardManager.Deck.Count);

        for (int i = 0; i < CardManager.Deck.Count; i++)
        {
            if (CardManager.Deck[i].DeploymentCost == 1)
            {
                potentialMCs.Add(CardManager.Deck[i]);
            }
        }

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(SetMC);

        //makes the player choose a Cost 1 card as their Main Character (Lord).
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = potentialMCs,
            numberOfCardsToPick = 1,
            locationText = PlayerName + "'s Deck",
            instructionText = "Please choose a Cost 1 card to serve as your Main Character.",
            mayChooseLess = false,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();
        cardPicker.ChooseCards(details);
    }

    //This method actually sets the human player's MC and then checks for mulligans.
    public void SetMC(List<BasicCard> oneCard)
    {
        if (oneCard.Count < 1)
        {
            Debug.LogError("CardPicker returned an improper list to " + PlayerName + ".SetMC(). " +
                "List had 0 cards. Investigate!");
            return;
        }
        else if (oneCard.Count > 1)
        {
            Debug.LogWarning("CardPicker returned an improper list to " + PlayerName + ".SetMC(). " +
                "List had multiple cards. Investigate!");
        }

        CardManager.SetMCAtStart(oneCard[0]);
        CardReader.instance.DisplayGameLog();
    }

    //lets a human player choose to keep their hand or mulligan once. 
    //Moves to the PostMulligan method to choose the next appropriate action based on the board set up/status of the beginning.
    public void MulliganChoice()
    {
        //lets the player choose whether to mulligan their hand or not.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = PlayerName + ": Mulligan",
            questionText = "Would you like to keep your hand or draw a new one?",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Keep Hand",
                buttonAction = () => { PostMulligan(); },
            },
            button2Details = new DialogueButtonDetails
            {
                buttonText = "Draw New Hand",
                buttonAction = () => {
                    CardReader.instance.UpdateGameLog(PlayerName + " has mulliganed.");
                    CardManager.Mulligan(); PostMulligan();
                },
            }
        };

        DialogueWindow dialogueWindow = DialogueWindow.Instance();
        dialogueWindow.MakeChoice(details);
    }

    
    //This method helps make the transition from one DecisionMaker's Setup phase to the next.
    protected override void PostMulligan()
    {
        //Check if the opponent has already gone through the MC choice and Mulligan process.
        if (Opponent.CardManager.MCStack == null)
        {
            //Check if the opponent is also human.  If so, we need to hide the MC and allow time for a transition.
            //Player 1 needs to move the field view so that their new hand isn't showing. 
            if (Opponent is LocalPlayer)
            {
                //hide player 1 MC.
                CardManager.MCCard.FlipFaceDown();

                string buttonText = Opponent.PlayerName + " MC Choice";

                GameManager.instance.SetPhaseButtonAndHint("Press the red button to let the second player start choosing their MC.", 
                    buttonText, HandOff);

            }
            else //The Opponent is not Human/Local so for now is an AI.
            {
                //Comment out the following two lines to allow the AI to go first.
                /*
                GameManager.instance.turnPlayer = Opponent.CardManager;
                GameManager.instance.turnAgent = Opponent;
                */
                Opponent.PlayerSetup();
            }
        }
        else
            GameManager.instance.BeginGame();
    }
    
    //This method hides the first player's hand once they are ready for the second player to start their set up.
    public void HandOff()
    {
        //Hide card views from the other player.
        CardReader.instance.DisplayGameLog();
        GameManager.instance.turnPlayer = Opponent.CardManager;
        GameManager.instance.turnAgent = Opponent;
        GameManager.instance.ShowHand(Opponent);

        Opponent.PlayerSetup();
    }

    //This method gives a local human player the option to make decisions during the Beginning Phase.
    public override void OnBeginningPhase()
    {
        /* 
         * There isn't really a reason to have the game pause here as there's nothing for the player to do except hand off, so I'm removing that pause.
        string hint = "Push the red button to proceed with " + PlayerName + "'s turn.";

        //UnityAction buttonAction = () => { ContextMenu.instance.ClosePanel(); GameManager.instance.BondPhase(); };

        GameManager.instance.SetPhaseButtonAndHint(hint, "Begin Bond Phase", base.OnBeginningPhase);
        */
        base.OnBeginningPhase();
    }

    //This method allows a local human player to bond cards during their Bond Phase.
    public override void OnBondPhase()
    {
        string hint = "Choose up to one card from your hand to place in the bond area.  Click the red button when you are finished.";
        if (GameManager.instance.FirstTurn)
        {
            //hint += " In the beginning of the game, it's generally a good idea to bond non-main-character units with a high deployment cost.";
        }

        GameManager.instance.SetPhaseButtonAndHint(hint, "End Bond Phase", base.OnBondPhase);
    }

    //This method allows a local human player to deploy cards during their Deployment Phase.
    public override void OnDeployPhase()
    {
        GameManager.instance.SetPhaseButtonAndHint("", "End Deploy Phase", base.OnDeployPhase);

        UpdateDeploymentHintText();
    }

    //Updates the hint text based on how many and what color bonds are available at that moment in the Deployment Phase.
    public override void UpdateDeploymentHintText()
    {
        List<string> bondColors = CardManager.BondedColorNames;

        GameManager.instance.hintText.text = "Choose cards from your hand to deploy to the field.  Click the red button when you are finished deploying units.  " +
            "You have " + (CardManager.Bonds.Count - GameManager.instance.bondDeployCount) + " bonds remaining to use for deployment.  You may deploy the following colors: ";

        //posts a well-formatted list of the colors on the face-up bonds to the hintText.
        for (int i = 0; i < bondColors.Count - 1; i++)
        {
            GameManager.instance.hintText.text += bondColors[i] + ", ";
        }
        if (bondColors.Count > 0)
        {
            GameManager.instance.hintText.text += bondColors[bondColors.Count - 1];
        }
    }

    //This method is where a local human player decides where to deploy a card.
    public override void ChooseDeployLocation(BasicCard card)
    {
        //Call a dialogue box.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = "Deployment",
            questionText = "Where would you like to deploy " + card.CharName + "?",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Front Line",
                buttonAction = () => { CardManager.DeployToFrontLine(card); }
            },
            button2Details = new DialogueButtonDetails
            {
                buttonText = "Back Line",
                buttonAction = () => { CardManager.DeployToBackLine(card); }
            }
        };

        DialogueWindow dialogueWindow = DialogueWindow.Instance();
        dialogueWindow.MakeChoice(details);
    }

    //This method allows a local human player to decide the order for triggered card skills to resolve.
    //EDIT: Add an int field to the TriggerEventHandler class so that the below method can check the event for its int and know what text
    //to display to explain what happened (deployment, movement, battle destruction, etc.).
    public override void ChooseAmongTriggeredCards(TriggerEventHandler triggerEvent, List<BasicCard> triggeredCards)
    {
        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(triggerEvent.CallTriggerSkill);

        //makes the player choose one of the triggered/active cards to resolve first.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = triggeredCards,
            numberOfCardsToPick = 1,
            locationText = GameManager.instance.turnAgent.PlayerName + "'s Cards",
            instructionText = "The below cards have a skill triggered by " + triggerEvent.TriggeringCard.CharName + "'s deployment.  Please choose one card to resolve first.",
            mayChooseLess = false,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();
        cardPicker.ChooseCards(details);
    }

    //This method begins the human player logic to resolve the skill effect of whatever card was triggered.
    public override void ActivateTriggerSkill(BasicCard activatedCard, BasicCard triggeringCard)
    {
        activatedCard.ResolveTriggerSkillLP(triggeringCard);
    }

    //This method allows a local human player to perform different actions including attack and activate skills during their Action Phase.
    public override void OnActionPhase()
    {
        GameManager.instance.SetPhaseButtonAndHint("You may activate effects, move your units, and attack the opponent. You can do any of these actions in any order.", "End Turn", base.OnActionPhase);
    }

    //This method provides a way for the decision maker to choose whether to use a bond skill.
    //This function is not needed in the Local Player logic
    public override bool ShouldFlipBonds(BasicCard card, int numBondsToFlip)
    {
        Debug.LogError("The LocalPlayer has been asked whether to flip bonds.");
        return false;
    }

    //This method allows a local player to choose which bonds should be flipped due to a skill's activation or effect.
    //Note that since this is being called in the middle of a skill effect, I am not going to allow for a soft cancel.
    //I also can't both allow for a soft cancel and easily enforce that all required bonds were flipped.
    public override void ChooseBondsToFlip(BasicCard card, int numToFlip, string skillText)
    {
        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(CardManager.FlipBonds);

        //makes the player choose the faceup bond cards to flip for whatever effect.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = CardManager.FaceUpBonds,
            numberOfCardsToPick = numToFlip,
            locationText = playerName + "'s Bonds",
            instructionText = "Please choose " + numToFlip + " bond card",
            mayChooseLess = false,
            effectToActivate = eventToCall
        };

        //make the instruction text plural if we need to flip more than one bond.
        if (numToFlip > 1)
        {
            details.instructionText += "s";
        }

        details.instructionText += " to flip to activate " + card.CharName + "'s skill:\n\n" + skillText;

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //This method provides a local player with the choice of whether to use Elysian Emblem.
    //[ATK] Elysian Emblem [SUPP] Choose 1 ally other than your attacking unit. You may move that ally.
    public override void TryElysianEmblem()
    {
        //check if the player wants to activate this skill.  Call a dialogue box.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = "Elysian Emblem",
            questionText = "Would you like to activate the supported "
            + CardManager.SupportCard.CharName + "'s Elysian Emblem?" +
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

    //Choose a friendly target for Elysian Emblem.  Can be soft canceled.
    private void TargetElysianEmblem()
    {
        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(AbilitySupport.ActivateElysianEmblem);

        //makes the player choose another ally for the skill's effect.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = GameManager.instance.CurrentAttacker.OtherAllies,
            numberOfCardsToPick = 1,
            locationText = PlayerName + "'s Field",
            instructionText = "Please choose one ally to move using Elysian Emblem.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //This method provides a way for the local player to choose which cards should be discarded from the Hand due to a skill's activation or effect.
    public override void ChooseCardsToDiscardFromHand(BasicCard card, List<BasicCard> listToChooseFrom, int numToDiscard, string skillText)
    {
        //This sets up the method to call after the CardPicker finishes setting up the discard choice.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(CardManager.DiscardCardsFromHand);

        //makes the player choose one card to discard from their hand.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = listToChooseFrom,
            numberOfCardsToPick = numToDiscard,
            locationText = PlayerName + "'s Hand",
            instructionText = "Please choose " + numToDiscard + " card",
            mayChooseLess = false,
            effectToActivate = eventToCall
        };

        //make the instruction text plural if we need to discard more than one card.
        if (numToDiscard > 1)
        {
            details.instructionText += "s";
        }

        details.instructionText += " to discard for " + card.CharName + "'s skill:\n\n" + skillText;

        CardPickerWindow.Instance().ChooseCards(details);
    }

    //This method helps a local player determine its attack target.
    public override void ChooseAttackTarget(BasicCard aggressor, int expectedAttack, List<BasicCard> targets)
    {
        //Remember the attacker to start the attack.
        attacker = aggressor;

        //have the local player choose an enemy to be attacked with the card picker.
        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(DeclareAttack);

        //makes the player choose a target on the Opponent's field for the attack.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = targets,
            numberOfCardsToPick = 1,
            locationText = Opponent.PlayerName + "'s Field",
            instructionText = "Please choose one unit to target with " + aggressor.CharName + "'s attack.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //This is the method that actually processes the initiation of the attack.
    protected void DeclareAttack(List<BasicCard> list)
    {
        //Only proceed if there is actually a target for the attack.
        if (list.Count > 0)
        {
            GameManager.instance.StartBattle(attacker, list[0]);
        }
        else
        {
            //remove the attacker reference if the attck is soft canceled.
            Debug.Log("No attack target chosen.");
            attacker = null;
        }
    }

    //This methods let a local player decide whether to lauch a critical hit.
    public override void DecideToCrit()
    {
        //give the player the choice to perform a critical hit. Call a dialogue box.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = PlayerName + "'s Critical Hit",
            questionText = PlayerName + ": Discard a " + GameManager.instance.CurrentAttacker.CharName + " to activate a critical hit and double your attack power?",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Yes",
                buttonAction = () => { CriticalHitChoice(); }
            },
            button2Details = new DialogueButtonDetails
            {
                buttonText = "No",
                buttonAction = () => { GameManager.instance.EvadeChoice(); }
            }
        };

        DialogueWindow dialogueWindow = DialogueWindow.Instance();
        dialogueWindow.MakeChoice(details);
    }

    //This method calls the CardPicker to determine which card in the hand should be discarded for the critical hit.
    private void CriticalHitChoice()
    {
        //find the cards in the hand that have the same name as the attacking unit.
        List<BasicCard> possibleDiscards = CardManager.Hand.FindAll(x => x.CompareNames(GameManager.instance.CurrentAttacker));

        /*
        List<BasicCard> possibleDiscards = new List<BasicCard>(CardManager.Hand.Count);
        for (int i = 0; i < CardManager.Hand.Count; i++)
        {
            if (CardManager.Hand[i].CompareNames(GameManager.instance.CurrentAttacker))
            {
                possibleDiscards.Add(CardManager.Hand[i]);
            }
        }
        */

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(ActivateCriticalHit);

            //makes the player choose a card to discard for the critical hit.
            CardPickerDetails details = new CardPickerDetails
            {
                cardsToDisplay = possibleDiscards,
                numberOfCardsToPick = 1,
                locationText = PlayerName + "'s Hand",
                instructionText = PlayerName + ": Please choose one card to discard to activate " + GameManager.instance.CurrentAttacker.CharName + "'s critical hit.",
                mayChooseLess = true,
                effectToActivate = eventToCall
            };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }


    //Actually processes the critical hit.  Is semi-cancelable by not choosing any cards.
    private void ActivateCriticalHit(List<BasicCard> list)
    {
        if (list.Count > 0)
        {
            GameManager.instance.criticalHit = true;
            CardReader.instance.UpdateGameLog(PlayerName + "'s " + GameManager.instance.CurrentAttacker.CharName + " activates a critical hit!");
            CardManager.DiscardCardsFromHand(list);
            GameManager.instance.DisplayAttackValues();
        }

        //Call the evade choice method for the opponent
        GameManager.instance.EvadeChoice();
    }

    //This methods let a local player decide whether to evade an incoming attack.
    public override void DecideToEvade()
    {
        /*
         * The old structure had an inter-player hand-off here.  I don't think that hand-off really adds anything 
         * given that it's only called when a player can Evade.  Thus, I'm going to jump straight to the player's choice to evade.
         * 
        //reveals this player's hand so that they can make an evasion choice
        ShowHand(turnAgent.Opponent);

        string hintText = PlayerName + ": Push the button to resolve your evasion.";

        string buttonText = PlayerName + " Evade";

        GameManager.instance.SetPhaseButtonAndHint(hintText, buttonText, ChooseEvasion);
        */

        //give the player the choice to perform an evade. Call a dialogue box.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = PlayerName + "'s God-Speed Evasion",
            questionText = PlayerName + ": Discard a " + GameManager.instance.CurrentDefender.CharName + " to activate a god-speed evasion?",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Yes",
                buttonAction = () => { ActivateEvasion(); }
            },
            button2Details = new DialogueButtonDetails
            {
                buttonText = "No",
                buttonAction = () => { GameManager.instance.BattleCalculation(); }
            }
        };

        DialogueWindow dialogueWindow = DialogueWindow.Instance();
        dialogueWindow.MakeChoice(details);

    }

    //This method calls the CardPicker to choose the card to discard for a god-speed evasion.
    private void ActivateEvasion()
    {
        //find the cards in the hand that have the same name as the defending unit.
        List<BasicCard> possibleDiscards = CardManager.Hand.FindAll(x => x.CompareNames(GameManager.instance.CurrentDefender));

        /*
        for (int i = 0; i < turnPlayer.Opponent.Hand.Count; i++)
        {
            if (turnPlayer.Opponent.Hand[i].CompareNames(CurrentDefender, true))
            {
                possibleDiscards.Add(turnPlayer.Opponent.Hand[i]);
            }
        }
        */

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(ResolveEvasion);

        //makes the player choose a card to discard to activate the god-speed evasion.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = possibleDiscards,
            numberOfCardsToPick = 1,
            locationText = PlayerName + "'s Hand",
            instructionText = PlayerName + ": Choose one card to discard to activate " + GameManager.instance.CurrentDefender.CharName + "'s god-speed evasion.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //Actually processes the god-speed evasion.  Is semi-cancelable by not choosing any cards.
    private void ResolveEvasion(List<BasicCard> list)
    {
        if (list.Count > 0)
        {
            CardReader.instance.UpdateGameLog(PlayerName + "'s " + GameManager.instance.CurrentDefender.CharName + " activates a god-speed evasion!");
            CardManager.DiscardCardsFromHand(list);
            //Opponent.DefenderEvaded();        //I don't think this call is actually necessary at this point yet.
            GameManager.instance.EndBattle();
        }
        else
        {
            GameManager.instance.BattleCalculation();
        }
    }

    //This method allows a local human player to perform necessary actions during their End Phase.
    public override void OnEndPhase()
    {
        //There isn't really a reason to pause the game at this point, so let's just pass to the other player.
        //GameManager.instance.SetPhaseButtonAndHint("Many skill effects end in the End Phase. Once ready, push the button to begin the next player's turn.", "Begin Next Turn", base.OnEndPhase);
        base.OnEndPhase();
    }
}
