using UnityEngine;

public class FireDamageZone : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("每秒扣血量 (DPS)")]
    public float damagePerSecond = 15f;

    [Tooltip("只會扣這些 Layer 的血（建議只勾 PlayerBody）")]
    public LayerMask targetLayers;

    [Header("Debug Logs")]
    public bool enableLogs = true;

    [Tooltip("OnTriggerStay 印 log 的最小間隔秒數（避免狂刷）")]
    public float stayLogInterval = 0.5f;

    private float _nextStayLogTime = 0f;

    void OnTriggerEnter(Collider other)
    {
        if (!enableLogs) return;
        Debug.Log($"[FireDamageZone] ENTER: {other.name} (layer={LayerMask.LayerToName(other.gameObject.layer)})", other);
    }

    void OnTriggerExit(Collider other)
    {
        if (!enableLogs) return;
        Debug.Log($"[FireDamageZone] EXIT: {other.name} (layer={LayerMask.LayerToName(other.gameObject.layer)})", other);
    }

    void OnTriggerStay(Collider other)
    {
        // Layer 過濾
        bool layerMatched = (targetLayers.value & (1 << other.gameObject.layer)) != 0;

        if (!layerMatched)
        {
            if (enableLogs && Time.time >= _nextStayLogTime)
            {
                _nextStayLogTime = Time.time + stayLogInterval;
                Debug.Log($"[FireDamageZone] STAY (IGNORED by LayerMask): {other.name} layer={LayerMask.LayerToName(other.gameObject.layer)}", other);
            }
            return;
        }

        // 往上找 PlayerHealth（XR 常在父物件）
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health == null)
        {
            if (enableLogs && Time.time >= _nextStayLogTime)
            {
                _nextStayLogTime = Time.time + stayLogInterval;
                Debug.Log($"[FireDamageZone] STAY (NO PlayerHealth found in parents): {other.name}", other);
            }
            return;
        }

        float dmg = damagePerSecond * Time.fixedDeltaTime;
        health.TakeDamage(dmg);

        if (enableLogs && Time.time >= _nextStayLogTime)
        {
            _nextStayLogTime = Time.time + stayLogInterval;
            Debug.Log($"[FireDamageZone] STAY (DAMAGE): {other.name}, -{dmg:F2} HP/s={damagePerSecond}", other);
        }
    }
}
