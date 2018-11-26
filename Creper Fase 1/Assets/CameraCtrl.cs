using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCtrl : MonoBehaviour
{
    [Header("Target")]
    public Transform player;                        // Reference to the player position.
    private Vector3 target;                         // Position for the camera to follow.
    private Vector3 inputPos;                       // Position for the input to be centered around the player.
    private Vector3 refVel;                         // Velocity for the smooth damp to manage.
    public float cameraDist = 3f;                   // Max distance of the camera.
    public float smoothTime = 0.2f;                 // Travel speed of the camera.

    ////shake
    //Vector3 shakeOffset;
    //float shakeMag, shakeTimeEnd;
    //Vector3 shakeVector;
    //bool shaking;

    private bool controllerStick;

    void FixedUpdate()
    {
        // Check the input method.
        InputMethod();

        // Get the input position centered around the world.
        inputPos = CaptureInputPos(); 

        //shakeOffset = UpdateShake(); //account for screen shake

        // Get the target position moved and centered around the player.
        target = UpdateTargetPos();

        // Smoothly move the camera closer to it's target location.
        UpdateCameraPosition(); 
    }

    private void InputMethod()
    {
        // Check controller stick
        if (Input.GetAxis("Xaim") != 0 || Input.GetAxis("Yaim") != 0)
            controllerStick = true;
        // Check mouse
        else if (Input.GetAxis("Xmouse") != 0 || Input.GetAxis("Ymouse") != 0)
            controllerStick = false;
    }

    Vector3 CaptureInputPos()
    {
        Vector2 ret = Vector2.zero;
        // Mouse input.
        if (!controllerStick)
        {
            // Get the raw mouse position.
            ret = Camera.main.ScreenToViewportPoint(Input.mousePosition);

            // Center it around the world, (0, 0).
            ret *= 2;
            ret -= Vector2.one;

            // Smooth edges
            if (ret.magnitude > 1f)
                ret = ret.normalized;            
        }
        // Controller stick input.
        else if(controllerStick)
        {
            // Get the raw input vector, It is always centered and with smooth edges.
            ret.x = Input.GetAxis("Xaim");
            ret.y = Input.GetAxis("Yaim");
        }
        return ret;
    }

    //Vector3 UpdateShake()
    //{
    //    if (!shaking || Time.time > shakeTimeEnd)
    //    {
    //        shaking = false; //set shaking false when the shake time is up
    //        return Vector3.zero; //return zero so that it won't effect the target
    //    }
    //    Vector3 tempOffset = shakeVector;
    //    tempOffset *= shakeMag; //find out how far to shake, in what direction
    //    return tempOffset;
    //}

    Vector3 UpdateTargetPos()
    {
        // Multiply input vector by distance scalar.
        Vector3 inputOffset = inputPos * cameraDist;

        // Center the position around the player
        Vector3 ret = player.position + inputOffset;

        //ret += shakeOffset; //add the screen shake vector to the target

        // Make sure camera stays at same Z coord.
        ret.z = transform.position.z;

        return ret;
    }

    void UpdateCameraPosition()
    {
        // Smoothly move towards the target.
        Vector3 tempPos = Vector3.SmoothDamp(transform.position, target, ref refVel, smoothTime);
        // Update the position.
        transform.position = tempPos; 
    }

    //public void Shake(Vector3 direction, float magnitude, float length)
    //{ //capture values set for where it's called
    //    shaking = true; //to know whether it's shaking
    //    shakeVector = direction; //direction to shake towards
    //    shakeMag = -magnitude; //how far in that direction
    //    shakeTimeEnd = Time.time + length; //how long to shake
    //}
}
