using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class CardView2 : MonoBehaviour
{
    public Toggle toggleComponent;
    public Image toggleImage;
    public Image activatedImage;

    private CardViewerWindow cardViewerWindow;
    private BasicCard correspondingCard;

    void Start()
    {
        cardViewerWindow = CardViewerWindow.Instance();
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
    }
}
