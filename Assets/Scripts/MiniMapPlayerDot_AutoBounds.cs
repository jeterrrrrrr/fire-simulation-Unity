using UnityEngine;

public class MiniMapPlayerDot_AutoBounds : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public RectTransform mapRect;
    public RectTransform playerDot;

    [Header("Auto World Bounds")]
    public Transform minBound;   // MapBounds/Min
    public Transform maxBound;   // MapBounds/Max

    float worldMinX, worldMaxX;
    float worldMinZ, worldMaxZ;
    public float movementMultiplier = 1.8f;


    void Start()
    {
        AutoFetchBounds();
    }

    void AutoFetchBounds()
    {
        worldMinX = minBound.position.x;
        worldMaxX = maxBound.position.x;
        worldMinZ = minBound.position.z;
        worldMaxZ = maxBound.position.z;

        Debug.Log($"[MiniMap] Auto Bounds X({worldMinX},{worldMaxX}) Z({worldMinZ},{worldMaxZ})");
    }

    void Update()
    {
        Vector3 pos = player.position;

        float xN = Mathf.InverseLerp(worldMinX, worldMaxX, pos.x);
        float zN = Mathf.InverseLerp(worldMinZ, worldMaxZ, pos.z);

        float mapX = (xN - 0.5f) * mapRect.sizeDelta.x;
        float mapY = (zN - 0.5f) * mapRect.sizeDelta.y;

        playerDot.anchoredPosition = new Vector2(mapX, mapY);
    }

}
