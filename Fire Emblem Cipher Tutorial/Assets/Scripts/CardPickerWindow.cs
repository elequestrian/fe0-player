using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class CardPickerWindow : MonoBehaviour {

    public Text headerText;
    public Text instructionText;
    public Text errorText;
    public Transform contentPanel;
    public Button confirmButton;
    public SimpleObjectPool toggleObjectPool;
    public GameObject cardPickerObject;

    private static CardPickerWindow cardPicker;
    
    private int neededCards;
    private bool mayChooseFewer;
    private List<BasicCard> chosenCards = new List<BasicCard>();
    private MyCardListEvent actionToTake = new MyCardListEvent();

    public static CardPickerWindow Instance()
    {
        if (!cardPicker)
        {
            cardPicker = FindObjectOfType(typeof(CardPickerWindow)) as CardPickerWindow;
            if (!cardPicker)
                Debug.LogError("There needs to be one active CardPickerWindow script on a GameObject in your scene.");
        }

        return cardPicker;
    }


    public void ChooseCards(CardPickerDetails details)
    {
        //Debug.Log("Displaying " + details.cardsToDisplay.Count + " cards in the Card Picker Window.");

        cardPickerObject.SetActive(true);

        instructionText.text = details.instructionText;
        headerText.text = details.locationText;

        mayChooseFewer = details.mayChooseLess;

        if (details.numberOfCardsToPick > details.cardsToDisplay.Count && !mayChooseFewer)
        {
            neededCards = details.cardsToDisplay.Count;
            Debug.LogError("WARNING! Only " + neededCards + " may be chosen!");
            errorText.text = "WARNING!  Only " + neededCards + " may be chosen!";
            errorText.gameObject.SetActive(true);
        }
        else
        {
            neededCards = details.numberOfCardsToPick;
        }

        Debug.Log("Please choose " + neededCards);

        //Checks to be sure there is something for the CardPicker to do after you pick cards.
        if (details.effectToActivate == null)
        {
            Debug.LogError("WARNING! No event given to the CardPicker!  What do you want it to do?");
            errorText.text += "WARNING! This action will have no effect!";
            /*
            if (actionToTake == null)
            {
                actionToTake = new MyCardListEvent();
            }
            */
        }
        else
        {
            //actionToTake.RemoveAllListeners();
            actionToTake = details.effectToActivate;
        }

        for (int i = 0; i < details.cardsToDisplay.Count; i++)
        {
            GameObject newToggle = toggleObjectPool.GetObject();
            newToggle.transform.SetParent(contentPanel);

            CardToggle toggle = newToggle.GetComponent<CardToggle>();
            toggle.toggleComponent.isOn = false;                        //resets all CardToggles to off.
            toggle.Setup(details.cardsToDisplay[i]);
        }

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirm);
        

    }

    public void AddCard(BasicCard cardToAdd)
    {
        chosenCards.Add(cardToAdd);
        //Debug.Log(cardToAdd.ToString() + " was added to the return list.");
        //Debug.Log("The return list has " + chosenCards.Count + " cards in it.");
    }

    public void RemoveCard(BasicCard cardToRemove)
    {
        if (chosenCards.Remove(cardToRemove))
        {
            //Debug.Log(cardToRemove.ToString() + " was removed from the return list.");
            //Debug.Log("The return list has " + chosenCards.Count + " cards in it.");
        }
        else
        {
            //Debug.LogWarning(cardToRemove.ToString() + " was NOT removed from the return list.  Be sure the item was in the list in the first place!");
            //Debug.Log("The return list still has " + chosenCards.Count + " cards in it.");
        }
    }

    private void OnConfirm()
    {
        errorText.gameObject.SetActive(false);

        if (chosenCards.Count == neededCards)
        {
            ClosePanel();
            //GameManager.instance.ActivateEffect(chosenCards);
            actionToTake.Invoke(chosenCards);
            chosenCards.Clear();
        }
        else if (chosenCards.Count < neededCards && mayChooseFewer)
        {
            ClosePanel();
            //GameManager.instance.ActivateEffect(chosenCards);
            actionToTake.Invoke(chosenCards);
            chosenCards.Clear();
        }
        else if (chosenCards.Count < neededCards && !mayChooseFewer)
        {
            int n = neededCards - chosenCards.Count;

            //Error message: Please choose needed - Count more cards.

            if (n != 1)
            {
                errorText.text = "Please choose " + n + " more cards.";
                errorText.gameObject.SetActive(true);
                Debug.Log("Please choose " + n + " more cards.");
            }
            else    //n == 1
            {
                errorText.text = "Please choose " + n + " more card.";
                errorText.gameObject.SetActive(true);
                Debug.Log("Please choose " + n + " more card.");
            }
            
        } else          //Count > neededCards
        {
            int m = chosenCards.Count - neededCards;

            //Error: Please deselect count - needed cards to continue.

            if (m != 1)
            {
                errorText.text = "Please deselect " + m + " cards to continue.";
                errorText.gameObject.SetActive(true);
                Debug.Log("Please deselect " + m + " cards to continue.");
            }
            else    //m == 1
            {
                errorText.text = "Please deselect " + m + " card to continue.";
                errorText.gameObject.SetActive(true);
                Debug.Log("Please deselect " + m + " card to continue.");
            }
            
        }

        
    }

    private void ClosePanel()
    {
        //Debug.Log("Closing Panel now.  " + contentPanel.childCount + " left to remove.");


        while (contentPanel.childCount > 0)
        {
            GameObject toRemove = contentPanel.transform.GetChild(0).gameObject;
            toggleObjectPool.ReturnObject(toRemove);
        }

        cardPickerObject.SetActive(false);
    }

   
}

[System.Serializable]
public class MyCardListEvent : UnityEvent<List<BasicCard>>
{
}

public class CardPickerDetails
{
    public List<BasicCard> cardsToDisplay;
    public string locationText;                     //includes the player and the field location of the cards in the picker.
    public string instructionText;                  //Directions on how many cards to pick, what effect was activated, and which card it was on.
    public int numberOfCardsToPick;
    public bool mayChooseLess;                      //Allows for the possibility of choosing fewer cards than required.
    public MyCardListEvent effectToActivate;        //The action that should be taken using the given cards.  Requires an input of a List<BasicCard>.
}