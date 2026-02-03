using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMover : MonoBehaviour
{
    [Header("Target Scene Name")]
    [SerializeField] private string lobbySceneName = "Lobby";

    /// <summary>
    /// 重新載入目前場景（場景內物件會重建成預設狀態）
    /// </summary>
    public void ReloadCurrentScene()
    {
        // 如果你有暫停遊戲，回來前先恢復
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name, LoadSceneMode.Single);
    }

    /// <summary>
    /// 回到 Lobby 場景
    /// </summary>
    public void GoToLobby()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }
}
