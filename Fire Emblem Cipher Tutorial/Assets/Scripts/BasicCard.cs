using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[System.Serializable]
public abstract class BasicCard : MonoBehaviour, IPointerClickHandler {

    protected Animator anim;
    [SerializeField] protected CardData cardData;
    protected CardManager owner;
    protected bool tapped = false;                 
    protected bool faceup = true;
    public bool triggerResolved = false;
    public int attackModifier = 0;                      //Used to modify a card's attack power outside of battle (with the effects of skills, etc.)
    public int battleModifier = 0;                      //Used to update a card's attack power in battle (with supports, etc.)  Is reset after battle.
    public int supportModifier = 0;                     //NOTE: Not fully integrated.  Needs to have a way to reset similar to attackModifier.
                                                        //Used to modify a card's support power with the effects of skills, etc. Reset once the card leaves the field.
    public MyBoolEvent DeclareAttackEvent = new MyBoolEvent();
    public UnityEvent BattleSupportEvent = new UnityEvent();
    public UnityEvent AfterBattleEvent = new UnityEvent();
    public MyBasicCardEvent RemoveFromFieldEvent = new MyBasicCardEvent();
    protected List<string> skillChangeTracker = new List<string>();

    //These card stats are held locally in case they get changed by other cards.
    protected int localDeploymentCost;
    protected bool localCanPromote;
    protected int localPromotionCost;
    protected bool[] localCardColorArray;
    protected bool[] localCharGenderArray;
    protected bool[] localCharWeaponArray;
    protected bool[] localUnitTypeArray;
    protected int localBaseAttack;
    protected int localBaseSupport;
    protected bool[] localBaseRangeArray;
    
    public bool Tapped { get { return tapped; } }
    public bool FaceUp { get { return faceup; } }
    public CardData GetCardData { get { return cardData; } }

    //These are the properties to access the information about this card. 
    //Properties are made virtual to account for special cases and card abilities.
    //They are readonly as they cannot change the original CardData asset.
    public virtual string CardNumber { get { return cardData.cardNumber; } }
    public virtual string CharTitle { get { return cardData.charTitle; } }
    public virtual string CharQuote { get { return cardData.charQuote; } }
    public virtual string CardIllustrator { get { return cardData.cardIllustrator; } }
    public virtual string[] CardSkills { get { return cardData.cardSkills; } }
    public virtual string CharName { get { return cardData.charName; } }
    public virtual string ClassTitle { get { return cardData.classTitle; } }

    public virtual int DeploymentCost { get { return localDeploymentCost; } }
    public virtual int PromotionCost { get { return localPromotionCost; } }
    public virtual bool[] CardColorArray { get { return localCardColorArray; } }
    public virtual bool[] CharGenderArray { get { return localCharGenderArray; } }
    public virtual bool[] CharWeaponArray { get { return localCharWeaponArray; } }
    public virtual bool[] UnitTypeArray { get { return localUnitTypeArray; } }
    public virtual int BaseAttack { get { return localBaseAttack; } }
    public virtual int BaseSupport { get { return localBaseSupport; } }
    public virtual bool[] BaseRangeArray { get { return localBaseRangeArray; } }


    public virtual bool Bondable { get { return true; } }                                   //Allows for certain cards (like S01-003: Jagen) to be unable to be placed in the Bond area.
    public virtual CardManager Owner { get { return owner; } }                              //allows for skills or other cards to know the owner of a card and access the field state.
    public virtual bool Promotable { get { return localCanPromote; } }
    public List<string> SkillChangeTracker { get { return skillChangeTracker; } }
    public virtual int CurrentAttackValue { get { return BaseAttack + attackModifier; } }       //Returns the current attack stat including skill modifiers.
    public virtual int CurrentSupportValue { get { return BaseSupport + supportModifier; } }    //Returns the current support stat including skill modifiers.

