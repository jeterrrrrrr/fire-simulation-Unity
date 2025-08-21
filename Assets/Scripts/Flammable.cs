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

    [Header("熱量控制")]
    [Tooltip("熱量上限；<=0 表示不設上限")]
    public float maxHeat = 0f;

    [Tooltip("物件『已燃燒』後，每秒自發增加的固定熱量")]
    public float selfHeatPerSecond = 20f;

    [Header("視覺/音效")]
    public GameObject firePrefab;          // 火焰特效（由本腳本生成/管理）
    public GameObject residualSmokePrefab; // 熄滅後殘煙（可選）
    public Transform fireAnchor;           // 特效/傳熱中心（留空=物件自身）

    [Header("監看/控制")]
    [Tooltip("目前熱量（可在執行時調整）")]
    public float heat = 0f;

    //[Header("狀態顯示")]
    //[Tooltip("目前是否正在燃燒（僅供觀察）")]
    //public bool isBurning;

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
            if (heat >= ignitionHeat && fuel > 0f) Ignite();
            return;
        }

        // 燃燒中：燃料遞減 + 自發產熱（不再吃外部加熱）
        fuel -= Time.deltaTime;
        heat += selfHeatPerSecond * Time.deltaTime;
        ClampHeat();

        // 熄滅條件：燃料用盡 或 熱量跌破門檻
        if (fuel <= 0f || heat < ignitionHeat)
        {
            Extinguish();
            //if (fireInstance) Destroy(fireInstance);
            //if (residualSmokePrefab)
            //    Instantiate(residualSmokePrefab, fireAnchor.position, fireAnchor.rotation);
            //_isBurning = false;
            //isBurning = false;

        }
    }

    /// <summary>來自鄰居火源的加熱：已燃燒則忽略，未燃燒才接受。</summary>
    public void AddHeat(float amount)
    {
        if (_isBurning) return;           // ★ 已燃燒：不再接受外部加熱
        if (amount <= 0f) return;
        heat += amount;
        ClampHeat();
    }

    void Ignite()
    {
        if (_isBurning) return;
        Debug.Log($"[Flammable] {gameObject.name} 開始燃燒！");
        _isBurning = true;
        //isBurning = true;

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
        //isBurning = false;
        Debug.Log($"[Flammable] {gameObject.name} 熄滅！");
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

                // 距離衰減後加熱（只會影響「尚未燃燒」的鄰居；因為 f.AddHeat 內已過濾）
                float dist = Vector3.Distance(center, f.fireAnchor.position);
                float falloff = GetFalloff(dist, spreadRadius, 1.5f); // 前慢後快
                float add = heatPerSecond * falloff * 0.2f;           // 0.2s 一跳
                if (add > 0f) f.AddHeat(add);
            }
            yield return wait;
        }
    }

    float GetFalloff(float dist, float radius, float k = 2f)
    {
        if (dist >= radius) return 0f;
        float x = dist / radius;          // 0..1
        return 1f - Mathf.Pow(x, k);      // k>1：前慢後快
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
