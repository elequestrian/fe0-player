using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class represents an AI opponent in the game.

[System.Serializable]
public class AIPlayer : DecisionMaker
{
    //This is a theoretical repository/scriptable object which would contain the main compenents needed to generalize an AI opponent
    //for any deck.
    //[SerializeField] protected AIOpponentData data;
    //Below are some of the fields that should be included in the AI data.
    private string targetPromotion = "B01-007R";    //Cain 3
    private int activeBondReserve = 1;              //How many bonds the AI should try to keep active for activating skills.

    /*
    //A constructor which creates the cardManager by inheriting from the DecisionMaker class constructor.
    public AIPlayer(DeckList decklist, CardManager cm, string name) : base(decklist, cm, name)
    {

    }
    */

    //This method sets up the AI player based on the infomation provided in their decklist.
    public override void PlayerSetup()
    {
        SetDefaultMC();
        CardManager.Draw(6);

        //Gives a gap to confirm the AI is acting correctly if the game is in Playtest Mode.
        if (GameManager.instance.playtestMode)
        {
            GameManager.instance.SetPhaseButtonAndHint("Click the red button when you are ready to see if the AI mulliganed.",
            "AI Mulligan?", MulliganChoice);
        }
        else
        {
            MulliganChoice();
        }
        
    }

    //This method sets up the AI player's MC as the default option in the decklist.
    private void SetDefaultMC()
    {
        //First check if there is a default MC.
        if (decklist.DefaultMC != null)
        {
            //Find the MC card in the deck and set it as the MC.  -1 if not found.
            int MCIndex = CardManager.Deck.FindIndex(x => x.CardNumber.Equals(decklist.DefaultMC));

            if (MCIndex >= 0)
            {
                CardManager.SetMCAtStart(CardManager.Deck[MCIndex]);
                //CardReader.instance.DisplayGameLog();
            }
            //Could not find the MC in the deck; throw an error.
            else
            {
                Debug.LogError("Method CheckDefaultMC could not find the DeckList's DefaultMC: " + decklist.DefaultMC
                    + " in " + PlayerName + "'s Deck.");
            }
        }
        //If no default MC, then throw an error.
        else
        {
            Debug.LogError("No default MC assigned for " + PlayerName + "'s DeckList.");
        }
    }

    //This method helps the AI Player decide whether or not to Mulligan.
    //Moves to the PostMulligan method to choose the next appropriate action based on the board set up/status of the beginning.
    //The computer checks their hand for the "target Promotion" and mulligans if it's not there.
    //EXPAND: Check for both a cost 1 card and the target Promotion to avoid being a sitting duck.
    //Consider adding "secondary targets" like a healer.
    public void MulliganChoice()
    {
        if (!CardManager.Hand.Exists(x => x.CardNumber.Equals(targetPromotion)))
        {
            /*
             * Debug.Log(CardManager.Hand.Exists(x => x.CardNumber.Equals(targetPromotion)));
            string text = activeBondReserve + ": ";
            foreach (BasicCard card in CardManager.Hand)
            {
                text += card.CardNumber + " = " + targetPromotion + " " + card.CardNumber.Equals(targetPromotion) + ", ";
            }
            Debug.Log(text);
            */

            CardReader.instance.UpdateGameLog(PlayerName + " has mulliganed.");
            CardManager.Mulligan();
        }
        
        PostMulligan();
    }

    //This method allows an AI player to make choices during the Beginning Phase.
    //For now it allows humans to check on the AI's behavior.
    public override void OnBeginningPhase()
    {
        string hint = "Check which card the AI chooses to bond.  Click the red button to see.";

        GameManager.instance.SetPhaseButtonAndHint(hint, "Begin AI Bond", base.OnBeginningPhase);
    }