    //Property returns a List of the string names of the colors on this card.
    public virtual List<string> CardColorList
    {
        get
        {
            List<string> colorNames = new List<string>(CipherData.NumColors);

            //Loops through each possible color
            for (int i = 0; i < CardColorArray.Length; i++)
            {
                //if the color is on the card then add the color name to the list
                if (CardColorArray[i])
                {
                    colorNames.Add(((CipherData.ColorsEnum)i).ToString());
                }
            }

            return colorNames;
        }
    }

    //Property returns a List of the string names of the genders on this card.
    public virtual List<string> CharGenderList
    {
        get
        {
            List<string> genderNames = new List<string>(CipherData.NumGenders);

            //Loops through each possible gender
            for (int i = 0; i < CharGenderArray.Length; i++)
            {
                //if the gender is on the card then add the gender name to the list
                if (CharGenderArray[i])
                {
                    genderNames.Add(((CipherData.GendersEnum)i).ToString());
                }
            }

            return genderNames;
        }
    }

    //Property returns a List of the string names of the weapons on this card.
    public virtual List<string> CharWeaponList
    {
        get
        {
            List<string> weaponNames = new List<string>(CipherData.NumWeapons);

            //Loops through each possible weapon
            for (int i = 0; i < CharWeaponArray.Length; i++)
            {
                //if the weapon is on the card then add the weapon name to the list
                if (CharWeaponArray[i])
                {
                    weaponNames.Add(((CipherData.WeaponsEnum)i).ToString());
                }
            }

            return weaponNames;
        }
    }

    //Property returns a List of the string names of the unit types on this card.
    public virtual List<string> UnitTypeList
    {
        get
        {
            List<string> typeNames = new List<string>(CipherData.NumTypes);

            //Loops through each possible type
            for (int i = 0; i < UnitTypeArray.Length; i++)
            {
                //if the type is on the card then add the type name to the list
                if (UnitTypeArray[i])
                {
                    typeNames.Add(((CipherData.TypesEnum)i).ToString());
                }
            }

            return typeNames;
        }
    }

    //Returns the total attack (including battle modifiers) for battle calcuations.
    public virtual int TotalAttack
    {
        get
        {
            int totalAttack = CurrentAttackValue + battleModifier;

            //Only activates a critical hit if this unit is attacking.
            if (GameManager.instance.CriticalHit && GameManager.instance.CurrentAttacker == this)
            {
                totalAttack = totalAttack * 2;
            }

            return totalAttack;
        }
    }

    //Returns the other allies on the field besides this card.
    public virtual List<BasicCard> OtherAllies
    {
        get
        {
            List<BasicCard> otherAllies = Owner.FieldCards;
            otherAllies.Remove(this);
            return otherAllies;
        }
    }
    
    //Returns a list of the BasicCards that are in range for this card to attack.
    //Should I make a similar method that obtains the Card Stacks?
    public virtual List<BasicCard> AttackTargets
    {
        get
        {
            List<BasicCard> allTargets = new List<BasicCard>();

            if (Owner.FrontLineCards.Contains(this))   //This card is on the front line.
            {
                if (BaseRangeArray[(int)CipherData.RangesEnum.Range1])  //Check if this card has Range 1 and can attack enemy front line.
                {
                    allTargets.AddRange(Owner.Opponent.FrontLineCards);
                }

                if (BaseRangeArray[(int)CipherData.RangesEnum.Range2])   //Check if this card has Range 2 and can attack enemy back line.
                {
                    allTargets.AddRange(Owner.Opponent.BackLineCards);
                }
            }
            else if (Owner.BackLineCards.Contains(this))    //This card is on the back line.
            {
                if (BaseRangeArray[(int)CipherData.RangesEnum.Range2])  //Check if this card has Range 2 and can attack enemy front line.
                {
                    allTargets.AddRange(Owner.Opponent.FrontLineCards);
                }

                if (BaseRangeArray[(int)CipherData.RangesEnum.Range3])   //Check if this card has Range 3 and can attack enemy back line.
                {
                    allTargets.AddRange(Owner.Opponent.BackLineCards);
                }
            }
            else
            {
                Debug.LogError("Error!  " + this.ToString() + " is not on the field!  Please check why AttackTargets called!");
            }

            allTargets = Owner.AttackTargetHandler.MakeListenersEditList(this, allTargets);

            return allTargets;
        }
    }

