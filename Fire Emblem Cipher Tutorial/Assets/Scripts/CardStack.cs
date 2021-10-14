using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardStack : MonoBehaviour {

    private Stack<BasicCard> cardsInPile = new Stack<BasicCard>(10);        //all cards in the stack save the top one should be disabled
    private bool classChanged = false;

    public BasicCard TopCard
    {
        get
        {
            return cardsInPile.Peek();
        }
    }

    public int LvSx
    {
        get
        {
            return cardsInPile.Count;
        }
    }

    public bool ClassChanged { get { return classChanged; } }

    //Setup methods
    public void Setup(BasicCard topCard)
    {
        //a newly deployed card cannot be class Changed.
        classChanged = false;
        
        //ensures that the card lands face up.
        if (!topCard.FaceUp)
        {
            topCard.FlipFaceUp();
        }

        cardsInPile.Push(topCard);

        //sets all the currently deployed card's skills as active and registers them to the appropriate events.
        topCard.ActivateFieldSkills();

        //deals with positioning
        //cast the transform as a RectTransform to access more sophisticated methods.
        //Only necessary when there isn't an active Layout Group
        RectTransform cardTransform = topCard.gameObject.transform as RectTransform;
        cardTransform.SetParent(null, false);
        //places the card on the field in the correct location.
        cardTransform.SetParent(transform, false);
        cardTransform.anchorMax = new Vector2(0.5f, 0.5f);
        cardTransform.anchorMin = new Vector2(0.5f, 0.5f);
        cardTransform.anchoredPosition = Vector2.zero;
        //cardTransform.anchoredPosition = new Vector2(0.5f,0.5f);

        //Debug.Log(card.ToString() + " placed on the Front Line at " + card.gameObject.transform.position.ToString());
    }

    //NOT TESTED!
    //Unnecessary?
    /*
    public void Setup(List<BasicCard> cardList)
    {
        Debug.Log(cardList.Count + " cards will be used to crearte a new CardStack.");

        for (int i = cardList.Count; i < 1; i--)
        {
            BasicCard cardToAdd = cardList[i - 1];

            cardsInPile.Push(cardToAdd);
            Debug.Log(cardToAdd.ToString() + " has been added to the CardStack.");

            //ensures that the card lands face up.
            if (!cardToAdd.FaceUp)
            {
                cardToAdd.FlipFaceUp();
            }

            //deals with positioning
            cardToAdd.gameObject.transform.SetParent(null, false);
            cardToAdd.gameObject.transform.SetParent(transform, false);

            cardToAdd.gameObject.SetActive(false);
        }

        cardsInPile.Peek().gameObject.SetActive(true);
    }
    */

    
    public void AddCardToStack(BasicCard card, bool classChange)
    {
        //disable the current top card
        TopCard.DeactivateFieldSkills();

        //disabling an object likely resets it's Animator (flip and tapped visual status).
        //Consider disabling it's renderer instead to make it invisible
        //NEED TO ADD: Ensure that the card to be disabled is faceUp and untapped.

        TopCard.gameObject.SetActive(false);

        //add the new card to the stack and activates its skills
        cardsInPile.Push(card);
        TopCard.ActivateFieldSkills();

        //updates the classChanged bool depending on how the new card is played.
        classChanged = classChange;

        //ensures that the card lands face up.
        if (!card.FaceUp)
        {
            card.FlipFaceUp();
        }

        //deals with positioning
        //cast the transform as a RectTransform to access more sophisticated methods.
        //Only necessary when there isn't an active Layout Group
        RectTransform cardTransform = card.gameObject.transform as RectTransform;
        cardTransform.SetParent(null, false);
        //places the card on the field in the correct location.
        cardTransform.SetParent(transform, false);
        cardTransform.anchorMax = new Vector2(0.5f, 0.5f);
        cardTransform.anchorMin = new Vector2(0.5f, 0.5f);
        cardTransform.anchoredPosition = Vector2.zero;
        //cardTransform.anchoredPosition = new Vector2(0.5f,0.5f);
    }

    //NOT TESTED YET
    /*
    public BasicCard RemoveTopCard()
    {
        BasicCard removedCard = cardsInPile.Pop();

        //turn the new top card back on.
        cardsInPile.Peek().gameObject.SetActive(true);

        return removedCard;
    }
    */

    public List<BasicCard> EmptyTheStack()
    {
        //Deactivate the skills of the card being removed from the field.
        TopCard.DeactivateFieldSkills();
        classChanged = false;

        List<BasicCard> stackedCards = new List<BasicCard>(cardsInPile.Count);
        int size = cardsInPile.Count;

        //Debug.Log("Creating a List of " + size + " cards.");

        //Having the stack object and children deactivated while the cards are still children may be reseting the card's animators.
        //Try unparenting before deactivating the stack.
        for (int i = 0; i < size; i++)
        {
            cardsInPile.Peek().gameObject.SetActive(true);
            cardsInPile.Peek().gameObject.transform.SetParent(null, true);
            stackedCards.Add(cardsInPile.Pop());
            //Debug.Log("Gone through the popping loop.");
        }

        //Debug.Log("Returning a List of " + stackedCards.Count + " cards.  Note that the CardStack has " + cardsInPile.Count + " cards in it.");

        return stackedCards;
    }
}
