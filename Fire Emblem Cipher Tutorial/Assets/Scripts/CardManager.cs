using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

public class CardManager
{
    private List<BasicCard> deckList;
    private List<BasicCard> deck;                                   //this is a reference to the player's actual deck.  Note that the 0 indexed card is the top card of the deck. 
    private List<BasicCard> retreat;
    private List<BasicCard> hand = new List<BasicCard>(10);
    private List<BasicCard> orbs = new List<BasicCard>(5);
    private List<BasicCard> bonds = new List<BasicCard>(10);
    private List<CardStack> frontLine = new List<CardStack>(10);
    private List<CardStack> backLine = new List<CardStack>(10);
    private BasicCard supportCard;
    private LayoutManager layoutManager;
    

    public string playerName;
    public TriggerEventHandler deployTriggerTracker = new TriggerEventHandler();
    public TriggerEventHandler AttackTargetHandler = new TriggerEventHandler();
    public UnityEvent BeginTurnEvent = new UnityEvent();
    public UnityEvent endTurnEvent = new UnityEvent();
    public UnityEvent FinishBondFlipEvent = new UnityEvent();
    //An event to tell listeners that any change has occured to the number or type of allies deployed on the field.  Currently used by [ALWAYS] skills.  Has higher priority than the TriggerHandlers and those skills.
    public UnityEvent FieldChangeEvent = new UnityEvent();

    //public bool defenderEvaded = false;                           //a tag to tell skills if the battle resulted in an evade.

    private bool[] bondedColors = new bool[CipherData.NumColors];        //contains a bool array of all the colors currently present in the bonds.

    private CardManager opponent;                                   //a field to contain the opposing player for effect and attack calculations.

    //this is the field for the CardStack holding the player's main character.
    private CardStack playerMC;
    

    //These are properties that return the player's card information if necessary.
    public CardStack MCStack { get { return playerMC; } }
    public BasicCard MCCard { get { return playerMC.TopCard; } }
    public List<BasicCard> Deck { get { return deck; } }
    public List<BasicCard> Retreat { get { return retreat; } }
    public List<BasicCard> Hand { get { return hand; } }
    public List<BasicCard> Orbs { get { return orbs; } }
    public List<BasicCard> Bonds { get { return bonds; } }
    public List<CardStack> FrontLineStacks { get { return frontLine; } }
    public List<CardStack> BackLineStacks { get { return backLine; } }
    public BasicCard SupportCard { get { return supportCard; } }
    

    public CardManager Opponent { get { return opponent; } }
    public bool[] BondedColors { get { return bondedColors; } }

    //This property returns a List of the string names of all the colors currently bonded on the field.
    public List<string> BondedColorNames
    {
        get
        {
            List<string> colorNames = new List<string>(CipherData.NumColors);

            //Loops through each possible color
            for (int i = 0; i < BondedColors.Length; i++)
            {
                //if the color is bonded then add the color name to the list
                if (BondedColors[i])
                {
                    colorNames.Add(((CipherData.ColorsEnum)i).ToString());
                }
            }

            //If no colors on bonded cards, then return "Colorless".
            if (colorNames.Count == 0)
            {
                colorNames.Add("Colorless");
            }

            return colorNames;
        }
    }

    //This property returns a list of all the CardStacks that are active on the field in either the front or back lines.
    public List<CardStack> FieldStacks
    {
        get
        {
            List<CardStack> fieldStacks = new List<CardStack>(frontLine.Count + backLine.Count);
            fieldStacks.AddRange(FrontLineStacks);
            fieldStacks.AddRange(BackLineStacks);
            return fieldStacks;
        }
    }
    
    //The following properties return a List of BasicCards for ease of use by other functions.
    
    public List<BasicCard> FrontLineCards
    {
        get
        {
            List<BasicCard> frontLineCards = new List<BasicCard>(frontLine.Count);
            foreach (CardStack stack in frontLine)
                {
                    frontLineCards.Add(stack.TopCard);
                }
            return frontLineCards;
        }
    }
    
    public List<BasicCard> BackLineCards
    {
        get
        {
            List<BasicCard> backLineCards = new List<BasicCard>(backLine.Count);
            foreach (CardStack stack in backLine)
            {
                backLineCards.Add(stack.TopCard);
            }
            return backLineCards;
        }
    }

