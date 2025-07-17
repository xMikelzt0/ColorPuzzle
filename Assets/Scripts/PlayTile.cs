using UnityEngine;

public class PlayTile : MonoBehaviour
{
    // Marker script for play grid tiles
    public bool isOccupied = false;
    private Renderer rend;
    private Color originalColor;
    private bool colorInitialized = false;
    public Material highlightMaterial;
    private Material originalMaterial;

    // Grid coordinates for fast lookup
    public int gridX;
    public int gridZ;

    // --- Piece Slot System ---
    public static readonly Vector3[] SlotLocalPositions = new Vector3[4]
    {
        new Vector3(0f, 0f, 0.3f),   // Up
        new Vector3(0.3f, 0f, 0f),   // Right
        new Vector3(0f, 0f, -0.3f),  // Down
        new Vector3(-0.3f, 0f, 0f)   // Left
    };

    // Returns the world position of the slot by index (0=up, 1=right, 2=down, 3=left)
    public Vector3 GetSlotWorldPosition(int slotIndex)
    {
        return transform.position + transform.rotation * SlotLocalPositions[slotIndex % 4];
    }

    // Optional: Draw slot positions in the editor for debugging
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < 4; i++)
        {
            Vector3 worldPos = transform.position + transform.rotation * SlotLocalPositions[i];
            Gizmos.DrawSphere(worldPos + Vector3.up * 0.1f, 0.04f);
        }
    }

    // --- Slot Occupancy ---
    private Transform[] slotOccupants = new Transform[4];

    // Returns true if the slot is occupied
    public bool IsSlotOccupied(int slotIndex)
    {
        return slotOccupants[slotIndex % 4] != null;
    }

    // Returns the piece occupying the slot, or null
    public Transform GetSlotOccupant(int slotIndex)
    {
        return slotOccupants[slotIndex % 4];
    }

    // Sets the piece in the slot (null to clear)
    public void SetSlotOccupant(int slotIndex, Transform piece)
    {
        slotOccupants[slotIndex % 4] = piece;
    }

    // Clears all slots (e.g., when tile is reset)
    public void ClearAllSlots()
    {
        for (int i = 0; i < 4; i++)
            slotOccupants[i] = null;
    }

    // For interactive testing
    public static PlayTile lastClickedTile;

    void OnMouseDown()
    {
        lastClickedTile = this;
        Debug.Log($"Selected tile: ({gridX}, {gridZ})");
    }

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null && originalMaterial == null)
            originalMaterial = rend.material;
        if (rend != null && !colorInitialized)
        {
            originalColor = rend.material.color;
            colorInitialized = true;
        }
    }

    public void SetFaded(bool faded)
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (rend == null) return;
        if (!colorInitialized)
        {
            originalColor = rend.material.color;
            colorInitialized = true;
        }
        if (faded && highlightMaterial != null)
        {
            rend.material = highlightMaterial;
        }
        else if (originalMaterial != null)
        {
            rend.material = originalMaterial;
        }
        // Optionally, also set alpha if you want both effects
        // Color c = originalColor;
        // c.a = faded ? 0.3f : originalColor.a;
        // rend.material.color = c;
    }
} 