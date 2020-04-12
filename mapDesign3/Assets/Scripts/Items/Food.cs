using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Food", menuName = "Inventory/Food")]
public class Food : Item
{
   // public ResourceSlot resourceSlot;

    public int hungerModifier;

    public override void Use()
    {
        base.Use();

        GameObject.FindWithTag("Player").GetComponent<CharacterStats>().ModifyHungerValue(hungerModifier);
        RemoveFromInventory();
    }
}

//public enum ResourceSlot { Food, Branch }
