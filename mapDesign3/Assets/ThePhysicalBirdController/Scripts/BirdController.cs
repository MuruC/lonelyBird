using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.AI;

//implements flight physics and applies input values
public class BirdController : MonoBehaviour {

    public const float angleOfAttack = 0.1f; //used in lift calculation

    [Tooltip("the animator that animates the flight character")]
    public Animator WingAnimator;
    [Tooltip("configuration that defines the flight behaviour")]
    public FlightProfile Profile;
    [Tooltip("input variable that contains the z rotation")]
    public FloatValue RollAxisInput;
    [Tooltip("input variable that contains the x rotation")]
    public FloatValue PitchAxisInput;
    [Tooltip("input variable that contains the flap command value")]
    public FloatValue FlapInput;
    [Tooltip("input variable that contains the interact command value")]
    public FloatValue RushInput;
    [Tooltip("the ragdoll setup for this model, will only be enabled when a collision (crash) happens")]
    public GameObject RagdollModel;

    float fPitchAxisInput = 0.0f;

    Camera cam;

    public Interactable focus;

    public string stateName;

    public bool isSteadyFlight
    {
        get
        {
            return !(state is Standing || state is TakeOff || state is Crash || state is StationaryFlight || Vector3.Dot(body.velocity, transform.forward) < Mathf.Epsilon );
        }
    }
    private Rigidbody body;
    //state machine implementation
    private BirdState state;
    private void Start()
    {
        if(RagdollModel)
            RagdollModel.gameObject.SetActive(false);
        body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.mass = Profile.Mass;
        body.drag = Profile.LinearDrag;
        body.angularDrag = Profile.AngularDrag;
        state = new Standing(this);
        cam = Camera.main;
    }
    
    private void FixedUpdate()
    {
        if (state == null)
        {
            return;
        }
        state.Update();
        var tmpState = state.getNextState();
        //if (tmpState != state)
        //    Debug.Log("switch to state " + tmpState.toString());
        state = tmpState;
        //Debug.Log(state);
        stateName = state.toString();
    }

    public void OnCollisionStay(Collision collision)
    {
        state.OnCollisionStay(collision);
    }
    public void OnCollisionEnter(Collision col)
    {
        state.OnCollisionEnter(col);

    }
    public void applyTakeOffImpulse()
    {
        state.applyTakeOffImpulse();
    }

    private void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            // We create a ray
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // If the ray hits
            if (Physics.Raycast(ray, out hit, 100))
            {
                /*
                float distance = Vector3.Distance(transform.position,hit.transform.position);
                if (distance >= 20)
                {
                    return;
                }
                */
                Interactable interactable = hit.collider.GetComponent<Interactable>();
                if (interactable != null)
                {
                    SetFocus(interactable);
                }
                else
                {
                    RemoveFocus();
                }
            }
        }
    }

    void SetFocus(Interactable newFocus) {
        if (newFocus != focus) {
            if (focus != null)
            {
                focus.OnDefocused();
            }
   
            focus = newFocus;
        }     
        newFocus.OnFocused(transform);
    }

    void RemoveFocus() {

        if (focus != null)
        {
            focus.OnDefocused();
        }

        focus = null;
    }
}

//parent state class, in each state define specific flight behaviour 
public abstract class BirdState
{
    protected BirdController ctrlRef;
    protected Rigidbody body;

    public BirdState(BirdController c)
    {
        ctrlRef = c;
        body = c.GetComponent<Rigidbody>();
    }
    public abstract string toString();
    protected abstract void update();
    public abstract BirdState getNextState();
    public void Update()
    {
        update();
    }
    public virtual void OnCollisionStay(Collision col)
    {

    }
    public virtual void OnCollisionEnter(Collision col)
    {

    }

    public virtual void applyTakeOffImpulse()
    {

    }
}

