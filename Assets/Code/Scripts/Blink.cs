using System.Collections;
using UnityEngine;
using TMPro;

public class Blink : MonoBehaviour
{
    [SerializeField] private float timeBeforeBlinkStart = 1f;
    [SerializeField] private float blinkInterval = 1f;
    [SerializeField] private TextMeshProUGUI titleText;

    // Start is called before the first frame update
    void Start()
    {
        titleText = gameObject.GetComponent<TextMeshProUGUI>();
        if (!titleText) throw new UnassignedReferenceException("Title not found!");
        StartCoroutine(StartBlinking());
    }

    private IEnumerator OnBlink()
    {
        while(true)
        {
            yield return new WaitForSeconds(blinkInterval);
            titleText.enabled = !titleText.enabled;
        }
    }

    private IEnumerator StartBlinking()
    {
        yield return new WaitForSeconds(timeBeforeBlinkStart);
        StartCoroutine(OnBlink());
    }
}
