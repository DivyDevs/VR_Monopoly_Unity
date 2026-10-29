using UnityEngine;

public class PlayerPawnTracker : MonoBehaviour
{
    [Header("Tracking Status")]
    public Transform currentTile;
    public int currentTileIndex { get; private set; } = -1;
    [SerializeField] private BoardManager boardManager;

    private void Start()
    {
        if (boardManager == null) { boardManager = FindAnyObjectByType<BoardManager>(); }
        if (boardManager != null) { boardManager.SetPlayerOrigin(transform); }
    }

    private void Update()
    {
        if (boardManager == null || boardManager.currentTile < 0) { return; }

        currentTileIndex = boardManager.currentTile;
        GameObject tile = boardManager.GetTile(currentTileIndex);
        if (tile != null && currentTile != tile.transform)
        {
            currentTile = tile.transform;
            Debug.Log("Player is currently standing on: " + currentTile.name);
        }
    }
}