using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class RetreatView : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    public GameObject viewButtonObject;
    public Image background;

    private CardManager player;
    private DecisionMaker agent;

    //Sets up the reference to the player for the RetreatView script.  Must be done dynamically (for now) because the GameManager creates
    //the DecisionMaker for each player.
    public void Setup(CardManager cardManager, DecisionMaker dm)
    {
        player = cardManager;
        agent = dm;
    }
    
    //Detect if the Cursor starts to pass over the GameObject to display the button and background.
    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        //Debug.Log("Enter");
        
        //only display the button if there are cards in a player's retreat.
        if (player != null && player.Retreat.Count > 0)
        {
            viewButtonObject.SetActive(true);
            background.enabled = true;
        }
    }


    //Detect when Cursor leaves the GameObject to hide the button and background.
    public void OnPointerExit(PointerEventData pointerEventData)
    {
        //Debug.Log("Exit");
        viewButtonObject.SetActive(false);
        background.enabled = false;
    }


    //This was used to try to implement functionality to show the "top" facing card of the retreat.
    //However, due to a bug about how the cards in the retreat are displayed overall, this is disabled.
    public void OnPointerClick(PointerEventData data)
    {
        //Debug.Log("First Card");

        /*
        //Show the top card of the retreat (the one at the end of the list) when this area is clicked to mimic the card clicking functionality
        if (player != null && player.Retreat.Count > 0)
        {
            CardReader.instance.DisplayCard(player.Retreat[player.Retreat.Count - 1].gameObject);
        }
        */
    }

    public void OpenRetreatViewer()
    {
        //Debug.Log("Open Retreat!");

        //collects the information for the CardViewer
        CardViewerDetails details = new CardViewerDetails
        {
            cardsToDisplay = player.Retreat,
            locationText = agent.PlayerName + "'s Retreat",
        };

        CardViewerWindow.Instance().ViewCards(details);
    }
}