    public List<BasicCard> FieldCards
    {
        get
        {
            List<BasicCard> fieldCards = new List<BasicCard>(frontLine.Count + backLine.Count);
            fieldCards.AddRange(FrontLineCards);
            fieldCards.AddRange(BackLineCards);
            return fieldCards;
        }
    }

    //a property that returns a list of the face-up bond cards.
    public List<BasicCard> FaceUpBonds
    {
        get
        {
            int n = Bonds.Count;
            List<BasicCard> faceUpBonds = new List<BasicCard>(n);
            for (int i = 0; i < n; i++)
            {
                if (Bonds[i].FaceUp)
                {
                    faceUpBonds.Add(Bonds[i]);
                }
            }
            return faceUpBonds;
        }
    }


    //a constructor for the class assigning a given list of cards as the deck list.
    public CardManager(List<BasicCard> list, LayoutManager lm, RetreatView rv, string name)
    {
        layoutManager = lm;
        rv.Setup(this);

        deckList = list;
        foreach (BasicCard card in list)
        {
            card.SetOwner(this);
        }
        deck = deckList;
        for (int i = 0; i < deck.Count; i++)
        {
            layoutManager.PlaceInDeck(deck[i]);
        }
        ShuffleDeck();

        retreat = new List<BasicCard>(deckList.Count);

        playerName = name;
    }

    /*
    //a second constructor for the class without a retreat view.
    public CardManager(List<BasicCard> list, LayoutManager lm, string name)
    {
        layoutManager = lm;

        deckList = list;
        foreach (BasicCard card in list)
        {
            card.SetOwner(this);
        }
        deck = deckList;
        for (int i = 0; i < deck.Count; i++)
        {
            layoutManager.PlaceInDeck(deck[i]);
        }
        ShuffleDeck();

        retreat = new List<BasicCard>(deckList.Count);

        playerName = name;
    }
    */

    public void SetOpponent (CardManager otherPlayer)
    {
        opponent = otherPlayer;
    }

    //This method sets up the MC card on a player's field at the start of the game.
    public void SetMCAtStart(BasicCard card)
    {
        deck.Remove(card);
        playerMC = layoutManager.SetAsMC(card);
        frontLine.Add(playerMC);
        
        Debug.Log(playerName + "'s MC was set as " + MCCard.CharName + ": " + MCCard.ToString());
    }

    public void Mulligan()
    {
        Debug.Log(playerName + "'s hand will be mulliganed.");

        //Returns all cards in the Hand to the deck
        while (hand.Count > 0)
        {
            CardToTopDeck(hand[0], hand);
        }

        ShuffleDeck();

        Draw(6);
    }

    //Set up 5 orbs and flips the MC face up. 
    public void AtStart()
    {
        for (int i = 0; i < 5; i++)
        {
            AddOrbFromTopDeck();
        }

        if (!MCCard.FaceUp)
        {
            MCCard.FlipFaceUp();
        }
    }


    //Add a card from the top of the Deck to the Orb zone
    public void AddOrbFromTopDeck()
    {
        //Debug.Log("The top card of the deck will be added to the orbs.");

        //Warnings to handle unlikely situations.
        if (orbs.Count >= 5)
        {
            Debug.LogWarning("This player already has at least 5 orbs!  Is this intentional?");
        }
        if (deck.Count <= 0)
        {
            Debug.LogWarning("There are no cards in the deck!  Trying to shuffle retreat back into the deck.");
            ShuffleRetreatIntoDeck();

            if (deck.Count <= 0)
            {
                Debug.LogWarning("There are still no cards in the deck!  Cannot add an orb from the deck.");
                return;
            }
        }

        orbs.Add(deck[0]);
        layoutManager.PlaceInOrbs(deck[0]);
        //Debug.Log(deck[0].ToString() + " was added as an orb.  There are now " + orbs.Count + " orbs.");
        deck.RemoveAt(0);
    }

