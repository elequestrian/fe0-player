using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class DialogueWindow : MonoBehaviour {

    public Text windowTitle;
    public Text dialogueText;
    public GameObject dialogueWindowObject;
    public Button button1;
    public Button button2;
    public Text button1Text;
    public Text button2Text;

    private static DialogueWindow dialogueWindow;

    public static DialogueWindow Instance()
    {
        if (!dialogueWindow)
        {
            dialogueWindow = FindObjectOfType(typeof(DialogueWindow)) as DialogueWindow;
            if (!dialogueWindow)
                Debug.LogError("There needs to be one active DialogueWindow script on a GameObject in your scene.");
        }

        return dialogueWindow;
    }

    public void MakeChoice(DialogueWindowDetails details)
    {
        dialogueWindowObject.SetActive(true);

        windowTitle.text = details.windowTitleText;
        dialogueText.text = details.questionText;

        button1Text.text = details.button1Details.buttonText;
        button1.onClick.RemoveAllListeners();
        button1.onClick.AddListener(ClosePanel);
        button1.onClick.AddListener(details.button1Details.buttonAction);
        

        button2Text.text = details.button2Details.buttonText;
        button2.onClick.RemoveAllListeners();
        button2.onClick.AddListener(ClosePanel);
        button2.onClick.AddListener(details.button2Details.buttonAction);
       

    }

    private void ClosePanel()
    {
        
        dialogueWindowObject.SetActive(false);
    }
}

public class DialogueWindowDetails
{
    public string windowTitleText;
    public string questionText;
    public DialogueButtonDetails button1Details;
    public DialogueButtonDetails button2Details;
}

public class DialogueButtonDetails
{
    public string buttonText;
    public UnityAction buttonAction;
}
