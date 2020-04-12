using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FlightCam : MonoBehaviour {
    [Tooltip("a reference to the Transform this flight camera should follow")]
    public Transform Follow;
    [Tooltip("based on the speed, the camera's fov will be changed and interpolated by these two values (in degrees)")]
    public float fovMin = 55.0f;
    public float fovMax = 80.0f;
    [Tooltip("based on this value, the camera will be following the object quickly/slowly")]
    public float followspeed = 0.001f;

    [Tooltip("controls the orbit/rotation of the camera around the followed object")]
    public FloatValue OrbitX;
    [Tooltip("controls the orbit/rotation of the camera around the followed object")]
    public FloatValue OrbitY;
    [Tooltip("controls the translation offset of the camera from the followed object")]
    public FloatValue OffsetZ;
    [Tooltip("controls the translation offset of the camera from the followed object")]
    public FloatValue OffsetY;


    private const float userLineOfVisionMaxX = 45.0f;
    private const float userLineOfVisionMaxY = 45.0f;
    private const float userLineOfVisionSlerpT = 0.1f;
    private const int trajectoryControlPointCount = 50;
    private const int dampYWindow = 25;
    private const float updateAtVelocity = 1.0f; //camera is only moved if the body it follows moves faster than this value

    private Vector3[] trajectoryPositions;
    private Vector3[] trajectoryVelocities;
    private Quaternion[] trajectoryRotations;
    private int currentTrajectorIndex;
    private Vector2 userLineOfVisionAngles;
   
    private Vector3 originalOffset;
    private Quaternion originalRotOffset;
    private float maxBodySpeed;
    
    private float r;
    private Rigidbody followBody;
    private BirdController followCtrl;
    private Camera cam;
    private float transitionLerp = 0.0f;
    private bool isDynamicCam = false;
    void Start () {
        trajectoryPositions = new Vector3[trajectoryControlPointCount];
        trajectoryRotations = new Quaternion[trajectoryControlPointCount];
        trajectoryVelocities = new Vector3[trajectoryControlPointCount];
        for (int i = 0; i < trajectoryControlPointCount; i++)
        {
            trajectoryPositions[i] = Follow.position;
            trajectoryRotations[i] = Follow.rotation;
            trajectoryVelocities[i] = Vector3.zero;
        }
        currentTrajectorIndex = 0;
        originalOffset =  transform.position - Follow.position;
        originalOffset = Quaternion.Inverse(Follow.rotation) * originalOffset;
        originalRotOffset = Quaternion.Inverse(Follow.rotation) * transform.rotation;
        r = originalOffset.magnitude;
        cam = GetComponent<Camera>();
        maxBodySpeed = Follow.GetComponent<BirdController>().Profile.SpeedLimit;
        followCtrl = Follow.GetComponentInChildren<BirdController>();
        followCtrl = followCtrl == null ? Follow.GetComponentInParent<BirdController>() : followCtrl;
    }

    void FixedUpdate() {
        if (Input.GetMouseButton(1))
        {
            userLineOfVisionAngles += new Vector2(OrbitX.Value, OrbitY.Value);
        
        }
        
        transitionLerp = Mathf.Clamp01(transitionLerp + Time.fixedDeltaTime);
        //userLineOfVisionAngles.y = Mathf.Clamp(userLineOfVisionAngles.y, -userLineOfVisionMaxY, userLineOfVisionMaxY);
        //userLineOfVisionAngles.x = Mathf.Clamp(userLineOfVisionAngles.x, -userLineOfVisionMaxX, userLineOfVisionMaxX);
        Vector3 lerpFromPoint = Vector3.zero;
        Vector3 lookAtPoint = Vector3.zero;
        if(!followBody)
            followBody = Follow.GetComponent<Rigidbody>();
        if (!followCtrl)
        {
            followCtrl = Follow.GetComponentInChildren<BirdController>();
            followCtrl = followCtrl == null ? Follow.GetComponentInParent<BirdController>() : followCtrl;
        }
        if (followCtrl != null && !followCtrl.isSteadyFlight)//static cam
        {
            Vector3 flatForward = Follow.forward;
            flatForward.y = 0.0f;
            flatForward.Normalize();
            lookAtPoint = Follow.position;
            lerpFromPoint = Follow.position + flatForward * OffsetZ.Value + Vector3.up * OffsetY.Value;

            trajectoryPositions[currentTrajectorIndex] = Follow.position;
            trajectoryRotations[currentTrajectorIndex] = Follow.rotation;
            trajectoryVelocities[currentTrajectorIndex] = followBody.velocity;
            currentTrajectorIndex = (currentTrajectorIndex + 1) % trajectoryControlPointCount;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Mathf.Lerp(fovMin, fovMax, (followBody.velocity.magnitude / maxBodySpeed)), Time.fixedDeltaTime  / followspeed);
            if (isDynamicCam)
            {
                isDynamicCam = false;
                transitionLerp = 0.0f;
            }

        }
        else //flight cam
        {
            if (followBody.velocity.magnitude > updateAtVelocity)
            {
                trajectoryPositions[currentTrajectorIndex] = Follow.position;
                trajectoryRotations[currentTrajectorIndex] = Follow.rotation;
                trajectoryVelocities[currentTrajectorIndex] = followBody.velocity;
            }
            //line sphere intersection
            Vector3 p = Vector3.zero;
            Vector3 u = Vector3.one;
            Vector3 c = Follow.position;
            float d = 0.0f;
            bool foundValidPos = false;
            Vector3 final = Vector3.zero;
            //find trajectory part that intersects with original offset sphere
            for (int i = 0; i < trajectoryControlPointCount && !foundValidPos; i++)
            {
                if (currentTrajectorIndex != i)
                {
                    p = trajectoryPositions[i];
                    u = trajectoryPositions[(i + 1) % trajectoryControlPointCount] - p;
                    Debug.DrawLine(trajectoryPositions[i], trajectoryPositions[(i + 1) % trajectoryControlPointCount]);
                    float lineLength = u.magnitude;
                    if (lineLength > Mathf.Epsilon)
                    { //valid line
                        u.Normalize();
                        float alpha = -Vector3.Dot((p - c), u);
                        Vector3 q = p + alpha * u;
                        float dist = Vector3.Distance(q, c);
                        if (dist < r && !foundValidPos)
                        {
                            float x = Mathf.Sqrt(r * r - dist * dist);
                            if (alpha >= x && alpha - x > 0.0f && alpha - x < lineLength)
                            {
                                foundValidPos = true;
                                d = alpha - x;
                                final = p + d * u;
                                final += Follow.rotation * (Vector3.up * originalOffset.y);
                            }
                        }
                    }
                }
            }

            if (foundValidPos)
            {
                float dampedY = 0.0f;
                Vector3 dampedForward = Vector3.zero;
                float dampedSpeed = 0.0f;
                int n = 0;
                for (int j = currentTrajectorIndex - dampYWindow < 0 ? trajectoryControlPointCount + currentTrajectorIndex - dampYWindow : currentTrajectorIndex - dampYWindow
                    ; j % trajectoryControlPointCount != (currentTrajectorIndex + 1) % trajectoryControlPointCount; j = (j + 1) % trajectoryControlPointCount)
                {
                    int lastJ = j - 1 < 0 ? trajectoryControlPointCount + j - 1 : j - 1;
                    lastJ = lastJ % trajectoryControlPointCount;

                    dampedForward += (trajectoryPositions[j] - trajectoryPositions[lastJ]).normalized;
                    Debug.DrawLine(trajectoryPositions[j], trajectoryPositions[lastJ], Color.red);
                    dampedY += trajectoryPositions[j].y;
                    dampedSpeed += trajectoryVelocities[j].magnitude;
                    n++;
                }
                if (dampedForward.magnitude > Mathf.Epsilon)
                    dampedForward.Normalize();
                dampedY /= (float)(n == 0 ? 1 : n);
                dampedSpeed /= (float)(n == 0 ? 1 : n);
                


                final.y = Mathf.Lerp(final.y, dampedY, 0.1f);

                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Mathf.Lerp(fovMin , fovMax, (dampedSpeed / maxBodySpeed)) , (dampedSpeed * transitionLerp) );

                Vector3 flatForward = dampedForward;
                flatForward.y = 0.0f;
                lerpFromPoint = final;
                lookAtPoint = lerpFromPoint - dampedForward * OffsetZ.Value;
                if (dampedForward.magnitude > Mathf.Epsilon && flatForward.magnitude > Mathf.Epsilon)
                {
                    Debug.DrawLine(transform.position - 2.0f * dampedForward, transform.position + 2.0f * dampedForward, Color.green);
                }
            }
            if (followBody.velocity.magnitude > updateAtVelocity)
                currentTrajectorIndex = (currentTrajectorIndex + 1) % trajectoryControlPointCount;

            if (!isDynamicCam)
            {
                isDynamicCam = true;
                transitionLerp = 0.0f;
            }
        }
        Vector3 lineOfSightAxis = (Follow.position - lerpFromPoint).normalized;
        transform.position = Vector3.Lerp( transform.position, Follow.position + Quaternion.AngleAxis(userLineOfVisionAngles.x, Vector3.up) * Quaternion.AngleAxis(userLineOfVisionAngles.y, Follow.right) * (Vector3.up * OffsetY.Value + lineOfSightAxis * OffsetZ.Value), 0.6f * transitionLerp);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation((lookAtPoint - transform.position).normalized, Vector3.up) * originalRotOffset, transitionLerp * transitionLerp);


    }
}
