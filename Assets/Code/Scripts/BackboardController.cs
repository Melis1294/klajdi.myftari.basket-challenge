using UnityEngine;
using TMPro;

public class BackboardController : MonoBehaviour
{
    public static BackboardController instance { get; private set; }

    [SerializeField] private int baseValue = 2;
    [SerializeField] private int[] upgradeValues = { 4, 6, 8 };
    [SerializeField] private float totalTime = 60f;
    [SerializeField] private float upgradeDuration = 5f;
    [SerializeField] private TextMeshPro bonusText;

    private int currentValue;
    private int upgradesDone = 0;
    private float nextUpgradeTime;
    private float upgradeEndTime;
    private bool isUpgraded = false;

    private void Awake()
    {
        // Prevent class instance duplicates
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
        bonusText.enabled = isUpgraded;
    }

    void Start()
    {
        currentValue = baseValue;

        // Schedule first upgrade randomly between 20–30s
        nextUpgradeTime = Random.Range(totalTime * 0.33f, totalTime * 0.5f);
    }

    void Update()
    {
        float time = Time.timeSinceLevelLoad;

        // Trigger upgrade
        if (!isUpgraded && upgradesDone < 2 && time >= nextUpgradeTime)
        {
            DoUpgrade(time);
        }

        // End upgrade after duration
        if (isUpgraded && time >= upgradeEndTime)
        {
            ResetValue();
        }
    }

    void DoUpgrade(float time)
    {
        // Pick upgrade from array sequentially
        if (upgradesDone < upgradeValues.Length)
        {
            currentValue = upgradeValues[upgradesDone];
            Debug.Log("Upgraded! Current value = " + currentValue);
        }

        isUpgraded = true;
        upgradeEndTime = time + upgradeDuration;
        upgradesDone++;

        // Schedule next upgrade if allowed
        if (upgradesDone < 2)
        {
            // Second upgrade happens later (40–60s)
            nextUpgradeTime = Random.Range(totalTime * 0.66f, totalTime);
        }
        bonusText.text = string.Format("Bonus {0} points!", currentValue);
        bonusText.enabled = isUpgraded;
    }

    public void ResetValue()
    {
        currentValue = baseValue;
        isUpgraded = false;
        bonusText.enabled = isUpgraded;
        Debug.Log("Reset to base value = " + currentValue);
    }

    public int GetValue()
    {
        return currentValue;
    }
}