    //Moves a card from its present location to the top of the deck.
    public void CardToTopDeck(BasicCard card, List<BasicCard> presentLocation)
    {
        //Debug.Log(card.ToString() + " will be placed on top of the deck.");

        if (!presentLocation.Contains(card))
        {
            Debug.LogError("ERROR! " + card.ToString() + " not found in " + presentLocation.ToString());
        }
        else
        {
            presentLocation.Remove(card);
            deck.Insert(0, card);
            layoutManager.PlaceInDeck(card);
            Debug.Log(card.ToString() + " placed in Deck.  Deck now has " + deck.Count + " cards, and " + presentLocation.ToString() + " has " + presentLocation.Count + " cards.");
        }
        
    }

    //Moves a card from its present location (save the field) to the hand.
    public void CardToHand(BasicCard card, List<BasicCard> presentLocation)
    {
        //Debug.Log(card.ToString() + " will be placed in the hand.");

        if (!presentLocation.Contains(card))
        {
            Debug.LogError("ERROR! " + card.ToString() + " not found in " + presentLocation.ToString());
        }
        else
        {
            presentLocation.Remove(card);
            hand.Add(card);
            layoutManager.PlaceInHand(card);
            Debug.Log(card.ToString() + " placed in Hand.  Hand now has " + hand.Count + " cards, and " + presentLocation.ToString() + " has " + presentLocation.Count + " cards.");
        }

    }

    //A helper method to post how many cards were drawn to the Game Log.
    private void DrawMessage(int cardsDrawn)
    {
        string drawMessage = playerName + " draws " + cardsDrawn;
        if (cardsDrawn == 1)
            drawMessage += " card.";
        else
            drawMessage += " cards.";
        CardReader.instance.UpdateGameLog(drawMessage);
    }

    public void Draw(int numCards)
    {
        if (numCards < deck.Count)
        {
            DrawMessage(numCards);

            while (numCards > 0)
            {
                DrawOneCard();
                numCards--;
            }
        }
        else
        {
            Debug.Log("There are not enough cards in the deck.  Remaining cards will be drawn and the retreat shuffled into the deck.");
            
            //Find the number of cards to be drawn after emptying the deck.
            int remainingCards = numCards - deck.Count;

            DrawMessage(deck.Count);

            //Draw as many cards as possible from the deck.
            while (deck.Count > 0)
            {
                DrawOneCard();
            }

            ShuffleRetreatIntoDeck();

            //Draw remaining cards from the newly shuffled deck.
            while (remainingCards > 0)
            {
                if (DrawOneCard())
                    DrawMessage(1);
                else
                    CardReader.instance.UpdateGameLog("There are no more cards in " + playerName + "'s Deck or Retreat Area! " +
                        "This draw is forfeited.");

                remainingCards--;
            }
        }
    }

    private bool DrawOneCard()
    {
        if (deck.Count > 0)
        {
            hand.Add(deck[0]);
            layoutManager.PlaceInHand(deck[0]);
            Debug.Log(deck[0].ToString() + " was drawn to the hand.");
            deck.RemoveAt(0);
            return true;
        }
        else
        {
            Debug.LogWarning("There are no more cards in the deck or the retreat!  This draw is forfeited.");
            return false;
        }
        
    }

    //may be able to make a private function, though I guess skills can call it...
    public void ShuffleRetreatIntoDeck()
    {
        Debug.Log("The retreat will be shuffled into the deck.");

        if (Deck.Count > 0)
        {
            Debug.LogWarning("The deck is not empty!  Is this intentional?");

            for (int i = 0; i < Retreat.Count; i++)
            {
                deck.Add(Retreat[i]);
                layoutManager.PlaceInDeck(Retreat[i]);
            }
            retreat.Clear();
            ShuffleDeck();

            CardReader.instance.UpdateGameLog(playerName + "'s Retreat Area was shuffled into the Deck!");
        }
        else
        {
            deck.AddRange(Retreat);
            //Debug.Log("This is a test.  The Deck has this many cards: " + Deck.Count + " and the deck has this many: " + deck.Count);
            for (int i = 0; i < Deck.Count; i++)
            {
                layoutManager.PlaceInDeck(Deck[i]);
            }
            retreat.Clear();
            ShuffleDeck();

            CardReader.instance.UpdateGameLog(playerName + "'s Deck is empty; shuffling the Retreat Area into the Deck!");
        }

        Debug.Log("The retreat has been shuffled back into the deck.  The deck now has " + deck.Count + " cards.");
    }

