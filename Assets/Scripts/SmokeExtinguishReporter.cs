using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class SmokeExtinguishReporter : MonoBehaviour
{
    [Header("Cooling")]
    [Tooltip("每顆粒子降低的熱量")]
    public float coolPerParticle = 1f;

    [Tooltip("本幀最多可造成的總降溫(<=0 不限)")]
    public float maxCoolPerFrame = 0f;

    [Header("Filtering")]
    [Tooltip("只影響這些圖層(建議只勾 FireLayer)")]
    public LayerMask affectLayers;

    ParticleSystem ps;
    readonly List<ParticleCollisionEvent> eventsBuf = new(256);

    // 需在粒子系統的 Collision 模組勾選「Send Collision Messages」
    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void OnParticleCollision(GameObject other)
    {
        // Layer 過濾
        if (((1 << other.layer) & affectLayers.value) == 0) return;

        // 取得本次碰撞的事件數（≈命中顆數）
        int count = ps.GetCollisionEvents(other, eventsBuf);
        if (count <= 0) return;

        var flam = other.GetComponentInParent<Flammable>();
        if (!flam) return;

        float totalCool = count * coolPerParticle;
        if (maxCoolPerFrame > 0f) totalCool = Mathf.Min(totalCool, maxCoolPerFrame);
        if (totalCool <= 0f) return;

        flam.CoolDown(totalCool);
        // 如需觀察粒子命中量，取消註解：
        Debug.Log($"[SmokeExtinguish] Hit {other.name} x{count} → Cool {totalCool}");
    }
}