    //Returns the current deployment cost for this card given the board state.
    //NOTE: This was made obsolete in the current iteration of the deploy/levelUp/classChange logic.
    /*
    public virtual int ActualDeployCost
    {
        get
        {
            //if there is a card on the field with the same name, then check for a promotion cost, otherwise return its deployment cost
            foreach (BasicCard card in owner.FieldCards)
            {
                if (card.CharName == CharName && Promotable)
                {
                    return PromotionCost;
                }
            }
            
            
            return DeploymentCost;
        }
    }
    */

    // This method takes the place of the Start method as Start is not called in inherited classes. 
    //NOTE: need to clone the arrays to store a shallow copy and not a reference to the cardData.
    //Thus changes will only impact the card itself, not the cardData and hence ALL cards.
    protected void SetUp()
    {
        anim = GetComponent<Animator>();
        localDeploymentCost = cardData.deploymentCost;
        localPromotionCost = cardData.promotionCost;
        localCardColorArray = (bool[])cardData.cardColor.Clone();
        localCharGenderArray = (bool[])cardData.charGender.Clone();
        localCharWeaponArray = (bool[])cardData.charWeaponType.Clone();
        localUnitTypeArray = (bool[])cardData.unitTypes.Clone();
        localBaseAttack = cardData.baseAttack;
        localBaseSupport = cardData.baseSupport;
        localBaseRangeArray = (bool[])cardData.baseRange.Clone();
        localCanPromote = cardData.canPromote;

    }

    //This method adds a text string to this card's SkillChangeTracker list for display in the card reader.
    public void AddToSkillChangeTracker(string skillInfo)
    {
        //check if there is an exisiting empty string in the list and use that spot for the skillInfo text.
        for (int i = 0; i < skillChangeTracker.Count; i++)
        {
            if (skillChangeTracker[i].Equals(""))
            {
                skillChangeTracker[i] = skillInfo;
                return;
            }
        }

        //if all extries in the List have text (or there are no entries) add the text to the end.
        skillChangeTracker.Add(skillInfo);
        return; 
    }

    //This method attempts to remove a text string from this card's SkillChangeTracker list.
    public bool RemoveFromSkillChangeTracker (string skillInfo)
    {
        //Search for the index of the entry that matches the given skillInfo
        int index = skillChangeTracker.FindIndex(x => x.Equals(skillInfo));

        //Change the value of the given index to an empty string if the skill was found.
        if (index >= 0)
        {
            skillChangeTracker[index] = "";
            return true;
        }
        else
        {
            return false;
        }
    }

    //This method activates all skills that can be triggered while the card is on the field.
    //It is virtual and blank so that it can be overridden by individual cards, but still easily called by other methods generically.
    public virtual void ActivateFieldSkills()
    {

    }

    //This method is used to remove skills from event calls when the card is "disabled" (under a stack) and/or removed from the field.
    public virtual void DeactivateFieldSkills()
    {
        RemoveFromFieldEvent.Invoke(this);
    }

    /* The below two methods were used in an older architecture.  They have been replaced with more localized tracking and removal of buffs.
    //This method is used to cancel any attack modifiers added to a card.  It may need to be expanded in the future as more complex modifiers get overlaid.
    public virtual void RemoveAttackModifier(int deduction)
    {
        //Only activate this method when the card is actually on the field.  This prevents attack from being modified in the retreat, hand, etc.
        if (Owner.FieldCards.Contains(this))
        {
            attackModifier -= deduction;
        }

        //Ensures that the attack modifier cannot be below 0
        if (attackModifier < 0)
        {
            attackModifier = 0;
        }

        Debug.Log(this.ToString() + "'s current attackModifier value is " + attackModifier);
    }

    //This methods sets a callback to the RemoveAttackModifier method above at the appropriate time (end of player turn or end of opponent's turn)
    public void SetBuffRemovalCallback(int deduction, bool opponentTurn)
    {
        if (opponentTurn)
        {
            Owner.Opponent.endTurnEvent.AddListener
                (
                    () => { RemoveAttackModifier(deduction); }
                );
        }
        else //the callback needs to be on the player's turn
        {
            Owner.endTurnEvent.AddListener
                (
                    () => { RemoveAttackModifier(deduction); }
                );
        }
    }
    */