public abstract class CrashAwareBirdState : BirdState
{
    private const int maxContactPointCount = 50; //max count of contact points to consider on collision
    protected bool isCrash; // true if the collision should result in a crash, i.e. ragdoll
    protected bool isLand; // or in a landing animation
    protected Collision crashData;
    protected ContactPoint[] ctctPts;
    public CrashAwareBirdState(BirdController b) : base(b) {
        isCrash = false;
        isLand = false;
        crashData = null;
        ctctPts = new ContactPoint[maxContactPointCount];
    }
    public override void OnCollisionEnter(Collision col)
    {
        isCrash = col.relativeVelocity.magnitude > ctrlRef.Profile.MaxValidLandingVelocity;
        isLand = !isCrash;
        crashData = col;
        col.GetContacts(ctctPts);
        Vector3 normal = Vector3.zero;
        for (int i = 0; i < col.contactCount; i++)
            normal += ctctPts[i].normal;
        normal.Normalize();
        if (isLand && Vector3.Angle(normal, Vector3.up) > ctrlRef.Profile.MaxValidSlope) {
            isLand = false;
            isCrash = true;
        }
    }
    public override void OnCollisionStay(Collision col)
    {
       if(!isCrash && !isLand)
        {
            OnCollisionEnter(col);
        }
    }
}

public class Standing : BirdState
{
    private const int maxContactPointCount = 50; //max count of contact points to consider on collision

    private const float lerpToTarget = 0.1f;
    private const float distToTargetUntilKinematic = 0.05f;
    private const float slerpToTarget = 0.7f;
    //dont apply initial input, only from point in time when it starts to change (while this state is active)
    private bool flapValChangedSinceStart;
    private float initialFlapValue;
    private Vector3 target;
    private Vector3 targetUp;
    protected ContactPoint[] ctctPts;
    private bool tookCollisionInfo;
    public Standing(BirdController c) : base(c) {
        tookCollisionInfo = false;
        ctctPts = new ContactPoint[maxContactPointCount];
        initialFlapValue = c.FlapInput.Value;
        flapValChangedSinceStart = false;
        ctrlRef.WingAnimator.SetFloat("FlapSpeed", 1.0f / ctrlRef.Profile.FlapDuration);
        ctrlRef.WingAnimator.SetFloat("Land", 1.0f);
        ctrlRef.WingAnimator.SetFloat("Flap", 0.0f);
    }
    public override BirdState getNextState()
    {
        if (ctrlRef.FlapInput.Value > 0.0f && flapValChangedSinceStart)
        {
            ctrlRef.WingAnimator.SetFloat("Land", 0.0f);
            ctrlRef.WingAnimator.SetFloat("Flap", 0.0f);
            ctrlRef.WingAnimator.SetFloat("Rush", 0.0f);
            return new TakeOff(ctrlRef);
        }
        return this;
        
    }

    public override void OnCollisionStay(Collision col)
    {
        if(!tookCollisionInfo)
        {

            col.GetContacts(ctctPts);
            targetUp = Vector3.up; //it seems the best to just use global up
           
            target = col.collider.ClosestPoint(ctrlRef.transform.position) ;
            tookCollisionInfo = true;
            body.isKinematic = true;
        }

    }


    public override string toString()
    {
        return "Standing";
    }
    protected override void update()
    {
        if (ctrlRef.FlapInput.Value != initialFlapValue)
            flapValChangedSinceStart = true;

        if (tookCollisionInfo)
        {
            Quaternion lerpRotation =
            Quaternion.FromToRotation(ctrlRef.transform.up, targetUp) *
                Quaternion.FromToRotation(ctrlRef.transform.forward, Quaternion.AngleAxis(-ctrlRef.RollAxisInput.Value, Vector3.up) * ctrlRef.transform.forward) *
                ctrlRef.transform.rotation;


            ctrlRef.transform.rotation = Quaternion.Slerp(ctrlRef.transform.rotation, lerpRotation, slerpToTarget);
            ctrlRef.transform.position = Vector3.Lerp(ctrlRef.transform.position, target + ctrlRef.Profile.StandingYOffset * targetUp, lerpToTarget);
        }
    }
}