    public void ShuffleDeck()
    {
        //Debug.Log("The deck will be shuffled.  It currently has " + deck.Count + " cards.");

        int numCardsInDeck = Deck.Count;
        List<BasicCard> shuffledPile = new List<BasicCard>(Deck.Capacity);
        for (int i = 0; i < numCardsInDeck; i++)
        {
            //chooses a random integer from 0 to the number of cards in the deck
            int cardIndexToMove = Mathf.FloorToInt(UnityEngine.Random.value * (numCardsInDeck - i));

            //Debug.Log("Attempted Index: " + cardIndexToMove);

            //It is possible for the above method to return an invalid index (when Random.value = 1).
            if (cardIndexToMove == Deck.Count)
            {
                cardIndexToMove--;
                Debug.LogWarning("You got lucky?!  Double check your deck-shuffling algorithm.");
            }

            //places the card at that index in the shuffled pile, removing it from the deck.
            shuffledPile.Add(Deck[cardIndexToMove]);
            deck.RemoveAt(cardIndexToMove);
        }
        deck = shuffledPile;

        Debug.Log("The deck was shuffled.  It still has " + deck.Count + " cards.");
    }

    public void PlaceCardInBonds(BasicCard card, List<BasicCard> presentLocation, bool placeFaceUp)
    {
        if (!presentLocation.Contains(card))
        {
            Debug.LogError("ERROR! Tried to place card in bonds, but " + card.ToString() + " not found in " + presentLocation.ToString());
        }
        else
        {
            presentLocation.Remove(card);
            Bonds.Add(card);

            //text for the Game Log
            string bondText = playerName + " places " + card.CharName + ": " + card.CharTitle + " in the Bond Area ";

            if (placeFaceUp)
            {
                AddColorToBonds(card);
                bondText += "faceup.";
            }
            else
                bondText += "faceudown.";

            layoutManager.PlaceInBonds(card, placeFaceUp);
            CardReader.instance.UpdateGameLog(bondText);
            Debug.Log(card.ToString() + " placed in Bonds.  Bonds now has " + Bonds.Count + " cards, and " + presentLocation.ToString() + " has " + presentLocation.Count + " cards.");
        }
    }

    private void AddColorToBonds(BasicCard card)
    {
        //Loops through each possible color
        for (int i = 0; i < CipherData.NumColors; i++)
        {
            //if the card contains that color, it adds the color to the BondedColors array
            if (card.CardColorArray[i])
            {
                bondedColors[i] = true;
            }
        }
    }

    //This helper method can be called after flipping some bonds facedown to redo the Bonded Colors list
    //may need to be public if color changing is allowed on bonds due to effects so that this can be called when appropriate.
    private void UpdateBondedColors()
    {
        //Resets the bondedColors list
        Array.Clear(bondedColors, 0, bondedColors.Length);

        //Re-adds all the current faceup bonds' colors to the list.
        for (int i = 0; i < FaceUpBonds.Count; i++)
        {
            AddColorToBonds(FaceUpBonds[i]);
        }
    }

    //This method checks to see if a particular card's colors have been bonded, so that it can be played.
    public bool AreCardColorsBonded(BasicCard card)
    {
        //Loop through each possible color
        for (int i = 0; i < card.CardColorArray.Length; i++)
        {
            //if card does have color, but bondedColors doesn't then return false.
            if (card.CardColorArray[i])
            {
                if (!BondedColors[i])
                {
                    return false;
                }
            }
        }

        //if bondedColors contains all of the card's colors, return true.
        return true;
    }

    //This method allows the user to choose bonds to flip
    //Note that since this is being called in the middle of a skill effect, I am not going to allow for a soft cancel.
    //ADDITION: Consider adding a reference to the name of the skill being called as well as provide that text.
    public void ChooseBondsToFlip(int numToFlip)
    {
        //This sets up the method to call after the CardPicker finishes.
        MyCardListEvent eventToCall = new MyCardListEvent();
        eventToCall.AddListener(FlipBonds);

        //makes the player choose the faceup bond cards to flip for whatever effect.
        CardPickerDetails details = new CardPickerDetails
        {
            cardsToDisplay = FaceUpBonds,
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

        details.instructionText += " to flip to activate this skill.";

        CardPickerWindow cardPicker = CardPickerWindow.Instance();

        cardPicker.ChooseCards(details);
    }

    //This helper method actually flips the bonds in question facedown.  After flipping it calls back the method which presumably activating the bond flip.
    private void FlipBonds(List<BasicCard> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            list[i].FlipFaceDown();
            CardReader.instance.UpdateGameLog(playerName + " flips the bonded " + list[i].CharName + ": " + list[i].CharTitle 
                + " facedown!");
        }

        UpdateBondedColors();

        FinishBondFlipEvent.Invoke();
    }


