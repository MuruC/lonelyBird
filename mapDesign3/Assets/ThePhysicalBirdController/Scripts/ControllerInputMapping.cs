using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerInputMapping : MonoBehaviour {

    public FloatValue[] PropagateInputAxis1To;
    public FloatValue[] PropagateInputAxis2To;
    public FloatValue[] PropagateInputAxis3To;
    public FloatValue[] PropagateInputAxis4To;
    public FloatValue[] PropagateInputAxis5To;
    public FloatValue[] PropagateInputAxis6To;
    public FloatValue[] PropagateInputAxis7To;

    private void Update()
    {
        /*Debug.Log(Input.GetAxis("Axis 1"));
        Debug.Log(Input.GetAxis("Axis 2"));
        Debug.Log(Input.GetAxis("Axis 3"));
        Debug.Log(Input.GetAxis("Axis 4"));
        Debug.Log(Input.GetAxis("Axis 5"));
        Debug.Log(Input.GetAxis("Axis 6"));*/
        if (PropagateInputAxis1To != null)
            foreach(var i in PropagateInputAxis1To)
                i.Value = Input.GetAxis("Axis 1");
        if (PropagateInputAxis2To != null)
            foreach (var i in PropagateInputAxis2To)
                i.Value = Input.GetAxis("Axis 2");
        if (PropagateInputAxis3To != null)
            foreach (var i in PropagateInputAxis3To)
                i.Value = Input.GetAxis("Axis 3");
        if (PropagateInputAxis4To != null)
            foreach (var i in PropagateInputAxis4To)
                i.Value = Input.GetAxis("Axis 4");
        if (PropagateInputAxis5To != null)
            foreach (var i in PropagateInputAxis5To)
                i.Value = Input.GetAxis("Axis 5");
        if (PropagateInputAxis6To != null)
            foreach (var i in PropagateInputAxis6To)
                i.Value = Input.GetAxis("Axis 6");
        if (PropagateInputAxis7To != null)
            foreach (var i in PropagateInputAxis7To)
                i.Value = Input.GetAxis("Axis 7");

    }
}
