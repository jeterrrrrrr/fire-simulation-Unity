using UnityEngine;
public class RoomTriggerZone : MonoBehaviour
{
    [Header("房間設定")] 
    [Tooltip("這個空間的名稱，例如：大廳、臥室")] 
    public string message = "未命名空間";

    public bool triggerOnce = false;
    private bool _hasTriggered = false;

    void OnTriggerEnter(Collider other){
        if (triggerOnce && _hasTriggered) return;

        var feedbackScript = other.GetComponentInParent<VRInteractionFeedback>();

        if (feedbackScript != null){
            feedbackScript.UpdateLocation(message);

            _hasTriggered = true;
        }
    }
}