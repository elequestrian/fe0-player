using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CardData)), CanEditMultipleObjects]
public class CardDataEditor : Editor
{
    /*
    //unchangeable attributes of each card.
    //These are loaded from a card specific ScriptableObject to save memory as they are shared among one card type.
    //These fields are protected and only accessible via public properties to allow for alterations if need be (alternate names for instance).
    public string cardNumber;
    public string charTitle;
    public string charQuote;
    public string cardIllustrator;
    public string[] cardSkills;
    public bool[] skillTypes;

    public string charName;
    public string classTitle;
    public int deploymentCost;
    public bool canPromote;
    public int promotionCost;
    public bool[] cardColor;
    public bool[] charGender;
    public bool[] charWeaponType;
    public bool[] unitTypes;
    public int baseAttack;
    public int baseSupport;
    public bool[] baseRange;               
    
    */

    SerializedProperty cardNumber;
    SerializedProperty charTitle;
    SerializedProperty charQuote;
    SerializedProperty cardIllustrator;
    SerializedProperty cardSkills;
    SerializedProperty skillTypes;

    SerializedProperty charName;
    SerializedProperty classTitle;
    SerializedProperty deploymentCost;
    SerializedProperty canPromote;
    SerializedProperty promotionCost;
    SerializedProperty cardColor;
    SerializedProperty charGender;
    SerializedProperty charWeaponType;
    SerializedProperty unitTypes;
    SerializedProperty baseAttack;
    SerializedProperty baseSupport;
    SerializedProperty baseRange;
    

    void OnEnable()
    {
        cardNumber = serializedObject.FindProperty("cardNumber"); ;
        charTitle = serializedObject.FindProperty("charTitle");
        charQuote = serializedObject.FindProperty("charQuote");
        cardIllustrator = serializedObject.FindProperty("cardIllustrator");
        cardSkills = serializedObject.FindProperty("cardSkills");
        skillTypes = serializedObject.FindProperty("skillTypes");

        charName = serializedObject.FindProperty("charName");
        classTitle = serializedObject.FindProperty("classTitle");
        deploymentCost = serializedObject.FindProperty("deploymentCost");
        canPromote = serializedObject.FindProperty("canPromote");
        promotionCost = serializedObject.FindProperty("promotionCost");
        cardColor = serializedObject.FindProperty("cardColor");
        charGender = serializedObject.FindProperty("charGender");
        charWeaponType = serializedObject.FindProperty("charWeaponType");
        unitTypes = serializedObject.FindProperty("unitTypes");
        baseAttack = serializedObject.FindProperty("baseAttack");
        baseSupport = serializedObject.FindProperty("baseSupport");
        baseRange = serializedObject.FindProperty("baseRange");
        
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(cardNumber);
        EditorGUILayout.PropertyField(charTitle);
        EditorGUILayout.PropertyField(charQuote);
        EditorGUILayout.PropertyField(cardIllustrator);
        EditorGUILayout.PropertyField(cardSkills, true, GUILayout.ExpandHeight(true));
        EditorList.ShowSkillTypeList(skillTypes);

        EditorGUILayout.PropertyField(charName);
        EditorGUILayout.PropertyField(classTitle);
        EditorGUILayout.PropertyField(deploymentCost);
        EditorGUILayout.PropertyField(canPromote);
        EditorGUILayout.PropertyField(promotionCost);

        EditorList.ShowColorList(cardColor);
        EditorList.ShowGenderList(charGender);
        EditorList.ShowWeaponList(charWeaponType);
        EditorList.ShowUnitList(unitTypes);

        EditorGUILayout.PropertyField(baseAttack);
        EditorGUILayout.PropertyField(baseSupport);

        EditorList.ShowRangeList(baseRange);
       

        serializedObject.ApplyModifiedProperties();
    }


    
}