public class Flying : CrashAwareBirdState
{
    private const float timeUntilNoseDiveDetected = 0.3f;
    private float noseDiveTimer;
    private const float timeUntilNewInputAccepted = 0.5f;
    private float newInputTimer;
    //to make steering easier (hiting left performs a left curve without pitching up or down, apply the yaw torque mixed between world up and local up
    private const float shareApplyYawToWorldUp = 0.4f;
    
    //current torques
    protected float yawTorque;
    protected float pitchTorque;
    protected float rollTorque;

    float pitchAxisInput_;
    public Flying(BirdController c) : base(c) {
        noseDiveTimer = 0.0f;
        newInputTimer = 0.0f;
    }
    public override BirdState getNextState()
    {
        if (isCrash)
            return new Crash(ctrlRef,crashData);
        if (isLand)
        {
            return new Standing(ctrlRef);
        }

        //break if not accepting new input
        if (newInputTimer < timeUntilNewInputAccepted)
            return this;

        if (Input.GetAxis("Hanging") > 0.1f)
            return new StationaryFlight(ctrlRef);
        if (Input.GetAxis("Dive") > 0.1f)
        {
            if (noseDiveTimer > timeUntilNoseDiveDetected)
                return new NoseDive(ctrlRef);
        }
        else
            noseDiveTimer = 0.0f;
      //  Debug.Log("Dive: " + Input.GetAxis("Dive"));
        
        return this;
    }
    public override string toString()
    {
        return "Flying";
    }
    protected virtual void calculateNewTorques()
    {
       
        if (Input.GetKey(KeyCode.Space))
        {
            pitchAxisInput_ = -1.0f;
           
        }
        else {
            pitchAxisInput_ = ctrlRef.PitchAxisInput.Value;
        }
      
       // pitchAxisInput_ = -1.0f;
        float rollTarget = ctrlRef.RollAxisInput.Value * ctrlRef.Profile.RollMaxAngle;
        float pitchTarget = pitchAxisInput_ * ctrlRef.Profile.PitchMaxAngle;
        Vector3 currentHorizontalForward = Vector3.ProjectOnPlane(ctrlRef.transform.rotation * Vector3.forward, Vector3.up).normalized;
        Vector3 currentHorizontalRight = Vector3.ProjectOnPlane(ctrlRef.transform.rotation * Vector3.right, Vector3.up).normalized;
        float currentPitch = Vector3.SignedAngle(currentHorizontalForward, ctrlRef.transform.forward, currentHorizontalRight);
        float currentRoll = Vector3.SignedAngle(Vector3.up, ctrlRef.transform.up, currentHorizontalForward);
        rollTorque = (rollTarget - currentRoll) * ctrlRef.Profile.RollTorque;
        pitchTorque = (pitchTarget - currentPitch) * ctrlRef.Profile.PitchTorque;
        yawTorque = -ctrlRef.RollAxisInput.Value * ctrlRef.Profile.YawTorque;
    }
    protected virtual void updateAnimatorParameters()
    {
        ctrlRef.WingAnimator.SetFloat("FlapSpeed", 1.0f / ctrlRef.Profile.FlapDuration);

        //break if not accepting new input
        if (newInputTimer < timeUntilNewInputAccepted)
        {
            ctrlRef.WingAnimator.SetFloat("Flap", 0.0f);
            ctrlRef.WingAnimator.SetFloat("Rush", 0.0f);
        }
        else
        {
            ctrlRef.WingAnimator.SetFloat("Flap", ctrlRef.FlapInput.Value);
            pitchAxisInput_ = -1;
            if (noseDiveTimer > timeUntilNoseDiveDetected)
                ctrlRef.WingAnimator.SetFloat("Rush", ctrlRef.RushInput.Value);
        }
    }
    protected virtual Vector3 calculateFlapForce()
    {
        if (newInputTimer < timeUntilNewInputAccepted) //some flap from another state, dont accept as force
        {
            return Vector3.zero;
        }

        float normalizedForce = ctrlRef.WingAnimator.GetFloat("NormalizedForce");
        Vector3 flapUpForce = ctrlRef.transform.up * Mathf.Max(0.0f, normalizedForce) * ctrlRef.Profile.UpwardsForce + ctrlRef.transform.forward * Mathf.Max(0.0f, normalizedForce) * ctrlRef.Profile.ForwardForce;

        //prevent pushing the body 'backwards' (project to global up instead)
        Vector3 planeNormal = ctrlRef.transform.forward;
        planeNormal.y = 0.0f;
        planeNormal.Normalize();
        if (Vector3.Dot(planeNormal, flapUpForce.normalized) < 0.0) //flapUpForce looks 'backwards'
        {
            var projectedUp = Vector3.Cross(planeNormal, ctrlRef.transform.right);
            flapUpForce = Vector3.Dot(flapUpForce, projectedUp) * projectedUp;
        }
        return flapUpForce;
    }
    protected override void update()
    {
        noseDiveTimer += Time.fixedDeltaTime;
        newInputTimer += Time.fixedDeltaTime;

        calculateNewTorques();
        updateAnimatorParameters();

        body.AddRelativeTorque(new Vector3(pitchTorque, (1.0f - shareApplyYawToWorldUp) * yawTorque, rollTorque));
        body.AddTorque(new Vector3(0.0f, shareApplyYawToWorldUp * yawTorque, 0.0f));

        Quaternion newRollPitchModelRotation = ctrlRef.transform.rotation;
        Vector3 RollPitchModelUp = (newRollPitchModelRotation * Vector3.up).normalized;
        Vector3 RollPitchModelRight = (newRollPitchModelRotation * Vector3.right).normalized;
        Vector3 RollPitchModelForward = (newRollPitchModelRotation * Vector3.forward).normalized;
        float normalizedGlideArea = ctrlRef.WingAnimator.GetFloat("NormalizedGlideArea");

        Vector3 flapUpForce = calculateFlapForce();
        
        //drag projections
        Vector3 velocityToUpProjectionForce = Vector3.Dot(body.velocity, RollPitchModelForward) > 0.0 ? -ctrlRef.Profile.DragCoefficients.y * normalizedGlideArea * Vector3.Project(body.velocity, RollPitchModelUp) : Vector3.zero;
        Vector3 velocityToForwardProjectionForce = -ctrlRef.Profile.DragCoefficients.z * Vector3.Project(body.velocity, RollPitchModelForward);
        Vector3 velocityToRightProjectionForce = -ctrlRef.Profile.DragCoefficients.x * Vector3.Project(body.velocity, RollPitchModelRight);

        Vector3 dragForce = velocityToUpProjectionForce + velocityToForwardProjectionForce + velocityToRightProjectionForce;

        Plane perpendicularToVelocity = new Plane(RollPitchModelForward, ctrlRef.transform.position);
        bool forceApplicationPositionSign = perpendicularToVelocity.GetSide(ctrlRef.transform.position + body.velocity);

        Vector3 upliftForce = Vector3.zero;
        //only apply uplift if body is moving, and apply physics laws
        if (body.velocity.magnitude > Mathf.Epsilon)
        {
            float uplift = ctrlRef.Profile.LiftCoefficient * (1.2f / 2.0f) * body.velocity.sqrMagnitude * BirdController.angleOfAttack * (-Vector3.Dot(body.velocity.normalized, RollPitchModelUp)) * Mathf.Clamp01(Vector3.Dot(body.velocity.normalized, RollPitchModelForward));
            upliftForce = Vector3.Cross(body.velocity.normalized, RollPitchModelRight) * uplift * normalizedGlideArea;
            //dont apply this for the moment
            //float upliftResistance = Profile.LiftCoefficients.y * (1.2f / 2.0f) * body.velocity.sqrMagnitude * (1.0f - angleOfAttack) * Mathf.Clamp01(Vector3.Dot(body.velocity.normalized, RollPitchModelForward));
            //upliftForce -= body.velocity.normalized * upliftResistance;
        }
        Debug.DrawLine(ctrlRef.transform.position, ctrlRef.transform.position + upliftForce, Color.blue);

        body.AddForceAtPosition(dragForce, ctrlRef.transform.position, ForceMode.Force);
        body.AddForceAtPosition(flapUpForce + upliftForce,
        ctrlRef.transform.position
        + ctrlRef.transform.up * ctrlRef.Profile.ApplyForcesAt.y
        + (forceApplicationPositionSign ? 1.0f : -1.0f) * ctrlRef.transform.forward * ctrlRef.Profile.ApplyForcesAt.z,
        ForceMode.Force);

        //clamp velocity
        body.velocity = Vector3.ClampMagnitude(body.velocity, ctrlRef.Profile.SpeedLimit);
    }

    
}

