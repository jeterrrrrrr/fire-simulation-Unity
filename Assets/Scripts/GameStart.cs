using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStart : MonoBehaviour
{
    public string nextSceneName = "SampleScene"; // 下一個關卡場景名稱

    public void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
