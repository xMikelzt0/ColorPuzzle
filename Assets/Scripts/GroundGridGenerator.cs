using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GroundGridGenerator : MonoBehaviour
{
    public static GroundGridGenerator Instance;

    void Awake()
    {
        Instance = this;
    }

    [Header("Grid Settings")]
    public GameObject tilePrefab; // Assign your tile prefab in the Inspector
    public int rows = 3;         // Number of rows
    public int columns = 3;      // Number of columns
    private const float tileSpacing = 0.8f; // Always 0.8, not editable

    private float autoTileScale = 0.8f; // Automatically scale tiles to 80% of original size

    public GameObject queueBarPrefab; // Assign your queue bar prefab in the Inspector
    public PieceGroupGenerator groupGenerator; // Assign in Inspector
    public List<QueueTile> queueTiles = new List<QueueTile>();
    public Material highlightMaterial; // Assign in Inspector
    public PlayTile[,] playTiles; // 2D array for fast lookup

    public void CenterGridInCamera(Camera cam)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Ray from camera center through the screen center
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        // Find intersection with Y=0 plane
        float t = -ray.origin.y / ray.direction.y;
        Vector3 centerWorld = ray.origin + ray.direction * t;

        // Move the parent GameObject to this position
        transform.position = centerWorld;
    }

    public void FitCameraToGrid(Camera cam, float duration = 0.5f)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Calculate grid size in world units
        float gridWidth = (columns - 1) * tileSpacing;
        float gridHeight = (rows - 1) * tileSpacing;

        // Get the largest dimension to fit (diagonal for safety)
        float gridDiagonal = Mathf.Sqrt(gridWidth * gridWidth + gridHeight * gridHeight);

        // Camera FOV calculation
        float halfFOV = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float requiredDistance = (gridDiagonal / 2f) / Mathf.Tan(halfFOV);
        requiredDistance *= 1.2f; // Add 20% padding to ensure full fit

        // Use the camera's forward direction, projected onto the XZ plane, and reversed
        Vector3 camForward = cam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();
        Vector3 camDir = -camForward;

        // Set camera position (keep current height)
        Vector3 center = transform.position;
        Vector3 targetPos = center + camDir * requiredDistance;
        targetPos.y = cam.transform.position.y; // Preserve camera height

        // Target rotation
        Quaternion targetRot = Quaternion.LookRotation(center - targetPos, Vector3.up);

        // Start coroutine for smooth movement
        StartCoroutine(AnimateCameraMove(cam, targetPos, targetRot, duration));
    }

    private IEnumerator AnimateCameraMove(Camera cam, Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.transform.position = targetPos;
        cam.transform.rotation = targetRot;
    }

    public void GenerateQueueTilesAndBar()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Tile prefab not assigned!");
            return;
        }
        if (queueBarPrefab == null)
        {
            Debug.LogError("Queue bar prefab not assigned!");
            return;
        }
        if (groupGenerator == null)
        {
            Debug.LogError("Group generator not assigned!");
            return;
        }

        int queueCount = 3;
        float queueWidth = (queueCount - 1) * tileSpacing;
        float gridBottomZ = -((rows - 1) * tileSpacing / 2f);
        float extraGap = 0.5f; // Additional gap below play tiles
        float queueTilesZ = gridBottomZ - tileSpacing - extraGap; // Place queue tiles further below the play grid

        // 1. Instantiate the queue bar at Y = 0
        float barZ = queueTilesZ;
        Vector3 barPos = new Vector3(0, 0, barZ);
        GameObject barObj = Instantiate(queueBarPrefab, barPos, Quaternion.identity, transform);

        // 2. Instantiate a temp tile to get its bounds
        GameObject tempTile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity);
        tempTile.transform.localScale = tempTile.transform.localScale * autoTileScale;
        Renderer tileRenderer = tempTile.GetComponentInChildren<Renderer>();
        float tileYOffset = 0f;
        if (tileRenderer != null)
        {
            float tileBottom = tileRenderer.bounds.min.y;
            tileYOffset = -tileBottom;
        }
        DestroyImmediate(tempTile);

        // 3. Place queue tiles so their bottoms sit exactly at Y = 0
        for (int i = 0; i < queueCount; i++)
        {
            float x = i * tileSpacing - queueWidth / 2f;
            Vector3 position = new Vector3(x, tileYOffset, queueTilesZ);
            // 1. Instantiate the queue tile
            GameObject queueTile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
            queueTile.transform.localScale = queueTile.transform.localScale * autoTileScale;
            // Add QueueTile script only (do NOT add PlayTile)
            QueueTile qt = queueTile.AddComponent<QueueTile>();
            queueTiles.Add(qt);
            // 2. Instantiate the group at the queue tile's position and parent it
            GameObject group = groupGenerator.CreateRandomGroup(position, 1, 4);
            group.transform.SetParent(queueTile.transform);
            group.transform.localPosition = Vector3.zero;
            qt.isOccupied = true;
            qt.currentGroup = group;
        }
    }

    public void RefillQueue()
    {
        StartCoroutine(RefillQueueCoroutine());
    }

    private IEnumerator RefillQueueCoroutine()
    {
        foreach (QueueTile qt in queueTiles)
        {
            if (!qt.isOccupied)
            {
                GameObject group = groupGenerator.CreateRandomGroup(qt.transform.position, 1, 4);
                group.transform.SetParent(qt.transform);
                group.transform.localPosition = Vector3.zero;
                qt.isOccupied = true;
                qt.currentGroup = group;
                yield return new WaitForSeconds(0.3f); // Short delay between spawns
            }
        }
    }

    void Start()
    {
        CenterGridInCamera(Camera.main);
        GenerateGrid();
        GenerateQueueTilesAndBar();
        FitCameraToGrid(Camera.main, 0.5f);
    }

    public void GenerateGrid()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Tile prefab not assigned!");
            return;
        }

        playTiles = new PlayTile[columns, rows]; // Allocate the array

        // Calculate center offset
        float offsetX = (columns - 1) * tileSpacing / 2f;
        float offsetZ = (rows - 1) * tileSpacing / 2f;

        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                Vector3 position = new Vector3(x * tileSpacing - offsetX, 0, z * tileSpacing - offsetZ);
                GameObject playTile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                playTile.transform.localScale = playTile.transform.localScale * autoTileScale;
                playTile.layer = LayerMask.NameToLayer("PlayTile");

                // Add BoxCollider if not present
                if (playTile.GetComponent<Collider>() == null)
                    playTile.AddComponent<BoxCollider>();

                // Add PlayTile script if not present
                PlayTile playTileScript = playTile.GetComponent<PlayTile>();
                if (playTileScript == null)
                    playTileScript = playTile.AddComponent<PlayTile>();
                // Assign highlight material
                playTileScript.highlightMaterial = highlightMaterial;

                // Store in 2D array
                playTiles[x, z] = playTileScript;
                // Optionally, store grid coordinates in the tile
                playTileScript.gridX = x;
                playTileScript.gridZ = z;
            }
        }
    }

    // Fast lookup for a play tile by grid coordinates
    public PlayTile GetPlayTile(int x, int z)
    {
        if (x >= 0 && x < columns && z >= 0 && z < rows)
            return playTiles[x, z];
        return null;
    }

    public PlayTile FindFurthestAvailableTile(Vector3 startPos, Vector3 direction, float tileSpacing, int maxSteps)
    {
        Vector3 currentPos = startPos;
        PlayTile lastAvailable = null;
        for (int i = 0; i < maxSteps; i++)
        {
            currentPos += direction * tileSpacing;
            // Find the play tile at this position (within a small threshold)
            foreach (PlayTile tile in FindObjectsOfType<PlayTile>())
            {
                if (!tile.isOccupied && Vector3.Distance(tile.transform.position, currentPos) < tileSpacing * 0.5f)
                {
                    lastAvailable = tile;
                }
            }
        }
        return lastAvailable;
    }

    // direction: new Vector2Int(0,1)=up, (1,0)=right, (0,-1)=down, (-1,0)=left
    public PlayTile FindLandingTile(PlayTile startTile, Vector2Int direction, int slotIndex)
    {
        int x = startTile.gridX;
        int z = startTile.gridZ;
        PlayTile furthestEmpty = null;
        int steps = Mathf.Max(columns, rows); // Max possible steps
        for (int i = 1; i < steps; i++)
        {
            int nx = x + direction.x * i;
            int nz = z + direction.y * i;
            PlayTile tile = GetPlayTile(nx, nz);
            if (tile == null) break; // Out of bounds
            bool hasAnyPiece = false;
            bool hasFreeSlot = false;
            for (int s = 0; s < 4; s++)
            {
                if (tile.IsSlotOccupied(s)) hasAnyPiece = true;
                else hasFreeSlot = true;
            }
            if (hasFreeSlot && hasAnyPiece)
            {
                // Nearest partially filled tile
                return tile;
            }
            if (!hasAnyPiece && hasFreeSlot)
            {
                // Remember furthest empty tile
                furthestEmpty = tile;
            }
            // If all slots are full, keep searching
        }
        return furthestEmpty;
    }

    void Update()
    {
        Vector2Int direction = Vector2Int.zero;
        string dirName = "";

        if (Input.GetKeyDown(KeyCode.UpArrow)) { direction = new Vector2Int(0, 1); dirName = "Up"; }
        if (Input.GetKeyDown(KeyCode.RightArrow)) { direction = new Vector2Int(1, 0); dirName = "Right"; }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { direction = new Vector2Int(0, -1); dirName = "Down"; }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) { direction = new Vector2Int(-1, 0); dirName = "Left"; }

        if (direction != Vector2Int.zero)
        {
            PlayTile startTile = PlayTile.lastClickedTile;
            if (startTile == null)
            {
                int centerX = columns / 2;
                int centerZ = rows / 2;
                startTile = GetPlayTile(centerX, centerZ);
            }
            int slotIndex = 0; // You can also make this interactive if needed

            PlayTile result = FindLandingTile(startTile, direction, slotIndex);
            if (result != null)
                Debug.Log($"[{dirName}] Landing tile found at ({result.gridX}, {result.gridZ})");
            else
                Debug.Log($"[{dirName}] No landing tile found in that direction.");
        }
    }
} 