    public void SetOwner(CardManager newOwner)
    {
        owner = newOwner;
    }

    //These two virtual methods form the base for the implementation of ACT skills and can be overridden in the specific card scripts if necessary to implement specific skills.
    protected virtual bool CheckActionSkillConditions()
    {
        return false;
    }

    protected virtual void PayActionSkillCost()
    {
    }

    //This virtual method forms the base for the trigger skill implementation and is referenced in the TriggerEventHandler.
    public virtual bool CheckTriggerSkillCondition(BasicCard triggeringCard)
    {
        return false;
    }

    //This virtual method forms the base for the trigger skill implementation and is references in the TriggerEventHandler.
    public virtual void ActivateTriggerSkill(BasicCard triggeringCard)
    {

    }

    //This virtual method works with the TriggerEventHandler's list editing feature.
    public virtual List<BasicCard> EditList(BasicCard requestingCard, List<BasicCard> listToEdit)
    {
        return listToEdit;
    }

    //This virtual method forms the base for Offensive Support Skills
    public virtual void ActivateAttackSupportSkill()
    {
        GameManager.instance.ActivateDefenderSupport();
    }

    //This virtual method forms the base for Defensive support skills.
    public virtual void ActivateDefenseSupportSkill()
    {
        GameManager.instance.AddSupportValues();
    }

    //resets the battle modified stats and ability text after battle.  Called by the EndBattle method in the GameManager.
    public void ResetAfterBattle()
    {
        AfterBattleEvent.Invoke();

        if (battleModifier != 0)
        {
            battleModifier = 0;
        }
    }

    //This virtual method compares the name of the current card to a given string.
    public virtual bool CompareNames(string name)
    {
        return CharName.Equals(name);
    }

    //An overload to the above method which compares a card to a given card to see if the names match.
    //The passed boolean operator determines if this is the first time this method has been called as some recursion is necessary:
    //The method checks if the current card matches the passed card and then calls the passed card's CompareNames method in case of overrides.
    public virtual bool CompareNames(BasicCard otherCard, bool firstCheck)
    {
        bool thisMatch = CompareNames(otherCard.CharName);
        bool otherMatch = false;

        if (firstCheck)
        {
            otherMatch = otherCard.CompareNames(this, false);
        }

        return thisMatch || otherMatch;
    }

    //A more user-friendly overload of the above method which assumes that the recursive check is necessary.
    public virtual bool CompareNames(BasicCard otherCard)
    {
        return CompareNames(otherCard, true);
    }

