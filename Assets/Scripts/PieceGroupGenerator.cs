using UnityEngine;

public class PieceGroupGenerator : MonoBehaviour
{
    [Header("Piece Group Settings")]
    public GameObject piecePrefab; // Assign your quarter-circle piece prefab in the Inspector
    public float groupRadius = 0f; // For quarter-circle with pivot at center of full circle
    public Material[] colorMaterials; // Assign mat_red, mat_green, mat_purple, mat_yellow in Inspector

    /// <summary>
    /// Creates a group of 1-4 pieces arranged in a full circle.
    /// </summary>
    public GameObject CreateRandomGroup(Vector3 position, int minPieces = 1, int maxPieces = 4)
    {
        int pieceCount = Random.Range(minPieces, maxPieces + 1);
        GameObject group = new GameObject("PieceGroup");
        group.transform.position = position;

        // Cardinal angles: top (0), right (90), down (180), left (270)
        int[] angles = new int[] { 0, 90, 180, 270 };
        // Shuffle angles
        for (int i = 0; i < angles.Length; i++)
        {
            int j = Random.Range(i, angles.Length);
            int temp = angles[i];
            angles[i] = angles[j];
            angles[j] = temp;
        }

        for (int i = 0; i < pieceCount; i++)
        {
            float angle = angles[i];
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 pos = Vector3.zero; // No offset, just rotate

            GameObject piece = Instantiate(piecePrefab, group.transform);
            piece.transform.localPosition = pos;
            piece.transform.localRotation = rotation;
            piece.transform.localScale = Vector3.one * 0.6f; // Always 60% of original size

            // Assign slot index based on angle
            int slotIndex = 0;
            if (angle == 0) slotIndex = 0;      // up
            else if (angle == 90) slotIndex = 1; // right
            else if (angle == 180) slotIndex = 2; // down
            else if (angle == 270) slotIndex = 3; // left
            var tracker = piece.GetComponent<PieceSlotTracker>();
            if (tracker == null) tracker = piece.AddComponent<PieceSlotTracker>();
            tracker.slotIndex = slotIndex;

            // Assign random color material
            var renderer = piece.GetComponentInChildren<Renderer>();
            if (renderer != null && colorMaterials != null && colorMaterials.Length > 0)
            {
                renderer.material = colorMaterials[Random.Range(0, colorMaterials.Length)];
            }
        }

        // Add DraggableGroup script
        group.AddComponent<DraggableGroup>();
        // Add BoxCollider for mouse events (adjust size as needed)
        BoxCollider col = group.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 0.2f, 1f);
        col.center = new Vector3(0f, 0.1f, 0f);

        return group;
    }
} 