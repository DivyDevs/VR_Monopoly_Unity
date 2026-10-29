using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private Transform playerOrigin;
    [SerializeField] private Transform startTile;
    [SerializeField] private string boardTileTag = "BoardTile";

    public GameObject[] orderedTiles { get; private set; } = Array.Empty<GameObject>();
    public int currentTile { get; private set; } = -1;
    public int TileCount => orderedTiles.Length;

    private void Awake()
    {
        RebuildBoard();
    }

    private void Update()
    {
        UpdateCurrentTile();
    }

    public void RebuildBoard()
    {
        GameObject[] taggedTiles = GameObject.FindGameObjectsWithTag(boardTileTag);
        List<GameObject> uniqueTiles = new List<GameObject>();
        Dictionary<GameObject, Vector2> tilePositions = new Dictionary<GameObject, Vector2>();
        if (startTile == null)
        {
            foreach (GameObject tile in taggedTiles)
            {
                if (tile.name == "1")
                {
                    startTile = tile.transform;
                    break;
                }
            }
        }

        foreach (GameObject tile in taggedTiles)
        {
            Vector2 position = GetBoardLocalPosition(tile.transform);
            int overlappingTile = FindTileAtPosition(uniqueTiles, tilePositions, position);
            if (overlappingTile >= 0)
            {
                GameObject existingTile = uniqueTiles[overlappingTile];
                if (tile == startTile || existingTile.name != "1" && string.CompareOrdinal(tile.name, existingTile.name) < 0)
                {
                    Debug.LogWarning($"Ignoring overlapping board tile '{existingTile.name}' at {position}; using '{tile.name}'.", existingTile);
                    uniqueTiles[overlappingTile] = tile;
                    tilePositions.Remove(existingTile);
                    tilePositions.Add(tile, position);
                }
                else
                {
                    Debug.LogWarning($"Ignoring overlapping board tile '{tile.name}' at {position}; using '{existingTile.name}'.", tile);
                }
                continue;
            }

            uniqueTiles.Add(tile);
            tilePositions.Add(tile, position);
        }

        if (startTile == null || !tilePositions.ContainsKey(startTile.gameObject))
        {
            Debug.LogError($"Assign the GO tile to Start Tile, or name it '1'. Found {uniqueTiles.Count} unique tagged tiles.", this);
            ClearBoard();
            return;
        }

        if (uniqueTiles.Count != 40)
        {
            Debug.LogError($"Expected 40 unique board tile positions, found {uniqueTiles.Count}. Check the BoardTile tags and layout.", this);
            ClearBoard();
            return;
        }

        if (!TryBuildPerimeterOrder(uniqueTiles, tilePositions, startTile.gameObject, out List<GameObject> perimeterOrder))
        {
            ClearBoard();
            return;
        }

        orderedTiles = perimeterOrder.ToArray();
        for (int i = 0; i < orderedTiles.Length; i++)
        {
            BoardTileData tileData = orderedTiles[i].GetComponent<BoardTileData>();
            if (tileData == null) { tileData = orderedTiles[i].AddComponent<BoardTileData>(); }
            tileData.Configure(i);
        }

        UpdateCurrentTile();
    }

    private void ClearBoard()
    {
        orderedTiles = Array.Empty<GameObject>();
        currentTile = -1;
    }

    private Vector2 GetBoardLocalPosition(Transform tile)
    {
        Transform boardFrame = transform;
        if (tile.parent != null) { boardFrame = tile.parent; }
        Vector3 localPosition = boardFrame.InverseTransformPoint(tile.position);
        return new Vector2(localPosition.x, localPosition.z);
    }

    private static int FindTileAtPosition(List<GameObject> tiles, Dictionary<GameObject, Vector2> positions, Vector2 position)
    {
        const float overlapTolerance = 0.05f;
        for (int i = 0; i < tiles.Count; i++)
        {
            if ((positions[tiles[i]] - position).sqrMagnitude <= overlapTolerance * overlapTolerance) { return i; }
        }
        return -1;
    }

    private static bool TryBuildPerimeterOrder(
        List<GameObject> tiles,
        Dictionary<GameObject, Vector2> positions,
        GameObject start,
        out List<GameObject> ordered)
    {
        const float maximumStepFactor = 1.25f;
        ordered = new List<GameObject>(tiles.Count);
        float minimumSpacing = float.PositiveInfinity;
        for (int i = 0; i < tiles.Count; i++)
        {
            for (int j = i + 1; j < tiles.Count; j++)
            {
                float distance = Vector2.Distance(positions[tiles[i]], positions[tiles[j]]);
                if (distance > 0f && distance < minimumSpacing) { minimumSpacing = distance; }
            }
        }

        if (float.IsInfinity(minimumSpacing))
        {
            Debug.LogError("Board tile positions are not distinct.", start);
            return false;
        }

        GameObject firstStep = null;
        float firstStepDistance = float.PositiveInfinity;
        foreach (GameObject tile in tiles)
        {
            Vector2 offset = positions[tile] - positions[start];
            if (offset.x <= 0f) { continue; }

            float distance = offset.magnitude;
            if (distance < firstStepDistance)
            {
                firstStep = tile;
                firstStepDistance = distance;
            }
        }

        if (firstStep == null || firstStepDistance > minimumSpacing * maximumStepFactor)
        {
            Debug.LogError("Could not find the first adjacent tile to the right of GO. Check the board layout and start tile.", start);
            return false;
        }

        HashSet<GameObject> visited = new HashSet<GameObject> { start };
        ordered.Add(start);
        GameObject current = firstStep;

        while (ordered.Count < tiles.Count)
        {
            float distance = Vector2.Distance(positions[ordered[ordered.Count - 1]], positions[current]);
            if (distance > minimumSpacing * maximumStepFactor)
            {
                Debug.LogError($"Board route has a gap between '{ordered[ordered.Count - 1].name}' and '{current.name}'.", current);
                return false;
            }

            ordered.Add(current);
            visited.Add(current);
            if (ordered.Count == tiles.Count) { break; }

            GameObject next = null;
            float nextDistance = float.PositiveInfinity;
            foreach (GameObject candidate in tiles)
            {
                if (visited.Contains(candidate)) { continue; }
                float candidateDistance = Vector2.Distance(positions[current], positions[candidate]);
                if (candidateDistance < nextDistance)
                {
                    next = candidate;
                    nextDistance = candidateDistance;
                }
            }

            if (next == null || nextDistance > minimumSpacing * maximumStepFactor)
            {
                Debug.LogError($"Board route cannot continue from tile '{current.name}'.", current);
                return false;
            }

            current = next;
        }

        float closingDistance = Vector2.Distance(positions[ordered[ordered.Count - 1]], positions[start]);
        if (closingDistance > minimumSpacing * maximumStepFactor)
        {
            Debug.LogError($"Board route does not close from '{ordered[ordered.Count - 1].name}' back to GO.", start);
            return false;
        }

        return true;
    }

    public void SetPlayerOrigin(Transform origin)
    {
        playerOrigin = origin;
        UpdateCurrentTile();
    }

    public bool IsPlayerOnTile(int tileIndex)
    {
        return tileIndex >= 0 && tileIndex < orderedTiles.Length && IsPositionOnTile(playerOrigin.position, orderedTiles[tileIndex]);
    }

    public GameObject GetTile(int tileIndex)
    {
        return tileIndex >= 0 && tileIndex < orderedTiles.Length ? orderedTiles[tileIndex] : null;
    }

    public BoardTileData GetTileData(int tileIndex)
    {
        GameObject tile = GetTile(tileIndex);
        return tile != null ? tile.GetComponent<BoardTileData>() : null;
    }

    public Vector3 GetTilePosition(int tileIndex)
    {
        GameObject tile = GetTile(tileIndex);
        if (tile == null) { return transform.position; }

        Collider collider = tile.GetComponentInChildren<Collider>();
        return collider != null ? collider.bounds.center : tile.transform.position;
    }

    public Vector3 GetBoardCenter()
    {
        if (orderedTiles.Length == 0) { return transform.position; }
        Vector3 center = Vector3.zero;
        foreach (GameObject tile in orderedTiles) { center += tile.transform.position; }
        return center / orderedTiles.Length;
    }

    public void HighlightPath(int startTile, int spaces)
    {
        if (TileCount == 0 || startTile < 0 || startTile >= TileCount || spaces <= 0) { return; }

        ClearHighlights();
        StringBuilder highlightedTiles = new StringBuilder();
        int stepCount = Mathf.Min(spaces, TileCount);
        for (int step = 1; step <= stepCount; step++)
        {
            int index = (startTile + step) % TileCount;
            BoardTileData tileData = GetTileData(index);
            if (tileData != null)
            {
                tileData.SetHighlight(step == spaces ? new Color(0.95f, 0.7f, 0.18f) : new Color(0.2f, 0.8f, 0.75f));
            }
            else
            {
                Debug.LogError($"Board tile {index + 1} ('{orderedTiles[index].name}') has no BoardTileData and cannot be highlighted.", orderedTiles[index]);
            }

            if (step > 1) { highlightedTiles.Append(" -> "); }
            highlightedTiles.Append($"{index + 1}:{orderedTiles[index].name}");
        }

        Debug.Log($"Highlighted dice path from tile {startTile + 1}: {highlightedTiles}", this);
    }

    public void ClearHighlights()
    {
        foreach (GameObject tile in orderedTiles)
        {
            BoardTileData tileData = tile.GetComponent<BoardTileData>();
            if (tileData != null) { tileData.ClearHighlight(); }
        }
    }

    private void UpdateCurrentTile()
    {
        if (playerOrigin == null || orderedTiles.Length == 0) { return; }

        float closestDistance = float.PositiveInfinity;
        int closestIndex = -1;
        for (int i = 0; i < orderedTiles.Length; i++)
        {
            float distance = HorizontalDistance(playerOrigin.position, orderedTiles[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        currentTile = closestDistance <= 2f ? closestIndex : -1;
    }

    private static bool IsPositionOnTile(Vector3 position, GameObject tile)
    {
        Collider collider = tile.GetComponentInChildren<Collider>();
        if (collider == null) { return false; }

        Bounds bounds = collider.bounds;
        Vector3 closest = collider.ClosestPoint(new Vector3(position.x, bounds.center.y, position.z));
        Vector2 horizontalDistance = new Vector2(closest.x - position.x, closest.z - position.z);
        float tolerance = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.2f);
        return bounds.Contains(new Vector3(position.x, bounds.center.y, position.z)) || horizontalDistance.magnitude <= tolerance;
    }

    private static float HorizontalDistance(Vector3 position, GameObject tile)
    {
        Collider collider = tile.GetComponentInChildren<Collider>();
        if (collider == null) { return float.PositiveInfinity; }

        Bounds bounds = collider.bounds;
        Vector3 closest = collider.ClosestPoint(new Vector3(position.x, bounds.center.y, position.z));
        return Vector2.Distance(new Vector2(position.x, position.z), new Vector2(closest.x, closest.z));
    }
}
