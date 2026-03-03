using UnityEngine;
using TMPro;
using System.Collections;

public class DangerTrigger : MonoBehaviour
{
    [Header("提示文字")]
    [Tooltip("顯示在玩家頭頂的提示文字（World Space TMP）")]
    public TextMeshProUGUI headHintText;

    [TextArea]
    public string hintMessage = "偵測到危險源\n請停留 3 秒開始作答";

    [Header("答題 UI")]
    public GameObject quizUI;   // 答題介面 Panel

    [Header("設定")]
    public float stayTime = 3f;

    bool playerInside = false;
    bool triggered = false;
    Coroutine stayCoroutine;

    void Start()
    {
        if (headHintText)
            headHintText.gameObject.SetActive(false);

        if (quizUI)
            quizUI.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        playerInside = true;

        // 顯示頭頂提示
        if (headHintText)
        {
            headHintText.text = hintMessage;
            headHintText.gameObject.SetActive(true);
        }

        stayCoroutine = StartCoroutine(StayCountdown());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;

        if (headHintText)
            headHintText.gameObject.SetActive(false);

        if (stayCoroutine != null)
            StopCoroutine(stayCoroutine);
    }

    IEnumerator StayCountdown()
    {
        float timer = 0f;

        while (timer < stayTime)
        {
            if (!playerInside)
                yield break;

            timer += Time.deltaTime;
            yield return null;
        }

        TriggerQuiz();
    }

    void TriggerQuiz()
    {
        triggered = true;

        if (headHintText)
            headHintText.gameObject.SetActive(false);

        if (quizUI)
            quizUI.SetActive(true);

        Debug.Log($"[DangerTrigger] 開始作答：{gameObject.name}");
    }
}
