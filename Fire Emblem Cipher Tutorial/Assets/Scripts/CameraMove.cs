using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour {

	//This script takes input from the slider bar and positions the camera approrpriately to see various views of the board.

    public void PositionCamera(float input)
    {
        //Close any currently open context menu.
        ContextMenu.instance.ClosePanel();

        Vector3 newLocation = new Vector3(transform.position.x, 0, transform.position.z);

        if (input < 0.25)
        {
            newLocation.y = 1;
        }
        else if (input > 0.75)
        {
            newLocation.y = 14;
        }
        else        //input is between 0.25 and 0.75
        {
            newLocation.y = 7.5f;
        }

        transform.position = newLocation;
    }
	
}
