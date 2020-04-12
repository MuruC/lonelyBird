using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Food", menuName = "Inventory/Seed")]
public class Seed : Item
{
    public override void Use()
    {
        base.Use();

        float posX = GameObject.FindGameObjectWithTag("Player").transform.position.x;
        float height = Terrain.activeTerrain.SampleHeight(GameObject.FindGameObjectWithTag("Player").transform.position);
        float terrainHeight = Terrain.activeTerrain.GetPosition().y;
        float posY = (height + terrainHeight) * 1.5f;
        float posZ = GameObject.FindGameObjectWithTag("Player").transform.position.z;

        TreeManager.instance.plantTree(posX,posY,posZ);

        RemoveFromInventory();
    }
}
