using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {

    public static GameManager instance = null;

    public string testDeckFileName;
    public Text hintText;
    public Button phaseButton;
    public Text phaseButtonText;
    public bool cardBonded;                         //Tracks whether or not a card has already been bonded this turn.
    public int bondDeployCount;                    //Tracks the number of bonds used to deploy units this turn.
    public CardManager turnPlayer;
    public LayoutManager playerLayoutManager;
    public LayoutManager enemyLayoutManager;
    public RetreatView playerRetreatView;
    public RetreatView enemyRetreatView;
    public bool playtestMode = true;                //checks if the game is in Playtest mode and if so allows for convenient features.

    private bool firstTurn = false;
    private CipherData.PhaseEnum currentPhase;      //This enum keeps track of the current phase in the game: 0 = Beginning, 1 = Bond, 2 = Deployment, 3 = Action, 4 = End;
    
    private CardManager player1;                    //this is a reference to the Cardmanager for the first player's cards.
    private CardManager player2;
    private DeckList decklist1;
    private DeckList decklist2;

    //this is a reference to the deck being used in this test environment.
    //It is a class level property because it's needed for separate methods (MC setting). 
    private DeckList testDeck;

    //this is a delegate to hold effects that need to be activated once the card picker has been called.
    private delegate void Effect(BasicCard card);
    Effect applyEffect;

    //This is a delegate to hold the next Action the GameManager needs to take.
    private delegate void Action();
    Action nextAction;

    //These are battle related fields/properties
    private BasicCard currentAttacker;
    private BasicCard currentDefender;
    public bool inCombat = false;
    private bool criticalHit = false;
    public int numOrbsToBreak = 1;
    private bool canDefenderEvade = true;
    private bool canAttackerCrit = true;
    public TriggerEventHandler battleDestructionTriggerTracker = new TriggerEventHandler();


    public CipherData.PhaseEnum CurrentPhase { get { return currentPhase; } }
    public bool FirstTurn { get { return firstTurn; } }
    public BasicCard CurrentAttacker { get { return currentAttacker; } }
    public BasicCard CurrentDefender { get { return currentDefender; } }
    public bool CriticalHit { get { return criticalHit; } }

    //Awake is always called before any Start functions
    void Awake()
    {
        //Check if instance already exists
        if (instance == null)

            //if not, set instance to this
            instance = this;

        //If instance already exists and it's not this:
        else if (instance != this)

            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);
    }

    // Use this for initialization
    void Start ()
    {
        
        hintText.text = "Press the red button to the left to start the game.";
        phaseButtonText.text = "Start Game";
        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(SetupGame);
        
        /*
        //Create, save, and load a decklist for player 1.
        DeckList testDeck = CreateTestDeck();
        SaveDeckList(testDeck, testDeckFileName);
        List<BasicCard> loadedDeck = LoadDeckList(testDeckFileName);
        player1 = new CardManager(loadedDeck);
        DrawHand();
        */
    }

    void SetupGame()
    {
        //Create, save, and load a decklist for each player.

        //Create and save the decklist.  Needs to be moved to a Deck creator mode.
        //testDeck = CreateTestDeck();
        //SaveDeckList(testDeck, testDeckFileName);


        decklist1 = LoadDeckList(testDeckFileName);
        decklist2 = LoadDeckList(testDeckFileName);
        player1 = new CardManager(CreateDeck(decklist1), playerLayoutManager, playerRetreatView, "Player 1");

        //creates a second player as well.
        player2 = new CardManager(CreateDeck(decklist2), enemyLayoutManager, enemyRetreatView,"Player 2");

        //sets the two players as opponents
        player1.SetOpponent(player2);
        player2.SetOpponent(player1);




        /*
         *  player1.Draw(1);
         *  player1.DeployToFrontLine(player1.Hand[0]);
         *  player1.FrontLineCards[0].Tap();
         */

        //Sets the first player's MC.
        //Note that the following methods need to be called separately since I don't know how to pause the GameManager's processes.
        CheckDefaultMCPlayer1(player1, decklist1);


    }

    //This method checks if the first player wants to use the default MC.
    private void CheckDefaultMCPlayer1(CardManager firstPlayer, DeckList deckList)
    {
        //First check if there is a default MC.
        if (deckList.DefaultMC != null)
        {
            //Find the MC card in the deck.  -1 if not found.
            int MCIndex = firstPlayer.Deck.FindIndex(x => x.CardNumber.Equals(deckList.DefaultMC));

            if (MCIndex >= 0)
            {
                List<BasicCard> defaultMCList = new List<BasicCard>(1);
                defaultMCList.Add(firstPlayer.Deck[MCIndex]);

                //display the defaultMC in the Card Reader
                CardReader.instance.DisplayCard(defaultMCList[0].gameObject);

                //Ask if the player wants to use the Default Lord
                DialogueWindowDetails details = new DialogueWindowDetails
                {
                    windowTitleText = firstPlayer.playerName + "'s Main Character Choice",
                    questionText = "Would you like to use " + defaultMCList[0].CharName 
                        + " as your Main Character or choose a different one?",
                    button1Details = new DialogueButtonDetails
                    {
                        buttonText = "Use " + defaultMCList[0].CharName,
                        buttonAction = () => { SetPlayer1MC(defaultMCList); },
                    },
                    button2Details = new DialogueButtonDetails
                    {
                        buttonText = "See All Choices",
                        buttonAction = () => { ChooseMCPlayer1(firstPlayer); },
                    }
                };

                DialogueWindow dialogueWindow = DialogueWindow.Instance();
                dialogueWindow.MakeChoice(details);
            }
            //Could not find the MC in the deck; have the player choose one.
            else
            {
                Debug.LogWarning("Method CheckDefaultMC could not find the DeckList's DefaultMC: " + deckList.DefaultMC 
                    + " in " + firstPlayer.playerName + "'s Deck.");
                ChooseMCPlayer1(firstPlayer);
            }
        }
        //If no default MC, then choose one.
        else
        {
            Debug.Log("No default MC assigned to for " + firstPlayer.playerName + "'s DeckList.");
            ChooseMCPlayer1(firstPlayer);
        }
    }

    //This method lets the first player choose their MC from all possible 1 cost cards in the deck.
    public void ChooseMCPlayer1(CardManager firstPlayer)
    {
        //Identify which cards are possible lords
        List<BasicCard> potentialMCs = new List<BasicCard>(firstPlayer.Deck.Count);

        for (int i = 0; i < firstPlayer.Deck.Count; i++)
        {
            if (firstPlayer.Deck[i].DeploymentCost == 1)
            {
                potentialMCs.Add(firstPlayer.Deck[i]);
            }
        }

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(SetPlayer1MC);

        //makes the player choose a Cost 1 card as their Main Character (Lord).
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = potentialMCs,
            numberOfCardsToPick = 1,
            locationText = firstPlayer.playerName + "'s Deck",
            instructionText = "Please choose a Cost 1 card to serve as your Main Character.",
            mayChooseLess = false,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();
        cardPicker.ChooseCards(details);
    }

    //This method actually sets the first player's MC and then triggers a similar set of logic for the second player.
    public void SetPlayer1MC(List<BasicCard> oneCard)
    {
        if (oneCard.Count < 1)
        {
            Debug.LogError("CardPicker returned an improper list to SetPlayer1MC(). List had 0 cards. Investigate!");
            return;
        }
        else if (oneCard.Count > 1)
        {
            Debug.LogWarning("CardPicker returned an improper list to SetPlayer1MC(). List had multiple cards. Investigate!");
        }

        player1.SetMCAtStart(oneCard[0]);
        CardReader.instance.DisplayGameLog();

        hintText.text = "Press the red button to let the second player choose their Main Character.";
        phaseButtonText.text = "Player 2 MC";
        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(Player2MCChoice);
    }

    //A tiny no argument method to allow for the triggering of the second player's MC choice from the Phase Button.
    private void Player2MCChoice()
    {
        CheckDefaultMCPlayer2(player2, decklist2);
    }

    //This method checks if the second player wants to use the default MC.
    private void CheckDefaultMCPlayer2(CardManager secondPlayer, DeckList deckList)
    {
        //First check if there is a default MC.
        if (deckList.DefaultMC != null)
        {
            //Find the MC card in the deck.  -1 if not found.
            int MCIndex = secondPlayer.Deck.FindIndex(x => x.CardNumber.Equals(deckList.DefaultMC));

            if (MCIndex >= 0)
            {
                List<BasicCard> defaultMCList = new List<BasicCard>(1);
                defaultMCList.Add(secondPlayer.Deck[MCIndex]);

                //display the defaultMC in the Card Reader
                CardReader.instance.DisplayCard(defaultMCList[0].gameObject);

                //Ask if the player wants to use the Default Lord
                DialogueWindowDetails details = new DialogueWindowDetails
                {
                    windowTitleText = secondPlayer.playerName + "'s Main Character Choice",
                    questionText = "Would you like to use " + defaultMCList[0].CharName
                        + " as your Main Character or choose a different one?",
                    button1Details = new DialogueButtonDetails
                    {
                        buttonText = "Use " + defaultMCList[0].CharName,
                        buttonAction = () => { SetPlayer2MC(defaultMCList); },
                    },
                    button2Details = new DialogueButtonDetails
                    {
                        buttonText = "See All Choices",
                        buttonAction = () => { ChooseMCPlayer2(secondPlayer); },
                    }
                };

                DialogueWindow dialogueWindow = DialogueWindow.Instance();
                dialogueWindow.MakeChoice(details);
            }
            //Could not find the MC in the deck; have the player choose one.
            else
            {
                Debug.LogWarning("Method CheckDefaultMC could not find the DeckList's DefaultMC: " + deckList.DefaultMC
                    + " in " + secondPlayer.playerName + "'s Deck.");
                ChooseMCPlayer2(secondPlayer);
            }
        }
        //If no default MC, then choose one.
        else
        {
            Debug.Log("No default MC assigned to for " + secondPlayer.playerName + "'s DeckList.");
            ChooseMCPlayer2(secondPlayer);
        }
    }

    //This method lets the second player choose their MC from all possible 1 cost cards in the deck.
    public void ChooseMCPlayer2(CardManager secondPlayer)
    {
        //Identify which cards are possible lords
        List<BasicCard> potentialMCs = new List<BasicCard>(secondPlayer.Deck.Count);

        for (int i = 0; i < secondPlayer.Deck.Count; i++)
        {
            if (secondPlayer.Deck[i].DeploymentCost == 1)
            {
                potentialMCs.Add(secondPlayer.Deck[i]);
            }
        }

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(SetPlayer2MC);

        //makes the player choose a Cost 1 card as their Main Character (Lord).
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = potentialMCs,
            numberOfCardsToPick = 1,
            locationText = secondPlayer.playerName + "'s Deck",
            instructionText = "Please choose a Cost 1 card to serve as your Main Character.",
            mayChooseLess = false,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();
        cardPicker.ChooseCards(details);
    }

    //This method actually sets the second player's MC and then checks for mulligans.
    public void SetPlayer2MC(List<BasicCard> oneCard)
    {
        if (oneCard.Count < 1)
        {
            Debug.LogError("CardPicker returned an improper list to SetPlayer2MC(). List had 0 cards. Investigate!");
            return;
        }
        else if (oneCard.Count > 1)
        {
            Debug.LogWarning("CardPicker returned an improper list to SetPlayer2MC(). List had multiple cards. Investigate!");
        }

        player2.SetMCAtStart(oneCard[0]);
        CardReader.instance.DisplayGameLog();

        hintText.text = "Press the red button to let the first player draw their initial hand.";
        phaseButtonText.text = player1.playerName + " Draw";
        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(DrawHandPlayer1);
    }

    /*
    //This method can serve as a kind of Event holder which performs the appropriate actions given the necessary targets.
    public void ActivateEffect(List<BasicCard> targets)
    {
        if (applyEffect != null)
        {
            Debug.Log("Effect will be applied to " + targets.Count + " cards.");
            for (int i = 0; i < targets.Count; i++)
            {
                Debug.Log("Effect being applied to " + targets[i]);
                applyEffect(targets[i]);
            }
        }
        else
        {
            Debug.Log("The effect to be applied to the chosen cards was not set!");
        }

        //calls the next action to be taken.
        nextAction();

    }
    */

    /*
     * The following methods had to be replaced with more specific ones because of 
     * constraints on how the Game Manager flows through the beginning game logic.
     * I don't know how to make the game pause for new input without just running out of code in a method. :/
     * 
//This method checks if the player wants to use the default MC.
public void CheckDefaultMC(CardManager player, DeckList deckList)
{
    //First check if there is a default MC.
    if (deckList.DefaultMC != null)
    {
        //Find the MC card in the deck.  -1 if not found.
        int MCIndex = player.Deck.FindIndex(x => x.CardNumber.Equals(deckList.DefaultMC));

        if (MCIndex >= 0)
        {
            List<BasicCard> defaultMCList = new List<BasicCard>(1);
            defaultMCList.Add(player.Deck[MCIndex]);

            //display the defaultMC in the Card Reader
            CardReader.instance.DisplayCard(defaultMCList[0].gameObject);

            //Ask if the players wants to use the Default Lord
            DialogueWindowDetails details = new DialogueWindowDetails
            {
                windowTitleText = player.playerName + "'s Main Character Choice",
                questionText = "Would you like to use " + defaultMCList[0].CharName
                    + " as your Main Character or choose a different one?",
                button1Details = new DialogueButtonDetails
                {
                    buttonText = "Use " + defaultMCList[0].CharName,
                    buttonAction = () => { player.SetMCAtStart(defaultMCList); },
                },
                button2Details = new DialogueButtonDetails
                {
                    buttonText = "See All Choices",
                    buttonAction = () => { ChooseMC(player); },
                }
            };

            DialogueWindow dialogueWindow = DialogueWindow.Instance();
            dialogueWindow.MakeChoice(details);
        }
        //Could not find the MC in the deck; have the player choose one.
        else
        {
            Debug.LogWarning("Method CheckDefaultMC could not find the DeckList's DefaultMC: " + deckList.DefaultMC
                + " in " + player.playerName + "'s Deck.");
            ChooseMC(player);
        }
    }
    //If no default MC, then choose one.
    else
    {
        Debug.Log("No default MC assigned to for " + player.playerName + "'s DeckList.");
        ChooseMC(player);
    }
}

//This method lets the player choose their MC from all possible 1 cost cards in the deck.
public void ChooseMC(CardManager player)
{
    //Identify which cards are possible lords
    List<BasicCard> potentialMCs = new List<BasicCard>(player.Deck.Count);

    for (int i = 0; i < player.Deck.Count; i++)
    {
        if (player.Deck[i].DeploymentCost == 1)
        {
            potentialMCs.Add(player.Deck[i]);
        }
    }

    //This sets up the method to call after the CardPicker finishes.
    MyCardListEvent eventToCall = new MyCardListEvent();
    eventToCall.AddListener(player.SetMCAtStart);

    //makes the player choose a Cost 1 card as their Main Character (Lord).
    CardPickerDetails details = new CardPickerDetails
    {
        cardsToDisplay = potentialMCs,
        numberOfCardsToPick = 1,
        locationText = player.playerName + "'s Deck",
        instructionText = "Please choose a Cost 1 card to serve as your Main Character.",
        mayChooseLess = false,
        effectToActivate = eventToCall
    };

    CardPickerWindow cardPicker = CardPickerWindow.Instance();
    cardPicker.ChooseCards(details);
}
*/

    //Draws 6 cards to the player's hand and tells them to consider a mulligan.
    private void DrawHandPlayer1()
    {
        turnPlayer = player1;

        hintText.text = player1.playerName + ": Look over your hand, and decide if you would like to keep it or draw another. " +
            //"It's generally best to have at least one promotion for your Main Character in hand at the start of the game. " +
            "Click the red button when you are ready to choose.";
        phaseButtonText.text = "Mulligan?";
        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(MulliganChoicePlayer1);

        //lets the player look at thier MC while "mulling" a mulligan.
        player1.MCCard.FlipFaceUp();

        player1.Draw(6);
    }

    //lets them choose to keep their hand or mulligan once.
    private void MulliganChoicePlayer1()
    {
        //lets the player choose whether to mulligan their hand or not.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = player1.playerName + ": Mulligan",
            questionText = "Would you like to keep your hand or draw a new one?",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Keep Hand",
                buttonAction = () => { BeginPlayer2Mulligan(); },
            },
            button2Details = new DialogueButtonDetails
            {
                buttonText = "Draw New Hand",
                buttonAction = () => { CardReader.instance.UpdateGameLog(player1.playerName + " has mulliganed.");
                    player1.Mulligan(); BeginPlayer2Mulligan(); },
            }
        };

        DialogueWindow dialogueWindow = DialogueWindow.Instance();
        dialogueWindow.MakeChoice(details);
    }

    private void BeginPlayer2Mulligan()
    {
        //hide player 1 MC.
        player1.MCCard.FlipFaceDown();

        hintText.text = "Press the red button to let the second player draw their initial hand.";
        phaseButtonText.text = player2.playerName + " Draw";
        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(DrawHandPlayer2);
    }

    //Draws 6 cards to the player's hand and tells them to consider a mulligan.
    private void DrawHandPlayer2()
    {
        //Hide card views from the other player.
        CardReader.instance.DisplayGameLog();
        turnPlayer = player2;
        ShowHand(player2);

        hintText.text = player2.playerName + ": Look over your hand, and decide if you would like to keep it or draw another. " +
            //"It's generally best to have at least one promotion for your Main Character in hand at the start of the game. " +
            "Click the red button when you are ready to choose.";
        phaseButtonText.text = "Mulligan?";
        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(MulliganChoicePlayer2);

        //lets the player look at thier MC while "mulling" a mulligan.
        player2.MCCard.FlipFaceUp();

        player2.Draw(6);       
    }

    //This helper method sets up the board for the new incoming turn player.
    //Usually this means flipping hands face-up or down as appropriate.
    public void ShowHand(CardManager newActivePlayer)
    {
        
        //flip the current turn player's hand faceUp.
        foreach (var card in newActivePlayer.Hand)
        {
            if (!card.FaceUp)
            {
                card.FlipFaceUp();
            }
        }

        if (!playtestMode)
        {
            //flip the opponent's hand facedown.
            foreach (var card in newActivePlayer.Opponent.Hand)
            {
                if (card.FaceUp)
                {
                    card.FlipFaceDown();
                }
            }
        }
    }

    //lets them choose to keep their hand or mulligan once. 
    private void MulliganChoicePlayer2()
    {
        //lets the player choose whether to mulligan their hand or not.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = player2.playerName + ": Mulligan",
            questionText = "Would you like to keep your hand or draw a new one?",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Keep Hand",
                buttonAction = () => { BeginGame(); },
            },
            button2Details = new DialogueButtonDetails
            {
                buttonText = "Draw New Hand",
                buttonAction = () => { CardReader.instance.UpdateGameLog(player2.playerName + " has mulliganed.");
                    player2.Mulligan(); BeginGame(); },
            }
        };

        DialogueWindow dialogueWindow = DialogueWindow.Instance();
        dialogueWindow.MakeChoice(details);

    }

    //Begins the game by setting out orbs and flipping the players' MCs face up.
    private void BeginGame()
    {
        hintText.text = "Let's start the game!";
        firstTurn = true;

        CardReader.instance.UpdateGameLog("");
        CardReader.instance.UpdateGameLog(player1.playerName + "'s " + player1.MCCard.CharName + ": " + player1.MCCard.CharTitle + " vs. "
            + player2.playerName + "'s " + player2.MCCard.CharName + ": " + player2.MCCard.CharTitle + ".  Let's start the game!\n");
        CardReader.instance.UpdateGameLog("The first player cannot draw a card or attack on the first turn.");

        player1.AtStart();
        player2.AtStart();

        BeginningPhase();
    }

    private void BeginningPhase()
    {
        currentPhase = CipherData.PhaseEnum.Beginning;

        //Sets the current turnPlayer
        turnPlayer = turnPlayer.Opponent;
        ShowHand(turnPlayer);

        //updates the Game Log
        CardReader.instance.UpdateGameLog("\nBegin " + turnPlayer.playerName + "'s Turn:");

        //Checks the other player (who just finished their turn) for a forced march.
        turnPlayer.Opponent.CheckForcedMarch();

        //resets all turn player's "once per turn" ability flags and resets their active turn abilities.
        turnPlayer.BeginTurnEvent.Invoke();

        //untap all units on the active turn player's field.
        turnPlayer.UntapAllUnits();

        //draws card if not the first turn.
        if (!firstTurn)
        {
            turnPlayer.Draw(1);
        }

        hintText.text = "Push the red button to proceed with " + turnPlayer.playerName + "'s turn.";

        phaseButtonText.text = "Begin Bond Phase";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(ContextMenu.instance.ClosePanel);
        phaseButton.onClick.AddListener(BondPhase);
    }

    private void BondPhase()
    {
        currentPhase = CipherData.PhaseEnum.Bond;
        cardBonded = false;
        CardReader.instance.UpdateGameLog("\nBegin " + turnPlayer.playerName +"'s Bond Phase:");

        hintText.text = "Choose up to one card from your hand to place in the bond area.  Click the red button when you are finished.";
        if (firstTurn)
        {
            //hintText.text += " In the beginning of the game, it's generally a good idea to bond non-main-character units with a high deployment cost.";
        }

        phaseButtonText.text = "End Bond Phase";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(ContextMenu.instance.ClosePanel);
        phaseButton.onClick.AddListener(DeploymentPhase);
    }

    private void DeploymentPhase()
    {
        currentPhase = CipherData.PhaseEnum.Deployment;
        bondDeployCount = 0;
        CardReader.instance.UpdateGameLog("\nBegin " + turnPlayer.playerName + "'s Deployment Phase:");

        UpdateDeploymentHintText();

        phaseButtonText.text = "Finish Deployment Phase";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(ContextMenu.instance.ClosePanel);
        phaseButton.onClick.AddListener(ActionPhase);
    }

    public void UpdateDeploymentHintText ()
    {
        List<string> bondColors = turnPlayer.BondedColorNames;

        hintText.text = "Choose cards from your hand to deploy to the field.  Click the red button when you are finished deploying units.  " +
            "You have " + (turnPlayer.Bonds.Count - bondDeployCount) + " bonds remaining to use for deployment.  You may deploy the following colors: ";

        //posts a well-formatted list of the colors on the face-up bonds to the hintText.
        for (int i = 0; i < bondColors.Count - 1; i++)
        {
            hintText.text += bondColors[i] + ", ";
        }
        if (bondColors.Count > 0)
        {
            hintText.text += bondColors[bondColors.Count - 1];
        }

    }

    private void ActionPhase()
    {
        currentPhase = CipherData.PhaseEnum.Action;
        CardReader.instance.UpdateGameLog("\nBegin " + turnPlayer.playerName + "'s Action Phase:");

        hintText.text = "You may activate effects, move your units, and attack the opponent. You can do any of these actions in any order.";

        phaseButtonText.text = "End Turn";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(ContextMenu.instance.ClosePanel);
        phaseButton.onClick.AddListener(EndPhase);
    }

    private void EndPhase()
    {
        currentPhase = CipherData.PhaseEnum.End;
        CardReader.instance.UpdateGameLog("\nBegin " + turnPlayer.playerName + "'s End Phase:");

        hintText.text = "Many skill effects end in the End Phase. Once ready, push the button to begin the next player's turn.";

        phaseButtonText.text = "Begin next turn";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(ContextMenu.instance.ClosePanel);
        phaseButton.onClick.AddListener(BeginningPhase);

        /*
         * Any Skills that activate can do so, and any Skills that stop being active at the end of the turn now stop (such as Attack boosts).
         * You may now proceed to your opponent’s turn, which shifts to your opponent's Beginning Phase.
         */

        turnPlayer.endTurnEvent.Invoke();
        firstTurn = false;
    }

    //This method calls the CardPicker to choose which of the opponent's cards will be attacked.
    //It is not binding as you may choose 0 targets for the attack which cancels the action.
    public void AimAttack(BasicCard attacker)
    {
        //Save the attacking card for the method called after the choice.
        currentAttacker = attacker;

        //choose an enemy to be attacked.

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(DeclareAttack);

        //makes the player choose a target on the Opponent's field for attack.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = attacker.AttackTargets,
            numberOfCardsToPick = 1,
            locationText = turnPlayer.Opponent.playerName + "'s Field",
            instructionText = "Please choose one unit to target with " + attacker.CharName + "'s attack.",
            mayChooseLess = true,
            effectToActivate = eventToCall
        };

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //This is the method that actually processes the initiation of the attack.
    private void DeclareAttack(List<BasicCard> list)
    {
        //Only proceed if there is actually a target for the attack.
        if (list.Count > 0)
        {
            inCombat = true;

            currentDefender = list[0];

            //Tap the attacking card.
            currentAttacker.Tap();

            CardReader.instance.UpdateGameLog("\n" + turnPlayer.playerName + "'s " + currentAttacker.CharName + " attacks " 
                + turnPlayer.Opponent.playerName + "'s " + currentDefender.CharName + "!");

            //reset the battleModifiers that affect attack.
            currentAttacker.battleModifier = 0;
            currentDefender.battleModifier = 0;

            //Once an attack is declared, any AUTO or TRIGGER skills that activate upon attacking go here.
            //These may need to be separated out into small methods of their own.  We'll see as this gets expanded.

            //NOTE: Having these sequential may mean that the effects/called windows overwrite each other.  Be careful when adding events here!
            //First the player's skills activate in the order the player chooses. (:/)
            //Feeding 'true' represents attacking.
            CurrentAttacker.DeclareAttackEvent.Invoke(true);

            //Then the Opponent's skills activate after the player's again in the order the active player chooses (I assume).
            //Feeding 'false' represents defending.
            CurrentDefender.DeclareAttackEvent.Invoke(false);

            //Once skills have been activated, supports are rolled.  This is processed in another method.
            hintText.text = "Once ready, push the button to continue the attack and reveal support cards.";

            phaseButtonText.text = "Reveal Supports";

            phaseButton.onClick.RemoveAllListeners();
            phaseButton.onClick.AddListener(SupportRoll);
        }
        else
        {
            //remove the currentAttacker reference if the attck is soft canceled.
            currentAttacker = null;
        }
    }

    //This method takes care of the high level support roll for both players as well as checking for self-supports.
    //It currently also adds the support value to the cards for attack calculation.
    private void SupportRoll()
    {
        turnPlayer.PlaySupportCard();
        
        //Check player for self-supports
        if (turnPlayer.SupportCard != null)
        {
            if (turnPlayer.SupportCard.CompareNames(currentAttacker))
            {
                CardReader.instance.UpdateGameLog("Due to a self-support, " + turnPlayer.playerName + "'s support draw fails!");
                turnPlayer.DiscardSupport();
            }
        }

        turnPlayer.Opponent.PlaySupportCard();

        //Check opponent for self-supports
        if (turnPlayer.Opponent.SupportCard != null)
        {
            if (turnPlayer.Opponent.SupportCard.CompareNames(currentDefender))
            {
                CardReader.instance.UpdateGameLog("Due to a self-support, " + turnPlayer.Opponent.playerName + "'s support draw fails!");
                turnPlayer.Opponent.DiscardSupport();
            }
        }

        //Activates skills based on supporting cards for both the attacker and the defender.
        //These may need to be separated out into small methods of their own.  We'll see as this gets expanded.
        //NOTE: Having these sequential may mean that the effects/called windows overwrite each other.  Be careful when adding events here!
        //First the player's skills activate in the order the player chooses. (:/)
        CurrentAttacker.BattleSupportEvent.Invoke();

        //Then the Opponent's skills activate after the player's again in the order the active player chooses (I assume).
        CurrentDefender.BattleSupportEvent.Invoke();

        hintText.text = "Once ready, push the button to begin resolving support skills.";

        phaseButtonText.text = "Resolve Support Skills";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(ActivateAttackerSupport);
    }

    //Activates the attacker's support skills in the order the active player chooses.
    private void ActivateAttackerSupport()
    {
        if (turnPlayer.SupportCard != null)
        {
            turnPlayer.SupportCard.ActivateAttackSupportSkill();
        }
        else
        {
            ActivateDefenderSupport();
        }
    }

    //The defender's support skills activate after the attackers's again in the order the active player chooses (I assume).
    public void ActivateDefenderSupport()
    {
        if (turnPlayer.Opponent.SupportCard != null)
        {
            turnPlayer.Opponent.SupportCard.ActivateDefenseSupportSkill();
        }
        else
        {
            AddSupportValues();
        }
    }

    //This method actually removes the defending card's ability to evade by setting the evading bool to false for this battle.
    public void PreventEvade()
    {
        canDefenderEvade = false;
    }

    //This method removes an attacker's abilty to perform a critical hit by setting the critical potential bool to false for this battle.
    public void PreventCritical()
    {
        canAttackerCrit = false;
    }

    //This method continues the attack process by adding the support values to each battle participant's attack.  
    public void AddSupportValues()
    {
        //Support values get added to each respective characters' battleModifier values.
        if (turnPlayer.SupportCard != null)
        {
            currentAttacker.battleModifier += turnPlayer.SupportCard.CurrentSupportValue;
        }

        if (turnPlayer.Opponent.SupportCard != null)
        {
            currentDefender.battleModifier += turnPlayer.Opponent.SupportCard.CurrentSupportValue;
        }

        DisplayAttackValues();

        hintText.text = "Once ready, push the button to resolve potential critical hits and evasions.";

        phaseButtonText.text = "Continue Battle";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(CheckCriticalAndEvasion);
    }

    //Displays the current attack values on the Game Log.
    private void DisplayAttackValues()
    {
        Debug.Log("The current attack is " + CurrentAttacker.CharName + "(" + CurrentAttacker.TotalAttack + ") vs. "
            + CurrentDefender.CharName + "(" + CurrentDefender.TotalAttack + ").");
        CardReader.instance.UpdateGameLog("The current battle state is " + turnPlayer.playerName + "'s " + CurrentAttacker.CharName 
            + " with " + CurrentAttacker.TotalAttack + " attack vs. " + turnPlayer.Opponent.playerName + "'s " 
            + CurrentDefender.CharName + " with " + CurrentDefender.TotalAttack + " attack.");
    }

    //Continues the battle logic by checking for Critical Hits and Evasions
    private void CheckCriticalAndEvasion()
    {
        //Check if the player can perform a critical hit
        if (canAttackerCrit && turnPlayer.Hand.Exists(x => x.CompareNames(currentAttacker, true)))
        {
            //give the player the choice to perform a critical hit. Call a dialogue box.
            DialogueWindowDetails details = new DialogueWindowDetails
            {
                windowTitleText = turnPlayer.playerName + "'s Critical Hit",
                questionText = turnPlayer.playerName + ": Discard a " + currentAttacker.CharName + " to activate a critical hit and double your attack power?",
                button1Details = new DialogueButtonDetails
                {
                    buttonText = "Yes",
                    buttonAction = () => { CriticalHitChoice(); }
                },
                button2Details = new DialogueButtonDetails
                {
                    buttonText = "No",
                    buttonAction = () => { EvadeChoice(); }
                }
            };

            //Debug.Log("We made it here in Support Roll.");

            DialogueWindow dialogueWindow = DialogueWindow.Instance();
            dialogueWindow.MakeChoice(details);
        }
        else            //The player does not have the appropriate cards in hand to perform a critical hit or has been prevented by a skill.
        {
            EvadeChoice();
        }
    }

    //This method calls the CardPicker to determine which card in the hand should be discarded for the critical hit.
    private void CriticalHitChoice()
    {
        //find the cards in the hand that have the same name as the attacking unit.
        List<BasicCard> possibleDiscards = new List<BasicCard>(turnPlayer.Hand.Count);
        for (int i = 0; i < turnPlayer.Hand.Count; i++)
        {
            if (turnPlayer.Hand[i].CompareNames(currentAttacker, true))
            {
                possibleDiscards.Add(turnPlayer.Hand[i]);
            }
        }

        /*
         * Is not implemented as this makes the operation uncancelable...
        //only calls the CardPicker if necessary (there is more than one possible discard).
        if (possibleDiscards.Count > 1)
        {
            //This sets up the method to call after the CardPicker finishes.
            MyCardListEvent eventToCall = new MyCardListEvent();
            eventToCall.AddListener(ActivateCriticalHit);

            //makes the player choose another ally for the skill's effect.
            CardPickerDetails details = new CardPickerDetails
            {
                cardsToDisplay = possibleDiscards,
                numberOfCardsToPick = 1,
                locationText = "Player's Hand",
                instructionText = "Please choose one card to discard to activate " + currentAttacker.CharName + "'s critical hit.",
                mayChooseLess = true,
                effectToActivate = eventToCall
            };

            CardPickerWindow cardPicker = CardPickerWindow.Instance();

            cardPicker.ChooseCards(details);
        }
        else            //There is only one card that can be discarded.
        {
            ActivateCriticalHit(possibleDiscards);
        }
        */

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(ActivateCriticalHit);

        //makes the player choose another ally for the skill's effect.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = possibleDiscards,
            numberOfCardsToPick = 1,
            locationText = turnPlayer.playerName + "'s Hand",
            instructionText = turnPlayer.playerName + ": Please choose one card to discard to activate " + currentAttacker.CharName + "'s critical hit.",
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
            criticalHit = true;
            CardReader.instance.UpdateGameLog(turnPlayer.playerName + "'s " + CurrentAttacker.CharName + " activates a critical hit!");
            turnPlayer.DiscardCardFromHand(list[0]);
            DisplayAttackValues();
        }

        //Call the evade choice method for the opponent
        EvadeChoice();
    }

    //Evades can only be completed if the defending unit is going to be defeated.
    //The method only calls the choice if an evasion is possible.
    public void EvadeChoice()
    {
        //Debug.Log("Now we are in the Evade Choice method on the other side");

        if (canDefenderEvade && CurrentAttacker.TotalAttack >= CurrentDefender.TotalAttack)
        {
            if (turnPlayer.Opponent.Hand.Exists(x => x.CompareNames(CurrentDefender, true)))
            {
                //reveals the opponent's hand so that they can make an evasion choice
                ShowHand(turnPlayer.Opponent);

                hintText.text = turnPlayer.Opponent.playerName + ": Push the button to resolve your evasion.";

                phaseButtonText.text = turnPlayer.Opponent.playerName + " Evade";

                phaseButton.onClick.RemoveAllListeners();
                phaseButton.onClick.AddListener(ChooseEvasion);
            }
            else
            {
                BattleCalculation();
            }
        }
        else
        {
            BattleCalculation();
        }
    }

    private void ChooseEvasion()
    {
        //give the player the choice to perform an evade. Call a dialogue box.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = turnPlayer.Opponent.playerName + "'s God-Speed Evasion",
            questionText = turnPlayer.Opponent.playerName + ": Discard a " + CurrentDefender.CharName + " to activate a god-speed evasion?",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Yes",
                buttonAction = () => { ActivateEvasion(); }
            },
            button2Details = new DialogueButtonDetails
            {
                buttonText = "No",
                buttonAction = () => { BattleCalculation(); }
            }
        };

        DialogueWindow dialogueWindow = DialogueWindow.Instance();
        dialogueWindow.MakeChoice(details);

        //Debug.Log("Did the dialogue box actually get called?");

        //return;
        //Debug.Log("Will this mess up the logic?");
    }

    //This method calls the CardPicker to choose the card to discard for a god-speed evasion.
    private void ActivateEvasion()
    {
        //find the cards in the hand that have the same name as the defending unit.
        List<BasicCard> possibleDiscards = new List<BasicCard>(turnPlayer.Opponent.Hand.Count);
        for (int i = 0; i < turnPlayer.Opponent.Hand.Count; i++)
        {
            if (turnPlayer.Opponent.Hand[i].CompareNames(CurrentDefender, true))
            {
                possibleDiscards.Add(turnPlayer.Opponent.Hand[i]);
            }
        }

        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(ResolveEvasion);

        //makes the player choose a card to discard to activate the god-speed evasion.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = possibleDiscards,
            numberOfCardsToPick = 1,
            locationText = turnPlayer.Opponent.playerName + "'s Hand",
            instructionText = turnPlayer.Opponent.playerName + ": Choose one card to discard to activate " + CurrentDefender.CharName + "'s god-speed evasion.",
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
            CardReader.instance.UpdateGameLog(turnPlayer.Opponent.playerName + "'s " + CurrentDefender.CharName + " activates a god-speed evasion!");
            turnPlayer.Opponent.DiscardCardFromHand(list[0]);
            //Opponent.DefenderEvaded();        //I don't think this call is actually necessary at this point yet.
            EndBattle();
        }
        else
        {
            BattleCalculation();
        }
    }

    /* This method isn't yet necessary but will probably contain a callback to an evade method.
    public void DefenderEvaded()
    {

    }
    */

    //This is the BattleCalculation step that actually evaluates the results of the attack.
    public void BattleCalculation()
    {
        CardReader.instance.UpdateGameLog("The final battle state is " + turnPlayer.playerName + "'s " + CurrentAttacker.CharName
            + " with " + CurrentAttacker.TotalAttack + " attack vs. " + turnPlayer.Opponent.playerName + "'s "
            + CurrentDefender.CharName + " with " + CurrentDefender.TotalAttack + " attack.");

        //Check to see if the attack was strong enough to defeat the defender.  Ties go to the aggressor.
        if (CurrentAttacker.TotalAttack >= CurrentDefender.TotalAttack)
        {
            CardReader.instance.UpdateGameLog(turnPlayer.Opponent.playerName + "'s " + CurrentDefender.CharName + " is defeated!");

            if (CurrentDefender == turnPlayer.Opponent.MCCard)           //The defending card was the MC.  Take at least one orb or win the game!
            {
                turnPlayer.Opponent.BreakOrbs(numOrbsToBreak);
            }
            else                //The defending card isn't the lord.
            {
                //NOTE: I may need to add a tag here for skill effects to say that the card was discarded as a result of battle.
                turnPlayer.Opponent.DiscardCardFromField(CurrentDefender);
            }

            //Skills which activate when an enemy is defeated activate here.
            battleDestructionTriggerTracker.CheckTrigger(CurrentDefender);
        }
        else
            CardReader.instance.UpdateGameLog(turnPlayer.Opponent.playerName + "'s " + CurrentDefender.CharName + " resists the attack!");

        EndBattle();
    }

    //This method resets the field and various stats, etc. to their pre-battle state.
    public void EndBattle()
    {
        //Show the turn Player's hand once more after the evasion has been processed. 
        ShowHand(turnPlayer);

        //Discard support cards if any.
        if (turnPlayer.SupportCard != null)
        {
            turnPlayer.DiscardSupport();
        }

        if (turnPlayer.Opponent.SupportCard != null)
        {
            turnPlayer.Opponent.DiscardSupport();
        }

        inCombat = false;

        //reset the battle modifiers for the involved cards;
        CurrentAttacker.ResetAfterBattle();
        CurrentDefender.ResetAfterBattle();

        //reset the attacker and defender slots
        currentAttacker = null;
        currentDefender = null;

        //reset the number of orbs to be destroyed on an attack.
        numOrbsToBreak = 1;

        //resets the criticalHit trigger
        criticalHit = false;

        //resets if the attacker can perform a critical and if the defender can evade
        canDefenderEvade = true;
        canAttackerCrit = true;

        //Check if there was a forced march because of the battle.
        turnPlayer.Opponent.CheckForcedMarch();

        CardReader.instance.UpdateGameLog("");

        //reset the hint text and phase button at the end of the battle.
        hintText.text = "You may activate effects, move your units, and attack the opponent.  You can do any of these actions in any order.";

        phaseButtonText.text = "End Turn";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(ContextMenu.instance.ClosePanel);
        phaseButton.onClick.AddListener(EndPhase);
    }

    //This method declares the winner of the game!
    //NOTE: This is still a work in progress.
    public void EndGame(CardManager winner)
    {
        CardReader.instance.UpdateGameLog(winner.playerName + " wins the game!");

        //Call a dialogue box to announce the winner.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = "End Game",
            questionText = winner.playerName + " has won!  Would you like to play again?",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Yes",
                buttonAction = () => { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
            },
            button2Details = new DialogueButtonDetails
            {
                buttonText = "No",
                buttonAction = () => { Debug.Log("Quit!"); QuitGame(); }
            }
        };

        DialogueWindow dialogueWindow = DialogueWindow.Instance();
        dialogueWindow.MakeChoice(details);
    }

    //Quits the game whether in the Editor or an application.
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public CardManager FindOpponent(CardManager askingPlayer)
    {
        if (askingPlayer == player1)
        {
            return player2;
        }
        else    //the askingPlayer is player2
        {
            return player1;
        }
    }

    private DeckList CreateTestDeck()
    {
        DeckList testDeckList = new DeckList();
        //Debug.Log(testDeckList + " has been created.");

        
        testDeckList.deckListDictionary.Add("S01-001ST", 4);        //Marth 4
        testDeckList.deckListDictionary.Add("S01-002ST", 2);        //Caeda 3
        testDeckList.deckListDictionary.Add("S01-003ST", 2);        //Jagen 3
        testDeckList.deckListDictionary.Add("S01-004ST", 3);        //Ogma 3
        testDeckList.deckListDictionary.Add("S01-005ST", 3);        //Navarre 3
        

        testDeckList.deckListDictionary.Add("B01-003HN", 2);        //Marth 1
        testDeckList.deckListDictionary.Add("B01-006HN", 2);        //Caeda 1
        testDeckList.deckListDictionary.Add("B01-007R", 1);         //Cain 3
        testDeckList.deckListDictionary.Add("B01-008N", 3);         //Cain 1
        testDeckList.deckListDictionary.Add("B01-009R", 1);         //Abel 3
        testDeckList.deckListDictionary.Add("B01-010N", 3);         //Abel 1
        testDeckList.deckListDictionary.Add("B01-012N", 2);         //Draug 1
        testDeckList.deckListDictionary.Add("B01-013HN", 1);        //Gordin 3
        testDeckList.deckListDictionary.Add("B01-014N", 2);         //Gordin 1
        testDeckList.deckListDictionary.Add("B01-018HN", 2);        //Ogma 1
        testDeckList.deckListDictionary.Add("B01-019N", 2);         //Bord 1
        testDeckList.deckListDictionary.Add("B01-020N", 2);         //Cord 1
        testDeckList.deckListDictionary.Add("B01-021HN", 2);        //Barst 2
        testDeckList.deckListDictionary.Add("B01-024HN", 2);        //Navarre 1
        testDeckList.deckListDictionary.Add("B01-026N", 3);         //Lena 1
        testDeckList.deckListDictionary.Add("B01-028R", 1);         //Merric 4
        testDeckList.deckListDictionary.Add("B01-029N", 3);         //Merric 1
        testDeckList.deckListDictionary.Add("B01-036N", 2);         //Linde 1

        //testDeckList.deckListDictionary.Add("B01-027HN", 2);         //Julian 2
        //testDeckList.deckListDictionary.Add("B04-029R", 8);         //Julian 3
        //testDeckList.deckListDictionary.Add("B01-076N", 4);        //Cordelia 1
        //testDeckList.deckListDictionary.Add("B01-053HN", 2);        //Chrom 1
        //testDeckList.deckListDictionary.Add("B01-054SR", 2);        //Lucina 4
        //testDeckList.deckListDictionary.Add("B01-056HN", 2);        //Lucina 1

        testDeckList.DefaultMC = "B01-003HN";

        /*
        foreach (KeyValuePair<string, int> kvp in testDeckList.deckListDictionary)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                Debug.Log(kvp.Key + " has been added to the deck.");
            }
        }
        */

        return testDeckList;
    }
   
    //This method loads a decklist from a JSON data resource.
    private DeckList LoadDeckList (string deckFileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, deckFileName);

        //Debug.Log("Deck will be loaded from " + filePath);

        if (File.Exists(filePath))
        {
            string dataAsJson = File.ReadAllText(filePath);

            DeckList deckList = JsonUtility.FromJson<DeckList>(dataAsJson);
            deckList.OnAfterDeserialize();

            
            return deckList;
        
        }
        else
        {
            Debug.LogError("WARNING: Cannot load deck list!");
            DeckList deckList = new DeckList();
            return deckList;
        }
    }

    //Instantiates the card gameobjects listed in a decklist and returns a list of those objects.
    //NOTE: The method only instantiates cards at their default location.  This will need to be changed later to happen offscreen.
    private List<BasicCard> CreateDeck(DeckList decklist)
    {
        List<BasicCard> deck = new List<BasicCard>();

        foreach (KeyValuePair<string, int> kvp in decklist.deckListDictionary)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                //Debug.Log("Trying to load " + kvp.Key + " from Resources.");
                GameObject loadedObject = Instantiate(Resources.Load(kvp.Key)) as GameObject;
                //Debug.Log(loadedObject + " has been successfully loaded.");

                BasicCard cardToAdd = loadedObject.GetComponent<BasicCard>();
                deck.Add(cardToAdd);
                //Debug.Log(cardToAdd.ToString() + " has been added to the deck.");
            }
        }

        Debug.Log("Deck contains " + deck.Count + " cards.");

        return deck;
    }

    private void SaveDeckList (DeckList deckList, string deckFileName)
    {
        deckList.OnBeforeSerialize();

        string dataAsJson = JsonUtility.ToJson(deckList);

        //Debug.Log("JsonUtility will write the following to file: " + dataAsJson);

        string filePath = Path.Combine(Application.streamingAssetsPath, deckFileName);

        if (!File.Exists(filePath))
        {
            FileStream file = File.Create(filePath);
            file.Close();
            Debug.Log(filePath + " did not exist and was created.");
        }

        File.WriteAllText(filePath, dataAsJson);

        //Debug.Log("DeckList data was written to " + filePath);
    }
}