    //Requires an appropriate Raycaster component be on a camera in order to trigger.
    public void OnPointerClick(PointerEventData data)
    {
        //ensures that only the correct people look at each other's cards unless this is debug mode. 
        if (FaceUp || owner.Bonds.Contains(this) || GameManager.instance.playtestMode)
        {
            CardReader.instance.DisplayCard(gameObject);
        }
        
        ContextMenu.instance.ClosePanel();


        if (FaceUp && owner == GameManager.instance.turnPlayer && !GameManager.instance.inCombat)
        {
            //pulls up the ContextMenu centered on the card when the card is clicked. 
            ContextMenuDetails menuDetails = new ContextMenuDetails
            {
                rawPosition = Camera.main.WorldToScreenPoint(transform.position),
                //canAttack = false,
                //attackAction = () => { Debug.Log("Attack!"); },
                //canBond = true,
                //bondAction = () => { Debug.Log("Bond!"); },
                //canDeploy = false,
                //deployAction = () => { Debug.Log("Deploy!"); }, //owner.DeployToFrontLine(this);
                //canClassChange = false,
                //classChangeAction = () => { Debug.Log("Class Change!"); },
                //canLevelUp = false,
                //levelUpAction = () => { Debug.Log("Level Up!"); }
                //canEffect = false,
                //effectAction = () => { Debug.Log("Effect!");  }, //owner.LevelUp(this);
                //canMove = false,
                //moveAction = () => { Debug.Log("Move!"); } //owner.ReturnCardStackToHandFromField(this);
            };

            //If it's the main phase, this card is on the field, this card isn't tapped, it isn't the first turn, and there are other cards in its range then let it attack.
            if (GameManager.instance.CurrentPhase == CipherData.PhaseEnum.Action && Owner.FieldCards.Contains(this) && !Tapped 
                && (!GameManager.instance.FirstTurn || GameManager.instance.playtestMode) && AttackTargets.Count > 0)
            {
                menuDetails.canAttack = true;
                menuDetails.attackAction = () => { Debug.Log("Attack!"); GameManager.instance.AimAttack(this); };
            }
            else
            {
                menuDetails.canAttack = false;
            }
            
            //If it's the bond phase, no other cards have been bonded, this card is in the hand, and it can be bonded then let it be bonded.
            if (GameManager.instance.CurrentPhase == CipherData.PhaseEnum.Bond && owner.Hand.Contains(this) && Bondable && (!GameManager.instance.cardBonded || GameManager.instance.playtestMode))
            {
                menuDetails.canBond = true;
                menuDetails.bondAction = () => { owner.PlaceCardInBonds(this, owner.Hand, true); GameManager.instance.cardBonded = true; };
            }
            else
            {
                menuDetails.canBond = false;
            }

            //The following determines whether a card can be deployed, leveled up, or class changed. Start by assuming that none of those actions can occur.
            menuDetails.canDeploy = false;
            menuDetails.canClassChange = false;
            menuDetails.canLevelUp = false;

            //If it's the deployment phase, there is an appropriate color present in the bonds, and this card is in the hand, then it might be playable to the field.
            if (GameManager.instance.CurrentPhase == CipherData.PhaseEnum.Deployment && owner.AreCardColorsBonded(this) && owner.Hand.Contains(this) )
            {
                int usableBonds = owner.Bonds.Count - GameManager.instance.bondDeployCount;

                //if the card is present on the field, it may be able to be leveled up or class changed or both, but not deployed.
                if (owner.FieldCards.Exists(x => x.CompareNames(this, true)))
                {
                    //Check if there are enough bonds for this card to be classChanged.
                    if (usableBonds >= PromotionCost && Promotable)
                    {
                        menuDetails.canClassChange = true;
                        menuDetails.classChangeAction = () => { Debug.Log("Class Change!"); owner.PlayToFieldFromHand(this, true); };
                    }

                    //also check if there are enough bonds for this card to be leveled up
                    if (usableBonds >= DeploymentCost)
                    {
                        menuDetails.canLevelUp = true;
                        menuDetails.levelUpAction = () => { Debug.Log("Level Up!"); owner.PlayToFieldFromHand(this, false); };
                    }
                }
                else    //The card is not present on the field, so this is a deployment if there are sufficient bonds.
                {
                    //Check if there are sufficent bonds to deploy this card.
                    if (usableBonds >= DeploymentCost)
                    {
                        menuDetails.canDeploy = true;
                        menuDetails.deployAction = () => { Debug.Log("Deploy!"); owner.DeployToFieldFromHand(this); };
                    }
                }
            }

            //If it's the action phase, a skill's conditions are met, and the card is on the field, then start paying the cost to use the skill.
            if (GameManager.instance.CurrentPhase == CipherData.PhaseEnum.Action && CheckActionSkillConditions() && owner.FieldCards.Contains(this))
            {
                menuDetails.canEffect = true;
                menuDetails.effectAction = () => { Debug.Log("Effect!"); PayActionSkillCost(); }; 
            }
            else
            {
                menuDetails.canEffect = false;
            }
            

            //If it's the action phase, the card is not already tapped, and the card is on the field, then let it be moved.
            //Also taps the card as part of the cost before moving.
            if (GameManager.instance.CurrentPhase == CipherData.PhaseEnum.Action && !Tapped && owner.FieldCards.Contains(this))
            {
                menuDetails.canMove = true;
                menuDetails.moveAction = () => { Debug.Log("Move!"); Tap(); owner.MoveCard(this); };
            }
            else
            {
                menuDetails.canMove = false;
            }


            if (menuDetails.ShouldOpen)
            {
                ContextMenu.instance.PopUpMenu(menuDetails);
            }
        }
    }

    

