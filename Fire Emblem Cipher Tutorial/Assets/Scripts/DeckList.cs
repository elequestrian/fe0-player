using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DeckList : ISerializationCallbackReceiver
{
    public Dictionary<string, int> deckListDictionary = new Dictionary<string, int>();
    public string DefaultMC;
    public List<string> cardNameKeys = new List<string>();
    public List<int> numInDeckValues = new List<int>();

    //Default contructor
    public DeckList() { }

    //This is a constructor to quickly create a DeckList if need be.
    //NOTE: Add validation checks for a minimum deck of 50 cards with no more than 4 units of the same name.
    //Should also check that the MC is actually in the deck.  These can be added with Deck building capabilities.
    public DeckList(Dictionary<string, int> dic, string MC)
    {
        deckListDictionary = dic;
        DefaultMC = MC;
    }

    //Because Unity won't Serialize Dictionaries, it's necessary to convert the Dictionary to a list when serializing and back again on deserialization.
    public void OnBeforeSerialize()
    {
        cardNameKeys.Clear();
        numInDeckValues.Clear();

        foreach (var kvp in deckListDictionary)
        {
            cardNameKeys.Add(kvp.Key);
            numInDeckValues.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        deckListDictionary = new Dictionary<string, int>();

        if (cardNameKeys.Count != numInDeckValues.Count)
        {
            Debug.LogError("SERIALIZATION ERROR: Count of lists is different!");
        }
        else
        {
            for (int i = 0; i < cardNameKeys.Count; i++)
                deckListDictionary.Add(cardNameKeys[i], numInDeckValues[i]);
        }
    }

    /*
    //Method which returns the cards in the decklist as a List of BasicCards.
    public List<BasicCard> GetCardsInDeck()
    {
        List<BasicCard> deck = new List<BasicCard>();

        foreach (KeyValuePair<string, int> kvp in deckListDictionary)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                //Debug.Log("Trying to load " + kvp.Key + " from Resources.");
                GameObject loadedObject = Instantiate(Resources.Load(kvp.Key)) as GameObject;
                //Debug.Log(loadedObject + " has been successfully loaded.");

                BasicCard cardToAdd = loadedObject.GetComponent<BasicCard>();
                deck.Add(cardToAdd);
                //Debug.Log(cardToAdd.ToString() + " has been added to the deck.");
            }
        }

        Debug.Log("Deck contains " + deck.Count + " cards.");

        return deck;
    }
    */
}
