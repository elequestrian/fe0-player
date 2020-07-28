using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//[Serializeable]
public class MyTestClassAttribute : PropertyAttribute
{
    public string colorTest;

    public MyTestClassAttribute(string list)
    {
        this.colorTest = list;

        /*
        for (int i = 0; i < colorTest.Length; i++)
        {
            colorTest[i] = true;
        }
        */
    }

}
