using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ContextMenu : MonoBehaviour {

    public static ContextMenu instance = null;

    public Button attackButton;
    public Button bondButton;
    public Button deployButton;
    public Button classChangeButton;
    public Button levelUpButton;
    public Button effectButton;
    public Button moveButton;
    public GameObject contextMenuObject;

    private RectTransform rectTransform;

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

    // Use this for initialization
    void Start ()
    {
        rectTransform = contextMenuObject.transform as RectTransform;

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void PopUpMenu(ContextMenuDetails details)
    {
        contextMenuObject.SetActive(true);
        
        //Sets the Popup menu to appear centered on the given click point
        Rect rectangle = rectTransform.rect;

        Vector2 position = details.rawPosition;

        position.x -= rectangle.width / 2;
        position.y -= rectangle.height / 2;

        rectTransform.anchoredPosition = position;

        //deactivate all the Buttons
        attackButton.gameObject.SetActive(false);
        bondButton.gameObject.SetActive(false);
        deployButton.gameObject.SetActive(false);
        classChangeButton.gameObject.SetActive(false);
        levelUpButton.gameObject.SetActive(false);
        effectButton.gameObject.SetActive(false);
        moveButton.gameObject.SetActive(false);

        //activate and set the Actions of the different Buttons.
        //Note that the CancelButton is set in the inspector to just close the panel.
        if (details.canAttack)
        {
            attackButton.gameObject.SetActive(true);
            attackButton.onClick.RemoveAllListeners();
            if (details.attackAction != null)
            {
                attackButton.onClick.AddListener(ClosePanel);
                attackButton.onClick.AddListener(details.attackAction);
            }
            else
            {
                Debug.LogError("The Attack Action was not set!  Clicking this button won't do anything!");
            }
        }

        if (details.canBond)
        {
            bondButton.gameObject.SetActive(true);
            bondButton.onClick.RemoveAllListeners();
            if (details.bondAction != null)
            {
                bondButton.onClick.AddListener(ClosePanel);
                bondButton.onClick.AddListener(details.bondAction);
            }
            else
            {
                Debug.LogError("The Bond Action was not set!  Clicking this button won't do anything!");
            }
        }

        if (details.canDeploy)
        {
            deployButton.gameObject.SetActive(true);
            deployButton.onClick.RemoveAllListeners();
            if (details.deployAction != null)
            {
                deployButton.onClick.AddListener(ClosePanel);
                deployButton.onClick.AddListener(details.deployAction);
            }
            else
            {
                Debug.LogError("The Deploy Action was not set!  Clicking this button won't do anything!");
            }
        }

        if (details.canClassChange)
        {
            classChangeButton.gameObject.SetActive(true);
            classChangeButton.onClick.RemoveAllListeners();
            if (details.classChangeAction != null)
            {
                classChangeButton.onClick.AddListener(ClosePanel);
                classChangeButton.onClick.AddListener(details.classChangeAction);
            }
            else
            {
                Debug.LogError("The Class Change Action was not set!  Clicking this button won't do anything!");
            }
        }

        if (details.canLevelUp)
        {
            levelUpButton.gameObject.SetActive(true);
            levelUpButton.onClick.RemoveAllListeners();
            if (details.levelUpAction != null)
            {
                levelUpButton.onClick.AddListener(ClosePanel);
                levelUpButton.onClick.AddListener(details.levelUpAction);
            }
            else
            {
                Debug.LogError("The Level Up Action was not set!  Clicking this button won't do anything!");
            }
        }

        if (details.canEffect)
        {
            effectButton.gameObject.SetActive(true);
            effectButton.onClick.RemoveAllListeners();
            if (details.effectAction != null)
            {
                effectButton.onClick.AddListener(ClosePanel);
                effectButton.onClick.AddListener(details.effectAction);
            }
            else
            {
                Debug.LogError("The Effect Action was not set!  Clicking this button won't do anything!");
            }
        }

        if (details.canMove)
        {
            moveButton.gameObject.SetActive(true);
            moveButton.onClick.RemoveAllListeners();
            if (details.moveAction != null)
            {
                moveButton.onClick.AddListener(ClosePanel);
                moveButton.onClick.AddListener(details.moveAction);
            }
            else
            {
                Debug.LogError("The Move Action was not set!  Clicking this button won't do anything!");
            }
        }

    }

    public void ClosePanel()
    {
        
        contextMenuObject.SetActive(false);
    }
}

public class ContextMenuDetails
{
    public Vector2 rawPosition;

    //Checks if there is an option besides "Cancel" in the context menu so it doesn't open unnecessarily.
    public bool ShouldOpen
    {
        get
        {
            return canAttack || canBond || canDeploy || canClassChange || canLevelUp || canEffect || canMove;
        }
    }

    public bool canAttack;
    public UnityAction attackAction;

    public bool canBond;
    public UnityAction bondAction;

    public bool canDeploy;
    public UnityAction deployAction;

    public bool canClassChange;
    public UnityAction classChangeAction;

    public bool canLevelUp;
    public UnityAction levelUpAction;

    public bool canEffect;
    public UnityAction effectAction;

    public bool canMove;
    public UnityAction moveAction;
    
}
