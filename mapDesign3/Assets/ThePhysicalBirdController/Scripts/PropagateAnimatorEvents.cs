using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropagateAnimatorEvents : MonoBehaviour
{
    private BirdController controller;
    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponentInParent<BirdController>();
    }
    
    void TakeOffHopEvent()
    {
        controller.applyTakeOffImpulse();
    }

}
