using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Creating an interface that will let my static skill classes be somewhat standardized with the most basic function calls.
public interface ISkill {

    bool CheckConditions();

    void PayCost();
}
