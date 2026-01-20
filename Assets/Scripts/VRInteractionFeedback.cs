using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine;

public class TouchDescription : MonoBehaviour
{
    [TextArea]
    [Tooltip("撞到物體時顯示的文字")]
    public string message = "這是一個物體";
}   

[RequireComponent(typeof(CharacterController))]
public class VRInteractionFeedback : MonoBehaviour
{
    [Header("UI 設定")]
    [Tooltip("請拖入 Canvas 下的 Legacy Text 物件")]
    public Text promptText; 
    
    [Tooltip("臨時提示(如撞牆)的顯示時間 (秒)")]
    public float promptDuration = 2.0f;

    // ----- 內部變數 -----
    private Coroutine _hideTextCoroutine;
    private float _lastTouchTime = 0f;
    private float _touchCooldown = 1.0f;

    // ★ 新增：用來記住現在在哪個房間
    private string _currentLocation = ""; 

    // -------------------- 1. 臨時提示功能 (給撞牆、機關用) --------------------
    // 這些訊息顯示幾秒後會消失，然後「變回房間名稱」
    public void ShowPrompt(string message)
    {
        Debug.Log($"[臨時提示] {message}"); 

        if (promptText != null)
        {
            promptText.text = message;
            promptText.gameObject.SetActive(true);

            // 重新計時倒數
            if (_hideTextCoroutine != null) StopCoroutine(_hideTextCoroutine);
            _hideTextCoroutine = StartCoroutine(RestoreLocationRoutine());
        }
    }

    // ★ 修改：倒數結束後，不是清空，而是還原成地點名稱
    IEnumerator RestoreLocationRoutine()
    {
        yield return new WaitForSeconds(promptDuration);
        
        if (promptText != null)
        {
            // 還原回當前的地點名稱
            promptText.text = _currentLocation;

            // 如果目前沒有地點名稱 (例如還沒進任何房間)，才把文字隱藏
            if (string.IsNullOrEmpty(_currentLocation))
            {
                promptText.gameObject.SetActive(false);
            }
        }
    }

    // -------------------- 2. ★ 新增：切換地點功能 (給房間用) --------------------
    // 這個訊息會一直留著，不會消失，直到進入下一個房間
    public void UpdateLocation(string locationName)
    {
        Debug.Log($"[切換地點] {locationName}");

        // 1. 記住新的地點
        _currentLocation = locationName;

        // 2. 馬上顯示新的地點
        if (promptText != null)
        {
            promptText.text = locationName;
            promptText.gameObject.SetActive(true);
        }

        // 3. 因為進入新房間了，取消之前的「撞牆提示倒數」(如果有)，直接定格在新房間名
        if (_hideTextCoroutine != null) StopCoroutine(_hideTextCoroutine);
    }

    // -------------------- 3. 撞牆偵測 --------------------
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Time.time - _lastTouchTime < _touchCooldown) return;

        TouchDescription info = hit.gameObject.GetComponent<TouchDescription>();
        
        if (info != null)
        {
            // 撞牆屬於「臨時提示」，所以呼叫 ShowPrompt
            ShowPrompt("碰到：" + info.message);
            _lastTouchTime = Time.time;
        }
    }
}