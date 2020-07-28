using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class CardToggle : MonoBehaviour {

    public Toggle toggleComponent;
    public Image toggleImage;
    public Image activatedImage;

    private CardPickerWindow cardPickerWindow;
    private BasicCard correspondingCard;
   

    
    void Start()
    {
        cardPickerWindow = CardPickerWindow.Instance();
        toggleComponent.onValueChanged.AddListener(HandleClick);
    }

    public void Setup(BasicCard card)
    {
        correspondingCard = card;

        GameObject cardObject = card.gameObject;
        SpriteRenderer[] cardSprites = cardObject.GetComponentsInChildren<SpriteRenderer>();
        toggleImage.sprite = cardSprites[cardSprites.Length - 1].sprite;
        activatedImage.sprite = cardSprites[cardSprites.Length - 1].sprite;
    }
	
    public void HandleClick(bool toggleState)
    {
        //Display the card in the CardReader
        CardReader.instance.DisplayCard(correspondingCard.gameObject);
        
        //If the toggle was clicked on, add the corresponding card to the return list in the card picker window.
        if (toggleState)
        {
            cardPickerWindow.AddCard(correspondingCard);
        }
        //Else, if the toggle was clicked off, remove the corresponding card from the return list.
        else
        {
            cardPickerWindow.RemoveCard(correspondingCard);
        }
    }
    
}
