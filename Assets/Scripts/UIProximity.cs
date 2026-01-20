using UnityEngine;

public class UIProximity : MonoBehaviour
{
    public float distance = 2.0f; // UI 出現在眼前幾公尺處
    public float heightOffset = 0.0f; // 高度微調

    // 當物件被 SetActive(true) 時執行一次
    void OnEnable()
    {
        // 找到主攝影機 (玩家的頭)
        Transform cameraTransform = Camera.main.transform;

        // 1. 設定位置：在攝影機前方 distance 公尺處
        Vector3 targetPosition = cameraTransform.position + (cameraTransform.forward * distance);
        
        // (選擇性) 保持 UI 高度水平，不要因為玩家抬頭 UI 就飛上天，可自行決定是否保留
        targetPosition.y = cameraTransform.position.y + heightOffset; 

        transform.position = targetPosition;

        // 2. 設定旋轉：讓 UI 面向攝影機
        // 這種寫法是讓 UI 的「背部」對準攝影機，因為 UI 預設是朝向 Z 軸
        transform.LookAt(transform.position + cameraTransform.forward);
    }
}