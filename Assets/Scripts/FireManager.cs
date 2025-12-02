using UnityEngine;
using UnityEngine.Events;

public class FireManager : MonoBehaviour
{
    public UnityEvent onAllFiresOut;

    int alive;

    void Start()
    {
        var nodes = FindObjectsOfType<Flammable>(true);
        alive = 0;
        foreach (var n in nodes)
        {
            if (!n.IsBurning)
            {
                alive++;
                n.onExtinguished.AddListener(OnOneOut);
            }
        }
        // 若一開始就沒有火，也可以直接觸發
        if (alive == 0) onAllFiresOut?.Invoke();
    }

    void OnOneOut()
    {
        alive--;
        if (alive <= 0) onAllFiresOut?.Invoke();
    }
}