    //This general method decides where/how to deploy a given card and then calls an appropriate helper method.
    //NOTE: This method was made obsolete by the deploy/LevelUp/ClassChange split, but is being kept for reintegration for skill deploys
    //New Name: PlayToFieldWithSkill
    /*
    public void DeployToField(BasicCard card, List<BasicCard> presentLocation)
    {
        //Check if the card is where it should be right now, then remove it, else throw an error.
        if (!presentLocation.Contains(card))
        {
            Debug.LogError("ERROR! Tried to deploy card, but " + card.ToString() + " not found in " + presentLocation.ToString());
        }
        else
        {
            presentLocation.Remove(card);

            //Checks if the card is already on the field in which case this is a level up or class change.
            if (FieldCards.Exists(x => x.CharName.Equals(card.CharName)))
            {
                Debug.Log(card.CharName + " is already on the field.  Performing a Level up.");
                CardStack stackToAddTo = FieldStacks.Find(x => x.TopCard.CharName.Equals(card.CharName));
                stackToAddTo.AddCardToStack(card);
                if (card.PromotionCost > 0)
                {
                    GameManager.instance.bondDeployCount += card.PromotionCost;
                }
                else
                {
                    GameManager.instance.bondDeployCount += card.DeploymentCost;
                }
                GameManager.instance.UpdateDeploymentHintText();
            }
            else //The card is not on the field, so we need to know where to deploy it.  Call a dialogue box.
            {
                DialogueWindowDetails details = new DialogueWindowDetails
                {
                    windowTitleText = "Deployment",
                    questionText = "Where would you like to deploy " + card.CharName + "?",
                    button1Details = new DialogueButtonDetails
                    {
                        buttonText = "Front Line",
                        buttonAction = () => { frontLine.Add(layoutManager.PlaceInFrontLine(card));
                            GameManager.instance.bondDeployCount += card.DeploymentCost; GameManager.instance.UpdateDeploymentHintText();
                        },   
                    },
                    button2Details = new DialogueButtonDetails
                    {
                        buttonText = "Back Line",
                        buttonAction = () => { backLine.Add(layoutManager.PlaceInBackLine(card));
                            GameManager.instance.bondDeployCount += card.DeploymentCost; GameManager.instance.UpdateDeploymentHintText();
                        },  
                    }
                };

                DialogueWindow dialogueWindow = DialogueWindow.Instance();
                dialogueWindow.MakeChoice(details);
            }
        }
    }
    */

    //moves a card from one part of the field to the other.  This is something of a catch all method and so tapping is done in another place.
    public void MoveCard(BasicCard card)
    {
        Debug.Log("Moving " + card.ToString() + ".");
        
        //Check if the card is where it should be else throw an error.
        if (!FieldCards.Contains(card))
        {
            Debug.LogError("ERROR! Tried to move card, but " + card.ToString() + " not found on the field.");
        }
        else
        {
            //Locates the CardStack to move on the field.
            CardStack stackToMove = FieldStacks.Find(x => x.TopCard == card);

            //determines how and from where to move the stack.
            if (FrontLineStacks.Contains(stackToMove))
            {
                MoveStackToBackLine(stackToMove);
                CardReader.instance.UpdateGameLog(playerName + "'s " + card.CharName + ": " + card.CharTitle
                    + " moves to the Back Line!");
            }
            else if (BackLineStacks.Contains(stackToMove))
            {
                MoveStackToFrontLine(stackToMove);
                CardReader.instance.UpdateGameLog(playerName + "'s " + card.CharName + ": " + card.CharTitle
                    + " moves to the Front Line!");
            }
            else
            {
                Debug.LogError("The CardStack " + stackToMove.ToString() + " could not be found on the field...");
            }

            

            CheckForcedMarch();
        }
    }

