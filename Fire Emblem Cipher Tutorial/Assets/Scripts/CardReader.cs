using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.IO;

public class CardReader : MonoBehaviour {

    public static CardReader instance = null;

    public Sprite defaultImage;             //The default image to show with the Game Log
    public UnityEngine.UI.Image displayImage;             //The location we're placing the image.
    public Text displayText;               //The location of the text.
    public Scrollbar scrollbar;             //The scrollbar used to display informational text.
    private StringBuilder fullgameLog = new StringBuilder("Welcome to the Fire Emblem 0 Player!\n\n");

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

    public void DisplayCard(GameObject card)
    {
        //Debug.Log("Now displaying " + card.ToString());
        
        //Find the script with card information on the passed game object.
        BasicCard cardScript = card.GetComponent<BasicCard>();
        //Debug.Log("cardInfo now set to " + cardInfo.ToString());

        //Set the image of the panel to the face of the chosen card.
        //Note that GetComponentsInChildren includes the GameObject itself, necessitating this design. 
        //The lord marker is added as a child to the Lord card, necessitating this specificity.  :/
        //NOTE: This will probably need to be tweaked again when I implement class changing, but for now it takes care of the lord marker problem.  XD
        SpriteRenderer[] cardSprites = card.GetComponentsInChildren<SpriteRenderer>();
        displayImage.sprite = cardSprites[1].sprite;
        //Debug.Log("The displayed sprite is " + displayImage.sprite.ToString());

        //Format the card's information and store it to be displayed.
        displayText.text = TranslateCard(cardScript);

        //Set the scroll bar to the top of the card's information.
        MoveScrollBarToValue(1f);
    }

    //A public function which will display the Game Log when the game board is clicked.
    public void DisplayGameLog()
    {
        displayImage.sprite = defaultImage;
        displayText.text = fullgameLog.ToString();

        //Set the scroll bar to the bottom of the game log automatically once the system updates with the new text.
        StartCoroutine("MoveScrollBarToBottom");
    }

