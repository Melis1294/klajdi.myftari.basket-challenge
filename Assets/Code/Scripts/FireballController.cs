using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Manage slider UI (enabled/disabled fireball modes)
public class FireballController : MonoBehaviour
{
    [SerializeField] private int fireballMultiplier = 1;
    public float FireballCounter;
    [SerializeField] private const int FIREBALL_MAX_VALUE = 4;

    public static FireballController Instance { get; private set; }

    // Start is called before the first frame update
    private void Awake()
    {
        // Prevent class instance duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        else
        {
            Instance = this;
        }
    }

    // Use during game mode
    private void OnEnable()
    {
        
    }

    // Update fireball slider
    void Update()
    {
        // TODO: create a way to update it until it reaches target value
    }

    public void UpdateFireBallCounter(float fireScore = -FIREBALL_MAX_VALUE)
    {
        FireballCounter += fireScore;
        if (FireballCounter > FIREBALL_MAX_VALUE) FireballCounter = FIREBALL_MAX_VALUE;
        else if (FireballCounter < 0) FireballCounter = 0;
    }
}
