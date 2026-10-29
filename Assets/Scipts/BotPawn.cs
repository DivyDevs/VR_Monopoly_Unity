using System.Collections;
using UnityEngine;

public class BotPawn : MonoBehaviour
{
    public int CurrentTileIndex { get; private set; }

    public void PlaceOnTile(BoardManager boardManager, int tileIndex)
    {
        CurrentTileIndex = tileIndex;
        transform.position = GetPawnPosition(boardManager, tileIndex);
    }

    public IEnumerator Move(BoardManager boardManager, int steps, float secondsPerTile)
    {
        for (int step = 0; step < steps; step++)
        {
            int nextTile = (CurrentTileIndex + 1) % boardManager.TileCount;
            Vector3 start = transform.position;
            Vector3 destination = GetPawnPosition(boardManager, nextTile);
            float elapsed = 0f;
            while (elapsed < secondsPerTile)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, destination, Mathf.Clamp01(elapsed / secondsPerTile));
                yield return null;
            }

            CurrentTileIndex = nextTile;
            transform.position = destination;
        }
    }

    private Vector3 GetPawnPosition(BoardManager boardManager, int tileIndex)
    {
        return boardManager.GetTilePosition(tileIndex) + Vector3.up * 0.18f;
    }
}