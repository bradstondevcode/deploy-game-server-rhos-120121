using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherPlayer : MonoBehaviour
{
    public GameObject opBody;
    public GameObject opRotBody;
    public Quaternion targetRotation;
    public Vector3 targetPosition;
    public Vector3 oldTargetPosition;

    private float moveSpeed = 10.0f;

    float rotSpeed = 15.0f;

    public double timestamp = 0.0;
    public double oldTimestamp = 0.0f;

    private Vector3 normalizeDirection;

    float movementStoppedCounter = 0.0f;
    float countToStop = 0.15f;


    void Start()
    {
        targetPosition = transform.position;
    }

    void FixedUpdate()
    {

        //Use player current position and target position to determine the direction of their movement
        normalizeDirection = (targetPosition - transform.position).normalized;

        //Setting "old" values for comparing new values to old values
        oldTimestamp = timestamp;
        oldTargetPosition = targetPosition;

        //If gameobject is significantly far from target position, instantly move them to target position
        if ((targetPosition - transform.position).magnitude > 20f)
        {
            //Automatically move character to position if they are far away from their target position
            transform.position = targetPosition;
        }
        //If gameobject isn't a certain distance from target position, don't bother moving them (keeps gameobject from jittering when near target position)
        else if ((targetPosition - transform.position).magnitude > 0.1f)
        {
            //Move character in direction at desired movement speed (smoothly)
            transform.position += normalizeDirection * moveSpeed * Time.deltaTime;
        }

        //Rotate character to target rotation smoothly
        opRotBody.transform.rotation = Quaternion.Lerp(opRotBody.transform.rotation, targetRotation, rotSpeed * Time.deltaTime);
        
    }

}