    //helper method to move a CardStack directly from the Front to the Back Line.
    private void MoveStackToBackLine(CardStack stack)
    {
        frontLine.Remove(stack);
        backLine.Add(stack);
        layoutManager.MoveStack(stack, false);
    }

    //helper method to move a CardStack directly from the Back to the Front Line.
    private void MoveStackToFrontLine(CardStack stack)
    {
        backLine.Remove(stack);
        frontLine.Add(stack);
        layoutManager.MoveStack(stack, true);
    }

    //Determines if a forced march is necessary and carries it out.
    public void CheckForcedMarch ()
    {
        //Forced march only happens on the opponent's turn if the Front Line is empty 
        if (GameManager.instance.turnPlayer != this)
        {
            if (FrontLineStacks.Count == 0)
            {
                Debug.Log("Activating a Forced March!");
                CardReader.instance.UpdateGameLog(playerName + "'s units must undergo a Forced March!");

                int n = BackLineStacks.Count;

                //moves each stack one at a time down the list of CardStacks
                for (int i = 0; i < n; i++)
                {
                    /* I don't believe that units are automatically tapped.  This isn't a real 'movement'.
                    //Tap the card if it was not tapped already.
                    if (!BackLineStacks[0].TopCard.Tapped)
                    {
                        BackLineStacks[0].TopCard.Tap();
                    }
                    */

                    MoveStackToFrontLine(BackLineStacks[0]);
                }
            }
            else
            {
                //Debug.Log("No forced march necessary; Front Line not empty.");
            }
        }
        else
        {
            Debug.Log("It's still " + this.ToString() + "'s turn.");
        }
    }

    

    //levels up or class changes a card on the field
    //should only be called by the ContextMenu buttons and so doesn't do any checks on location, etc.
    public void PlayToFieldFromHand(BasicCard card, bool classChange)
    {
        //Increment the used bond count before anything on the board changes. 
        if (classChange)
        {
            GameManager.instance.bondDeployCount += card.PromotionCost;
        }
        else    //this is just a level up
        {
            GameManager.instance.bondDeployCount += card.DeploymentCost;
        }
        
        //remove the card from the hand.
        Hand.Remove(card);

        //Find the location on the field to play the card to.
        CardStack stackToAddTo = FieldStacks.Find(x => x.TopCard.CompareNames(card, true));

        //Post this action to the Game Log
        if (classChange)
            CardReader.instance.UpdateGameLog(card.Owner.playerName + " class changes " + stackToAddTo.TopCard.CharName + " into "
                + card.CharName + ": " + card.CharTitle + "!");
        else
            CardReader.instance.UpdateGameLog(card.Owner.playerName + " levels up " + stackToAddTo.TopCard.CharName + " into "
                + card.CharName + ": " + card.CharTitle + "!");
        
        stackToAddTo.AddCardToStack(card, classChange);

        //Draw a card and/or call skills as appropriate.
        if (classChange)
        {
            Draw(1);
            //Call class change SKILLS
        }
        else //this is just a level up
        {
            //call level up SKILLS
        }

        //TEMP location for the FieldChangeEvent skill calls.  Should actually be before class change and/or level up skills once implemented?
        FieldChangeEvent.Invoke();

        //once any skills have activated, then we can update the deployment text with any changes.
        GameManager.instance.UpdateDeploymentHintText();
    }

    //deploys a card on the field
    //should only be called by the ContextMenu buttons and so doesn't do any checks on location, etc.
    public void DeployToFieldFromHand (BasicCard card)
    {
        //Update the used bonds before anything changes on the board.
        GameManager.instance.bondDeployCount += card.DeploymentCost;

        //remove the card from the hand.
        Hand.Remove(card);

        //We need to know where to deploy this card.  Call a dialogue box.
        DialogueWindowDetails details = new DialogueWindowDetails
        {
            windowTitleText = "Deployment",
            questionText = "Where would you like to deploy " + card.CharName + "?",
            button1Details = new DialogueButtonDetails
            {
                buttonText = "Front Line",
                buttonAction = () => { DeployToFrontLine(card); }
            },
            button2Details = new DialogueButtonDetails
            {
                buttonText = "Back Line",
                buttonAction = () => { DeployToBackLine(card); }
            }
        };

        DialogueWindow dialogueWindow = DialogueWindow.Instance();
        dialogueWindow.MakeChoice(details);
    }

