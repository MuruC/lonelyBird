using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeManager : MonoBehaviour
{
    #region Singleton
    public static TreeManager instance;

    void Awake()
    {
        instance = this;    
    }
    #endregion

    public GameObject treeObj;
    Dictionary<int, Vector3> treeDic;
    int treeIndex = 0;
    // Start is called before the first frame update
    void Start()
    {
        treeDic = new Dictionary<int, Vector3>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setTreeDic() { 
        
    }

    public void plantTree(float posX_, float posY_, float posZ_) {
        tree treeObjScript = treeObj.GetComponent<tree>();
        treeObjScript.posX = posX_;
        treeObjScript.posY = posY_;
        treeObjScript.posZ = posZ_;
        Vector3 pos = new Vector3(posX_,posY_,posZ_);
        Instantiate(treeObj, pos, Quaternion.identity);

        treeDic.Add(treeIndex,pos);
        treeIndex += 1;
    }
}
