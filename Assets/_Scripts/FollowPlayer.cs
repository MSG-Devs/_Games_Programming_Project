using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*BUGS / TO DOS
 * Add preset camera veiws for player to select e.g certain levels of zoom 
 */
public class FollowPlayer : MonoBehaviour
{
    private GameObject playerObject; // Pos camera will follow
    public Vector3 offset; // cam distance from the above pos 

    //speed camera lerps for pos to pos
    private float smoothSpeed = 0.125f;

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    private void Awake()
    {
        playerObject = GameObject.Find("Player"); //auto assign object

        //check for player object 
        if(playerObject == null)
        {

            playerObject = GameObject.Find("Gideon_PlayerPrefab"); //auto assign object

            if (playerObject == null)
            {
                Debug.LogError("The follow player script cannot find the gameobject player");
            }
            else
            {
                Debug.Log("The camera is following Gideons test player character");
            }
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    private void FixedUpdate() // using fixed update makes the transition smoother 
    {
        //get player pos and add offset
        Vector3 playerPosition = playerObject.transform.position + offset;

        //get current pos and player pos and lerp
        Vector3 lerpPositions = Vector3.Lerp(transform.position, playerPosition, smoothSpeed);

        //move cam
        transform.position = lerpPositions;

        //ensure cam is focused on player
        transform.LookAt(playerObject.transform.position);
    }
}
