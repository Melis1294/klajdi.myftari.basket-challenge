using UnityEngine;
using UnityEngine.UI;

public class FireballController : MonoBehaviour
{
    public int FireballMultiplier { get; private set; }
    public float FireballCounter;
    private const float FIREBALL_MAX_VALUE = 1f;

    [SerializeField] private float drainSpeedIncreasing = 1f;
    [SerializeField] private float drainSpeedOnFire = 0.17f;
    [SerializeField] private float drainSpeedMiss = 2f;
    private float currentDrainSpeed;

    private bool isDraining = false;
    private bool pendingDrain = false; // Wait until slider actually reaches max

    private float targetValue; // Where the slider should animate to

    // UI
    private Slider _slider;
    private Image _sliderFill;
    private Color _normalColor = new Color(150f / 255f, 150f / 255f, 150f / 255f);
    private Color _bonusColor = new Color(233f / 255f, 79f / 255f, 55f / 255f);

    // Audio
    [SerializeField] private AudioClip fireball;

    public static FireballController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        FireballMultiplier = 1;
        currentDrainSpeed = drainSpeedIncreasing;
        targetValue = 0f;
        _slider = GetComponent<Slider>();
        _sliderFill = _slider.gameObject.transform.Find("Fill Area").Find("Fill").GetComponent<Image>();
        UpdateSliderColor();
    }

    private void Update()
    {
        // Smoothly animate slider towards target value
        _slider.value = Mathf.MoveTowards(_slider.value, targetValue, currentDrainSpeed * Time.deltaTime);

        // If we were waiting to drain, check if slider visually reached max
        if (pendingDrain && Mathf.Approximately(_slider.value, FIREBALL_MAX_VALUE))
        {
            StartDraining(drainSpeedOnFire);
            pendingDrain = false;
        }

        if (isDraining)
        {
            DrainSlider();
        }
    }

    private void DrainSlider()
    {
        FireballCounter -= currentDrainSpeed * Time.deltaTime;
        FireballCounter = Mathf.Clamp(FireballCounter, 0f, FIREBALL_MAX_VALUE);
        targetValue = FireballCounter; // slider animates smoothly

        if (FireballCounter <= 0f)
        {
            FireballCounter = 0f;
            FireballMultiplier = 1;
            currentDrainSpeed = drainSpeedIncreasing;
            isDraining = false;
            UpdateSliderColor();
        }
    }

    public void AddScore(float amount)
    {
        if (isDraining) return; // Don't add while draining

        FireballCounter += amount;
        FireballCounter = Mathf.Clamp(FireballCounter, 0f, FIREBALL_MAX_VALUE);
        targetValue = FireballCounter; // Slider animates smoothly

        if (FireballCounter >= FIREBALL_MAX_VALUE)
        {
            FireballMultiplier = 2;
            pendingDrain = true; // Wait until visually full before draining
        }
    }

    public void OnMissedShot()
    {
        FireballMultiplier = 1;
        StartDraining(drainSpeedMiss);
    }

    private void StartDraining(float speed)
    {
        currentDrainSpeed = speed;
        isDraining = true;
        UpdateSliderColor();
    }
    private void UpdateSliderColor()
    {
        if (FireballMultiplier == 2) { 
            _sliderFill.color = _bonusColor;
            GameManager.Instance.SFXManager.PlayOneShot(fireball);
        }
        else
            _sliderFill.color = _normalColor;
    }
}
