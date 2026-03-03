using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 100f;
    public float currentHP = 100f;

    [Header("UI")]
    [Tooltip("左下角血條的 Fill Image（Image Type 要設 Filled）")]
    public Image hpFillImage;

    [Header("Events")]
    [Tooltip("血量歸零時觸發（可在 Inspector 拖拉事件）")]
    public UnityEvent onDead;

    public bool IsDead => currentHP <= 0f;

    private bool deadInvoked = false; // 避免重複觸發

    void Start()
    {
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        UpdateUI();

        // 如果一開始就已經是 0，也要觸發（可選）
        if (IsDead && !deadInvoked)
        {
            deadInvoked = true;
            Debug.Log("[PlayerHealth] Player Dead!");
            onDead?.Invoke();
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        if (amount <= 0f) return;

        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        UpdateUI();

        if (IsDead && !deadInvoked)
        {
            deadInvoked = true;
            Debug.Log("[PlayerHealth] Player Dead!");
            onDead?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;

        // 你原本是 IsDead 就不給補；我保留同樣邏輯
        if (IsDead) return;

        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = (maxHP <= 0f) ? 0f : (currentHP / maxHP);
        }
    }
}