    //This method allows an AI player to choose which cards to bond during their Bond Phase.
    //KEEP IT SIMPLE, STUPID!
    //Keep doubles in hand if possible.
    public override void OnBondPhase()
    {
        //Start by assigning each bondable card in the hand a rank based on how likely it is to be needed later.
        //Use rules based on similarity to MC, preferred promotion, card already on the table, deployment cost versus bonds, 
        //turn count, etc.

        //Proceed with bonding if the AI has cards in their hand and less than 5 bonds or no face-up bonds.
        if ((CardManager.Bonds.Count < 5 || CardManager.FaceUpBonds.Count < activeBondReserve) && CardManager.Hand.Count > 0)
        {
            List<BasicCard> potentialBonds = new List<BasicCard>(CardManager.Hand.Count);

            //Create a list of all bondable cards in the hand.
            foreach (BasicCard card in CardManager.Hand)
            {
                if (card.Bondable)
                {
                    potentialBonds.Add(card);
                }
                else    //Card cannot be bonded, so exclude.
                {
                    card.aiRanking = 0;
                }
            }

            //Confirm that there are Bondable cards.
            if (potentialBonds.Count > 0)
            {
                //Rank potential bonds and determine the lowest ranking.
                int lowestRank = BondRanking(potentialBonds);

                //Confirm the rankings in the debug code
                string rankings = "";

                foreach (BasicCard card in CardManager.Hand)
                {
                    rankings += "Bond Ranking: " + card.CardNumber + " -> " + card.aiRanking + "\n";
                }

                Debug.Log(rankings);

                //Remove all but the lowest ranked cards from potentialBonds
                for (int i = potentialBonds.Count - 1; i >= 0; i--)
                {
                    if (potentialBonds[i].aiRanking > lowestRank)
                    {
                        potentialBonds.RemoveAt(i);
                    }
                }

                //Choose a card to bond based on the rank of possibilities and then randomly within the rank.
                switch(lowestRank)
                {
                    //Level 5: The card is the target promotion and the MC is not at that promotion yet, so we want to save this card.  DON'T BOND!
                    case 5:
                        /*
                        //check if there is more than one target promotion in the hand.  If so, sacrifice/bond one, otherwise save it and don't bond anything.
                        if (potentialBonds.Count > 1)
                        {
                            CardManager.PlaceCardInBonds(potentialBonds[Random.Range(0,potentialBonds.Count)], CardManager.Hand, true);
                        }
                        */
                        break;
                    //Level 4: Cards with the MC's name and duplicate target promotions (dodge bait)
                    case 4:
                        //EXPAND: Try to bond duplicates in the hand.

                        //only do the following checks if we have more than one rank 4 to consider.
                        if (potentialBonds.Count > 1)
                        {
                            //Try not to bond cards you could use to promote later
                            List<BasicCard> nonPromotions = new List<BasicCard>(potentialBonds.Count);

                            foreach (BasicCard card in potentialBonds)
                            {
                                if (card.DeploymentCost <= CardManager.MCCard.DeploymentCost)
                                {
                                    nonPromotions.Add(card);
                                }
                            }

                            if (nonPromotions.Count > 1)
                            {
                                //try to bond a card without a support effect and with the lowest support value
                                //Show a preference to bond MC cards without a support skill.
                                List<BasicCard> noSupportList = nonPromotions.FindAll(card => !card.SkillTypes[(int)CipherData.SkillTypeEnum.Support]);
                                List<BasicCard> lowList = new List<BasicCard>(nonPromotions.Count);

                                //Cull the potential bonds to only the ones of lowest support.
                                if (noSupportList.Count > 0)
                                {
                                    lowList = OnlyExtremeSupportValues(noSupportList, true);
                                }
                                else    //All bondable nonpromtion MC cards in the hand have a support skill.
                                {
                                    lowList = OnlyExtremeSupportValues(nonPromotions, true);
                                }

                                CardManager.PlaceCardInBonds(lowList[Random.Range(0, lowList.Count)], CardManager.Hand, true);
                            }
                            //check if there is only one nonpromotion card in the hand.
                            else if (nonPromotions.Count == 1)
                            {
                                CardManager.PlaceCardInBonds(nonPromotions[Random.Range(0, nonPromotions.Count)], CardManager.Hand, true);
                            }
                            else    //All potential bonds in the hand are MC promotions
                            {
                                //Try not to bond those cards we can actually deploy.
                                List<BasicCard> unplayable = new List<BasicCard>(potentialBonds.Count);

                                int n = CardManager.Bonds.Count + 1;

                                foreach (BasicCard card in potentialBonds)
                                {
                                    if (card.ActualDeployCost > n)
                                    {
                                        unplayable.Add(card);
                                    }
                                }

                                if (unplayable.Count > 0)
                                {
                                    CardManager.PlaceCardInBonds(unplayable[Random.Range(0, unplayable.Count)], CardManager.Hand, true);
                                }
                                else    //All potential MC bonds in the hand are deployable promotions with the expected bonds.
                                {
                                    //Try to bond a card with the lowest expected attack.
                                    List<BasicCard> lowCards = OnlyExtremeExpectedAttack(potentialBonds, true);

                                    if (lowCards.Count > 0)
                                    {
                                        CardManager.PlaceCardInBonds(lowCards[Random.Range(0, lowCards.Count)], CardManager.Hand, true);
                                    }
                                    //All cards hold the same expected attack, so choose one randomly.
                                    else
                                    {
                                        CardManager.PlaceCardInBonds(potentialBonds[Random.Range(0, potentialBonds.Count)], CardManager.Hand, true);
                                    }
                                }
                            }





                            
                        }
                        else    //there is only one rank 4 card available to bond, so bond it.
                        {
                            CardManager.PlaceCardInBonds(potentialBonds[0], CardManager.Hand, true);
                        }
                        
                        break;
                    //Level 3: Cards for characters already in play. (promotion or critical/dodge)
                    case 3:
                        //only do the following checks if we have more than one rank 3 to consider.
                        if (potentialBonds.Count > 1)
                        {
                            //Try not to bond cards you could use to promote later
                            List<BasicCard> nonPromotions = new List<BasicCard>(potentialBonds.Count);

                            foreach (BasicCard card in potentialBonds)
                            {
                                if (card.DeploymentCost <= CardManager.FieldCards.Find(x => x.CompareNames(card)).DeploymentCost)
                                {
                                    nonPromotions.Add(card);
                                }
                            }

                            if (nonPromotions.Count > 0)
                            {
                                CardManager.PlaceCardInBonds(nonPromotions[Random.Range(0, nonPromotions.Count)], CardManager.Hand, true);
                            }
                            else    //All potential bonds in the hand are promotions
                            {
                                //Try not to bond those cards we can actually deploy.
                                List<BasicCard> unplayable = new List<BasicCard>(potentialBonds.Count);

                                int n = CardManager.Bonds.Count + 1;

                                foreach (BasicCard card in potentialBonds)
                                {
                                    if (card.ActualDeployCost > n)
                                    {
                                        unplayable.Add(card);
                                    }
                                }

                                if (unplayable.Count > 0)
                                {
                                    CardManager.PlaceCardInBonds(unplayable[Random.Range(0, unplayable.Count)], CardManager.Hand, true);
                                }
                                else    //All potential bonds in the hand are deployable with the expected bonds.
                                {
                                    //Try to bond a card with the lowest expected attack.
                                    List<BasicCard> lowCards = OnlyExtremeExpectedAttack(potentialBonds, true);

                                    if (lowCards.Count > 0)
                                    {
                                        CardManager.PlaceCardInBonds(lowCards[Random.Range(0, lowCards.Count)], CardManager.Hand, true);
                                    }
                                    //All cards hold the same expected attack, so choose one randomly.
                                    else
                                    {
                                        CardManager.PlaceCardInBonds(potentialBonds[Random.Range(0, potentialBonds.Count)], CardManager.Hand, true);
                                    }
                                }
                            }
                        }
                        //There is only one card in the hand to bond.
                        else
                        {
                            CardManager.PlaceCardInBonds(potentialBonds[0], CardManager.Hand, true);
                        }
                        break;
                    //Level 2: Characters not in play, but with deployment costs within bonds +1 (could be played this turn).
                    //This leve also includes higher deploy cost cards with the same name as cards that can be deployed.
                    //Level 2 - Try to check for cards with the same name in the hand and don't bond those.  So look for singles and bond those.
                    //Also should consider the potential attack for the cards getting bonded.  Want to maximize the attack of the cards on the field.
                    case 2:
                        //only do the following checks if we have more than one rank 2 to consider.
                        if (potentialBonds.Count > 1)
                        {
                            //Try not to bond cards where you have another card of the same name in your hand.
                            List<BasicCard> singletons = new List<BasicCard>(potentialBonds.Count);
                            List<BasicCard> handMinusOne = CardManager.Hand;

                            //check if each card has another copy of the character in the hand besides itself.
                            foreach (BasicCard card in potentialBonds)
                            {
                                handMinusOne.Remove(card);

                                if (!handMinusOne.Exists(x => x.CompareNames(card)))
                                {
                                    singletons.Add(card);
                                }

                                handMinusOne.Add(card);
                            }

                            if (singletons.Count > 0)
                            {
                                CardManager.PlaceCardInBonds(singletons[Random.Range(0, singletons.Count)], CardManager.Hand, true);
                            }
                            else    //All potential bonds in the hand are duplicates
                            {
                                //Try not to bond those cards we can actually deploy.
                                List<BasicCard> unplayable = new List<BasicCard>(potentialBonds.Count);

                                int n = CardManager.Bonds.Count + 1;

                                foreach (BasicCard card in potentialBonds)
                                {
                                    if (card.DeploymentCost > n)
                                    {
                                        unplayable.Add(card);
                                    }
                                }

                                if (unplayable.Count > 0)
                                {
                                    CardManager.PlaceCardInBonds(unplayable[Random.Range(0, unplayable.Count)], CardManager.Hand, true);
                                }
                                else    //All potential bonds in the hand are deployable with the expected bonds.
                                {
                                    //Try to bond a card with the lowest expected attack.
                                    List<BasicCard> lowCards = OnlyExtremeExpectedAttack(potentialBonds, true);

                                    if (lowCards.Count > 0)
                                    {
                                        CardManager.PlaceCardInBonds(lowCards[Random.Range(0, lowCards.Count)], CardManager.Hand, true);
                                    }
                                    //All cards hold the same expected attack, so choose one randomly.
                                    else
                                    {
                                        CardManager.PlaceCardInBonds(potentialBonds[Random.Range(0, potentialBonds.Count)], CardManager.Hand, true);
                                    }
                                }
                            }
                        }
                        //There is only one card in the hand to bond.
                        else
                        {
                            CardManager.PlaceCardInBonds(potentialBonds[0], CardManager.Hand, true);
                        }
                        break;
                    
                    //Level 1: Everything else (single character cards not in play with deployment cost more than current bonds +1; presumably cannot be played this turn)
                    default:
                        //only do the following checks if we have more than one rank 1 to consider.
                        if (potentialBonds.Count > 1)
                        {
                            /*
                             * Given the ranking code, any cards that share a name with others should be at level 2.
                             * Thus we just need to choose one randomly.
                             * 
                            //Try not to bond cards where you have another card of the same name in your hand.
                            //This check should be moved to the ranking section so as to avoid throwing all high level promotions in the bond area.
                            List<BasicCard> singletons = new List<BasicCard>(potentialBonds.Count);
                            List<BasicCard> handMinusOne = CardManager.Hand;

                            //check if each card has another copy of the character in the hand besides itself.
                            foreach (BasicCard card in potentialBonds)
                            {
                                handMinusOne.Remove(card);

                                if (!handMinusOne.Exists(x => x.CompareNames(card)))
                                {
                                    singletons.Add(card);
                                }

                                handMinusOne.Add(card);
                            }

                            if (singletons.Count > 0)
                            {
                                CardManager.PlaceCardInBonds(singletons[Random.Range(0, singletons.Count)], CardManager.Hand, true);
                            }
                            else    //All potential bonds in the hand are duplicates
                            {
                                CardManager.PlaceCardInBonds(potentialBonds[Random.Range(0, potentialBonds.Count)], CardManager.Hand, true);
                            }
                            */
                            CardManager.PlaceCardInBonds(potentialBonds[Random.Range(0, potentialBonds.Count)], CardManager.Hand, true);
                        }
                        else
                        {
                            CardManager.PlaceCardInBonds(potentialBonds[0], CardManager.Hand, true);
                        }
                        break;
                }

            }
        }

        //This allows humans to check on the AI's behavior.
        string hint = "Check which card(s) the AI chooses to deploy.  Click the red button to see.";

        GameManager.instance.SetPhaseButtonAndHint(hint, "AI Deploy?", base.OnBondPhase);
    }

    //A helper method which ranks a list of cards based on their desirability to be bonded, then returns the lowest rank.
    protected int BondRanking(List<BasicCard> list)
    {
        int lowestRank = 10;

        //remembers if there is a targetPromotion in the hand.
        bool havePromotion = false;

        //loop through the bondable cards and assign each a ranking based on how likely it is to be needed later.
        foreach (BasicCard card in list)
        {
            //Level 5: The card is the target promotion, the MC is not at that promotion yet, so we want to save this card.
            if (card.CardNumber.Equals(targetPromotion) && !CardManager.MCCard.CardNumber.Equals(targetPromotion))
            {
                if (!havePromotion)
                {
                    card.aiRanking = 5;
                    havePromotion = true;
                }
                else    //There is already a target Promotion card in the hand.
                {
                    card.aiRanking = 4;
                }
            }
            //Level 4: Cards with the MC's name (dodge bait/bridges/finishers)
            else if (card.CompareNames(CardManager.MCCard))
            {
                card.aiRanking = 4;
            }
            //Level 3: Cards for characters already in play. (promotion or critical/dodge)
            else if (CardManager.FieldCards.Exists(x => x.CompareNames(card)))
            {
                card.aiRanking = 3;
            }
            //Level 2: Characters not in play, but with deployment costs within bonds +1 (could be played this turn).
            else if (card.DeploymentCost <= CardManager.Bonds.Count + 1)
            {
                card.aiRanking = 2;
            }
            //Also Level 2: Cards with the same name as another card in the hand that could be played.
            //Level 1: Everything else (characters not in play with deployment cost more than current bonds +1; presumably cannot be played this turn)
            //May need to switch ranks 1 and 2 as the game advances (at higher turn counts).
            else
            {
                //check the hand minus this card to see if this is a double.  If so, rank at level 2, otherwise rank as 1. 
                List<BasicCard> handMinusOne = CardManager.Hand;
                handMinusOne.Remove(card);

                if (handMinusOne.Exists(x => x.CompareNames(card)))
                    card.aiRanking = 2;
                else
                    card.aiRanking = 1;

                handMinusOne.Add(card);
            }

            //tracks the lowest assigned rank
            if (card.aiRanking < lowestRank)
            {
                lowestRank = card.aiRanking;
            }
        }

        return lowestRank;
    }