public class TakeOff : Flying
{
    private const float takeOffFlapSpeedFactor = 1.0f; //flap faster when taking off
    private const float takeOffPitchMultiplicator = 1.2f; //fly steeper upwards
    private const float takeOffTimeLimit = 1.5f; // define how many seconds the take off lasts
    private float takeOffTime; //current accumulated takeoff time
    private bool gaveImpulse;
    public TakeOff(BirdController c) : base(c) {
        takeOffTime = 0.0f;
        body.isKinematic = false;
        ctrlRef.WingAnimator.SetFloat("Land", 1.0f);
        

    }
    public override BirdState getNextState()
    {
        if (takeOffTime > takeOffTimeLimit)
        {
            ctrlRef.WingAnimator.SetFloat("Land", 0.0f);
            ctrlRef.WingAnimator.SetFloat("FlapSpeed", 1.0f / ctrlRef.Profile.FlapDuration);
            return new Flying(ctrlRef);
        }
        return this;
    }
    public override string toString()
    {
        return "TakeOff";
    }
    protected override void calculateNewTorques()
    {
        float rollTarget = ctrlRef.RollAxisInput.Value * ctrlRef.Profile.RollMaxAngle;
        float pitchTarget = 0.0f;

        if (gaveImpulse)
            pitchTarget = -ctrlRef.Profile.PitchMaxAngle * takeOffPitchMultiplicator;

        Vector3 currentHorizontalForward = Vector3.ProjectOnPlane(ctrlRef.transform.rotation * Vector3.forward, Vector3.up).normalized;
        Vector3 currentHorizontalRight = Vector3.ProjectOnPlane(ctrlRef.transform.rotation * Vector3.right, Vector3.up).normalized;
        float currentPitch = Vector3.SignedAngle(currentHorizontalForward, ctrlRef.transform.forward, currentHorizontalRight);
        float currentRoll = Vector3.SignedAngle(Vector3.up, ctrlRef.transform.up, currentHorizontalForward);
        rollTorque = (rollTarget - currentRoll) * ctrlRef.Profile.RollTorque;
        pitchTorque = (pitchTarget - currentPitch) * ctrlRef.Profile.PitchTorque;
        yawTorque = -ctrlRef.RollAxisInput.Value * ctrlRef.Profile.YawTorque;
    }
    protected override void updateAnimatorParameters()
    {
        ctrlRef.WingAnimator.SetFloat("Flap", 1.0f);
        ctrlRef.WingAnimator.SetFloat("FlapSpeed", takeOffFlapSpeedFactor / ctrlRef.Profile.FlapDuration);
    }
    protected override void update()
    {
        base.update();
        takeOffTime += Time.fixedDeltaTime;

    }
    protected override Vector3 calculateFlapForce()
    {
        float normalizedForce = ctrlRef.WingAnimator.GetFloat("NormalizedForce");
        Vector3 flapUpForce = ctrlRef.transform.up * Mathf.Max(0.0f, normalizedForce) * ctrlRef.Profile.UpwardsForce + ctrlRef.transform.forward * Mathf.Max(0.0f, normalizedForce) * ctrlRef.Profile.ForwardForce;
        flapUpForce *= ctrlRef.Profile.TakeOffForceFactor;
        Vector3 planeNormal = ctrlRef.transform.forward;
        planeNormal.y = 0.0f;
        planeNormal.Normalize();
        if (Vector3.Dot(planeNormal, flapUpForce.normalized) < 0.0) //flapUpForce looks 'backwards'
        {
            var projectedUp = Vector3.Cross(planeNormal, ctrlRef.transform.right);
            flapUpForce = Vector3.Dot(flapUpForce, projectedUp) * projectedUp;
        }
        return flapUpForce;
    }
    //hop event
    public override void applyTakeOffImpulse()
    {
        body.AddRelativeForce(ctrlRef.Profile.StartHop, ForceMode.Impulse);
        gaveImpulse = true;
    }
    public override void OnCollisionStay(Collision col)
    {
       //do nothing
    }
}



