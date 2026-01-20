using UnityEngine;

public class GameExit : MonoBehaviour
{
    public void ExitGame()
    {
        Debug.Log("Exit button pressed. Quitting game...");
        Application.Quit();

        // 在編輯器中不會真的關閉，所以加上這行方便測試
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