    //The following methods control the card's position on the board.
    public void Tap()
    {
        if (!tapped)
        {
            tapped = true;
            anim.SetBool("Tapped?", tapped);
        }
        else
        {
            Debug.LogError(gameObject.ToString() + " is already tapped.");
        }
        
    }

    public void Untap()
    {
        if (tapped)
        {
            tapped = false;
            anim.SetBool("Tapped?", tapped);
        }
        else
        {
            Debug.LogError(gameObject.ToString() + " is not tapped.");
        }

    }

    public void FlipFaceDown()
    {
        if (faceup)
        {
            faceup = false;
            //Debug.Log("Parameter list: " + anim.GetParameter(3).ToString() + " whose value is " + anim.GetParameter(3).defaultBool);
            //Debug.Log("Parameter list:  whose value is ");
            anim.SetBool("FaceUp?", faceup);
            //Debug.Log(gameObject.ToString() + "is flipping facedown.");
        }
        else
        {
            Debug.LogError(gameObject.ToString() + " is already face up.");
        }
       
    }

    public void FlipFaceUp()
    {
        if (!faceup)
        {
            faceup = true;
            anim.SetBool("FaceUp?", faceup);
            //Debug.Log(gameObject.ToString() + "is flipping face up.");
        }
        else
        {
            Debug.LogError(gameObject.ToString() + " is not face down.");
        }

    }

    /*
    //resets this card's animator bools based on the inherent position bools.  
    //Note that with the animator active, there is no way to directly change rotation values as they're drived by the animator.  
    void OnEnable ()
    {
        Debug.Log("On Enable");

        Debug.Log(gameObject.ToString() + " has the following rotation: " + gameObject.transform.eulerAngles);
        //gameObject.transform.Rotate(45f, 0.0f, 0.0f, Space.Self);

        Debug.Log(gameObject.ToString() + " now has the following rotation: " + gameObject.transform.eulerAngles);

    //The below are likely the best choice!!!  But do test this thoroughly.  :)
        anim.SetBool("FaceUp?", faceup);
        anim.SetBool("Tapped?", tapped);

    }

    void OnDisable()
    {
        Debug.Log("On Disable");

        /*
        if (!faceup)
            anim.SetBool("FaceUp?", true);
        if (tapped)
            anim.SetBool("Tapped?", false);
        
    }
    */
    /*
    * These methods were used primarily to test out how things were functioning and breaking.  They are no longer needed or used.
   // Use this for initialization
   public void Info()
   {
       Debug.Log(gameObject.ToString() + " is being controlled by " + anim.ToString());


       AnimatorControllerParameter[] parameters = anim.parameters;
       for (int i = 0; i < parameters.Length; i++)
       {
           Debug.Log("Parameter " + i + ": " + parameters[i].name);
           Debug.Log("of type " + parameters[i].type);

       }
   }



    // Requires a collider be on the object.
    public void OnMouseDown()
    {
        Debug.Log("OnMouseDown()");
        //FlipFaceUp();
        //FlipFaceDown();
        Info();

        Tap();
        Untap();
    }
   */
}