public class StationaryFlight : Flying
{
    private const float maxPitchMult = 2.0f;
    private const float pitchTorqueMult = 2.0f;
    private const float waitUntilLeave = 0.5f;
    private const float flapSpeedFactor = 1.0f; //flap faster when taking off
    private const float timeUntilNonPhysicsMode = 0.3f;
    private const float lerpStrength = 0.03f;
    private const float dragForceFactor = 0.1f;
    private float nonPhysicsModeTimer;
    private bool wantsLeaveState = false;
    private float leaveTimer;
    public StationaryFlight(BirdController c) : base(c) {
        leaveTimer = 0.0f;
        nonPhysicsModeTimer = 0.0f;
    }
    public override BirdState getNextState()
    {
        if (isCrash)
        {
            body.useGravity = true;
            return new Crash(ctrlRef, crashData);
        }
        if (isLand)
        {
            body.useGravity = true;
            return new Standing(ctrlRef);
        }
        wantsLeaveState = !(Input.GetAxis("Hanging") > 0.0f);
        if (wantsLeaveState)
        {
            leaveTimer += Time.fixedDeltaTime;
            if (leaveTimer > waitUntilLeave)
            {
                body.useGravity = true;
                return new Flying(ctrlRef);
            }
        }
        

        return this;
    }
    public override string toString()
    {
        return "StationaryFlight";
    }

