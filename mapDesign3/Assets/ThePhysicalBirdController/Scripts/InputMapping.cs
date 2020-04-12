using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputMapping : MonoBehaviour {

    public string InputAxis0Name;
    public FloatValue[] PropagateInputAxis0;
    public string InputAxis1Name;
    public FloatValue[] PropagateInputAxis1;
    public string InputAxis2Name;
    public FloatValue[] PropagateInputAxis2;
    public string InputAxis3Name;
    public FloatValue[] PropagateInputAxis3;
    public string InputButton0Name;
    public FloatValue[] PropagateInputButton0;
    public string InputButton1Name;
    public FloatValue[] PropagateInputButton1;
    public string InputButton2Name;
    public FloatValue[] PropagateInputButton2;
    public string InputButton3Name;
    public FloatValue[] PropagateInputButton3;
    private void Update()
    {
        if (PropagateInputAxis0 != null)
            foreach(var i in PropagateInputAxis0)
                i.Value = Input.GetAxis(InputAxis0Name);
        if (PropagateInputAxis1 != null)
            foreach (var i in PropagateInputAxis1)
                i.Value = Input.GetAxis(InputAxis1Name);
        if (PropagateInputAxis2 != null)
            foreach (var i in PropagateInputAxis2)
                i.Value = Input.GetAxis(InputAxis2Name);
        if (PropagateInputAxis3 != null)
            foreach (var i in PropagateInputAxis3)
                i.Value = Input.GetAxis(InputAxis3Name);
        if (PropagateInputButton0 != null)
            foreach (var i in PropagateInputButton0)
                i.Value = Input.GetButton(InputButton0Name) ? 1.0f : 0.0f;
        if (PropagateInputButton1 != null)
            foreach (var i in PropagateInputButton1)
                i.Value = Input.GetButton(InputButton1Name) ? 1.0f : 0.0f;
        if (PropagateInputButton2 != null)
            foreach (var i in PropagateInputButton2)
                i.Value = Input.GetButton(InputButton2Name) ? 1.0f : 0.0f;
    }
}
