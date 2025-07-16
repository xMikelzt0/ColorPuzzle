using UnityEngine;
using System.Linq;

public class DraggableGroup : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private Vector3 originalPosition;
    private Collider groupCollider;
    private PlayTile currentlyHighlightedTile = null;

    void Start()
    {
        mainCamera = Camera.main;
        groupCollider = GetComponent<Collider>();
    }

    void OnMouseDown()
    {
        isDragging = true;
        originalPosition = transform.position;
        offset = transform.position - GetMouseWorldPosition();
        if (groupCollider != null)
            groupCollider.enabled = false;
        // Do NOT highlight all tiles here
    }

    void OnMouseUp()
    {
        isDragging = false;
        // Unhighlight any highlighted tile
        if (currentlyHighlightedTile != null)
        {
            currentlyHighlightedTile.SetFaded(false);
            currentlyHighlightedTile = null;
        }
        // Unfade all play tiles
        foreach (PlayTile tile in FindObjectsOfType<PlayTile>())
            tile.SetFaded(false);
        // Raycast to find PlayTile under mouse, only on PlayTile layer
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int playTileLayerMask = 1 << LayerMask.NameToLayer("PlayTile");
        if (Physics.Raycast(ray, out hit, 100f, playTileLayerMask))
        {
            Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
            PlayTile playTile = hit.collider.GetComponent<PlayTile>();
            if (playTile != null)
            {
                if (playTile.isOccupied)
                {
                    Debug.Log("Tried to place on an occupied tile: " + playTile.gameObject.name);
                }
                else
                {
                    transform.position = playTile.transform.position;
                    playTile.isOccupied = true;
                    if (groupCollider != null)
                        groupCollider.enabled = true;
                    // Disable this script and collider so the group can't be dragged or clicked again
                    if (groupCollider != null)
                        groupCollider.enabled = false;
                    enabled = false;
                    // Mark queue tile as empty and trigger refill if all are empty
                    QueueTile parentQueueTile = GetComponentInParent<QueueTile>();
                    if (parentQueueTile != null)
                    {
                        parentQueueTile.isOccupied = false;
                        parentQueueTile.currentGroup = null;
                    }
                    if (GroundGridGenerator.Instance != null && GroundGridGenerator.Instance.queueTiles.TrueForAll(qt => !qt.isOccupied))
                    {
                        GroundGridGenerator.Instance.RefillQueue();
                    }
                    // Trigger the flying animation
                    PieceFlyer.FlyGroupToTile(gameObject, playTile);
                    return;
                }
            }
            else
            {
                Debug.Log("Tried to place on a non-play tile: " + hit.collider.gameObject.name);
            }
        }
        else
        {
            Debug.Log("Tried to place on empty space (no collider hit).");
        }
        // If not valid, return to original position
        transform.position = originalPosition;
        if (groupCollider != null)
            groupCollider.enabled = true;
    }

    void Update()
    {
        if (isDragging)
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            transform.position = mouseWorld + offset;

            // Raycast to find play tile under mouse
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            int playTileLayerMask = 1 << LayerMask.NameToLayer("PlayTile");
            PlayTile tileUnderMouse = null;
            if (Physics.Raycast(ray, out hit, 100f, playTileLayerMask))
            {
                tileUnderMouse = hit.collider.GetComponent<PlayTile>();
                if (tileUnderMouse != null && !tileUnderMouse.isOccupied)
                {
                    if (currentlyHighlightedTile != tileUnderMouse)
                    {
                        if (currentlyHighlightedTile != null)
                            currentlyHighlightedTile.SetFaded(false);
                        tileUnderMouse.SetFaded(true);
                        currentlyHighlightedTile = tileUnderMouse;
                    }
                }
                else
                {
                    if (currentlyHighlightedTile != null)
                    {
                        currentlyHighlightedTile.SetFaded(false);
                        currentlyHighlightedTile = null;
                    }
                }
            }
            else
            {
                if (currentlyHighlightedTile != null)
                {
                    currentlyHighlightedTile.SetFaded(false);
                    currentlyHighlightedTile = null;
                }
            }
        }
    }

    // Projects mouse position onto XZ plane (Y=0)
    Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }
}