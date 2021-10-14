using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class serves as the template for the different types of players which can exist in the game
//such as a human player, and online player, or an AI player.  Each makes decisions that impact the game, but each makes them differently.

[System.Serializable]
public abstract class DecisionMaker : MonoBehaviour
{
    protected CardManager cardManager;
    protected string playerName;
    protected DeckList decklist;
    protected DecisionMaker opponent;

    public List<BasicCard> initiativeList = new List<BasicCard>(20);    //Used to store the cards to be used in the Action Phase.

    //These are properties to access information about this Decision Maker. 
    //They are readonly for now.
    public CardManager CardManager { get { return cardManager; } }
    public string PlayerName { get { return playerName; } }
    public DecisionMaker Opponent { get { return opponent; } }

    /*
    //A protected constructor to allow inherited classes a standard way to create a CardManager
    protected DecisionMaker(DeckList list, LayoutManager lm, RetreatView rv, string name)
    {
        decklist = list;
        cardManager = new CardManager(GameManager.instance.CreateDeck(decklist), lm, rv, name, this);
        playerName = name;
    }
    

    ///A protected constructor to allow inherited classes a standard way to construct themselves.
    protected DecisionMaker(DeckList list, CardManager cm, string name)
    {
        decklist = list;
        cardManager = cm;
        cardManager.Setup(list, this);
        playerName = name;
    }
    */

    //This method sets up the DecisonMaker once it has been added as a component by the GameManager. 
    public virtual void Setup(DeckList list, CardManager cm, string name)
    {
        decklist = list;
        cardManager = cm;
        cardManager.Setup(list, this);
        playerName = name;
    }

    //A method to use to set another DecisionMaker as this agen's opponent.
    public void SetOpponent(DecisionMaker otherAgent)
    {
        opponent = otherAgent;
    }

    //This method provides the foundation for how each DecisionMaker will set up their MC and Mulligan at the beginning of the game.
    public abstract void PlayerSetup();

    //This method helps the DecisionMaker know which process should be resumed following the MulliganChoice
    protected virtual void PostMulligan()
    {
        //Check if the opponent has already gone through the MC choice and Mulligan process.
        if (Opponent.CardManager.MCStack == null)
        {
            GameManager.instance.turnPlayer = Opponent.CardManager;
            GameManager.instance.turnAgent = Opponent;
            Opponent.PlayerSetup();
        }
        else
            GameManager.instance.BeginGame();
    }

    //This method provides the foundation for any decisions that need to be made in the Beginning Phase.
    //The basic function though is to pass the game onto the next phase.
    public virtual void OnBeginningPhase()
    {
        ContextMenu.instance.ClosePanel();
        GameManager.instance.BondPhase();
    }

    //This method provides the foundation for any decisions that need to be made in the Bond Phase.
    public virtual void OnBondPhase()
    {
        ContextMenu.instance.ClosePanel();
        GameManager.instance.DeploymentPhase();
    }

    //This method provides the foundation for any decisions that need to be made in the Deployment Phase.
    public virtual void OnDeployPhase()
    {
        ContextMenu.instance.ClosePanel();
        GameManager.instance.ActionPhase();
    }

    //This method provides the base for how each decision maker will update the hint text during the Deployment Phase
    public abstract void UpdateDeploymentHintText();

    //This abstract method is where each DecisionMaker will decide the row to deploy a new card.
    public abstract void ChooseDeployLocation(BasicCard card);

    //This abstract method is where the DecisionMaker decides the order for triggered card skills to resolve.
    public abstract void ChooseAmongTriggeredCards(TriggerEventHandler triggerEvent, List<BasicCard> triggeredCards);

    //This abstract method insures each DecisionMaker has a way to activate trigger skills.
    public abstract void ActivateTriggerSkill(BasicCard activatedCard, BasicCard triggeringCard);

    //This method provides the foundation for any decisions that need to be made in the Action Phase.
    public virtual void OnActionPhase()
    {
        ContextMenu.instance.ClosePanel();
        GameManager.instance.EndPhase();
    }

    //This abstract method provides a way for the decision maker to choose whether to use a bond skill.
    //For now it is exclusive to the AI.
    public abstract bool ShouldFlipBonds(BasicCard card, int numBondsToFlip);

    //This abstract method provides a way for the decision maker to choose which bonds should be flipped due to a skill's activation or effect.
    public abstract void ChooseBondsToFlip(BasicCard card, int numToFlip, string skillText);

    //This abstract method provides a way for the decision maker to choose whether to use Elysian Emblem.
    public abstract void TryElysianEmblem();

    //This abstract method provides a way for the decision maker to choose which cards should be discarded from the Hand due to a skill's activation or effect.
    public abstract void ChooseCardsToDiscardFromHand(BasicCard card, List<BasicCard> listToChooseFrom, int numToDiscard, string skillText);

    //This abstract method ensures each DecisionMaker has a way to determine an attack target and start a battle.
    public abstract void ChooseAttackTarget(BasicCard aggressor, int expectedAttack, List<BasicCard> targets);

    //This overload method provides a simpler way for a card to find an attack target by assuming
    //we're looking at its current attack and full range of attack targets.
    public virtual void ChooseAttackTarget(BasicCard aggressor)
    {
        ChooseAttackTarget(aggressor, aggressor.CurrentAttackValue, aggressor.AttackTargets);
    }

    //This abstract method insures each DecisionMaker has a way to decide whether to lauch a critical hit.
    public abstract void DecideToCrit();

    //This abstract method insures each DecisionMaker has a way to decide whether to evade an incoming attack.
    public abstract void DecideToEvade();

   
    //This method provides the foundation for any decisions that need to be made in the End Phase.
    public virtual void OnEndPhase()
    {
        ContextMenu.instance.ClosePanel();
        GameManager.instance.BeginningPhase();
    }
}
