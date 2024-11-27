using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

public class ForwardJoystick : MonoBehaviour
{
    public Action<float> forwardAction;
    public Action<float> backwardAction;
    public Transform JoystickRod;
    public OneGrabRotateTransformer oneGrabRotateTransformer;
    private Vector3 initialEulerRotation;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = JoystickRod.rotation;
        initialEulerRotation = JoystickRod.eulerAngles;
        initialPosition = JoystickRod.position;
    }

    private void FixedUpdate()
    {
        MakeMoveRod();
    }

    public void MakeMoveRod()
    {
        float eulerAnglesX = JoystickRod.eulerAngles.x;

        if (eulerAnglesX > 5 && eulerAnglesX <= oneGrabRotateTransformer.Constraints.MaxAngle.Value)
        {
            forwardAction?.Invoke(eulerAnglesX / oneGrabRotateTransformer.Constraints.MaxAngle.Value);
        }
        else if (eulerAnglesX < 355 && eulerAnglesX >= 360+oneGrabRotateTransformer.Constraints.MinAngle.Value)
        {
            backwardAction?.Invoke((360 - eulerAnglesX) / (oneGrabRotateTransformer.Constraints.MinAngle.Value));
            Debug.Log("burda" + oneGrabRotateTransformer.Constraints.MinAngle.Value);
        }
    }
}