    protected override void calculateNewTorques()
    {
        float rollTarget = 0.0f;
        float pitchTarget = wantsLeaveState ? ctrlRef.PitchAxisInput.Value * ctrlRef.Profile.PitchMaxAngle  : - ctrlRef.Profile.PitchMaxAngle * maxPitchMult;
        Vector3 currentHorizontalForward = Vector3.ProjectOnPlane(ctrlRef.transform.rotation * Vector3.forward, Vector3.up).normalized;
        Vector3 currentHorizontalRight = Vector3.ProjectOnPlane(ctrlRef.transform.rotation * Vector3.right, Vector3.up).normalized;
        float currentPitch = Vector3.SignedAngle(currentHorizontalForward, ctrlRef.transform.forward, currentHorizontalRight);
        float currentRoll = Vector3.SignedAngle(Vector3.up, ctrlRef.transform.up, currentHorizontalForward);
        rollTorque = (rollTarget - currentRoll) * ctrlRef.Profile.RollTorque;
        pitchTorque = (pitchTarget - currentPitch) * ctrlRef.Profile.PitchTorque * pitchTorqueMult;
        yawTorque = -ctrlRef.RollAxisInput.Value * ctrlRef.Profile.YawTorque;
    }
    protected override void updateAnimatorParameters()
    {
        ctrlRef.WingAnimator.SetFloat("FlapSpeed", flapSpeedFactor / ctrlRef.Profile.FlapDuration);
        ctrlRef.WingAnimator.SetFloat("Flap", 1.0f);
        ctrlRef.WingAnimator.SetFloat("Rush", 1.0f);
    }
    protected override Vector3 calculateFlapForce()
    {
        return Vector3.zero;
    }

