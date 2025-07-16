using System.Collections;
using UnityEngine;

public class PieceFlyer : MonoBehaviour
{
    public static void FlyGroupToTile(GameObject group, PlayTile playTile, float duration = 0.5f)
    {
        if (group == null || playTile == null) return;
        PieceFlyer flyer = playTile.gameObject.AddComponent<PieceFlyer>();
        Transform[] pieces = new Transform[group.transform.childCount];
        Vector3[] flyDirections = new Vector3[pieces.Length];
        PlayTile[] destinations = new PlayTile[pieces.Length];
        float tileSpacing = 0.8f; // Should match your grid spacing
        int maxSteps = 10; // Should be at least as large as your grid size
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i] = group.transform.GetChild(i);
            flyDirections[i] = -pieces[i].right;
            // Find the furthest available tile in this direction
            destinations[i] = GroundGridGenerator.Instance.FindFurthestAvailableTile(
                playTile.transform.position, flyDirections[i], tileSpacing, maxSteps);
        }
        flyer.StartCoroutine(flyer.FlyPiecesToTiles(pieces, playTile, destinations, duration, group));
    }

    private IEnumerator FlyPiecesToTiles(Transform[] pieces, PlayTile startTile, PlayTile[] destinations, float duration, GameObject group)
    {
        Vector3[] startPositions = new Vector3[pieces.Length];
        Vector3[] endPositions = new Vector3[pieces.Length];
        float[] durations = new float[pieces.Length];
        float maxDistance = 0f;
        for (int i = 0; i < pieces.Length; i++)
        {
            startPositions[i] = pieces[i].position;
            if (destinations[i] != null)
                endPositions[i] = destinations[i].transform.position;
            else
                endPositions[i] = startTile.transform.position;
            float distance = Vector3.Distance(startPositions[i], endPositions[i]);
            durations[i] = distance; // Duration proportional to distance
            if (distance > maxDistance) maxDistance = distance;
        }
        // Normalize durations so the closest piece flies in 'duration' seconds
        for (int i = 0; i < pieces.Length; i++)
        {
            if (maxDistance > 0.0001f)
                durations[i] = duration * (durations[i] / maxDistance);
            else
                durations[i] = duration;
        }

        float[] t = new float[pieces.Length];
        bool[] finished = new bool[pieces.Length];
        int finishedCount = 0;
        while (finishedCount < pieces.Length)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                if (finished[i]) continue;
                t[i] += Time.deltaTime / durations[i];
                if (t[i] >= 1f)
                {
                    t[i] = 1f;
                    finished[i] = true;
                    finishedCount++;
                }
                pieces[i].position = Vector3.Lerp(startPositions[i], endPositions[i], t[i]);
            }
            yield return null;
        }
        // Snap to final position and parent to destination tile
        bool anyStayed = false;
        for (int i = 0; i < pieces.Length; i++)
        {
            var tracker = pieces[i].GetComponent<PieceSlotTracker>();
            int slotIndex = tracker != null ? tracker.slotIndex : 0;
            pieces[i].position = endPositions[i];
            if (destinations[i] != null)
            {
                PlayTile destTile = destinations[i];
                if (!destTile.IsSlotOccupied(slotIndex))
                {
                    // Slot is free, place piece
                    pieces[i].SetParent(destTile.transform);
                    pieces[i].localPosition = Vector3.zero;
                    pieces[i].localRotation = Quaternion.Euler(0, slotIndex * 90, 0);
                    if (tracker == null) tracker = pieces[i].gameObject.AddComponent<PieceSlotTracker>();
                    tracker.slotIndex = slotIndex;
                    destTile.SetSlotOccupant(slotIndex, pieces[i]);
                }
                else
                {
                    Transform pushedPiece = destTile.GetSlotOccupant(slotIndex);
                    int pushFrom = slotIndex;
                    int pushTo = -1;
                    for (int s = 1; s < 4; s++)
                    {
                        int candidate = (slotIndex + s) % 4;
                        if (!destTile.IsSlotOccupied(candidate))
                        {
                            pushTo = candidate;
                            break;
                        }
                    }
                    if (pushTo != -1)
                    {
                        yield return StartCoroutine(AnimatePush(pushedPiece, destTile, pushFrom, pushTo, 0.25f));
                        var pushTracker = pushedPiece.GetComponent<PieceSlotTracker>();
                        if (pushTracker == null) pushTracker = pushedPiece.gameObject.AddComponent<PieceSlotTracker>();
                        pushTracker.slotIndex = pushTo;
                        pushedPiece.localPosition = Vector3.zero;
                        pushedPiece.localRotation = Quaternion.Euler(0, pushTo * 90, 0);
                        // Place the new piece in its intended slot
                        pieces[i].SetParent(destTile.transform);
                        if (tracker == null) tracker = pieces[i].gameObject.AddComponent<PieceSlotTracker>();
                        tracker.slotIndex = slotIndex;
                        pieces[i].localPosition = Vector3.zero;
                        pieces[i].localRotation = Quaternion.Euler(0, slotIndex * 90, 0);
                        destTile.SetSlotOccupant(slotIndex, pieces[i]);
                    }
                    else
                    {
                        Debug.Log($"All slots full on tile, cannot push");
                    }
                }
                destTile.isOccupied = true;
            }
            else
            {
                pieces[i].SetParent(startTile.transform);
                startTile.isOccupied = true;
                anyStayed = true;
            }
        }
        if (!anyStayed)
            startTile.isOccupied = false;
        Destroy(group);
        Destroy(this);
    }

    // Animate a piece moving from one slot to another along a circular arc, rotating 90 degrees
    private IEnumerator AnimatePush(Transform piece, PlayTile tile, int fromSlot, int toSlot, float duration)
    {
        Vector3 center = tile.transform.position;
        float startAngle = Mathf.Atan2(PlayTile.SlotLocalPositions[fromSlot].z, PlayTile.SlotLocalPositions[fromSlot].x);
        float endAngle = Mathf.Atan2(PlayTile.SlotLocalPositions[toSlot].z, PlayTile.SlotLocalPositions[toSlot].x);
        if (endAngle < startAngle) endAngle += 2 * Mathf.PI;
        Quaternion startRot = Quaternion.Euler(0, fromSlot * 90, 0);
        Quaternion endRot = Quaternion.Euler(0, toSlot * 90, 0);
        Vector3 pos = center;
        for (float t = 0; t < 1f; t += Time.deltaTime / duration)
        {
            piece.position = pos;
            piece.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        piece.position = pos;
        piece.localRotation = endRot;
        var tracker = piece.GetComponent<PieceSlotTracker>();
        if (tracker == null) tracker = piece.gameObject.AddComponent<PieceSlotTracker>();
        tracker.slotIndex = toSlot;
        tile.SetSlotOccupant(fromSlot, null);
    }
} 