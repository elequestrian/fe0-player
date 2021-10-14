using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

//class to hold static Editor methods
public static class EditorList
{
    
    //Custom Editor for the Color List 
    public static void ShowColorList(SerializedProperty list)
    {
        //check in case we try to show an non-array!
        if (!list.isArray)
        {
            EditorGUILayout.HelpBox(list.name + " is neither an array nor a list!", MessageType.Error);
            return;
        }

        EditorGUILayout.PropertyField(list);

        EditorGUI.indentLevel += 1;
        if (list.isExpanded)
        {
            SerializedProperty size = list.FindPropertyRelative("Array.size");
            EditorGUILayout.PropertyField(size);
            for (int i = 0; i < list.arraySize; i++)
            {
                string name = "";
                if (Enum.IsDefined(typeof(CipherData.ColorsEnum), i))
                    name = ((CipherData.ColorsEnum)i).ToString();
                
                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent(name));
            }
        }

        EditorGUI.indentLevel -= 1;
    }
    
    //Custom Editor for the Gender List 
    public static void ShowGenderList(SerializedProperty list)
    {
        //check in case we try to show an non-array!
        if (!list.isArray)
        {
            EditorGUILayout.HelpBox(list.name + " is neither an array nor a list!", MessageType.Error);
            return;
        }

        EditorGUILayout.PropertyField(list);

        EditorGUI.indentLevel += 1;
        if (list.isExpanded)
        {
            SerializedProperty size = list.FindPropertyRelative("Array.size");
            EditorGUILayout.PropertyField(size);
            for (int i = 0; i < list.arraySize; i++)
            {
                string name = "";
                if (Enum.IsDefined(typeof(CipherData.GendersEnum), i))
                    name = ((CipherData.GendersEnum)i).ToString();

                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent(name));
            }
        }

        EditorGUI.indentLevel -= 1;
    }

    //Custom Editor for the Weapon List
    public static void ShowWeaponList(SerializedProperty list)
    {
        //check in case we try to show an non-array!
        if (!list.isArray)
        {
            EditorGUILayout.HelpBox(list.name + " is neither an array nor a list!", MessageType.Error);
            return;
        }

        EditorGUILayout.PropertyField(list);

        EditorGUI.indentLevel += 1;
        if (list.isExpanded)
        {
            SerializedProperty size = list.FindPropertyRelative("Array.size");
            EditorGUILayout.PropertyField(size);
            for (int i = 0; i < list.arraySize; i++)
            {
                string name = "";
                if (Enum.IsDefined(typeof(CipherData.WeaponsEnum), i))
                    name = ((CipherData.WeaponsEnum)i).ToString();

                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent(name));
            }
        }

        EditorGUI.indentLevel -= 1;
    }

    //Custom Editor for the Unit Types List
    public static void ShowUnitList(SerializedProperty list)
    {
        //check in case we try to show an non-array!
        if (!list.isArray)
        {
            EditorGUILayout.HelpBox(list.name + " is neither an array nor a list!", MessageType.Error);
            return;
        }

        EditorGUILayout.PropertyField(list);

        EditorGUI.indentLevel += 1;
        if (list.isExpanded)
        {
            SerializedProperty size = list.FindPropertyRelative("Array.size");
            EditorGUILayout.PropertyField(size);
            for (int i = 0; i < list.arraySize; i++)
            {
                string name = "";
                if (Enum.IsDefined(typeof(CipherData.TypesEnum), i))
                    name = ((CipherData.TypesEnum)i).ToString();

                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent(name));
            }
        }

        EditorGUI.indentLevel -= 1;
    }

    //Custom Editor for the Range List
    public static void ShowRangeList(SerializedProperty list)
    {
        //check in case we try to show an non-array!
        if (!list.isArray)
        {
            EditorGUILayout.HelpBox(list.name + " is neither an array nor a list!", MessageType.Error);
            return;
        }

        EditorGUILayout.PropertyField(list);

        EditorGUI.indentLevel += 1;
        if (list.isExpanded)
        {
            SerializedProperty size = list.FindPropertyRelative("Array.size");
            EditorGUILayout.PropertyField(size);
            for (int i = 0; i < list.arraySize; i++)
            {
                string name = "";
                if (Enum.IsDefined(typeof(CipherData.RangesEnum), i))
                    name = ((CipherData.RangesEnum)i).ToString();

                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent(name));
            }
        }

        EditorGUI.indentLevel -= 1;
    }

    //Custom Editor for the Skill Types List
    public static void ShowSkillTypeList(SerializedProperty list)
    {
        //check in case we try to show an non-array!
        if (!list.isArray)
        {
            EditorGUILayout.HelpBox(list.name + " is neither an array nor a list!", MessageType.Error);
            return;
        }

        EditorGUILayout.PropertyField(list);

        EditorGUI.indentLevel += 1;
        if (list.isExpanded)
        {
            SerializedProperty size = list.FindPropertyRelative("Array.size");
            EditorGUILayout.PropertyField(size);
            for (int i = 0; i < list.arraySize; i++)
            {
                string name = "";
                if (Enum.IsDefined(typeof(CipherData.SkillTypeEnum), i))
                    name = ((CipherData.SkillTypeEnum)i).ToString();

                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent(name));
            }
        }

        EditorGUI.indentLevel -= 1;
    }
}