    //This method allows an AI player to decide which cards to deploy during their Deployment Phase.
    public override void OnDeployPhase()
    {
        
        List<BasicCard> potentialDeploys = new List<BasicCard>(CardManager.Hand.Count);
        int lowestDeployCost = 10;
        int availableBonds = CardManager.Bonds.Count - GameManager.instance.bondDeployCount;

        foreach (BasicCard card in CardManager.Hand)
        {
            int n = card.ActualDeployCost;

            //Update the lowestDeployCost in the hand for iteration purposes.
            if (n < lowestDeployCost)
            {
                lowestDeployCost = n;
            }

            //Create a list of poential deploys right now.
            if (n <= availableBonds)
            {
                potentialDeploys.Add(card);
            }
            else    //Card cannot be deployed right now, so exclude.
            {
                card.aiRanking = 0;
            }
        }

        /*
        //The analysis will need to iterate due to the potential for the hand to change during deployment.
        //Keep trying to deploy while the lowest deploy cost for a card in the hand is less than the number of bonds availabe for deployment.
        while (lowestDeployCost < (CardManager.Bonds.Count - GameManager.instance.bondDeployCount))
        {

        }
        */

        //Similar to the Bond Analysis, start by ranking each card in the hand by desireability to be deployed.
        //Consider if it is the target promotion, a promotion for a card already on the field, or even playable right now
        //based on the bonds available.

        //Confirm that there are deployable cards.
        if (potentialDeploys.Count > 0)
        {
            //Begin new DeployRanking Method? (returns highestRank)
            int highestRank = 0;

            //loop through the deployable cards and assign each a ranking based on prioity to deploy.
            //NOTE: Consider/Test how to deal with name Shenanigans (Lucina, Marth, etc.)
            foreach (BasicCard card in potentialDeploys)
            {
                //Level 10: The card is the target promotion and the MC is not at that promotion yet.
                //Do I need to check if I want to deploy the target promotion even if it's already on the field? (Green?)
                if (card.CardNumber.Equals(targetPromotion) && !CardManager.MCCard.CardNumber.Equals(targetPromotion))
                {
                    card.aiRanking = 10;
                }
                //Level 9: Cards with the MC's name and a higher promotion cost than we are currently at (finishers + bridges)
                else if (card.CompareNames(CardManager.MCCard))
                {
                    if (card.DeploymentCost > CardManager.MCCard.DeploymentCost)
                    {
                        card.aiRanking = 9;
                    }
                    //MC Cards with the same or lower deployment cost as what one is already at are generally not great targets for deployment.
                    //NOTE: This is a point of expansion based on particular decks, Green especially.
                    else if (card.DeploymentCost == CardManager.MCCard.DeploymentCost)
                    {
                        card.aiRanking = 5;
                    }
                    else    //The card is a lower deploy cost MC card and should probably be kept in the hand for dodges.
                    {
                        card.aiRanking = 1;
                    }
                    
                }
                //Level 8: Promotions and Level ups for cards already in play. 
                else if (CardManager.FieldCards.Exists(x => x.CompareNames(card)))
                {
                    //Find the card with the same name.
                    BasicCard fieldCard = CardManager.FieldCards.Find(x => x.CompareNames(card));

                    if (card.DeploymentCost > fieldCard.DeploymentCost)
                    {
                        card.aiRanking = 8;
                    }
                    //As above, cards with equal or less deployment cost than the one on the field are generally not great options for deployment.
                    else if (card.DeploymentCost == fieldCard.DeploymentCost)
                    {
                        card.aiRanking = 4;
                    }
                    else    //The card is a lower deployment cost version of the one on the field and should probably be kept in the hand for dodges.
                    {
                        card.aiRanking = 2;
                    }

                }
                //Level 7: Characters not currently in play, but playable
                else
                {
                    card.aiRanking = 7;
                }

                //tracks the lowest assigned rank
                if (card.aiRanking > highestRank)
                {
                    highestRank = card.aiRanking;
                }
            }
            
            //Confirm the rankings in the debug code
            string rankings = "";

            foreach (BasicCard card in CardManager.Hand)
            {
                rankings += "Deploy Ranking: " + card.CardNumber + " -> " + card.aiRanking + "\n";
            }

            Debug.Log(rankings);

            //end new method?


            //Now come decisions about which one to actually deploy.

            //Remove all but the highest ranked cards from potentialDeploys
            for (int i = potentialDeploys.Count - 1; i >= 0; i--)
            {
                if (potentialDeploys[i].aiRanking < highestRank)
                {
                    potentialDeploys.RemoveAt(i);
                }
            }

            //Choose a card to deploy based on the rank of possibilities and then usually randomly within the rank.
            //NOTE: Need to do a better job differentiating between cards with similar Deployment Costs
            switch (highestRank)
            {
                //Level 10: The card is the target promotion and the MC is not at that promotion yet.
                //Play the card to the field class changing if possible and leveling up if not.
                case 10:
                    CardManager.PlayToFieldFromHand(potentialDeploys[0], potentialDeploys[0].Promotable);
                    break;
                //Level 9: Cards with the MC's name and a higher promotion cost than we are currently at (finishers + bridges)
                //Try to play the highest deploy cost MC card in the player's hand 
                case 9:
                    //Only work to choose one if there are multiple possibilities.
                    if (potentialDeploys.Count > 1)
                    {
                        BasicCard cardToPlay = potentialDeploys[0];

                        for (int i = 1; i < potentialDeploys.Count; i++)
                        {
                            if (potentialDeploys[i].DeploymentCost > cardToPlay.DeploymentCost)
                            {
                                cardToPlay = potentialDeploys[i];
                            }
                        }

                        CardManager.PlayToFieldFromHand(cardToPlay, cardToPlay.Promotable);
                    }
                    //There is only one choice
                    else
                    {
                        CardManager.PlayToFieldFromHand(potentialDeploys[0], potentialDeploys[0].Promotable);
                    }
                    
                    break;
                //Level 8: Promotions and Level ups for cards already in play.
                //Promotions are geneally more desireable given the extra card draw, but if you get a stronger level up that may be better.
                //Tries to maximize the expected attack of the chosen card while minimizing the deployment cost (to allow for more deploys).
                case 8:
                    //Only cull if there is more than 1 choice to save performance
                    if (potentialDeploys.Count > 1)
                    {
                        //Cull the potential deploys to only equal the ones of highest expected attack.
                        List<BasicCard> powerList = OnlyExtremeExpectedAttack(potentialDeploys, false);

                        //Confirm if we need to cull further
                        if (powerList.Count > 1)
                        {
                            //Cull again down to those with the lowest actual deploy cost to maximize bond usage.
                            List<BasicCard> lowList = OnlyLowestActualDeployCost(powerList);

                            //Confirm if we need to cull more
                            if (lowList.Count > 1)
                            {
                                //Show a preference to promotions
                                List<BasicCard> potentialPromotes = lowList.FindAll(card => card.Promotable);

                                if (potentialPromotes.Count > 0)
                                {
                                    CardManager.PlayToFieldFromHand(potentialPromotes[Random.Range(0, potentialPromotes.Count)], true);
                                }
                                else    //No promotions in the considered deploys
                                {
                                    CardManager.PlayToFieldFromHand(lowList[Random.Range(0, lowList.Count)], false);
                                }
                            }
                            //There is only one entry in the list of low actual deploy costs.
                            else
                            {
                                CardManager.PlayToFieldFromHand(lowList[0], lowList[0].Promotable);
                            }
                        }
                        //There is only one entry in power list.
                        else
                        {
                            CardManager.PlayToFieldFromHand(powerList[0], powerList[0].Promotable);
                        }                       
                    }
                    //There is only one choice
                    else
                    {
                        CardManager.PlayToFieldFromHand(potentialDeploys[0], potentialDeploys[0].Promotable);
                    }
                    
                    break;
                //Level 7: Characters not currently in play, but playable
                //Tries to maximize the deployment cost on the chosen card
                //and then tries to choose a card with a duplicate in hand (promotion/critical/dodge).
                //There is room here for nuance/further development.
                //For instance, trying to maximize attack while minizing deployment cost.
                case 7:
                    //Only cull if there is more than 1 choice to save performance
                    if (potentialDeploys.Count > 1)
                    {
                        //Cull the potential deploys to only equal the ones of highest expected attack.
                        List<BasicCard> powerList = OnlyExtremeExpectedAttack(potentialDeploys, false);

                        //Confirm if we need to cull further
                        if (powerList.Count > 1)
                        {
                            //Cull again down to those with the lowest actual deploy cost to maximize bond usage.
                            List<BasicCard> lowList = OnlyLowestActualDeployCost(powerList);

                            //Confirm if we need to cull more
                            if (lowList.Count > 1)
                            {
                                //try to play a card that has other cards of the same name in the hand.
                                List<BasicCard> doubles = new List<BasicCard>(lowList.Count);
                                List<BasicCard> handMinusOne = CardManager.Hand;

                                //check if each card has another copy of the character in the hand besides itself.
                                foreach (BasicCard card in lowList)
                                {
                                    handMinusOne.Remove(card);

                                    if (handMinusOne.Exists(x => x.CompareNames(card)))
                                    {
                                        doubles.Add(card);
                                    }

                                    handMinusOne.Add(card);
                                }

                                //Check if we have any doubles we can play then randomly choose one.
                                if (doubles.Count > 0)
                                    CardManager.DeployToFieldFromHand(doubles[Random.Range(0, doubles.Count)]);
                                //All considered deploys in the hand are singles.
                                else
                                    CardManager.DeployToFieldFromHand(lowList[Random.Range(0, lowList.Count)]);
                            }
                            //There is only one entry in the list of low actual deploy costs.
                            else
                            {
                                CardManager.DeployToFieldFromHand(lowList[0]);
                            }
                        }
                        //There is only one entry in power list.
                        else
                        {
                            CardManager.DeployToFieldFromHand(powerList[0]);
                        }
                    }
                    //There is only one choice
                    else
                        CardManager.DeployToFieldFromHand(potentialDeploys[0]);

                    break;
                //Level 6-1: These cards are generally ones we don't want to deploy.
                //Have the computer exit the deploy phase loop and move on
                default:
                    base.OnDeployPhase();
                    return;
            }

        }
        else    //There are no deployable cards in the hand (no cards in hand, or none below the available bond cost). 
        {
            base.OnDeployPhase();
            return;
        }
        
        //Allow a player to see what was deployed and why.
        GameManager.instance.SetPhaseButtonAndHint("", "More Deploy?", OnDeployPhase);

        UpdateDeploymentHintText();
    }

