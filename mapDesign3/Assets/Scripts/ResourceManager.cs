using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    #region Singleton
    public static ResourceManager instance;

    void Awake() {
        instance = this;
    }
    #endregion

   // Resource[] currentResource;

    void Start()
    {
      //  int numSlots = System.Enum.GetNames(typeof(ResourceSlot)).Length;
       // currentResource = new Resource[numSlots];
    }
}
