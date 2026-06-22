using UnityEngine;
using UnityEngine.UI;

public class HeartUIController : MonoBehaviour
{
    [Header("Heart Images")]
    public Image[] hearts;

    [Header("Health")]
    public int maxHealth = 3;
    public int currentHealth = 3;

    void Start()
    {
        UpdateHearts();
    }

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        UpdateHearts();
    }

    public void Damage(int amount = 1)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHearts();
    }

    public void Heal(int amount = 1)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHearts();
    }

    // 현재 체력 수치에 맞춰 하트 UI 활성화/비활성화 처리
    private void UpdateHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].enabled = i < currentHealth;
        }
    }
}