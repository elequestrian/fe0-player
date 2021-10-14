using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerEventHandler
{
    private List<BasicCard> listenerList = new List<BasicCard>();
    private BasicCard triggeringCard;

    //A public-facing property to return the card which triggered this effect. 
    public BasicCard TriggeringCard { get { return triggeringCard; } }

    //Adds a card to the listenerList
    public void AddListener(BasicCard card)
    {
        listenerList.Add(card);
        card.triggerResolved = false;
    }

    //Removes a card from the listenerList
    public bool RemoveListener(BasicCard card)
    {
        if (listenerList.Contains(card))
        {
            listenerList.Remove(card);
            card.triggerResolved = false;
            return true;
        }
        else
        {
            return false;
        }

    }

    //Checks to see if a trigger event has actived any listening card's abilities 
    public void CheckTrigger(BasicCard card)
    {
        //only proceed if there are some listeners
        if (listenerList.Count > 0)
        {
            //save the triggering card for future reference.
            triggeringCard = card;

            //create a list of triggered cards
            List<BasicCard> activeCards = new List<BasicCard>(listenerList.Count);

            //check the Trigger Skill conditions on each card and if met add that card to the list of "triggered"/active cards.
            foreach (BasicCard listener in listenerList)
            {
                if (listener.CheckTriggerSkillCondition(triggeringCard))
                {
                    activeCards.Add(listener);
                    listener.triggerResolved = false;
                }
            }

            //if only one active card, call its Trigger skill, else have the player choose which card's ability to activate first.
            if (activeCards.Count == 1)
            {
                CallTriggerSkill(activeCards);
            }
            else if (activeCards.Count > 1)
            {
                GameManager.instance.turnAgent.ChooseAmongTriggeredCards(this, activeCards);
            }

        }
    }

    //This is the method called by the Card Picker which activates the first trigger ability.
    public void CallTriggerSkill(List<BasicCard> cardList)
    {
        //be sure the list only has one card as intended.
        if (cardList.Count == 1)
        {
            cardList[0].triggerResolved = true;
            cardList[0].DM.ActivateTriggerSkill(cardList[0], triggeringCard);
        }
        else
        {
            Debug.LogError("The CardPicker gave TriggerEventHandler a list with " + cardList.Count + " cards instead of only 1 card." +
                "  Investigate!");
        }
    }

    //recheck the trigger conditions in case the board state changed after the last ability.  Repeat until all cards are resolved.
    public void RecheckTrigger()
    {
        //only proceed if there are some listeners
        if (listenerList.Count > 0)
        {
            //create a list of triggered cards
            List<BasicCard> activeCards = new List<BasicCard>(listenerList.Count);

            //check the Trigger Skill conditions on each card and if met add that card to the list of "triggered"/active cards
            //unless the card has already been resolved.
            foreach (BasicCard listener in listenerList)
            {
                if (listener.CheckTriggerSkillCondition(triggeringCard) && !listener.triggerResolved)
                {
                    activeCards.Add(listener);
                }
            }

            //if only one active card, call its Trigger skill,
            //else if there is more than one triggered card have the player choose which card's ability to activate next,
            if (activeCards.Count == 1)
            {
                CallTriggerSkill(activeCards);
            }
            else if (activeCards.Count > 1)
            {
                GameManager.instance.turnAgent.ChooseAmongTriggeredCards(this, activeCards);
            }
            else    //no active cards.
            {
                //reset the resolution bool on each of the listeners for the next triggering event.
                foreach (BasicCard listener in listenerList)
                {
                    listener.triggerResolved = false;
                }
            }

        }
    }

    //The below method allows the TriggerEventHandler to work with skills which edit lists that are provided.
    //These skills need to input a list, edit it, then output the list for the next card to revise and edit it.
    //This method runs that process.  Note this is not designed to allow choice in the process.
    public List<BasicCard> MakeListenersEditList(BasicCard requestingCard, List<BasicCard> listToEdit)
    {
        //only proceed if there are some listeners
        if (listenerList.Count > 0)
        {
            //loop through the listening cards to have them adjust the list in question.
            foreach (BasicCard listener in listenerList)
            {
                listToEdit = listener.EditList(requestingCard, listToEdit);
            }
        }

        return listToEdit;
    }
}
