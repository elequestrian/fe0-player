using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayoutManager : MonoBehaviour {

    //public static LayoutManager instance = null;

    public Transform deckLocation;
    public Transform retreatLocation;
    public Transform supportLocation;
    public Transform orbLocation;
    public Transform bondLocation;
    public Transform frontLineLocation;
    public Transform backLineLocation;
    public Transform handLocation;

    public GameObject MCMarker;
    public SimpleObjectPool cardStackObjectPool;

    /*
     * I'm removing this static instance to make the Layout Manager local.
     * This way it can do the same functions for each side of the field.
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
    */
    

    //puts a card in the deck zone on the field
    public void PlaceInDeck (BasicCard card)
    {
        //cast the transform as a RectTransform to access more sophisticated methods.
        //Only necessary when there isn't an active Layout Group.
        //NOTE that the Layout groups also change the pivots of their children, but setting deckLocation to have a width and height of 0 seems to have solved that problem.
        //If in doubt, I can change the pivot in this calculation too.  Or just make these locations their own layout group...
        RectTransform cardTransform = card.gameObject.transform as RectTransform;
        cardTransform.SetParent(null, false);
        //places the card on the field in the correct location.
        cardTransform.SetParent(deckLocation, false);
        cardTransform.anchoredPosition = Vector2.zero;
        //Debug.Log(card.ToString() + " placed in the Deck Area at " + cardTransform.position.ToString());

        //ensures that the card remains face down.
        if (card.FaceUp)
        {
            card.FlipFaceDown();
        }
        //ensures that the card remains untapped.
        if (card.Tapped)
        {
            card.Untap();
        }
    }

    //puts a card in the retreat area on the field
    public void PlaceInRetreat(BasicCard card)
    {
        //cast the transform as a RectTransform to access more sophisticated methods.
        //Only necessary when there isn't an active Layout Group
        RectTransform cardTransform = card.gameObject.transform as RectTransform;
        cardTransform.SetParent(null, false);
        //places the card on the field in the correct location.
        cardTransform.SetParent(retreatLocation, false);
        //sets the card's transform as the last in a local list so as to be rendered on top... actually has no effect.
        //cardTransform.SetAsLastSibling();
        cardTransform.anchoredPosition = Vector2.zero;
        //Debug.Log(card.ToString() + " placed in the Retreat Area at " + cardTransform.position.ToString());

        //ensures that the card remains face up.
        if (!card.FaceUp)
        {
            card.FlipFaceUp();
        }
        //ensures that the card remains untapped.
        if (card.Tapped)
        {
            card.Untap();
        }

        
    }

    //puts a card in the support area on the field
    public void PlaceInSupport(BasicCard card)
    {
        //cast the transform as a RectTransform to access more sophisticated methods.
        //Only necessary when there isn't an active Layout Group
        RectTransform cardTransform = card.gameObject.transform as RectTransform;
        cardTransform.SetParent(null, false);
        //places the card on the field in the correct location.
        cardTransform.SetParent(supportLocation, false);
        cardTransform.anchoredPosition = Vector2.zero;
        //Debug.Log(card.ToString() + " placed in the Support Area at " + cardTransform.position.ToString());

        //ensures that the card lands face up.
        if (!card.FaceUp)
        {
            card.FlipFaceUp();
        }
        //ensures that the card remains untapped.
        if (card.Tapped)
        {
            card.Untap();
        }
    }

    //puts a card in the orbs area on the field.  Uses a vertical layout group to accomplish this.
    public void PlaceInOrbs(BasicCard card)
    {
        card.gameObject.transform.SetParent(null, false);
        //places the card on the field in the correct location.
        card.gameObject.transform.SetParent(orbLocation, false);
        card.gameObject.transform.SetAsLastSibling();
        //Debug.Log(card.ToString() + " placed in the Orb Area at " + card.gameObject.transform.position.ToString());

        //ensures that the card lands face down.
        if (card.FaceUp)
        {
            card.FlipFaceDown();
        }
        //ensures that the card remains untapped.
        if (card.Tapped)
        {
            card.Untap();
        }
    }

    //puts a card in the Bond area either Face up or Face down as desired
    public void PlaceInBonds(BasicCard card, bool placeFaceUp)
    {
        card.gameObject.transform.SetParent(null, false);
        //places the card on the field in the correct location.
        card.gameObject.transform.SetParent(bondLocation, false);
        card.gameObject.transform.SetAsFirstSibling();
        //Debug.Log(card.ToString() + " placed in the Bond Area at " + card.gameObject.transform.position.ToString());

        //ensures that the card lands facing correctly and tapped.
        if (placeFaceUp)
        {
            if (!card.FaceUp)
            {
                card.FlipFaceUp();
            }
        }
        else //the card should be placed face down
        {
            if (card.FaceUp)
            {
                card.FlipFaceDown();
            }
        }

        if (!card.Tapped)
        {
            card.Tap();
        }
    }

    //puts a new card on the Front Line Area and returns the CardStack it created.
    public CardStack PlaceInFrontLine(BasicCard card)
    {
        GameObject pooledObject = cardStackObjectPool.GetObject();
        CardStack stack = pooledObject.GetComponent<CardStack>();
        stack.Setup(card);

        pooledObject.transform.SetParent(null, false);

        //places the card on the field in the correct location.
        pooledObject.transform.SetParent(frontLineLocation, false);
        //Debug.Log(card.ToString() + " placed on the Front Line at " + card.gameObject.transform.position.ToString());

        //ensures that the card remains face up.
        if (!card.FaceUp)
        {
            card.FlipFaceUp();
        }
        //ensures that the card remains untapped.
        if (card.Tapped)
        {
            card.Untap();
        }

        return stack;
    }

    //puts a card in the Back Line Area and returns the CardStack it created.
    public CardStack PlaceInBackLine(BasicCard card)
    {
        GameObject pooledObject = cardStackObjectPool.GetObject();
        CardStack stack = pooledObject.GetComponent<CardStack>();
        stack.Setup(card);

        pooledObject.transform.SetParent(null, false);
        pooledObject.transform.SetParent(backLineLocation, false);
        //Debug.Log(card.ToString() + " placed on the back Line at " + card.gameObject.transform.position.ToString());

        //ensures that the card remains face up.
        if (!card.FaceUp)
        {
            card.FlipFaceUp();
        }
        //ensures that the card remains untapped.
        if (card.Tapped)
        {
            card.Untap();
        }

        return stack;
    }

    public List<BasicCard> RemoveStackFromField (CardStack stack)
    {
        List<BasicCard> cardList = stack.EmptyTheStack();

        //Having the stack object deactivated while the cards are still children may be reseting the card's animators.
        //Children cards should be unparented before deactivating the stack.
        cardStackObjectPool.ReturnObject(stack.gameObject);

        Debug.Log("Breadcrumb trail 1");
        return cardList;
    }

    //moves a stack from the Front Line to the Back Line or vice versa
    public void MoveStack (CardStack stack, bool moveToFront)
    {
        if (moveToFront)
        {
            stack.gameObject.transform.SetParent(frontLineLocation, false);
        }
        else //stack should be moved to the back
        {
            stack.gameObject.transform.SetParent(backLineLocation, false);
        }
    }

    //puts a card in the Hand Area
    public void PlaceInHand(BasicCard card)
    {
        card.gameObject.transform.SetParent(null, false);
        //places the card in the hard in an appropriate location.
        card.gameObject.transform.SetParent(handLocation, false);
        //Debug.Log(card.ToString() + " placed in the Hand at " + card.gameObject.transform.position.ToString());

        //ensures that the card lands face up.
        if (!card.FaceUp)
        {
            card.FlipFaceUp();
        }
        //ensures that the card remains untapped.
        if (card.Tapped)
        {
            card.Untap();
        }

        //ensures cards that are drawn by the opponent stay hidden.
        GameManager.instance.ShowHand(GameManager.instance.turnPlayer);
    }

    //This method moves the new MC to the proper location and puts the MCMarker behind it.
    //Since this method is called at the start of the game, it places the MC facedown.
    public CardStack SetAsMC(BasicCard newMC)
    {
        CardStack MCStack = PlaceInFrontLine(newMC);
        Instantiate(MCMarker, MCStack.gameObject.transform);

        if (newMC.FaceUp)
        {
            newMC.FlipFaceDown();
        }

        return MCStack;
    }
}
