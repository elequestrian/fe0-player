using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class GameManager : MonoBehaviour {

    public static GameManager instance = null;

    public string testDeckFileName;
    public Text hintText;
    public Button phaseButton;
    public Text phaseButtonText;
    public bool cardBonded;                         //Tracks whether or not a card has already been bonded this turn.
    public int bondDeployCount;                    //Tracks the number of bonds used to deploy units this turn.
    public CardManager turnPlayer;                  //Needs to be removed.
    public DecisionMaker turnAgent;
    public CardManager player1;                    //this is a reference to the Cardmanager for the first player's cards.
    public CardManager player2;
    public bool playtestMode = true;                //checks if the game is in Playtest mode and if so allows for convenient features.
    public bool versusAI = false;                   //checks if the game is set to be versus an AI opponent.

    private bool firstTurn = false;
    private CipherData.PhaseEnum currentPhase;      //This enum keeps track of the current phase in the game: 0 = Beginning, 1 = Bond, 2 = Deployment, 3 = Action, 4 = End;
    private float turnCount = 0f;

    private DecisionMaker agent1;
    private DecisionMaker agent2;

    
    private DeckList decklist1;
    private DeckList decklist2;
    private string playerDeck = "WarOfShadows.dek";

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
    public bool criticalHit = false;
    public int numOrbsToBreak = 1;
    private bool canDefenderEvade = true;
    private bool canAttackerCrit = true;
    public TriggerEventHandler battleDestructionTriggerTracker = new TriggerEventHandler();


    public CipherData.PhaseEnum CurrentPhase { get { return currentPhase; } }
    public bool FirstTurn { get { return firstTurn; } }
    public BasicCard CurrentAttacker { get { return currentAttacker; } }
    public BasicCard CurrentDefender { get { return currentDefender; } }
    public bool CriticalHit { get { return criticalHit; } }

    //A property that returns the per player turn count (1, 1, 2, 2, 3, 3, etc.)
    public int PlayerTurnCount { get { return (int)Mathf.Floor(turnCount + 1); } }

    //A property that returns the actual game turn count (zero indexed)
    public int GameTurnCount { get { return Mathf.RoundToInt(turnCount * 2f); } }

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
        //TEMP PAUSE
        //This pause is necessary because I don't have a separate landing screen, so this is where players get oriented.
        
        SetPhaseButtonAndHint("Press the red button to the left to start the game.", "Start Game", SetupGame);
    }

    //This method allows other classes (specifically player agents) to adjust the Phase Button and Hint text as needed.
    public void SetPhaseButtonAndHint(string hint, string phaseText, UnityAction action)
    {
        hintText.text = hint;
        phaseButtonText.text = phaseText;
        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(action);
    }

    void SetupGame()
    {
        //Create, save, and load a decklist for each player.

        //Create and save the decklist.  Needs to be moved to a Deck creator mode.
        testDeck = CreateTestDeck();
        SaveDeckList(testDeck, testDeckFileName);

        decklist1 = LoadDeckList(playerDeck);
        decklist2 = LoadDeckList(testDeckFileName);

        //creates a human player
        agent1 = player1.gameObject.AddComponent<LocalPlayer>() as LocalPlayer;
        agent1.Setup(decklist1, player1, "Player 1");
        //agent1 = new LocalPlayer(decklist1, player1, "Player 1");

        //creates a second player as well based on the type of game.
        if (versusAI)
        {
            agent2 = player2.gameObject.AddComponent<AIPlayer>() as AIPlayer;
            agent2.Setup(decklist2, player2, "AI Player");
            //agent2 = new AIPlayer(decklist2, player2, "AI Player");
        }
        else    //assumes the player is against another local human opponent.
        {
            agent2 = player2.gameObject.AddComponent<LocalPlayer>() as LocalPlayer;
            agent2.Setup(decklist2, player2, "Player 2");
            //agent2 = new LocalPlayer(decklist2, player2, "Player 2");
        }
        
        //sets the two players as opponents
        agent1.SetOpponent(agent2);
        agent2.SetOpponent(agent1);
        player1.SetOpponent(player2);
        player2.SetOpponent(player1);

        turnPlayer = player1;
        turnAgent = agent1;

        //Sets the first player's MC.
        agent1.PlayerSetup();
    }

    //This method helps the player choose their MC and then look over their hand to consider a mulligan.
    //The coroutine ensures that the player's hand isn't drawn until the MC has been chosen.
    //The game then sets up the Phase button to allow the player to look at their hand and choose whether to Mulligan.
    public IEnumerator HumanPlayerSetup(LocalPlayer agent)
    {
        agent.CheckDefaultMC();
        yield return new WaitUntil(() => agent.CardManager.MCStack != null);

        string hint = agent.PlayerName + ": Look over your hand, and decide if you would like to keep it or draw another. " +
            //"It's generally best to have at least one promotion for your Main Character in hand at the start of the game. " +
            "Click the red button when you are ready to choose.";

        SetPhaseButtonAndHint(hint, "Mulligan?", agent.MulliganChoice);

        //lets the player look at thier MC while "mulling" a mulligan.
        agent.CardManager.MCCard.FlipFaceUp();

        agent.CardManager.Draw(6);
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

    //This helper method sets up the board for the new incoming turn player.
    //Usually this means flipping hands face-up or down as appropriate.
    public void ShowHand(DecisionMaker newActiveAgent)
    {

        //flip the current turn player's hand faceUp.
        foreach (var card in newActiveAgent.CardManager.Hand)
        {
            if (!card.FaceUp)
            {
                card.FlipFaceUp();
            }
        }

        if (!playtestMode)
        {
            //flip the opponent's hand facedown.
            foreach (var card in newActiveAgent.Opponent.CardManager.Hand)
            {
                if (card.FaceUp)
                {
                    card.FlipFaceDown();
                }
            }
        }
    }

    //Begins the game by setting out orbs and flipping the players' MCs face up.
    public void BeginGame()
    {
        hintText.text = "Let's start the game!";
        firstTurn = true;

        CardReader.instance.UpdateGameLog("");
        CardReader.instance.UpdateGameLog(agent1.PlayerName + "'s " + agent1.CardManager.MCCard.CharName + ": "  
            + agent1.CardManager.MCCard.CharTitle + " vs. " + agent2.PlayerName + "'s " + agent2.CardManager.MCCard.CharName + ": " 
            + agent2.CardManager.MCCard.CharTitle + ".  Let's start the game!\n");
        CardReader.instance.UpdateGameLog("The first player cannot draw a card or attack on the first turn.");

        agent1.CardManager.AtStart();
        agent2.CardManager.AtStart();

        BeginningPhase();
    }

    public void BeginningPhase()
    {
        currentPhase = CipherData.PhaseEnum.Beginning;

        //Sets the current turnPlayer
        turnAgent = turnAgent.Opponent;
        turnPlayer = turnPlayer.Opponent;
        ShowHand(turnAgent);

        //updates the Game Log
        CardReader.instance.UpdateGameLog("\nBegin " + turnAgent.PlayerName + "'s Turn " + PlayerTurnCount + ":");

        //Checks the other player (who just finished their turn) for a forced march.
        turnAgent.Opponent.CardManager.CheckForcedMarch();

        //resets all turn player's "once per turn" ability flags and resets their active turn abilities.
        turnAgent.CardManager.BeginTurnEvent.Invoke();

        //untap all units on the active turn player's field.
        turnAgent.CardManager.UntapAllUnits();

        //draws card if not the first turn.
        if (!firstTurn)
        {
            turnAgent.CardManager.Draw(1);
            turnCount += 0.5f;
        }

        turnAgent.OnBeginningPhase();
    }

    public void BondPhase()
    {
        currentPhase = CipherData.PhaseEnum.Bond;
        cardBonded = false;
        CardReader.instance.UpdateGameLog("\nBegin " + turnAgent.PlayerName +"'s Bond Phase:");

        turnAgent.OnBondPhase();
    }

    public void DeploymentPhase()
    {
        currentPhase = CipherData.PhaseEnum.Deployment;
        bondDeployCount = 0;
        CardReader.instance.UpdateGameLog("\nBegin " + turnAgent.PlayerName + "'s Deployment Phase:");

        turnAgent.OnDeployPhase();
    }

    public void ActionPhase()
    {
        currentPhase = CipherData.PhaseEnum.Action;
        CardReader.instance.UpdateGameLog("\nBegin " + turnAgent.PlayerName + "'s Action Phase:");

        /*
        hintText.text = "You may activate effects, move your units, and attack the opponent. You can do any of these actions in any order.";

        phaseButtonText.text = "End Turn";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(ContextMenu.instance.ClosePanel);
        phaseButton.onClick.AddListener(EndPhase);
        */

        turnAgent.OnActionPhase();
    }

    public void EndPhase()
    {
        currentPhase = CipherData.PhaseEnum.End;
        CardReader.instance.UpdateGameLog("\nBegin " + turnAgent.PlayerName + "'s End Phase:");
        
        /*
         * Any Skills that activate can do so, and any Skills that stop being active at the end of the turn now stop (such as Attack boosts).
         * You may now proceed to your opponent’s turn, which shifts to your opponent's Beginning Phase.
         */

        turnAgent.CardManager.endTurnEvent.Invoke();
        firstTurn = false;

        /*
        hintText.text = "Many skill effects end in the End Phase. Once ready, push the button to begin the next player's turn.";

        phaseButtonText.text = "Begin next turn";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(ContextMenu.instance.ClosePanel);
        phaseButton.onClick.AddListener(BeginningPhase);
        */
        turnAgent.OnEndPhase();
    }

    /*
     * These two methods were the old attack architechture.  This has been streamlined by decision maker type.
     * 
    //This method calls the CardPicker to choose which of the opponent's cards will be attacked.
    //It is not binding as you may choose 0 targets for the attack which cancels the action.
    public void AimAttack(BasicCard attacker)
    {
        //Save the attacking card for the method called after the choice.
        currentAttacker = attacker;

        //have the DecisionMaker choose an enemy to be attacked.
        turnAgent.ChooseAttackTarget();

        /*
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
    public void DeclareAttack(List<BasicCard> list)
    {
        //Only proceed if there is actually a target for the attack.
        if (list.Count > 0)
        {
            inCombat = true;

            currentDefender = list[0];

            //Tap the attacking card.
            CurrentAttacker.Tap();

            CardReader.instance.UpdateGameLog("\n" + turnAgent.PlayerName + "'s " + CurrentAttacker.CharName + " attacks " 
                + turnAgent.Opponent.PlayerName + "'s " + CurrentDefender.CharName + "!");

            //reset the battleModifiers that affect attack.
            CurrentAttacker.battleModifier = 0;
            CurrentDefender.battleModifier = 0;

            //Once an attack is declared, any AUTO or TRIGGER skills that activate upon attacking go here.
            //These may need to be separated out into small methods of their own.  We'll see as this gets expanded.

            //NOTE: Having these sequential may mean that the effects/called windows overwrite each other.  Be careful when adding events here!
            //First the player's skills activate in the order the player chooses. (:/)
            //Feeding 'true' represents attacking.
            CurrentAttacker.DeclareAttackEvent.Invoke(true);

            //Then the Opponent's skills activate after the player's in the order the opponent chooses (whoever controls the cards chooses the order their skills activate).
            //Feeding 'false' represents defending.
            CurrentDefender.DeclareAttackEvent.Invoke(false);


            /*Update the skills above after handling the core attack logic.
             * 
             * For now, proceed with the support logic and update as appropriate.
             * 



            //HUMAN PAUSE
            //Candidate to adjust the attack skill sequence into a coroutine.
            //Can we use a "waitForHuman" bool which flips true and false in the coroutine?
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
    
         */


    //This method allows a Decision maker to start a battle by declaring an attacking unit and the target/defender. 
    public void StartBattle(BasicCard attacker, BasicCard defender)
    {
        inCombat = true;

        currentAttacker = attacker;
        currentDefender = defender;

        //Tap the attacking card.
        CurrentAttacker.Tap();

        CardReader.instance.UpdateGameLog("\n" + turnAgent.PlayerName + "'s " + CurrentAttacker.CharName + " attacks "
            + turnAgent.Opponent.PlayerName + "'s " + CurrentDefender.CharName + "!");

        //reset the battleModifiers that affect attack.
        CurrentAttacker.battleModifier = 0;
        CurrentDefender.battleModifier = 0;

        //Once an attack is declared, any AUTO or TRIGGER skills that activate upon attacking go here.
        //These may need to be separated out into small methods of their own.  We'll see as this gets expanded.

        //NOTE: Having these sequential may mean that the effects/called windows overwrite each other.  Be careful when adding events here!
        //First the player's skills activate in the order the player chooses. (:/)
        //Feeding 'true' represents attacking.
        CurrentAttacker.DeclareAttackEvent.Invoke(true);

        //Then the Opponent's skills activate after the player's in the order the opponent chooses (whoever controls the cards chooses the order their skills activate).
        //Feeding 'false' represents defending.
        CurrentDefender.DeclareAttackEvent.Invoke(false);


        /*Update the skills above after handling the core attack logic.
         * 
         * For now, proceed with the support logic and update as appropriate.
         * 
         */


        //HUMAN PAUSE
        //Candidate to adjust the attack skill sequence into a coroutine.
        //Can we use a "waitForHuman" bool which flips true and false in the coroutine?
        //Once skills have been activated, supports are rolled.  This is processed in another method.
        hintText.text = "Once ready, push the button to continue the attack and reveal support cards.";

        phaseButtonText.text = "Reveal Supports";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(SupportRoll);
    }

    //This method takes care of the high level support roll for both players as well as checking for self-supports.
    //It currently also adds the support value to the cards for attack calculation.
    private void SupportRoll()
    {
        turnAgent.CardManager.PlaySupportCard();
        
        //Check player for self-supports
        if (turnAgent.CardManager.SupportCard != null)
        {
            if (turnAgent.CardManager.SupportCard.CompareNames(CurrentAttacker))
            {
                CardReader.instance.UpdateGameLog("Due to a self-support, " + turnAgent.PlayerName + "'s support draw fails!");
                turnAgent.CardManager.DiscardSupport();
            }
        }

        turnAgent.Opponent.CardManager.PlaySupportCard();

        //Check opponent for self-supports
        if (turnAgent.Opponent.CardManager.SupportCard != null)
        {
            if (turnAgent.Opponent.CardManager.SupportCard.CompareNames(CurrentDefender))
            {
                CardReader.instance.UpdateGameLog("Due to a self-support, " + turnAgent.Opponent.PlayerName + "'s support draw fails!");
                turnAgent.Opponent.CardManager.DiscardSupport();
            }
        }

        //Activates skills based on supporting cards for both the attacker and the defender.
        //These may need to be separated out into small methods of their own.  We'll see as this gets expanded.
        //NOTE: Having these sequential may mean that the effects/called windows overwrite each other.  Be careful when adding events here!
        //First the player's skills activate in the order the player chooses. (:/)
        CurrentAttacker.BattleSupportEvent.Invoke();

        //Then the Opponent's skills activate after the player's again in the order the opponent chooses.
        CurrentDefender.BattleSupportEvent.Invoke();

        //HUMAN PAUSE
        //This is necessary due to the support skill logic
        //Candidate for coroutine transformation.
        hintText.text = "Once ready, push the button to begin resolving support skills.";

        phaseButtonText.text = "Resolve Support Skills";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(ActivateAttackerSupport);
    }

    //Activates the attacker's support skills in the order the active player chooses.
    private void ActivateAttackerSupport()
    {
        if (turnAgent.CardManager.SupportCard != null)
        {
            turnAgent.CardManager.SupportCard.ActivateAttackSupportSkill();
        }
        else
        {
            ActivateDefenderSupport();
        }
    }

    //The defender's support skills activate after the attackers's in the order the card's controller chooses.
    public void ActivateDefenderSupport()
    {
        if (turnAgent.Opponent.CardManager.SupportCard != null)
        {
            turnAgent.Opponent.CardManager.SupportCard.ActivateDefenseSupportSkill();
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
        if (turnAgent.CardManager.SupportCard != null)
        {
            CurrentAttacker.battleModifier += turnAgent.CardManager.SupportCard.CurrentSupportValue;
        }

        if (turnAgent.Opponent.CardManager.SupportCard != null)
        {
            CurrentDefender.battleModifier += turnAgent.Opponent.CardManager.SupportCard.CurrentSupportValue;
        }

        DisplayAttackValues();

        //HUMAN PAUSE
        //This is another human pause point where the human players need to assess the situation and confirm if they will be doing a
        //critical hit or an evade.  They just need to survey the battle.
        hintText.text = "Once ready, push the button to resolve potential critical hits and evasions.";

        phaseButtonText.text = "Continue Battle";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(CheckCriticalAndEvasion);
    }

    //Displays the current attack values on the Game Log.
    public void DisplayAttackValues()
    {
        Debug.Log("The current attack is " + CurrentAttacker.CharName + "(" + CurrentAttacker.TotalAttack + ") vs. "
            + CurrentDefender.CharName + "(" + CurrentDefender.TotalAttack + ").");
        CardReader.instance.UpdateGameLog("The current battle state is " + turnAgent.PlayerName + "'s " + CurrentAttacker.CharName 
            + " with " + CurrentAttacker.TotalAttack + " attack vs. " + turnAgent.Opponent.PlayerName + "'s " 
            + CurrentDefender.CharName + " with " + CurrentDefender.TotalAttack + " attack.");
    }

    //Continues the battle logic by checking for Critical Hits and Evasions
    private void CheckCriticalAndEvasion()
    {
        //Check if the player can perform a critical hit
        if (canAttackerCrit && turnAgent.CardManager.Hand.Exists(x => x.CompareNames(CurrentAttacker)))
        {
            turnAgent.DecideToCrit();
        }
        else            //The player does not have the appropriate cards in hand to perform a critical hit or has been prevented by a skill.
        {
            EvadeChoice();
        }
    }

        /*
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
    */

    //Evades can only be completed if the defending unit is going to be defeated.
    //The method only calls the choice if an evasion is possible.
    public void EvadeChoice()
    {
        //Debug.Log("Now we are in the Evade Choice method on the other side");

        if (canDefenderEvade && CurrentAttacker.TotalAttack >= CurrentDefender.TotalAttack)
        {
            if (turnAgent.Opponent.CardManager.Hand.Exists(x => x.CompareNames(CurrentDefender)))
            {
                turnAgent.Opponent.DecideToEvade();
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

    /* This method isn't yet necessary but will probably contain a callback to an evade method.
    public void DefenderEvaded()
    {

    }
    */

    //This is the BattleCalculation step that actually evaluates the results of the attack.
    public void BattleCalculation()
    {
        CardReader.instance.UpdateGameLog("The final battle state is " + turnAgent.PlayerName + "'s " + CurrentAttacker.CharName
            + " with " + CurrentAttacker.TotalAttack + " attack vs. " + turnAgent.Opponent.PlayerName + "'s "
            + CurrentDefender.CharName + " with " + CurrentDefender.TotalAttack + " attack.");

        //Check to see if the attack was strong enough to defeat the defender.  Ties go to the aggressor.
        if (CurrentAttacker.TotalAttack >= CurrentDefender.TotalAttack)
        {
            CardReader.instance.UpdateGameLog(turnAgent.Opponent.PlayerName + "'s " + CurrentDefender.CharName + " is defeated!");

            if (CurrentDefender == turnAgent.Opponent.CardManager.MCCard)           //The defending card was the MC.  Take at least one orb or win the game!
            {
                turnAgent.Opponent.CardManager.BreakOrbs(numOrbsToBreak);
            }
            else                //The defending card isn't the lord.
            {
                //NOTE: I may need to add a tag here for skill effects to say that the card was discarded as a result of battle.
                turnAgent.Opponent.CardManager.DiscardCardFromField(CurrentDefender);
            }

            //Skills which activate when an enemy is defeated activate here.
            battleDestructionTriggerTracker.CheckTrigger(CurrentDefender);
        }
        else
            CardReader.instance.UpdateGameLog(turnAgent.Opponent.PlayerName + "'s " + CurrentDefender.CharName + " resists the attack!");

        EndBattle();
    }

    //This method resets the field and various stats, etc. to their pre-battle state.
    public void EndBattle()
    {
        //Show the turn Player's hand once more after the evasion has been processed. 
        ShowHand(turnAgent);

        //Discard support cards if any.
        if (turnAgent.CardManager.SupportCard != null)
        {
            turnAgent.CardManager.DiscardSupport();
        }

        if (turnAgent.Opponent.CardManager.SupportCard != null)
        {
            turnAgent.Opponent.CardManager.DiscardSupport();
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
        turnAgent.Opponent.CardManager.CheckForcedMarch();

        CardReader.instance.UpdateGameLog("");

        /*
        //reset the hint text and phase button at the end of the battle.
        hintText.text = "You may activate effects, move your units, and attack the opponent.  You can do any of these actions in any order.";

        phaseButtonText.text = "End Turn";

        phaseButton.onClick.RemoveAllListeners();
        phaseButton.onClick.AddListener(ContextMenu.instance.ClosePanel);
        phaseButton.onClick.AddListener(EndPhase);
        */

        //exit the battle proceedings and allow the turn agent to choose their next action.
        turnAgent.OnActionPhase();
    }

    //This method declares the winner of the game!
    //NOTE: This is still a work in progress.
    public void EndGame(DecisionMaker winner)
    {
        CardReader.instance.UpdateGameLog(winner.PlayerName + " wins the game!");

        //Call a dialogue box to announce the winner.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = "End Game",
            questionText = winner.PlayerName + " has won!  Would you like to play again?",
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

        //AI Decklist Test - Cain
        testDeckList.DefaultMC = "B01-008N";                        //Cain 1

        //Easy to add
        testDeckList.deckListDictionary.Add("B01-008N", 4);         //Cain 1
        testDeckList.deckListDictionary.Add("B01-010N", 4);         //Abel 1
        testDeckList.deckListDictionary.Add("B01-019N", 4);         //Bord 1
        testDeckList.deckListDictionary.Add("B01-020N", 4);         //Cord 1
        testDeckList.deckListDictionary.Add("S01-003ST", 4);        //Jagen 3

        
        //Medium to implement
        testDeckList.deckListDictionary.Add("B01-007R", 4);         //Cain 3
        testDeckList.deckListDictionary.Add("B01-009R", 4);         //Abel 3
        testDeckList.deckListDictionary.Add("B01-036N", 4);         //Linde 1
        testDeckList.deckListDictionary.Add("B01-014N", 4);         //Gordin 1
        testDeckList.deckListDictionary.Add("B01-006HN", 4);        //Caeda 1
        
        
        //Harder to implement
        testDeckList.deckListDictionary.Add("S01-002ST", 4);        //Caeda 3
        testDeckList.deckListDictionary.Add("B01-013HN", 2);        //Gordin 3
        testDeckList.deckListDictionary.Add("B01-021HN", 4);        //Barst 2


        /*
         * War of Shadows Starter Deck
         * 
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
        */

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

    /*
    //Instantiates the card gameobjects listed in a decklist and returns a list of those objects.
    //NOTE: The method only instantiates cards at their default location.  This will need to be changed later to happen offscreen.
    //This method must be in the GameManager since only Objects can instantiate other Objects.
    public List<BasicCard> CreateDeck(DeckList decklist)
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

                //RectTransform cardTransform = cardToAdd.gameObject.transform as RectTransform;
            }
        }

        Debug.Log("Deck contains " + deck.Count + " cards.");

        return deck;
    }
    */


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
