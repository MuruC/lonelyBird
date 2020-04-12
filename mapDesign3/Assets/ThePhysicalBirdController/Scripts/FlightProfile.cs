using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Data", menuName = "FlightControl/FlightProfile", order = 1)]
public class FlightProfile : ScriptableObject
{
    [Header("General Settings")]
    [Tooltip("bird's mass in kg")]
    public float Mass = 0.15f;
    [Tooltip("1 means the bird flap animation is played in original time, a smaller value means animation is played in less time, a bigger value makes the animation play slower")]
    public float FlapDuration = 0.4f;
    [Tooltip("makes the bird slow down in local x, y or z (forward) direction. for x and y , a non zero value is recommended, otherwise the bird is somewhat not controllable. in z direction 0 (or sth very small) is almost always the right value.")]
    public Vector3 DragCoefficients = new Vector3(1,1,0);
    [Tooltip("makes the bird experience a lift (acceleration in upwards direction) when flying forward. ")]
    public float LiftCoefficient = 6.0f;
    [Tooltip("force that makes the bird accelerate in forward direction when flapping.")]
    public float ForwardForce = 5.0f;
    [Tooltip("force that makes the bird accelerate in upwards direction when flapping.")]
    public float UpwardsForce = 1.0f;
    [Tooltip("how agilely can the bird  roll around the forward axis")]
    public float RollTorque = 0.01f;
    [Tooltip("how agilely can the bird  roll around the horizontal axis")]
    public float PitchTorque = 0.002f;
    [Tooltip("how agelely can the bird  roll around the vertical axis")]
    public float YawTorque = 0.1f;
    [Tooltip("how far can the bird  roll around its forward axis")]
    public float RollMaxAngle = 35;
    [Tooltip("how far can the bird pitch down/up")]
    public float PitchMaxAngle = 30;
    [Tooltip("slows down rotational movement, increases control")]
    public float AngularDrag = 6;
    [Tooltip("slows down linear movement, 0 is a valid value (prefer to use DragCoefficients above)")]
    public float LinearDrag = 0;
    [Tooltip("defines how fast the bird can move in total")]
    public float SpeedLimit = 11;
    [Tooltip("defines in local coordinates where to apply the flap, drag and the uplift forces")]
    public Vector3 ApplyForcesAt = new Vector3(0,0,0);


    [Header("Landing")]
    [Tooltip("when the bird is faster than this speed during landing, it will crash instead (-> ragdoll)")]
    public float MaxValidLandingVelocity = 2.0f;
    [Tooltip("when the surface on which the bird tries to land is steeper than the given angle (in degrees), it will crash instead (-> ragdoll) ")]
    public float MaxValidSlope = 20.0f;
    [Tooltip("y offset applied so that the model doesnt sink into the ground when standing")]
    public float StandingYOffset = 0.1f;


    [Header("Take Off")]
    [Tooltip("defines how the bird hops off when starting (applied as impulse). ")]
    public Vector3 StartHop = new Vector3(0, 0.8f, 0);
    [Tooltip("should the bird have stronger flap force when taking off? 1 means no change")]
    public float TakeOffForceFactor = 1.5f;

    [Header("Stationary Flight")]
    [Tooltip("maximum movement speeds during stationary flight")]
    public Vector3 StraveSpeed = new Vector3(1, 3, 3);


}