    protected override void update()
    {
        nonPhysicsModeTimer += Time.fixedDeltaTime;

        
        calculateNewTorques();
        updateAnimatorParameters();
        body.AddRelativeTorque(new Vector3(pitchTorque, 0.0f, rollTorque));

        Quaternion newRollPitchModelRotation = ctrlRef.transform.rotation;
        Vector3 RollPitchModelUp = (newRollPitchModelRotation * Vector3.up).normalized;
        Vector3 RollPitchModelRight = (newRollPitchModelRotation * Vector3.right).normalized;
        Vector3 RollPitchModelForward = (newRollPitchModelRotation * Vector3.forward).normalized;
        float normalizedGlideArea = ctrlRef.WingAnimator.GetFloat("NormalizedGlideArea");


        //drag projections
        Vector3 velocityToUpProjectionForce = Vector3.Dot(body.velocity, RollPitchModelForward) > 0.0 ? -ctrlRef.Profile.DragCoefficients.y * normalizedGlideArea * Vector3.Project(body.velocity, RollPitchModelUp) : Vector3.zero;
        Vector3 velocityToForwardProjectionForce = -ctrlRef.Profile.DragCoefficients.z * Vector3.Project(body.velocity, RollPitchModelForward);
        Vector3 velocityToRightProjectionForce = -ctrlRef.Profile.DragCoefficients.x * Vector3.Project(body.velocity, RollPitchModelRight);
        Vector3 dragForce = velocityToUpProjectionForce + velocityToForwardProjectionForce + velocityToRightProjectionForce;

        if (nonPhysicsModeTimer > timeUntilNonPhysicsMode)
        {
            dragForce *= dragForceFactor;
            body.useGravity = false;
            //calculate forward backward strave
            Vector3 currentHorizontalForward = Vector3.ProjectOnPlane(ctrlRef.transform.rotation * Vector3.forward, Vector3.up).normalized;
            body.velocity = Vector3.Lerp(body.velocity, 
                ctrlRef.transform.forward * ctrlRef.PitchAxisInput.Value * ctrlRef.Profile.StraveSpeed.y
                - ctrlRef.transform.right * ctrlRef.RollAxisInput.Value * ctrlRef.Profile.StraveSpeed.x
                - ctrlRef.transform.up * ctrlRef.Profile.StraveSpeed.z, lerpStrength);

        } else
        {
            body.AddTorque(new Vector3(0.0f, yawTorque, 0.0f));

        }
        body.AddForceAtPosition(dragForce, ctrlRef.transform.position, ForceMode.Force);

    }
    
}

public class NoseDive : Flying
{
    private const float rotationDamp = 0.4f; //damps the torques because body is falling, has less control
    private const float pitchFactor = 1.7f; // defines how steep the nose dive is
    public NoseDive(BirdController c) : base(c) {  }

    protected override void updateAnimatorParameters()
    {
        ctrlRef.WingAnimator.SetFloat("FlapSpeed", 1.0f / ctrlRef.Profile.FlapDuration);
        ctrlRef.WingAnimator.SetFloat("Flap", ctrlRef.FlapInput.Value);
        ctrlRef.WingAnimator.SetFloat("Rush", Input.GetAxis("Dive"));
    }
    protected override Vector3 calculateFlapForce()
    {
        return Vector3.zero;
    }
    public override BirdState getNextState()
    {
        if (isCrash)
            return new Crash(ctrlRef, crashData);
        if (ctrlRef.FlapInput.Value > 0.1f)
            return new StationaryFlight(ctrlRef);
        if (Input.GetAxis("Dive") < 0.1f)
            return new Flying(ctrlRef);
        if (isLand)
        {

            return new Standing(ctrlRef);
        }
        return this;
    }
    protected override void calculateNewTorques()
    {
        float rollTarget = ctrlRef.RollAxisInput.Value * rotationDamp * ctrlRef.Profile.RollMaxAngle;
        float pitchTarget = ctrlRef.Profile.PitchMaxAngle * pitchFactor + (rotationDamp * ctrlRef.PitchAxisInput.Value * ctrlRef.Profile.PitchMaxAngle);
        Vector3 currentHorizontalForward = Vector3.ProjectOnPlane(ctrlRef.transform.rotation * Vector3.forward, Vector3.up).normalized;
        Vector3 currentHorizontalRight = Vector3.ProjectOnPlane(ctrlRef.transform.rotation * Vector3.right, Vector3.up).normalized;
        float currentPitch = Vector3.SignedAngle(currentHorizontalForward, ctrlRef.transform.forward, currentHorizontalRight);
        float currentRoll = Vector3.SignedAngle(Vector3.up, ctrlRef.transform.up, currentHorizontalForward);
        rollTorque = (rollTarget - currentRoll) * ctrlRef.Profile.RollTorque;
        pitchTorque = (pitchTarget - currentPitch) * ctrlRef.Profile.PitchTorque;
        yawTorque = -ctrlRef.RollAxisInput.Value * rotationDamp * ctrlRef.Profile.YawTorque;
    }
    public override string toString()
    {
        return "Nosediving";
    }
   
}

