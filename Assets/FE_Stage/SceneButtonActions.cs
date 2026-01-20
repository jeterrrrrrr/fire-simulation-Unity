using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonActions : MonoBehaviour
{
    [Header("主選單場景名稱（要跟你的 Scene 檔名完全一樣）")]
    public string mainMenuSceneName = "MainMenu";

    [Header("切場景前要不要把 Time.timeScale 還原（避免你有暫停遊戲造成卡住）")]
    public bool resetTimeScale = true;

    // ✅ 重新開始：重載目前場景（等同重製）
    public void RestartCurrentScene()
    {
        if (resetTimeScale) Time.timeScale = 1f;

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    // ✅ 回主選單：載入指定場景
    public void GoToMainMenu()
    {
        if (resetTimeScale) Time.timeScale = 1f;

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("mainMenuSceneName 沒有設定！");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // （可選）如果你想用 Build Index 方式回主選單
    public void GoToMainMenuByIndex(int buildIndex)
    {
        if (resetTimeScale) Time.timeScale = 1f;
        SceneManager.LoadScene(buildIndex);
    }
}