    //method to actually deploy a card on the Front Line as well as handle any skills, etc. Called by DeployToFieldFromHand.
    private void DeployToFrontLine(BasicCard card)
    {
        CardReader.instance.UpdateGameLog(card.Owner.playerName + " deploys " 
            + card.CharName + ": " + card.CharTitle + " to the Front Line!");
        frontLine.Add(layoutManager.PlaceInFrontLine(card));

        //Here is where a deploy SKILL check goes.
        CheckDeploymentSkill(card);

        //Only update the hint text if this is still the deployment phase
        if (GameManager.instance.CurrentPhase == CipherData.PhaseEnum.Deployment)
        {
            GameManager.instance.UpdateDeploymentHintText();
        }
    }

    //method to actually deploy a card on the Back Line as well as handle any skills, etc. Called by DeployToFieldFromHand.
    private void DeployToBackLine(BasicCard card)
    {
        CardReader.instance.UpdateGameLog(card.Owner.playerName + " deploys "
            + card.CharName + ": " + card.CharTitle + " to the Back Line!");
        backLine.Add(layoutManager.PlaceInBackLine(card));

        //Here is where a deploy SKILL check goes.
        CheckDeploymentSkill(card);

        if (GameManager.instance.CurrentPhase == CipherData.PhaseEnum.Deployment)
        {
            GameManager.instance.UpdateDeploymentHintText();
        }
    }

    //This helper method calls all the deployment related trigger skills currently registered.
    private void CheckDeploymentSkill(BasicCard card)
    {
        FieldChangeEvent.Invoke();
        deployTriggerTracker.CheckTrigger(card);
    }

    //returns a card from the field to your hand.
    //FOR TEST ONLY
    //need to implement the changes below in parentheses.
    /*
    public void ReturnCardStackToHandFromField(BasicCard card)
    {
        /*
         * need to determine if the card is in the back or front line
         * once that is determined, need to remove THE STACK from that line
         * in the process, need to return the stack to the object pool and take the cards out of it
         * 
         * (Similarly, if a card is taken from a stack, it should just activate the next card in the stack)
         * need to move the card back to the hand.
         * 
         *

        CardStack stackToRemove;
        List<BasicCard> cardsToReturn;
        if (frontLine.Exists(x => x.TopCard == card))
        {
            stackToRemove = frontLine.Find(x => x.TopCard == card);
            frontLine.Remove(stackToRemove);
            cardsToReturn = layoutManager.RemoveStackFromField(stackToRemove);
        }
        else if (backLine.Exists(x => x.TopCard == card))
        {
            stackToRemove = backLine.Find(x => x.TopCard == card);
            backLine.Remove(stackToRemove);
            cardsToReturn = layoutManager.RemoveStackFromField(stackToRemove);
        }
        else
        {
            Debug.LogError(card.ToString() + " cannot be found on the field!  It will not be moved to the hand.");
            return;
        }

        
        for (int i = 0; i < cardsToReturn.Count; i++)
        {
            hand.Add(cardsToReturn[i]);
            layoutManager.PlaceInHand(cardsToReturn[i]);
        }
    }
    */

    //This helper method flips a support card into the appropriate spot.
    public void PlaySupportCard()
    {
        if (Deck.Count > 1)             //There will be at least one card left after flipping the support.
        {
            FlipSupport();
        }
        else if (Deck.Count == 1)       //There will be no cards left after flipping the support.
        {
            FlipSupport();
            ShuffleRetreatIntoDeck();
        }
        else                            //There are no cards in the deck, so no support can be drawn.  Check that this is intentional.
        {
            supportCard = null;
            Debug.LogWarning("There are no cards in the deck!  This support is forfeited.");
            CardReader.instance.UpdateGameLog(playerName + "'s deck is empty; the support draw fails!");
        }
    }

    //This method actually reassigns/moves the support card.
    private void FlipSupport()
    {
        supportCard = Deck[0];
        layoutManager.PlaceInSupport(Deck[0]);
        Debug.Log(Deck[0].ToString() + " was placed in the Support Zone.");
        CardReader.instance.UpdateGameLog(playerName + " draws " + Deck[0].CharName + " as a support!");
        deck.RemoveAt(0);
    }