public class Crash : BirdState
{
    private const float restCountdown = 1.0f;
    private const float rotateBackCountown = 2.0f;
    private const float restVelocity = 0.1f;
    private bool hasRagdoll;
    private Renderer[] models;
    private Rigidbody[] ragdollRigidbodies;
    private GameObject ragdollInstance;
    private float restTimer;
    private Collision collision;
    public Crash(BirdController c, Collision crashData) :base(c)
    {
        collision = crashData;
        restTimer = 0.0f;
        hasRagdoll = c.RagdollModel;
        if (hasRagdoll) //disable other physics
        {
            ragdollInstance = GameObject.Instantiate(c.RagdollModel);
            ragdollRigidbodies = ragdollInstance.GetComponentsInChildren<Rigidbody>();
            foreach (var r in ragdollRigidbodies)
            {
                r.velocity = (crashData.rigidbody == null ? Vector3.zero : crashData.rigidbody.velocity) - crashData.impulse - crashData.relativeVelocity;
                //r.angularVelocity = body.angularVelocity;
            }
            body.isKinematic = true;
            var cs = body.GetComponents<Collider>();
            foreach (var col in cs)
                col.isTrigger = true;
            //we want to copy the animator pose
            ragdollInstance.transform.position = c.WingAnimator.transform.position;
            ragdollInstance.transform.rotation = c.WingAnimator.transform.rotation;
            ragdollInstance.gameObject.SetActive(true);

            ctrlRef.transform.SetParent(ragdollInstance.transform.Find("Spine"), true);

            models = c.WingAnimator.gameObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in models)
                renderer.enabled = false;
            

        }
    }

    public override BirdState getNextState()
    {

        if (restTimer <= restCountdown)
        {
            Vector3 meanV = Vector3.zero;
            foreach (var r in ragdollRigidbodies)
            {
                meanV += r.velocity;
            }
            meanV /= ragdollRigidbodies.Length;
            if (meanV.magnitude < restVelocity)
            {
                restTimer += Time.fixedDeltaTime;
            }
            else
                restTimer = 0.0f; //reset
        } else { 
            if (hasRagdoll) //enable back other physics
            {
                ctrlRef.transform.SetParent(null,true);
                ragdollInstance.gameObject.SetActive(false);
                GameObject.Destroy(ragdollInstance);
                var cs = body.GetComponents<Collider>();
                foreach (var col in cs)
                    col.isTrigger = false;
                foreach (var renderer in models)
                    renderer.enabled = true;
                hasRagdoll = false;
            }
            restTimer += Time.fixedDeltaTime;
            //now lerp back to up
            ctrlRef.transform.rotation = Quaternion.Slerp(ctrlRef.transform.rotation, Quaternion.Euler(0.0f, ctrlRef.transform.eulerAngles.y, 0.0f), (rotateBackCountown - restCountdown) * Time.fixedDeltaTime);
            if (restTimer > rotateBackCountown)
            {
                body.isKinematic = false;
                return new Standing(ctrlRef);
            }
        }
        return this;
    }
    
    public override string toString()
    {
        return "Crashing";
    }

    protected override void update()
    {
        ctrlRef.WingAnimator.SetFloat("Flap", 0.0f);
        ctrlRef.WingAnimator.SetFloat("Rush", 0.0f);
        ctrlRef.WingAnimator.SetFloat("Land", 1.0f);
    }
}