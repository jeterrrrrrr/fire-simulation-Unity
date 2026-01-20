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

    [Header("Debug")]
    [Tooltip("是否輸出 Debug.Log")]
    public bool enableLogs = true;

    [Tooltip("是否印出每次 callback（非常吵）")]
    public bool logEveryCallback = true;

    [Tooltip("是否印出 layer 過濾結果")]
    public bool logLayerFiltering = true;

    [Tooltip("是否印出 Flammable 搜尋結果")]
    public bool logFlammableFinding = true;

    [Tooltip("是否印出 GetCollisionEvents 結果")]
    public bool logCollisionEvents = true;

    [Tooltip("是否印出冷卻計算與呼叫 CoolDown")]
    public bool logCooling = true;

    [Tooltip("同一個目標物件最短 log 間隔（秒），避免 Console 爆炸。0=不節流")]
    public float logThrottleSeconds = 0.25f;

    private ParticleSystem ps;
    private readonly List<ParticleCollisionEvent> eventsBuf = new(256);

    // 節流用：記錄每個 other 上一次輸出時間
    private readonly Dictionary<int, float> lastLogTimeByOther = new();

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        if (enableLogs)
        {
            Debug.Log(
                $"[SmokeExtinguish] Awake on '{name}' | ParticleSystem='{ps.name}'\n" +
                $"- coolPerParticle={coolPerParticle}\n" +
                $"- maxCoolPerFrame={maxCoolPerFrame}\n" +
                $"- affectLayers=({LayerMaskToString(affectLayers)}) value={affectLayers.value}\n" +
                $"- NOTE: ParticleSystem Collision module must enable 'Send Collision Messages'."
            );
        }
    }

    void OnParticleCollision(GameObject other)
    {
        if (!enableLogs)
        {
            // 即使不 log，也照常跑冷卻邏輯
            DoCooling(other, false);
            return;
        }

        // 節流：同一個 other 太頻繁就不印（但仍執行邏輯）
        bool allowLog = AllowLog(other);

        if (logEveryCallback && allowLog)
        {
            Debug.Log(
                $"[SmokeExtinguish] CALLBACK ✅ | self='{name}' ps='{ps.name}' -> other='{other.name}'\n" +
                $"- other.layer={other.layer} ({LayerMask.LayerToName(other.layer)})\n" +
                $"- other.tag='{other.tag}'\n" +
                $"- other.path='{GetPath(other.transform)}'"
            );
        }

        DoCooling(other, allowLog);
    }

    private void DoCooling(GameObject other, bool allowLog)
    {
        // 1) Layer 過濾（注意：這裡是用 other.layer，也就是「掛 collider 的那個物件」）
        bool layerPass = ((1 << other.layer) & affectLayers.value) != 0;

        if (logLayerFiltering && allowLog)
        {
            Debug.Log(
                $"[SmokeExtinguish] LayerFilter {(layerPass ? "PASS ✅" : "BLOCK ❌")} | " +
                $"other.layer={other.layer}({LayerMask.LayerToName(other.layer)}) " +
                $"affectLayers=({LayerMaskToString(affectLayers)})"
            );
        }

        if (!layerPass) return;

        // 2) 取得碰撞事件數（≈命中顆數）
        int count = ps.GetCollisionEvents(other, eventsBuf);

        if (logCollisionEvents && allowLog)
        {
            Debug.Log($"[SmokeExtinguish] GetCollisionEvents => count={count} (bufferSize={eventsBuf.Count}) | other='{other.name}'");
        }

        if (count <= 0) return;

        // 3) 找 Flammable（從 other 往上找）
        Flammable flam = other.GetComponentInParent<Flammable>();

        if (logFlammableFinding && allowLog)
        {
            if (flam)
            {
                Debug.Log(
                    $"[SmokeExtinguish] Flammable FOUND ✅ | flam='{flam.name}'\n" +
                    $"- flam.path='{GetPath(flam.transform)}'\n" +
                    $"- flam.layer={flam.gameObject.layer}({LayerMask.LayerToName(flam.gameObject.layer)})"
                );
            }
            else
            {
                Debug.Log(
                    $"[SmokeExtinguish] Flammable NOT FOUND ❌ | searched GetComponentInParent<Flammable>() from other='{other.name}'"
                );
            }
        }

        if (!flam) return;

        // 4) 計算降溫
        float totalCool = count * coolPerParticle;
        float beforeClamp = totalCool;

        if (maxCoolPerFrame > 0f) totalCool = Mathf.Min(totalCool, maxCoolPerFrame);

        if (logCooling && allowLog)
        {
            Debug.Log(
                $"[SmokeExtinguish] CoolingCalc | hits={count}, coolPerParticle={coolPerParticle}\n" +
                $"- totalCool(beforeClamp)={beforeClamp}\n" +
                $"- totalCool(afterClamp) ={totalCool}\n" +
                $"- maxCoolPerFrame={maxCoolPerFrame}"
            );
        }

        if (totalCool <= 0f) return;

        // 5) 呼叫 CoolDown
        flam.CoolDown(totalCool);

        if (logCooling && allowLog)
        {
            Debug.Log($"[SmokeExtinguish] CoolDown CALLED ✅ | targetFlam='{flam.name}' cool={totalCool}");
        }
    }

    private bool AllowLog(GameObject other)
    {
        if (logThrottleSeconds <= 0f) return true;

        int id = other.GetInstanceID();
        float now = Time.time;

        if (lastLogTimeByOther.TryGetValue(id, out float last))
        {
            if (now - last < logThrottleSeconds) return false;
            lastLogTimeByOther[id] = now;
            return true;
        }

        lastLogTimeByOther[id] = now;
        return true;
    }

    private static string LayerMaskToString(LayerMask mask)
    {
        if (mask.value == 0) return "None";

        List<string> names = new();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                string n = LayerMask.LayerToName(i);
                names.Add(string.IsNullOrEmpty(n) ? $"Layer{i}" : n);
            }
        }
        return string.Join(", ", names);
    }

    private static string GetPath(Transform t)
    {
        if (!t) return "(null)";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
