using UnityEngine;
using System.Collections;

public class Flammable : MonoBehaviour
{
    [Header("燃燒參數")]
    [Tooltip("達到此熱量就會點燃")]
    public float ignitionHeat = 100f;
    [Tooltip("未燃燒時每秒自然散熱量")]
    public float heatDissipation = 15f;
    [Tooltip("燃料量（秒）。燃燒時會逐秒扣除，<=0 即熄滅")]
    public float fuel = 120f;
    [Tooltip("向外傳熱的最大距離")]
    public float spreadRadius = 2f;
    [Tooltip("燃燒時對鄰居每秒輸出的熱量（距離近者更多，遠者更少）")]
    public float heatPerSecond = 40f;
    [Tooltip("允許傳熱的目標 Layer（請把可燃物都放同一 Layer，並在這裡勾選）")]
    public LayerMask flammableMask;

    [Header("熱量上限")]
    [Tooltip("熱量上限；<=0 表示不設上限")]
    public float maxHeat = 0f;

    [Header("視覺/音效")]
    public GameObject firePrefab;          // 火焰特效（由本腳本生成/管理）
    public GameObject residualSmokePrefab; // 熄滅後殘煙（可選）
    public Transform fireAnchor;           // 特效/傳熱中心（留空=物件自身）

    [Header("監看/控制")]
    [Tooltip("目前熱量（可在執行時調整）")]
    public float heat = 0f;

    GameObject fireInstance;
    bool _isBurning; // 內部狀態（不顯示於 Inspector）

    void Awake()
    {
        if (!fireAnchor) fireAnchor = transform;
        ClampHeat(); // 確保初值在界內
    }

    void Start()
    {
        // 以 Heat 作為起始熱量；若已達門檻就點燃
        if (heat >= ignitionHeat) Ignite();
    }

    void Update()
    {
        if (!_isBurning)
        {
            // 未燃燒：自然散熱，達門檻則點燃
            heat -= heatDissipation * Time.deltaTime;
            ClampHeat();
            if (heat >= ignitionHeat) Ignite();
            return;
        }

        // 燃燒中：燃料遞減
        fuel -= Time.deltaTime;

        // 熄滅條件：燃料用盡 或 熱量跌破門檻
        if (fuel <= 0f || heat < ignitionHeat)
            Extinguish();
    }

    /// <summary>來自鄰居火源的加熱（已含上限夾值）</summary>
    public void AddHeat(float amount)
    {
        if (amount <= 0f) return;
        heat += amount;
        ClampHeat();
    }

    void Ignite()
    {
        if (_isBurning) return;
        _isBurning = true;

        if (firePrefab)
        {
            fireInstance = Instantiate(firePrefab, fireAnchor.position, fireAnchor.rotation, fireAnchor);
            foreach (var ps in fireInstance.GetComponentsInChildren<ParticleSystem>())
                ps.Play();
        }

        StartCoroutine(SpreadRoutine());
    }

    public void Extinguish(float cool = 0f)
    {
        if (!_isBurning)
        {
            // 未燃燒時可被動降溫
            if (cool > 0f) { heat -= cool; ClampHeat(); }
            return;
        }

        _isBurning = false;

        // 熄滅時（可選）順手降一點溫
        if (cool > 0f) { heat -= cool; ClampHeat(); }
        fuel = Mathf.Max(0f, fuel);

        if (fireInstance) Destroy(fireInstance);
        if (residualSmokePrefab)
            Instantiate(residualSmokePrefab, fireAnchor.position, fireAnchor.rotation);
    }

    IEnumerator SpreadRoutine()
    {
        var wait = new WaitForSeconds(0.2f);
        while (_isBurning)
        {
            var center = fireAnchor.position;
            var hits = Physics.OverlapSphere(center, spreadRadius, flammableMask, QueryTriggerInteraction.Ignore);

            foreach (var h in hits)
            {
                // 排除自己
                if (h.attachedRigidbody && h.attachedRigidbody.gameObject == gameObject) continue;

                var f = h.GetComponentInParent<Flammable>();
                if (f == null || f == this) continue;

                // 遮擋：第一個打到的不是對方就不傳熱
                if (Physics.Linecast(center, f.fireAnchor.position, out var rh))
                {
                    if (rh.collider.transform != f.transform && !rh.collider.transform.IsChildOf(f.transform))
                        continue;
                }

                // 距離衰減後加熱
                float dist = Vector3.Distance(center, f.fireAnchor.position);
                float falloff = Mathf.Clamp01(1f - dist / spreadRadius);
                float add = heatPerSecond * falloff * 0.2f; // 0.2s 一跳
                if (add > 0f) f.AddHeat(add); // f.AddHeat 內部已做 MaxHeat 夾值
            }
            yield return wait;
        }
    }

    /// <summary>滅火器：只降低熱量（已含下限0與上限 MaxHeat 夾值）</summary>
    public void CoolDown(float amount)
    {
        if (amount <= 0f) return;
        heat -= amount;
        ClampHeat();
        // 不在這裡直接 Extinguish；由 Update 判斷 (fuel<=0 || heat<ignitionHeat)
    }

    /// <summary>將 heat 夾在 [0, MaxHeat]；MaxHeat<=0 表示不設上限</summary>
    void ClampHeat()
    {
        // 下限
        if (heat < 0f) heat = 0f;

        // 上限（若 maxHeat <= 0 代表不限制）
        if (maxHeat > 0f && heat > maxHeat) heat = maxHeat;
    }
}