    public void DiscardSupport()
    {
        retreat.Add(SupportCard);
        layoutManager.PlaceInRetreat(SupportCard);
        Debug.Log(SupportCard.ToString() + " is placed in the Retreat Zone.");
        supportCard = null;
    }

    //This helper method discards a given card from the hand.
    public void DiscardCardFromHand(BasicCard card)
    {
        if (!Hand.Contains(card))
        {
            Debug.LogError(card.ToString() + " was not found in the hand.  Why are you calling DiscardCardFromHand()???");
            return;
        }

        retreat.Add(card);
        layoutManager.PlaceInRetreat(card);
        Debug.Log(card.ToString() + " was placed in the Retreat Zone.");
        CardReader.instance.UpdateGameLog(playerName + " discards " + card.CharName + ": " + card.CharTitle + " to the Retreat Zone.");
        hand.Remove(card);
    }

    //This helper method discards a given card from the field.  This is tricky as both the card and its stack need to be discarded.
    public void DiscardCardFromField(BasicCard card)
    {
        if (!FieldCards.Contains(card))
        {
            Debug.LogError(card.ToString() + " was not found on the field.  Why are you calling DiscardCardFromField()???");
            return;
        }

        //Find the stack that contains the card to be removed.
        CardStack stackToRemove = FieldStacks.Find(x => x.TopCard == card);

        //Remove the stack from the field.
        if (FrontLineStacks.Contains(stackToRemove))
        {
            frontLine.Remove(stackToRemove);
        }
        else if (BackLineStacks.Contains(stackToRemove))
        {
            backLine.Remove(stackToRemove);
        }
        else
        {
            Debug.LogError(stackToRemove.ToString() + " could not be found in the field stacks.  Please investigate!");
            return;
        }

        CardReader.instance.UpdateGameLog(playerName + "'s " + card.CharName + " is sent to the Retreat Area.");

        List<BasicCard> stackedCards = layoutManager.RemoveStackFromField(stackToRemove);

        //place the cards removed from the field in the retreat zone.
        for (int i = 0; i < stackedCards.Count; i++)
        {
            retreat.Add(stackedCards[i]);
            layoutManager.PlaceInRetreat(stackedCards[i]);
            Debug.Log(card.ToString() + " was placed in the Retreat Zone.");
        }

        //Activate any effects related to the change in the field or sending a card to the retreat.
        FieldChangeEvent.Invoke();
    }
    
    //This method takes an orb from the orb zone and adds it to the player's hand.
    //NOTE: this method needs to eventually be adjusted to account for the ability to choose orbs. 
    public void BreakOrbs(int num)
    {
        //Check if there are enough orbs to be broken as appropriate.
        if (Orbs.Count == 0)                            //The lord has been felled with no orbs.  This player has lost.
        {
            GameManager.instance.EndGame(Opponent);
        }
        else if (Orbs.Count > 0 && num <= Orbs.Count)    //There are enough orbs to break the full number requested.
        {
            for (int i = 0; i < num; i++)
            {
                BreakOneOrb();
            }
        }
        else if (Orbs.Count > 0 && num > Orbs.Count)    //There are not enough orbs to break the full number requested.  Break all of them!
        {
            num = Orbs.Count;
            for (int i = 0; i < num; i++)
            {
                BreakOneOrb();
            }
        }
    }
    
    //This helper method takes an individual orb from the orb zone and moves it to the hand.
    private void BreakOneOrb()
    {
        CardReader.instance.UpdateGameLog("One of " + playerName + "'s orbs breaks and is added to their hand.");
        hand.Add(Orbs[0]);
        layoutManager.PlaceInHand(Orbs[0]);
        orbs.RemoveAt(0);
    }
    
    //This method untaps all units on the player's field.  Useful at the beginning of each round.
    public void UntapAllUnits()
    {
        List<BasicCard> cardsToCheck = FieldCards;

        for (int i = 0; i < cardsToCheck.Count; i++)
        {
            if (cardsToCheck[i].Tapped)
            {
                cardsToCheck[i].Untap();
            }
        }
    }

}
