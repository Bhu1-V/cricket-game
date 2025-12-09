using UnityEngine;
using UnityEngine.InputSystem;

// Handles the movement of the visual bounce marker based on input from the InputReaderChannel.
public class BounceMarkerController : MonoBehaviour {
    [Header("Dependencies")]
    [Tooltip("The visual marker GameObject.")]
    public Transform markerVisual;
    public VoidEventChannelSO onBowlPressed; // Listen for the bowl trigger (Tap 2)
    public Vector2EventChannelSO onMoveMarker; // Listen for continuous movement from InputReader

    [Header("Settings")]
    public float moveSpeed = 5f;
    public Vector3 minBounds = new Vector3(-2f, 0f, 0f); // X-Z bounds on the pitch
    public Vector3 maxBounds = new Vector3(2f, 0f, 10f);

    // Output event channel to notify the State Manager of the final position
    public Vector3EventChannelSO onBounceMarkerSet;

    private Vector2 _moveDirection;
    private bool _isMarkerActive = true;

    void Start() {
        // Ensure the marker is initially visible and in a safe position.
        markerVisual.gameObject.SetActive(true);
        // Using localPosition ensures bounds checking works relative to the Bowler's position/parent.
        //markerVisual.localPosition = Vector3.zero;

        // Subscribe to input and bowl events
        onMoveMarker.OnEventRaised += SetMoveDirection;
        // NOTE: The InputReaderChannel raises this event on the second tap.
        onBowlPressed.OnEventRaised += LockMarkerPosition;
    }

    void OnDestroy() {
        onMoveMarker.OnEventRaised -= SetMoveDirection;
        onBowlPressed.OnEventRaised -= LockMarkerPosition;
    }

    void Update() {
        if(_isMarkerActive) {
            MoveMarker();
        }
    }

    // Subscribes to the event raised by InputReaderChannel
    public void SetMoveDirection(Vector2 direction) {
        if(!_isMarkerActive) return;
        _moveDirection = direction;
    }

    private void MoveMarker() {
        // Only move if there is active input
        if(_moveDirection.sqrMagnitude > 0.01f) {
            // The Vector2 input (X, Y) maps to world X (lateral) and Z (depth)
            Vector3 movement = new Vector3(_moveDirection.x, 0, _moveDirection.y) * moveSpeed * Time.deltaTime;
            markerVisual.localPosition += movement;

            // Clamp the marker position within the pitch bounds
            Vector3 clampedPosition = markerVisual.localPosition;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, minBounds.x, maxBounds.x);
            clampedPosition.z = Mathf.Clamp(clampedPosition.z, minBounds.z, maxBounds.z);
            markerVisual.localPosition = clampedPosition;
        }
    }

    private void LockMarkerPosition() {
        _isMarkerActive = false;
        // Broadcast the final, locked position to the State Manager
        onBounceMarkerSet.RaiseEvent(markerVisual.position);
    }

    // Utility to reset the marker after the ball has settled
    public void ResetMarker() {
        _isMarkerActive = true;
        // Optionally reset position here
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected() {
        // Visualize the movement bounds in the editor
        Gizmos.color = Color.green;
        Vector3 center = (minBounds + maxBounds) / 2f;
        Vector3 size = maxBounds - minBounds;
        Gizmos.DrawWireCube(transform.position + center, size);
    }
#endif
}