    //Writes the Gamelog to a file when the application is closed for error tracking.
    private void OnApplicationQuit()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "gamelog.txt");

        if (!File.Exists(filePath))
        {
            FileStream file = File.Create(filePath);
            file.Close();
            Debug.Log(filePath + " did not exist and was created.");
        }

        File.WriteAllText(filePath, fullgameLog.ToString());

        //Debug.Log("DeckList data was written to " + filePath);
    }

    private void MoveScrollBarToValue(float num)
    {
        scrollbar.value = num;
    }

    //A coroutine which updates the scroll view to the bottom after a full frame cycle.
    //This is due to the Unity Scrollview logic which takes multiple frames to update.
    private IEnumerator MoveScrollBarToBottom()
    {
        yield return null;
        yield return null;
        MoveScrollBarToValue(0f);
    }

    //A public function which adds a new message to the full Game Log for display.
    public void UpdateGameLog(string message)
    {
        fullgameLog.AppendLine(message);
        DisplayGameLog();
    }

    //This method formats a card's unique information to be displayed by the CardReader class.
    //This means the CardReader doesn't need to know what card it is looking at to display the proper information.
    //This method also compares the original card data and this card's local values for differences and colors them green.
    private string TranslateCard(BasicCard card)
    {
        StringBuilder cardInfo = new StringBuilder(1000);
        cardInfo.Append(card.CardNumber + " " + card.CharName + ": " + card.CharTitle +
            "\n" + card.ClassTitle + "/Cost: ");

        if (card.DeploymentCost == card.GetCardData.deploymentCost)
        {
            cardInfo.Append(card.DeploymentCost);
        }
        else
        {
            cardInfo.Append("<color=green>" + card.DeploymentCost + "</color>");
        }

        if (card.Promotable)
        {
            cardInfo.Append("(");
            if (card.PromotionCost == card.GetCardData.promotionCost)
            {
                cardInfo.Append(card.PromotionCost);
            }
            else
            {
                cardInfo.Append("<color=green>" + card.PromotionCost + "</color>");
            }
            cardInfo.Append(")");
        }
        cardInfo.Append("\n");

        //adds a well-formatted list of the colors on the card to the cardInfo.
        //This is complicated because we need to know how many entries to add before we add them due to the backslashes.
        //Start by adding all but the last color to the cardInfo.
        List<string> colorList = card.CardColorList;
        for (int i = 0; i < colorList.Count - 1; i++)
        {
            //confirms if the color in the Color List is part of the ColorsEnum. 
            CipherData.ColorsEnum colorValue;
            if (Enum.TryParse(colorList[i], out colorValue))
                if (Enum.IsDefined(typeof(CipherData.ColorsEnum), colorValue))
                {
                    //If the color is on the local card and not the cardData, print that color in green text.
                    if (card.CardColorArray[(int)colorValue] == card.GetCardData.cardColor[(int)colorValue])
                    {
                        cardInfo.Append(colorValue.ToString() + "/");
                    }
                    else
                    {
                        cardInfo.Append("<color=green>" + colorValue.ToString() + "</color>" + "/");
                    }
                }
                else
                    Debug.LogWarning(colorList[i] + " is not an underlying value of the ColorsEnum enumeration.");
            else
                Debug.LogWarning(colorList[i] + "is not a member of the Colors enumeration.");
        }
        
        //Adds the final color to the cardInfo
        if (colorList.Count > 0)
        {
            //confirms if the color in the Color List is part of the ColorsEnum. 
            CipherData.ColorsEnum colorValue;
            if (Enum.TryParse(colorList[colorList.Count - 1], out colorValue))
                if (Enum.IsDefined(typeof(CipherData.ColorsEnum), colorValue))
                {
                    //If the color is on the local card and not the cardData, print that color in green text.
                    if (card.CardColorArray[(int)colorValue] == card.GetCardData.cardColor[(int)colorValue])
                    {
                        cardInfo.Append(colorValue.ToString());
                    }
                    else
                    {
                        cardInfo.Append("<color=green>" + colorValue.ToString() + "</color>");
                    }
                }
                else
                    Debug.LogWarning(colorList[colorList.Count - 1] + " is not an underlying value of the ColorsEnum enumeration.");
            else
                Debug.LogWarning(colorList[colorList.Count - 1] + "is not a member of the Colors enumeration.");
        }
        //If no colors on card, then print "Colorless".
        else if (colorList.Count == 0)                     
        {
            //Check if there were colors on the original cardData
            int n = 0;
            for (int i = 0; i < card.GetCardData.cardColor.Length; i++)
            {
                if (card.GetCardData.cardColor[i])
                {
                    n++;
                }
            }

            if (n == 0)         //No color values in the card data or the local card
            {
                cardInfo.Append("Colorless");
            }
            else                //The cardData has colors which were removed on the local card; display the text in green.
            {
                cardInfo.Append("<color=green>Colorless</color>");
            }
        }


        //adds a well-formatted list of the genders on the card to the cardInfo.
        //Loops through each possible gender
        for (int i = 0; i < card.CharGenderArray.Length; i++)
        {
            //if the gender is on the card then add the gender name to the list
            if (card.CharGenderArray[i])
            {
                //Check if the gender was on the original card data and if not print it in green text.
                if (card.GetCardData.charGender[i])
                {
                    cardInfo.Append("/").Append(((CipherData.GendersEnum)i).ToString());
                }
                else
                {
                    cardInfo.Append("/<color=green>").Append(((CipherData.GendersEnum)i).ToString()).Append("</color>");
                }
            }
        }

        //adds a well-formatted list of the weapons on the card to the cardInfo.
        //Loops through each possible weapon
        for (int i = 0; i < card.CharWeaponArray.Length; i++)
        {
            //if the weapon is on the card then add the weapon name to the list
            if (card.CharWeaponArray[i])
            {
                //Check if the weapon was on the original card data and if not print it in green text.
                if (card.GetCardData.charWeaponType[i])
                {
                    cardInfo.Append("/").Append(((CipherData.WeaponsEnum)i).ToString());
                }
                else
                {
                    cardInfo.Append("/<color=green>").Append(((CipherData.WeaponsEnum)i).ToString()).Append("</color>");
                }
            }
        }

        //adds a well-formatted list of the unit types on the card to the cardInfo.
        //Loops through each possible unit type
        for (int i = 0; i < card.UnitTypeArray.Length; i++)
        {
            //if the unit type is on the card then add the type name to the list
            if (card.UnitTypeArray[i])
            {
                //Check if the unit type was on the original card data and if not print it in green text.
                if (card.GetCardData.unitTypes[i])
                {
                    cardInfo.Append("/").Append(((CipherData.TypesEnum)i).ToString());
                }
                else
                {
                    cardInfo.Append("/<color=green>").Append(((CipherData.TypesEnum)i).ToString()).Append("</color>");
                }
            }
        }

        cardInfo.Append("\n");

        if (card.CurrentAttackValue == card.GetCardData.baseAttack)
        {
            cardInfo.Append(card.CurrentAttackValue).Append(" ATK/");
        }
        else
        {
            cardInfo.Append("<color=green>").Append(card.CurrentAttackValue).Append("</color> ATK/");
        }

        if (card.CurrentSupportValue == card.GetCardData.baseSupport)
        {
            cardInfo.Append(card.CurrentSupportValue).Append(" SUPP/");
        }
        else
        {
            cardInfo.Append("<color=green>").Append(card.CurrentSupportValue).Append("</color> SUPP/");
        }

        //adds a card's range to the cardInfo
        cardInfo.Append(PrintRange(card));

        cardInfo.AppendLine("\n").Append(card.CharQuote);

        if (card.CardSkills.Length > 0)
        {
            for (int i = 0; i < card.CardSkills.Length; i++)
            {
                cardInfo.AppendLine("\n").Append(card.CardSkills[i]);
            }
        }

        cardInfo.AppendLine("\n").Append("Illust. " + card.CardIllustrator);

        //add skill change information if present on the card.
        if (card.SkillChangeTracker.Count > 0)
        {
            for (int i = 0; i < card.SkillChangeTracker.Count; i++)
            {
                //check if the tracker's entry is blank and ignore it if so.
                if (!card.SkillChangeTracker[i].Equals(""))
                {
                    cardInfo.AppendLine("\n").Append("<color=green>").Append(card.SkillChangeTracker[i]).Append("</color>");
                }
                
            }
        }
        
        
        return cardInfo.ToString();
    }

    //Formats a card's range information nicely.
    private string PrintRange(BasicCard card)
    {
        string rangeText = "";
        int numEntries = 0;
        int originalEntries = 0;            //helps with checks to confirm differences between the current card and the original.
        bool firstEntry = true;

        for (int i = 0; i < card.BaseRangeArray.Length; i++)
        {
            if (card.BaseRangeArray[i])
            {
                numEntries++;
            }
            if (card.GetCardData.baseRange[i])
            {
                originalEntries++;
            }
        }

        switch (numEntries)
        {
            //no range entries
            case 0:
                //checks for a difference between the original cardData and the current card's range.
                //If a difference is found, color the text in green.
                if (originalEntries == 0)
                {
                    rangeText += "NO";
                }
                else
                {
                    rangeText += "<color=green>NO</color>";
                }
                break;

            //A single range entry that needs to be posted.  Loop through the array to find the entry.
            case 1:
                for (int i = 0; i < card.BaseRangeArray.Length; i++)
                {
                    if (card.BaseRangeArray[i])
                    {
                        //checks for a difference between the original cardData and the current card's range.
                        //If a difference is found, color the text in green.
                        if (card.GetCardData.baseRange[i])
                        {
                            rangeText += (i + 1);
                        }
                        else
                        {
                            rangeText += "<color=green>" + (i + 1) + "</color>";
                        }
                    }
                }
                break;

            //Two or more range entries that need to be posted.  Loop through the array to find the first entry and then the rest.
            case 2:
            case 3:
                int n = 0;
                while (firstEntry)
                {
                    if (card.BaseRangeArray[n])
                    {
                        //checks for a difference between the original cardData and the current card's range.
                        //If a difference is found, color the text in green.
                        if (card.GetCardData.baseRange[n])
                        {
                            rangeText += (n + 1);
                        }
                        else
                        {
                            rangeText += "<color=green>" + (n + 1) + "</color>";
                        }
                        firstEntry = false;
                    }
                    n++;
                }

                while (n < card.BaseRangeArray.Length)
                {
                    if (card.BaseRangeArray[n])
                    {
                        rangeText += ", ";

                        //checks for a difference between the original cardData and the current card's range.
                        //If a difference is found, color the text in green.
                        if (card.GetCardData.baseRange[n])
                        {
                            rangeText += (n + 1);
                        }
                        else
                        {
                            rangeText += "<color=green>" + (n + 1) + "</color>";
                        }
                    }
                    n++;
                }
                break;

            //An unexpected number of range entires.
            default:
                Debug.LogWarning(gameObject.ToString() + " has an unexpected number of range elements!  Please check the cardData and PrintRange() method.");
                break;
        }

        rangeText += " RNG";

        return rangeText;
    }
}
