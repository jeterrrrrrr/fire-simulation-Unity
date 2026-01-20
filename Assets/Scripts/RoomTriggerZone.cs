using UnityEngine;

public class RoomTriggerZone : MonoBehaviour
{
    [Header("房間設定")]
    [Tooltip("這個空間的名稱，例如：大廳、臥室")]
    public string message = "未命名空間";
    
    // 因為空間名稱通常要一直更新，這裡建議設為 false (每次進出都觸發)
    // 但如果你希望進去後就不再觸發，可以設 true
    public bool triggerOnce = false;

    private bool _hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && _hasTriggered) return;

        var feedbackScript = other.GetComponentInParent<VRInteractionFeedback>();

        if (feedbackScript != null)
        {
            // ★ 修改處：改呼叫 UpdateLocation (永久切換)，而不是 ShowPrompt (暫時顯示)
            feedbackScript.UpdateLocation(message);
            
            _hasTriggered = true;
        }
    }
}