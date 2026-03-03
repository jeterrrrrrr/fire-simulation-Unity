using UnityEngine;

public static class GameState
{
    public const int LevelCount = 4;           // Level1 ~ Level4
    static readonly bool[] cleared = new bool[LevelCount];

    // （可選）要不要用 PlayerPrefs 永久保存
    public static bool usePlayerPrefs = false;
    const string KEY_PREFIX = "GS_Cleared_";   // GS_Cleared_1 ... GS_Cleared_4
    static bool loaded = false;

    // =========================
    // ✅ 你要的「兩個 public 函式」：更改通關狀態
    // =========================
    public static void SetCleared(int levelNumber)
    {
        EnsureLoadedIfNeeded();
        int idx = ToIndex(levelNumber);
        cleared[idx] = true;
        SaveIfNeeded(levelNumber, true);
    }

    public static void SetNotCleared(int levelNumber)
    {
        EnsureLoadedIfNeeded();
        int idx = ToIndex(levelNumber);
        cleared[idx] = false;
        SaveIfNeeded(levelNumber, false);
    }

    // =========================
    // 常用讀取（主選單顯示會用到）
    // =========================
    public static bool IsCleared(int levelNumber)
    {
        EnsureLoadedIfNeeded();
        int idx = ToIndex(levelNumber);
        return cleared[idx];
    }

    public static int ClearedCount()
    {
        EnsureLoadedIfNeeded();
        int c = 0;
        for (int i = 0; i < LevelCount; i++)
            if (cleared[i]) c++;
        return c;
    }

    public static string ProgressText()
    {
        return $"{ClearedCount()}/{LevelCount}";
    }

    // =========================
    // 內部工具
    // =========================
    static int ToIndex(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > LevelCount)
            throw new System.ArgumentOutOfRangeException(
                nameof(levelNumber),
                $"levelNumber 必須是 1~{LevelCount}，你給的是 {levelNumber}"
            );
        return levelNumber - 1;
    }

    static void EnsureLoadedIfNeeded()
    {
        if (!usePlayerPrefs) return;
        if (loaded) return;

        for (int lv = 1; lv <= LevelCount; lv++)
        {
            int v = PlayerPrefs.GetInt(KEY_PREFIX + lv, 0);
            cleared[lv - 1] = (v == 1);
        }
        loaded = true;
    }

    static void SaveIfNeeded(int levelNumber, bool value)
    {
        if (!usePlayerPrefs) return;
        PlayerPrefs.SetInt(KEY_PREFIX + levelNumber, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}
