using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    public int maxHunger = 100;
    public int maxFatigue = 100;
    public int currentHunger { get; private set; }
    public int currentFatigue { get; private set; }

    public Stat hunger;
    public Stat fatigue;

    void Awake()
    {
        currentHunger = 0;  
        
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ModifyHungerValue(10);
        }
    }

    public void ModifyHungerValue(int hungerValue) {
       // hungerValue = Mathf.Clamp(hungerValue ,0, int.MaxValue);
        currentHunger += hungerValue;
        currentHunger = Mathf.Clamp(currentHunger,0,maxHunger);

        Debug.Log(transform.name + " gets " + hungerValue + " hunger values.");

        if (currentHunger == 100)
        {
            Debug.Log(transform.name + " is starving!");
            Die();
        }
    }

    public int GetHungerValue() {
        return currentHunger;
    }

    public void SetHungerValue(int hungerValue) {
        currentHunger = hungerValue;
    }

    public virtual void Die() { 
        //Die in some way
        //This method is meant to be overwritten
    }

    public void ModifyFatigueValue(int fatigueValue) {
        currentFatigue += fatigueValue;
        currentFatigue = Mathf.Clamp(currentFatigue, 0 ,100);

        if (currentFatigue == 100)
        {
            Debug.Log("Bird needs rest. day change");
        }
    }

}