    public override void UpdateDeploymentHintText()
    {
        List<string> bondColors = CardManager.BondedColorNames;

        GameManager.instance.hintText.text = "Click the red button to see what the AI chooses to deploy.  " +
            "It has " + (CardManager.Bonds.Count - GameManager.instance.bondDeployCount) + " bonds remaining to use for deployment, and can deploy the following colors: ";

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

    //A helper method which returns a list with only the BasicCards with the highest or lowest expected attack as desired.
    protected List<BasicCard> OnlyExtremeExpectedAttack(List<BasicCard> originalList, bool minimize)
    {
        //Only cull if we actually have more than one choice so as to save performance.
        if (originalList.Count > 1)
        {
            int standard;
            int n;

            //We want to minimize the expected attack value of the cards in the list.
            if (minimize)
            {
                standard = 200;

                foreach (BasicCard card in originalList)
                {
                    n = card.ExpectedAttackValue;

                    if (n < standard)
                    {
                        standard = n;
                    }
                }
            }
            //We want to maximize the expected attack value of the cards.
            else
            {
                standard = 0;

                foreach (BasicCard card in originalList)
                {
                    n = card.ExpectedAttackValue;

                    if (n > standard)
                    {
                        standard = n;
                    }
                }
            }

            List<BasicCard> extremeList = originalList.FindAll(card => card.ExpectedAttackValue == standard);

            return extremeList;
        }
        //There is only one item in the list.
        else
            return originalList;
    }

    //A helper method which returns a list with only the BasicCards with the lowest actual deployment cost.
    protected List<BasicCard> OnlyLowestActualDeployCost(List<BasicCard> originalList)
    {
        //Only cull if we actually have more than one choice so as to save performance.
        if (originalList.Count > 1)
        {
            int lowestCost = 10;

            foreach (BasicCard card in originalList)
            {
                int n = card.ActualDeployCost;

                if (n < lowestCost)
                {
                    lowestCost = n;
                }
            }

            List<BasicCard> lowList = originalList.FindAll(card => card.ActualDeployCost == lowestCost);

            return lowList;
        }
        else
            return originalList;
    }

    //A helper method which returns a list with only the BasicCards with the highest deployment cost.
    protected List<BasicCard> OnlyHighestDeployCost(List<BasicCard> originalList)
    {
        //Only cull if we actually have more than one choice so as to save performance.
        if (originalList.Count > 1)
        {
            int highestCost = 0;

            foreach (BasicCard card in originalList)
            {
                if (card.DeploymentCost > highestCost)
                {
                    highestCost = card.DeploymentCost;
                }
            }

            List<BasicCard> highList = originalList.FindAll(card => card.DeploymentCost == highestCost);

            return highList;
        }
        else
            return originalList;
    }

    //A helper method which returns a list with only the BasicCards with extreme Support Values.
    //The bool allows for this single method to maximize or minize the support value.
    protected List<BasicCard> OnlyExtremeSupportValues(List<BasicCard> originalList, bool minimize)
    {
        int standard;
        
        //We want to minimize the support value of the cards in the list.
        if (minimize)
        {
            standard = 50;

            foreach (BasicCard card in originalList)
            {
                if (card.CurrentSupportValue < standard)
                {
                    standard = card.CurrentSupportValue;
                }
            }
        }
        //We want to maximize the support value of the cards.
        else
        {
            standard = 0;

            foreach (BasicCard card in originalList)
            {
                if (card.CurrentSupportValue > standard)
                {
                    standard = card.CurrentSupportValue;
                }
            }
        }

        List<BasicCard> extremeList = originalList.FindAll(card => card.CurrentSupportValue == standard);

        return extremeList;

    }

    //This method is where the AI decides the row to deploy a new card.
    //EXPAND: Use potential attack targets/attack differential to make this choice?
    public override void ChooseDeployLocation(BasicCard card)
    {
        if (card.BaseRangeArray[(int)CipherData.RangesEnum.Range1])
        {
            CardManager.DeployToFrontLine(card);
        }
        else    //This card doesn't have a range of 1
            CardManager.DeployToBackLine(card);
    }

    //This method allows an AI player to decide the order for triggered card skills to resolve.
    //EDIT: Will need to add some actual logic to this going forward.  This may also be personalized based on the deck. 
    public override void ChooseAmongTriggeredCards(TriggerEventHandler triggerEvent, List<BasicCard> triggeredCards)
    {
        //Randomly choose a card to activate first.  adds a copy of a random element in the list to the front.
        triggeredCards.Insert(0, triggeredCards[Random.Range(0, triggeredCards.Count)]);

        //removes all but the newly added element.
        triggeredCards.RemoveRange(1, triggeredCards.Count - 1);

        //activate the chosen card.
        triggerEvent.CallTriggerSkill(triggeredCards);
    }

    //This method allows the AI player logic to resolve the skill effect of whatever card was triggered.
    public override void ActivateTriggerSkill(BasicCard activatedCard, BasicCard triggeringCard)
    {
        activatedCard.ResolveTriggerSkillAI(triggeringCard);
    }

    //This method allows the AI player to perform different actions including attack and activate skills during their Action Phase.
    //Considering skill and movement will come in another iteration.  For now, this method will focus on attacking.
    //Move and Skill logic to come.
    public override void OnActionPhase()
    {
        //Below is some basic Action Turn logic.  Have each card add itself to an initiative order based on the game state.
        //On it's turn, each card will decide to attack or move or use its skills.
        //Once all of the cards have satisfied themselves, the game moves forward.
        //EXPANSION IDEAS: Move the MC out of the below initiative logic so that it knows to hide (if it doesn't have promotions)
        //and use its skills based on the other cards around.  This is also a good place for "character specific strategy" like aggression
        //or general defensiveness.  A place for strategy while each card takes care of its own tactics.

        //Empty the initiative list.
        initiativeList.Clear();

        //Have each card on the field assess the board state and add itself to the initiative list if appropriate.
        foreach (BasicCard card in CardManager.FieldCards)
        {
            card.AddToInitiative();
        }

        //Check if there are any cards waiting to act.
        if (initiativeList.Count > 0)
        {
            //Check if the MC is on the initiative list, and if so, give them priority to act.
            if (initiativeList.Contains(CardManager.MCCard))
            {
                CardManager.MCCard.Act();
            }
            else
            {
                //Check if there is a card with a Formation skill in the list.  If so, give it priority to act first.
                int n = initiativeList.FindIndex(ally => ally.SkillTypes[(int)CipherData.SkillTypeEnum.Formation]);

                if (n >= 0)
                {
                    initiativeList[n].Act();
                }
                //If not, have the first card in the initiative List act.
                else
                    initiativeList[0].Act();
            }

            //I need to prevent the below code from overwritting the Phase button for decisions made in combat. 
            if (!GameManager.instance.inCombat)
            {
                GameManager.instance.SetPhaseButtonAndHint("See what actions the AI takes by pressing the red button.", "AI Actions?", OnActionPhase);
            }
        }
        //The initiative list is empty.  Have the AI end its turn.
        else
            base.OnActionPhase();
        
        /*
        //Attacks are only allowed on the first turn.
        if (!GameManager.instance.FirstTurn)
        {
            //Create a list of characters who are not tapped (and so can presumably attack or perform another action).
            List<BasicCard> readiedCards = CardManager.FieldCards.FindAll(card => !card.Tapped);

            if (readiedCards.Count > 0)
            {
                //Choose a random card to act on.
                int n = Random.Range(0, readiedCards.Count);

                //Check that the card has attack targets.
                if (readiedCards[n].AttackTargets.Count > 0)
                {
                    //If so, start the attack logic.
                    GameManager.instance.AimAttack(readiedCards[n]);
                }
                else    //This card cannot attack anything right now.  Move it instead.
                {
                    readiedCards[n].Tap();
                    CardManager.MoveCard(readiedCards[n]);
                }
            }
            else //no cards are untapped.  Presumably, nothing more can be done, so end the turn.
            {
                Debug.Log("No untapped cards.  AI Finished!");
                base.OnActionPhase();
                return;
            }
            
        }
        else    //No attacking allowed on the first turn.  Move to next phase and stop this method.
        {
            Debug.Log("No attacking allowed on the first turn!");
            base.OnActionPhase();
            return;
        }

        //I need to prevent the below code from overwritting the Phase button for decisions made in combat. 
        if (!GameManager.instance.inCombat)
        {
            GameManager.instance.SetPhaseButtonAndHint("See what actions the AI takes by pressing the red button.", "AI Actions?", OnActionPhase);
        }
        */
        
    }

    //This method provides a way for the AI decision maker to choose whether to use a skill requiring bond flips.
    //Takes the card in to allow for more complex strategy based on specific cards or types of skills.
    public override bool ShouldFlipBonds(BasicCard card, int numBondsToFlip)
    {
        //Check if flipping the required number of bonds maintains our strategic bond reserve (for the deck's strategy).
        if (CardManager.FaceUpBonds.Count - numBondsToFlip >= activeBondReserve)
            return true;
        else
            return false;
    }

    //This method allows an AI player to choose which bonds should be flipped due to a skill's activation or effect.
    //This will need to be adjusted in light of multi-color decks.
    public override void ChooseBondsToFlip(BasicCard card, int numToFlip, string skillText)
    {
        List<BasicCard> faceUpBonds = CardManager.FaceUpBonds;

        //Check if we're just flipping all of our Face-up bonds.
        if (faceUpBonds.Count == numToFlip)
        {
            CardManager.FlipBonds(faceUpBonds);
        }
        //We need to randomly choose face-up bond cards to flip.
        else
        {
            List<BasicCard> cardsToFlip = new List<BasicCard>(numToFlip);

            for (int i = 0; i < numToFlip; i++)
            {
                int n = Random.Range(0, faceUpBonds.Count);

                cardsToFlip.Add(faceUpBonds[n]);
                faceUpBonds.RemoveAt(n);
            }

            CardManager.FlipBonds(cardsToFlip);
        }       
    }

    //This method allows an AI player to choose to use Elysian Emblem.
    //[ATK] Elysian Emblem [SUPP] Choose 1 ally other than your attacking unit. You may move that ally.
    public override void TryElysianEmblem()
    {
        //First, confirm if any of the available cards want or should be moved.
        List<BasicCard> otherAllies = GameManager.instance.CurrentAttacker.OtherAllies;
        List<BasicCard> targets = new List<BasicCard>(otherAllies.Count);

        /*
         * EDIT: Use more complex logic to allow the movement of the MC defensively in certain situations.
         * 
        //check if the MC is in those cards which can move.
        //Prioritize moving the MC if possible.
        if (otherAllies.Countains(CardManager.MCCard))
        {
            //Check if the MC card thinks it should be moved per normal logic.
            if (CardManager.MCCard.DecideToMove())
            {
                //move the MC
                targets.Add(CardManager.MCCard);
                AbilitySupport.ActivateElysianEmblem(targets);
            }
            //check if the MC card should be moved defensively.
            else
            {
                //Is the MC in the FrontLine?
            }

        }
        
        //The MC must have attacked, so check the other cards.
        else
        {
            //confirm which of the remaining cards want to move.
            foreach (BasicCard card in otherAllies)
            {
                if (card.DecideToMove())
                {
                    targets.Add(card);
                }
            }

            //confirm if we have allies that want to move.
            if (targets.Count > 1)
            {
                //randomly choose an ally to move.

                //adds a copy of a random element in the list to the front.
                targets.Insert(0, targets[Random.Range(0, targets.Count)]);

                //removes all but the newly added element.
                targets.RemoveRange(1, targets.Count - 1);

                AbilitySupport.ActivateElysianEmblem(targets);
            }
            else if (targets.Count == 1)
            {
                //move the target
                AbilitySupport.ActivateElysianEmblem(targets);
            }
            //no targets to move, so don't use the effect.
            else
            {
                //Proceed with the attack logic.
                GameManager.instance.ActivateDefenderSupport();
            }
        }
        */

        //confirm which of the cards want to move.
        foreach (BasicCard card in otherAllies)
        {
            if (card.DecideToMove())
            {
                targets.Add(card);
            }
        }

        //confirm if we have allies that want to move.
        if (targets.Count > 1)
        {
            //randomly choose an ally to move.

            //adds a copy of a random element in the list to the front.
            targets.Insert(0, targets[Random.Range(0, targets.Count)]);

            //removes all but the newly added element.
            targets.RemoveRange(1, targets.Count - 1);

            AbilitySupport.ActivateElysianEmblem(targets);
        }
        else if (targets.Count == 1)
        {
            //move the target
            AbilitySupport.ActivateElysianEmblem(targets);
        }
        //no targets to move, so don't use the effect.
        else
        {
            //Proceed with the attack logic.
            GameManager.instance.ActivateDefenderSupport();
        }

    }

    //This method provides a way for the AI to choose which cards should be discarded from the Hand due to a skill's activation or effect.
    //Rank cards using the same system as for bonds (till discard effects come into play), then choose some to discard.
    public override void ChooseCardsToDiscardFromHand(BasicCard card, List<BasicCard> listToChooseFrom, int numToDiscard, string skillText)
    {
        //Check if we're just discarding all of the given cards.
        if (listToChooseFrom.Count <= numToDiscard)
        {
            CardManager.DiscardCardsFromHand(listToChooseFrom);
        }
        //We need to choose cards to discard.
        else
        {
            //Rank cards using the same system as for bonds under the assumption that it helps define what we want to keep in the hand.
            int lowestRank = BondRanking(listToChooseFrom);

            //Confirm the rankings in the debug code
            string rankings = "";

            foreach (BasicCard basic in CardManager.Hand)
            {
                rankings += "Discard Ranking: " + basic.CardNumber + " -> " + basic.aiRanking + "\n";
            }

            Debug.Log(rankings);

            //Check if we can discard all the cards we need to from the lowest ranked set.
            List<BasicCard> lowestRankedCards = listToChooseFrom.FindAll(x => x.aiRanking == lowestRank);
            
            //We have exactly the number needed in lowestRankedCards.  Discard them all.
            if (lowestRankedCards.Count == numToDiscard)
            {
                CardManager.DiscardCardsFromHand(lowestRankedCards);
            }
            //We will need to choose cards from this and potentially other ranks.
            else
            {
                List<BasicCard> discards = new List<BasicCard>(numToDiscard);

                for (int i = 0; i < numToDiscard; i++)
                {
                    //Choose a card to discard based on the rank of possibilities and then randomly within the rank.
                    switch (lowestRank)
                    {
                        //Level 5: The card is the target promotion and the MC is not at that promotion yet,
                        //so we want to save this card, but we can't.  Just discard.  Can go to DEFAULT.
                        case 5:
                            int n = Random.Range(0, lowestRankedCards.Count);

                            discards.Add(lowestRankedCards[n]);
                            lowestRankedCards.RemoveAt(n);
                            break;
                        //Level 4: Cards with the MC's name and duplicate target promotions (dodge bait)
                        case 4:
                            //Check if we need to make a choice or if there is only one option.
                            if (lowestRankedCards.Count == 1)
                            {
                                discards.Add(lowestRankedCards[0]);
                                lowestRankedCards.RemoveAt(0);
                            }
                            //We will need to choose one from the current set of lowest ranked cards.
                            else
                            {
                                //Best choice is to discard something with a lower or equivalent cost than your current MC.
                                //Within that range you'll want to discard something with a support skill and max support

                                //First, find all the cards that have an equal or lower deployment cost as your MC. 
                                List<BasicCard> nonPromotions = lowestRankedCards.FindAll(y => y.DeploymentCost <=
                                CardManager.MCCard.DeploymentCost);

                                //Check how many of these nonPromotions there are and whether we need to differentiate them.
                                if (nonPromotions.Count > 1)
                                {
                                    //Cull the potential discards to only equal the ones of lowest expected attack.
                                    List<BasicCard> weakList = OnlyExtremeExpectedAttack(nonPromotions, true);

                                    //if we have more than one weak card, check additional criteria.
                                    if (weakList.Count > 1)
                                    {
                                        //try to discard a card with a support effect and with the highest support value
                                        //Show a preference to discard MC cards with a support skill.
                                        BasicCard discard = FindDiscardBasedOnSupport(weakList);

                                        discards.Add(discard);
                                        lowestRankedCards.Remove(discard);
                                    }
                                    //There is only one weak card, so discard it.
                                    else
                                    {
                                        discards.Add(weakList[0]);
                                        lowestRankedCards.Remove(weakList[0]);
                                    }
                                }
                                //there is only one non-promotion, so let's choose it.
                                else if (nonPromotions.Count == 1)
                                {
                                    discards.Add(nonPromotions[0]);
                                    lowestRankedCards.Remove(nonPromotions[0]);
                                }
                                //There are only MC promotions in the lowest ranked set of cards.
                                else
                                {
                                    //If you must discard something with a higher cost to your MC,
                                    //try to discard something you have two of to keep your options as open as possible
                                    List<BasicCard> doubles = new List<BasicCard>(lowestRankedCards.Count);
                                    List<BasicCard> handMinusOne = CardManager.Hand;

                                    //check if each card has another copy of the character in the hand besides itself.
                                    foreach (BasicCard basic in lowestRankedCards)
                                    {
                                        handMinusOne.Remove(basic);

                                        if (handMinusOne.Exists(x => x.CardNumber.Equals(basic.CardNumber)))
                                        {
                                            doubles.Add(basic);
                                        }

                                        handMinusOne.Add(basic);
                                    }

                                    //Check how many doubles we found.  If more than one, choose those with support skills and max support.
                                    if (doubles.Count > 1)
                                    {
                                        //try to discard a card with a support effect and with the highest support value
                                        //Show a preference to discard MC cards with a support skill.
                                        BasicCard discard = FindDiscardBasedOnSupport(doubles);

                                        discards.Add(discard);
                                        lowestRankedCards.Remove(discard);
                                    }
                                    //There is only one doubled card in this ranking.
                                    else if (doubles.Count == 1)
                                    {
                                        discards.Add(doubles[0]);
                                        lowestRankedCards.Remove(doubles[0]);
                                    }
                                    //No potential discards in the hand are duplicates.  Prioritize keeping cards you are likely to be able
                                    //to play soon (Deploy cost <= bond +1) to maximize your defense.
                                    //Note that we cannot discard our only target promotion here as that would be ranked 5.
                                    else
                                    {
                                        //Check which of the cards are not playable within the next turn.
                                        List<BasicCard> highCost = lowestRankedCards.FindAll(y => y.DeploymentCost > CardManager.Bonds.Count + 1);

                                        //check how many high cost cards we found and if we need to differentiate them.
                                        if (highCost.Count > 1)
                                        {
                                            //Cull the potential discards to only equal the ones of lowest expected attack.
                                            List<BasicCard> weakList = OnlyExtremeExpectedAttack(highCost, true);

                                            //if we have more than one weak card, check additional criteria.
                                            if (weakList.Count > 1)
                                            {
                                                //try to discard a card with a support effect and with the highest support value
                                                //Show a preference to discard MC cards with a support skill.
                                                BasicCard discard = FindDiscardBasedOnSupport(weakList);

                                                discards.Add(discard);
                                                lowestRankedCards.Remove(discard);
                                            }
                                            //There is only one weak card, so discard it.
                                            else
                                            {
                                                discards.Add(weakList[0]);
                                                lowestRankedCards.Remove(weakList[0]);
                                            }
                                        }
                                        //There is only one high cost card so discard it
                                        else if (highCost.Count == 1)
                                        {
                                            discards.Add(highCost[0]);
                                            lowestRankedCards.Remove(highCost[0]);
                                        }
                                        //There are no high cost cards.  Check the lowest ranked set of cards for support skills and support values.
                                        else
                                        {
                                            //Cull the potential discards to only equal the ones of lowest expected attack.
                                            List<BasicCard> weakList = OnlyExtremeExpectedAttack(lowestRankedCards, true);

                                            //if we have more than one weak card, check additional criteria.
                                            if (weakList.Count > 1)
                                            {
                                                //try to discard a card with a support effect and with the highest support value
                                                //Show a preference to discard MC cards with a support skill.
                                                BasicCard discard = FindDiscardBasedOnSupport(weakList);

                                                discards.Add(discard);
                                                lowestRankedCards.Remove(discard);
                                            }
                                            //There is only one weak card, so discard it.
                                            else
                                            {
                                                discards.Add(weakList[0]);
                                                lowestRankedCards.Remove(weakList[0]);
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        //Level 3: Cards for characters already in play. (promotion or critical/dodge)
                        case 3:
                            //only do the following checks if we have more than one rank 3 to consider.
                            if (lowestRankedCards.Count > 1)
                            {
                                //Try not to discard cards you could use to promote later
                                List<BasicCard> nonPromotions = new List<BasicCard>(lowestRankedCards.Count);

                                foreach (BasicCard basic in lowestRankedCards)
                                {
                                    if (basic.DeploymentCost <= CardManager.FieldCards.Find(x => x.CompareNames(basic)).DeploymentCost)
                                    {
                                        nonPromotions.Add(basic);
                                    }
                                }

                                //Check if we need to differentiate the nonPromotions we found based on support characteristics.
                                if (nonPromotions.Count > 1)
                                {
                                    //Cull the potential discards to only equal the ones of lowest expected attack.
                                    List<BasicCard> weakList = OnlyExtremeExpectedAttack(nonPromotions, true);

                                    //if we have more than one weak card, check additional criteria.
                                    if (weakList.Count > 1)
                                    {
                                        //try to discard a card with a support effect and with the highest support value
                                        //Show a preference to discard MC cards with a support skill.
                                        BasicCard discard = FindDiscardBasedOnSupport(weakList);

                                        discards.Add(discard);
                                        lowestRankedCards.Remove(discard);
                                    }
                                    //There is only one weak card, so discard it.
                                    else
                                    {
                                        discards.Add(weakList[0]);
                                        lowestRankedCards.Remove(weakList[0]);
                                    }
                                }
                                //There is only one non promtion so discard it.
                                else if (nonPromotions.Count == 1)
                                {
                                    discards.Add(nonPromotions[0]);
                                    lowestRankedCards.Remove(nonPromotions[0]);
                                }
                                //All potential discards in the hand are promotions
                                else
                                {
                                    //If you must discard something with a higher cost to your played card,
                                    //try to discard something you have two of to keep your options as open as possible
                                    List<BasicCard> doubles = new List<BasicCard>(lowestRankedCards.Count);
                                    List<BasicCard> handMinusOne = CardManager.Hand;

                                    //check if each card has another copy of the character in the hand besides itself.
                                    foreach (BasicCard basic in lowestRankedCards)
                                    {
                                        handMinusOne.Remove(basic);

                                        if (handMinusOne.Exists(x => x.CardNumber.Equals(basic.CardNumber)))
                                        {
                                            doubles.Add(basic);
                                        }

                                        handMinusOne.Add(basic);
                                    }

                                    //Check how many doubles we found.  If more than one, choose those with support skills and max support.
                                    if (doubles.Count > 1)
                                    {
                                        //try to discard a card with a support effect and with the highest support value
                                        //Show a preference to discard cards with a support skill.
                                        BasicCard discard = FindDiscardBasedOnSupport(doubles);

                                        discards.Add(discard);
                                        lowestRankedCards.Remove(discard);
                                    }
                                    //There is only one doubled card in this ranking.
                                    else if (doubles.Count == 1)
                                    {
                                        discards.Add(doubles[0]);
                                        lowestRankedCards.Remove(doubles[0]);
                                    }
                                    //No potential discards in the hand are duplicates.  Prioritize keeping cards you are likely to be able
                                    //to play soon (Deploy cost <= bond +1).
                                    else
                                    {
                                        //Check which of the cards are not playable within the next turn.
                                        List<BasicCard> highCost = lowestRankedCards.FindAll(y => y.DeploymentCost > CardManager.Bonds.Count + 1);

                                        //check how many high cost cards we found and if we need to differentiate them.
                                        if (highCost.Count > 1)
                                        {
                                            //This logic is potentially open for revision as we don't always want to discard the weaker (and so likely easier to play) high cost promotions.
                                            //Cull the potential discards to only equal the ones of lowest expected attack.
                                            List<BasicCard> weakList = OnlyExtremeExpectedAttack(highCost, true);

                                            //if we have more than one weak card, check additional criteria.
                                            if (weakList.Count > 1)
                                            {
                                                //try to discard a card with a support effect and with the highest support value
                                                //Show a preference to discard MC cards with a support skill.
                                                BasicCard discard = FindDiscardBasedOnSupport(weakList);

                                                discards.Add(discard);
                                                lowestRankedCards.Remove(discard);
                                            }
                                            //There is only one weak card, so discard it.
                                            else
                                            {
                                                discards.Add(weakList[0]);
                                                lowestRankedCards.Remove(weakList[0]);
                                            }
                                        }
                                        //There is only one high cost card so discard it
                                        else if (highCost.Count == 1)
                                        {
                                            discards.Add(highCost[0]);
                                            lowestRankedCards.Remove(highCost[0]);
                                        }
                                        //There are no high cost cards.  Check the lowest ranked set of cards for support skills and support values.
                                        //EXPAND: Check the expected attack of the remaining cards and try not to discard those with the most.
                                        else
                                        {
                                            //Cull the potential discards to only equal the ones of lowest expected attack.
                                            List<BasicCard> weakList = OnlyExtremeExpectedAttack(lowestRankedCards, true);

                                            //if we have more than one weak card, check additional criteria.
                                            if (weakList.Count > 1)
                                            {
                                                //try to discard a card with a support effect and with the highest support value
                                                //Show a preference to discard MC cards with a support skill.
                                                BasicCard discard = FindDiscardBasedOnSupport(weakList);

                                                discards.Add(discard);
                                                lowestRankedCards.Remove(discard);
                                            }
                                            //There is only one weak card, so discard it.
                                            else
                                            {
                                                discards.Add(weakList[0]);
                                                lowestRankedCards.Remove(weakList[0]);
                                            }
                                        }
                                    }
                                }
                            }
                            //Only one card in the lowest ranked set to discard.
                            else
                            {
                                discards.Add(lowestRankedCards[0]);
                                lowestRankedCards.RemoveAt(0);
                            }
                            break;
                        //Level 2: Characters not in play, but with deployment costs within bonds +1 (could be played next turn).
                        //Level 2 - Try to check for doubles in the hand and don't bond those.  So look for singles and discard those.
                        case 2:
                            //only do the following checks if we have more than one rank 2 to consider.
                            if (lowestRankedCards.Count > 1)
                            {
                                //Try not to discard cards where you have another card of the same name in your hand.
                                List<BasicCard> singletons = new List<BasicCard>(lowestRankedCards.Count);
                                List<BasicCard> handMinusOne = CardManager.Hand;

                                //check if each card has another copy of the character in the hand besides itself.
                                foreach (BasicCard basic in lowestRankedCards)
                                {
                                    handMinusOne.Remove(basic);

                                    if (!handMinusOne.Exists(x => x.CompareNames(basic)))
                                    {
                                        singletons.Add(basic);
                                    }

                                    handMinusOne.Add(basic);
                                }

                                if (singletons.Count > 1)
                                {
                                    //Cull the potential discards to only equal the ones of lowest expected attack.
                                    List<BasicCard> weakList = OnlyExtremeExpectedAttack(singletons, true);

                                    //if we have more than one weak card, check additional criteria.
                                    if (weakList.Count > 1)
                                    {
                                        //try to discard a card with a support effect and with the highest support value
                                        //Show a preference to discard cards with a support skill.
                                        BasicCard discard = FindDiscardBasedOnSupport(weakList);

                                        discards.Add(discard);
                                        lowestRankedCards.Remove(discard);
                                    }
                                    //There is only one weak card, so discard it.
                                    else
                                    {
                                        discards.Add(weakList[0]);
                                        lowestRankedCards.Remove(weakList[0]);
                                    }
                                }
                                else if (singletons.Count == 1)
                                {
                                    discards.Add(singletons[0]);
                                    lowestRankedCards.Remove(singletons[0]);
                                }
                                //All potential discards in the hand are members of other card sets.
                                else
                                {
                                    //Cull the potential discards to only equal the ones of lowest expected attack.
                                    List<BasicCard> weakList = OnlyExtremeExpectedAttack(lowestRankedCards, true);

                                    //if we have more than one weak card, check additional criteria.
                                    if (weakList.Count > 1)
                                    {
                                        //try to discard a card with a support effect and with the highest support value
                                        //Show a preference to discard cards with a support skill.
                                        BasicCard discard = FindDiscardBasedOnSupport(weakList);

                                        discards.Add(discard);
                                        lowestRankedCards.Remove(discard);
                                    }
                                    //There is only one weak card, so discard it.
                                    else
                                    {
                                        discards.Add(weakList[0]);
                                        lowestRankedCards.Remove(weakList[0]);
                                    }
                                }
                            }
                            //Only one card in the lowest ranked set to discard.
                            else
                            {
                                discards.Add(lowestRankedCards[0]);
                                lowestRankedCards.RemoveAt(0);
                            }
                            break;

                        //Level 1: Everything else (characters not in play with deployment cost more than current bonds +1; presumably cannot be played this turn)
                        default:
                            //only do the following checks if we have more than one card to consider in this rank.
                            if (lowestRankedCards.Count > 1)
                            {
                                //Try not to discard cards where you have another card of the same name in your hand.
                                List<BasicCard> singletons = new List<BasicCard>(lowestRankedCards.Count);
                                List<BasicCard> handMinusOne = CardManager.Hand;

                                //check if each card has another copy of the character in the hand besides itself.
                                foreach (BasicCard basic in lowestRankedCards)
                                {
                                    handMinusOne.Remove(basic);

                                    if (!handMinusOne.Exists(x => x.CompareNames(basic)))
                                    {
                                        singletons.Add(basic);
                                    }

                                    handMinusOne.Add(basic);
                                }

                                if (singletons.Count > 1)
                                {
                                    //Cull the potential discards to only equal the ones of lowest expected attack.
                                    List<BasicCard> weakList = OnlyExtremeExpectedAttack(singletons, true);

                                    //if we have more than one weak card, check additional criteria.
                                    if (weakList.Count > 1)
                                    {
                                        //try to discard a card with a support effect and with the highest support value
                                        //Show a preference to discard cards with a support skill.
                                        BasicCard discard = FindDiscardBasedOnSupport(weakList);

                                        discards.Add(discard);
                                        lowestRankedCards.Remove(discard);
                                    }
                                    //There is only one weak card, so discard it.
                                    else
                                    {
                                        discards.Add(weakList[0]);
                                        lowestRankedCards.Remove(weakList[0]);
                                    }
                                }
                                else if (singletons.Count == 1)
                                {
                                    discards.Add(singletons[0]);
                                    lowestRankedCards.Remove(singletons[0]);
                                }
                                //All potential discards in the hand are members of other card sets.
                                else
                                {
                                    //Cull the potential discards to only equal the ones of lowest expected attack.
                                    List<BasicCard> weakList = OnlyExtremeExpectedAttack(lowestRankedCards, true);

                                    //if we have more than one weak card, check additional criteria.
                                    if (weakList.Count > 1)
                                    {
                                        //try to discard a card with a support effect and with the highest support value
                                        //Show a preference to discard cards with a support skill.
                                        BasicCard discard = FindDiscardBasedOnSupport(weakList);

                                        discards.Add(discard);
                                        lowestRankedCards.Remove(discard);
                                    }
                                    //There is only one weak card, so discard it.
                                    else
                                    {
                                        discards.Add(weakList[0]);
                                        lowestRankedCards.Remove(weakList[0]);
                                    }
                                }
                            }
                            //Only one card in the lowest ranked set to discard.
                            else
                            {
                                discards.Add(lowestRankedCards[0]);
                                lowestRankedCards.RemoveAt(0);
                            }
                            break;
                    }

                    //Reset lowestRankedCards if we run out during this for loop.
                    //Increase the rank being considered by 1 and repull our list.
                    while (lowestRankedCards.Count == 0)
                    {
                        lowestRank++;

                        lowestRankedCards = listToChooseFrom.FindAll(x => x.aiRanking == lowestRank);
                    }
                }

                CardManager.DiscardCardsFromHand(discards);
            }
            
        }
    }

    //A helper method that streamlines the search in a list for a card to discard by finding one with support skills
    //and the maximum support value.  This means that the card will be most useful shuffled back into the deck.
    protected BasicCard FindDiscardBasedOnSupport(List<BasicCard> listToCheck)
    {
        //try to discard a card with a support effect and with the highest support value
        //Show a preference to discard MC cards with a support skill.
        List<BasicCard> supportSkillList = listToCheck.FindAll(card => card.SkillTypes[(int)CipherData.SkillTypeEnum.Support]);
        List<BasicCard> highList = new List<BasicCard>(listToCheck.Count);

        //Cull the potential discards to only the ones of highest support.
        if (supportSkillList.Count > 1)
        {
            highList = OnlyExtremeSupportValues(supportSkillList, false);
        }
        //There is only one card with support skills so return it.
        else if (supportSkillList.Count == 1)
        {
            return supportSkillList[0];
        }
        //No cards under consideration have a support skill.
        else
        {
            highList = OnlyExtremeSupportValues(listToCheck, false);
        }

        //Randomly choose one of the remaining cards to return.
        return highList[Random.Range(0, highList.Count)];
    }


    //This method helps the AI determine its attack target.
    //NOTE: There is room for expansion here to find an ideal target for this card's attack
    //based on what the card provided and the card's expected attack.
    public override void ChooseAttackTarget(BasicCard aggressor, int expectedAttack, List<BasicCard> targets)
    {
        /*
         * old attack logic
         * 
        //If the attacker can attack the MC, do so, otherwise choose a random target.
        
        //If the card can, try to attack the MC.
        if (targets.Contains(Opponent.CardManager.MCCard))
        {
            GameManager.instance.StartBattle(aggressor, Opponent.CardManager.MCCard);
        }
        else    //Otherwise choose a random target
        {
            GameManager.instance.StartBattle(aggressor, targets[Random.Range(0, targets.Count)]);
        }
        */

        //Confirm if we even have a choice in attack targets
        if (targets.Count > 1)
        {
            //filter through the targets to find those that are realistically possible to beat (no more than 10 more than the expected attack).
            List<BasicCard> goodTargets = targets.FindAll(card => expectedAttack - card.CurrentAttackValue >= -10);

            //Confirm we have some realistic targets
            if (goodTargets.Count > 1)
            {
                //Filter the good targets once more to find those that are close to this units expected attack.
                List<BasicCard> equalTargets = goodTargets.FindAll(card => expectedAttack - card.CurrentAttackValue <= 10);

                //Confirm we have more than one equal target.
                if (equalTargets.Count > 1)
                {
                    //Check if the MC is an equal or good target and if so, make us more likely to attack it by adding it to the list again.
                    if (equalTargets.Contains(Opponent.CardManager.MCCard) || goodTargets.Contains(Opponent.CardManager.MCCard))
                        equalTargets.Add(Opponent.CardManager.MCCard);

                    //Randomly choose a card to attack.
                    GameManager.instance.StartBattle(aggressor, equalTargets[Random.Range(0, equalTargets.Count)]);
                }
                //There is only one equal target.
                else if (equalTargets.Count == 1)
                {
                    //Check if the MC is a good target (and that it's not the only equal target) and if so, consider attacking the MC.
                    if (goodTargets.Contains(Opponent.CardManager.MCCard) && !equalTargets.Contains(Opponent.CardManager.MCCard))
                    {
                        equalTargets.Add(Opponent.CardManager.MCCard);
                        GameManager.instance.StartBattle(aggressor, equalTargets[Random.Range(0, equalTargets.Count)]);
                    }
                    else //Just attack the equal target.   
                        GameManager.instance.StartBattle(aggressor, equalTargets[0]);
                }
                //There are no equal targets, so let's smash one of the good ones.  :)
                else
                {
                    //Check if the MC is a good target and if so, make us more likely to attack it by adding it to the list again.
                    if (goodTargets.Contains(Opponent.CardManager.MCCard))
                        goodTargets.Add(Opponent.CardManager.MCCard);

                    GameManager.instance.StartBattle(aggressor, goodTargets[Random.Range(0, goodTargets.Count)]);
                }
            }
            //There is only one good target so attack it.
            else if (goodTargets.Count == 1)
            {
                GameManager.instance.StartBattle(aggressor, goodTargets[0]);
            }
            //There are no good targets.  
            else
            {
                //Find the targets with the lowest attack.
                int smallestAttackDifference = 100;

                foreach (BasicCard card in targets)
                {
                    int n = card.CurrentAttackValue - expectedAttack;

                    if (n < smallestAttackDifference)
                    {
                        smallestAttackDifference = n;
                    }
                }

                List<BasicCard> bestTargets = targets.FindAll(card => card.CurrentAttackValue - expectedAttack == smallestAttackDifference);

                //Randomly attack one of the targets with the lowest attack.
                GameManager.instance.StartBattle(aggressor, bestTargets[Random.Range(0, bestTargets.Count)]);
            }
        }
        else //There is only one enemy to target, so attack that enemy.
        {
            GameManager.instance.StartBattle(aggressor, targets[0]);
        }

    }

    //This method helps the AI determine if it should crit and discard the required card.
    public override void DecideToCrit()
    {
        //Check if a crit is necessary for a kill
        if (GameManager.instance.CurrentAttacker.TotalAttack < GameManager.instance.CurrentDefender.TotalAttack 
            && (2 * GameManager.instance.CurrentAttacker.TotalAttack) >= GameManager.instance.CurrentDefender.TotalAttack)
        {
            //Find all cards to discard.
            List<BasicCard> possibleDiscards = CardManager.Hand.FindAll(x => x.CompareNames(GameManager.instance.CurrentAttacker));

            //Find the cards with a deployment cost that is the same or lower than the current attacker.
            List<BasicCard> betterChoice = possibleDiscards.FindAll(card => card.DeploymentCost <= GameManager.instance.CurrentAttacker.DeploymentCost);

            //Check how many orbs are left to determine how aggressive the AI should be.
            int m = CardManager.Orbs.Count;
            int n = Opponent.CardManager.Orbs.Count;

            //Check if the attacker is the MC.
            if (GameManager.instance.CurrentAttacker == CardManager.MCCard)
            {
                //Only have the MC crit if the the defender is the Opponent MC.
                if (GameManager.instance.CurrentDefender == Opponent.CardManager.MCCard)
                {
                    //Check if the MC is in the target promotion to avoid criting when we need to play defensive.
                    if (CardManager.MCCard.CardNumber.Equals(targetPromotion))
                    {
                        //Check both that there are cards at or below the MC's current Deployment cost to use to crit, and that there is 
                        //still at least one saved card in hand to use as a dodge OR that we have at least 3 MC cards in hand.
                        //Otherwise, we probably shouldn't do a critical hit.
                        if (possibleDiscards.Count >= 3 || (betterChoice.Count > 0 && possibleDiscards.Count >= 2))
                        {
                            //Only crit if the Opponent is at 0 or 1 orbs.
                            if (n <= 1)
                            {
                                //The AI is more likely to crit if they are already at an advantage in orbs.
                                if (m > n)
                                {
                                    //Give a 66% chance to crit if the opponent has 0 orbs or a 33% chance when they have 1 orb.
                                    if (Random.Range(0, 3) >= n + 1)
                                    {
                                        //Randomly choose a card to use for the critical.
                                        if (betterChoice.Count > 0)
                                            ActivateCriticalHit(betterChoice[Random.Range(0, betterChoice.Count)]);
                                        else
                                            ActivateCriticalHit(possibleDiscards[Random.Range(0, possibleDiscards.Count)]);

                                    }
                                }
                                //The two players have the same number of orbs, so don't be as aggressive.
                                else if (m == n)
                                {
                                    //Give a 50% chance to crit if the opponent has 0 orbs or a 25% chance when they have 1 orb.
                                    if (Random.Range(0, 4) >= n + 2)
                                    {
                                        //Randomly choose a card to use for the critical.
                                        if (betterChoice.Count > 0)
                                            ActivateCriticalHit(betterChoice[Random.Range(0, betterChoice.Count)]);
                                        else
                                            ActivateCriticalHit(possibleDiscards[Random.Range(0, possibleDiscards.Count)]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else    //The attacker is not the MC.
            {
                //If the defender is the Opponent MC, then we're more likely to crit.
                if (GameManager.instance.CurrentDefender == Opponent.CardManager.MCCard)
                {
                    //Check if there are cards at or below the attacker's current Deployment cost to use to crit.
                    if (betterChoice.Count > 0)
                    {
                        //Increase the chance of a critical hit by 20% for each orb the Opponent is missing.
                        if (Random.Range(0, 5) >= n)
                        {
                            //Randomly choose a card to use for the critical.
                            ActivateCriticalHit(betterChoice[Random.Range(0, betterChoice.Count)]);
                        }
                    }
                    else //The potential critical fodder is only promotions or level ups.
                    {
                        //Add a broad 50% chance of not proceeding.
                        if (Random.Range(0, 2) == 1)
                        {
                            //Increase the chance of a critical hit by 20% for each orb the Opponent is missing.
                            if (Random.Range(0, 5) >= n)
                            {
                                //Randomly choose a card to use for the critical.
                                ActivateCriticalHit(possibleDiscards[Random.Range(0, possibleDiscards.Count)]);
                            }
                        }
                    }
                }
                else    //the defender is not the Opponent's MC.
                {
                    //Check if there are cards at or below the attacker's current Deployment cost to use to crit 
                    //or multiple fodder in the hand. If not, then don't crit.
                    if (betterChoice.Count > 0 || possibleDiscards.Count >= 2)
                    {
                        //Check the power difference (based on deploy cost) between the cards to determine how likey we should be to crit.
                        int a = GameManager.instance.CurrentAttacker.DeploymentCost;
                        int b = GameManager.instance.CurrentDefender.DeploymentCost;

                        //25% to crit when fighting same deploy cost and then +25% for each difference in power going forward.
                        if (Random.Range(0,4) <= a - b)
                        {
                            //Randomly choose a card to use for the critical.
                            if (betterChoice.Count > 0)
                                ActivateCriticalHit(betterChoice[Random.Range(0, betterChoice.Count)]);
                            else
                                ActivateCriticalHit(possibleDiscards[Random.Range(0, possibleDiscards.Count)]);
                        }
                    }
                }
            }
        }

        //No matter what happens above, we move to the Evade Choice method next.
        GameManager.instance.EvadeChoice();
    }

    //A helper method which actually activates the critical hit for the AI player.
    protected void ActivateCriticalHit(BasicCard cardToDiscard)
    {
        GameManager.instance.criticalHit = true;
        CardReader.instance.UpdateGameLog(PlayerName + "'s " + GameManager.instance.CurrentAttacker.CharName + " activates a critical hit!");

        List<BasicCard> discards = new List<BasicCard>(1);
        discards.Add(cardToDiscard);

        CardManager.DiscardCardsFromHand(discards);
        GameManager.instance.DisplayAttackValues();
    }

    //This methods let the AI decide whether to evade an incoming attack.
    //It is only called when the defender if going to be destroyed and we have cards to evade in the hand.
    public override void DecideToEvade()
    {
        //Find all cards to discard.
        List<BasicCard> possibleDiscards = CardManager.Hand.FindAll(x => x.CompareNames(GameManager.instance.CurrentDefender));

        //Find the cards with a deployment cost that is the same or lower than the current defender.
        List<BasicCard> betterChoice = possibleDiscards.FindAll(card => card.DeploymentCost <= GameManager.instance.CurrentDefender.DeploymentCost);

        //We may have to evade if the defender is the MC.
        if (GameManager.instance.CurrentDefender == CardManager.MCCard)
        {
            //Check how many orbs are left to determine how defensive the AI should be.
            int m = CardManager.Orbs.Count;

            //If we have zero orbs, we must evade to avoid losing.
            if (m == 0)
            {
                //Randomly choose a card to use for the evasion.
                if (betterChoice.Count > 0)
                    ActivateEvasion(betterChoice[Random.Range(0, betterChoice.Count)]);
                else
                    ActivateEvasion(possibleDiscards[Random.Range(0, possibleDiscards.Count)]);
            }
            //We know we have more than zero orbs.  Check if this is a multi-orb hit.
            else if (m >= 2 && GameManager.instance.numOrbsToBreak > 1)
            {
                //Check what kinds of cards we have available to use to evade.
                //If we have cards at or below the current MC's Deployment cost, then dodge the empowered hit.
                if (betterChoice.Count > 0)
                    ActivateEvasion(betterChoice[Random.Range(0, betterChoice.Count)]);
                //We only have potential level ups and promotions to evade with.
                else
                {
                    //Check if the MC is in the target promotion.
                    if (CardManager.MCCard.CardNumber.Equals(targetPromotion))
                    {
                        //Check how many potential discards we have and dodge if we have at least 2.  Otherwise, don't evade.
                        if (possibleDiscards.Count >= 2)
                            ActivateEvasion(possibleDiscards[Random.Range(0, possibleDiscards.Count)]);
                        else
                            GameManager.instance.BattleCalculation();
                    }
                    else    //The MC is not yet in the target promotion.  We probably want those extra orbs, so we won't dodge.
                    {
                        GameManager.instance.BattleCalculation();
                    }
                }
            }
            else    //We have orbs to spare.  Don't evade.
            {
                GameManager.instance.BattleCalculation();
            }
        }
        else    //The opponent is attacking something besides the MC.
        {
            //Check the Deploy cost of the defender and be more likely to save higher cost cards.
            //20%% chance to evade if defender is cost 1, + 20% for each extra deploy cost.
            if (Random.Range(1, 6) <= GameManager.instance.CurrentDefender.DeploymentCost)
            {
                //Check if we're discarding a card of the same or lower deployment cost.
                if (betterChoice.Count > 0)
                {
                    //Randomly choose a card to use for the evasion.
                    ActivateEvasion(betterChoice[Random.Range(0, betterChoice.Count)]);
                }
                //We have to use a potential promotion or level up to evade.  Add a 50% chance of just keeping the card in the hand.    
                else if (Random.Range(0, 2) == 1)
                {
                    GameManager.instance.BattleCalculation();
                }
                else
                    ActivateEvasion(possibleDiscards[Random.Range(0, possibleDiscards.Count)]);
            }
            else    //The card to be destroyed isn't worth saving, so don't evade.
            {
                GameManager.instance.BattleCalculation();
            }
        }
    }

    //A helper method which actually activates the evasion for the AI player.
    private void ActivateEvasion(BasicCard cardToDiscard)
    {
        CardReader.instance.UpdateGameLog(PlayerName + "'s " + GameManager.instance.CurrentDefender.CharName + " activates a god-speed evasion!");

        List<BasicCard> discards = new List<BasicCard>(1);
        discards.Add(cardToDiscard);

        CardManager.DiscardCardsFromHand(discards);
        //Opponent.DefenderEvaded();        //I don't think this call is actually necessary at this point yet.
        GameManager.instance.EndBattle();
    }
}
