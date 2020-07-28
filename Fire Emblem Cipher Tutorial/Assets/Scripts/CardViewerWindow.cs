using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class CardViewerWindow : MonoBehaviour
{
    public Text headerText;
    public Transform contentPanel;
    public SimpleObjectPool cardViewObjectPool;
    public GameObject cardViewerObject;

    private static CardViewerWindow cardviewer;

    public static CardViewerWindow Instance()
    {
        if (!cardviewer)
        {
            cardviewer = FindObjectOfType(typeof(CardViewerWindow)) as CardViewerWindow;
            if (!cardviewer)
                Debug.LogError("There needs to be one active CardViewerWindow script on a GameObject in your scene.");
        }

        return cardviewer;
    }

    public void ViewCards(CardViewerDetails details)
    {
        cardViewerObject.SetActive(true);

        headerText.text = details.locationText;

        Debug.Log("Displaying " + details.cardsToDisplay.Count + " cards in the Card Viewer Window.");

        //populate the display with CardView Prefabs
        for (int i = 0; i < details.cardsToDisplay.Count; i++)
        {
            GameObject newView = cardViewObjectPool.GetObject();
            newView.transform.SetParent(contentPanel);

            CardView2 view = newView.GetComponent<CardView2>();
            view.Setup(details.cardsToDisplay[i]);
        }

    }

    public void ClosePanel()
    {
        Debug.Log("Closing Panel now. " + contentPanel.childCount + " left to remove.");

        //returns all objects to the objectpool after use.
        while (contentPanel.childCount > 0)
        {
            GameObject toRemove = contentPanel.transform.GetChild(0).gameObject;
            cardViewObjectPool.ReturnObject(toRemove);
        }

        cardViewerObject.SetActive(false);
    }
}

public class CardViewerDetails
{
    public List<BasicCard> cardsToDisplay;
    public string locationText;                     //includes the player and the field location of the cards in the picker.
}
