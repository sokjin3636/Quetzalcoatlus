using UnityEngine;
using UnityEngine.UI;

public class MentalGaugeController : MonoBehaviour
{
    [Header("UI")]
    public Image fillImage;

    [Header("Mental")]
    public float maxMental = 100f;
    public float currentMental = 100f;

    void Start()
    {
        UpdateGauge();
    }

    public void SetMental(float value)
    {
        currentMental = Mathf.Clamp(value, 0f, maxMental);
        UpdateGauge();
    }

    public void DecreaseMental(float amount)
    {
        currentMental -= amount;
        currentMental = Mathf.Clamp(currentMental, 0f, maxMental);

        UpdateGauge();
    }

    public void RecoverMental(float amount)
    {
        currentMental += amount;
        currentMental = Mathf.Clamp(currentMental, 0f, maxMental);

        UpdateGauge();
    }

    // ∏‡≈ª ∞‘¿Ã¡ˆ UI fillAmount µø±‚»≠
    private void UpdateGauge()
    {
        if (fillImage == null)
            return;

        fillImage.fillAmount = currentMental / maxMental;
    }
}