using UnityEngine;
using UnityEngine.InputSystem;

public class BounceMarkerController : MonoBehaviour {
    [Header("Dependencies")]
    public Transform markerVisual;

    [Tooltip("The 'Lock' trigger. Should be the same event triggered by SPACE (Accuracy Capture).")]
    public VoidEventChannelSO onMarkerLock;

    public Vector2EventChannelSO onMoveMarker;

    [Header("Settings")]
    public float moveSpeed = 5f;
    public Vector3 minBounds = new Vector3(-2f, 0f, 0f);
    public Vector3 maxBounds = new Vector3(2f, 0f, 10f);

    public Vector3EventChannelSO onBounceMarkerSet;
    public VoidEventChannelSO onDeliveryFinished;

    private Vector2 _moveDirection;
    private bool _isMarkerActive = true;

    void Start() {
        markerVisual.gameObject.SetActive(true);

        onMoveMarker.OnEventRaised += SetMoveDirection;

        if(onMarkerLock != null) onMarkerLock.OnEventRaised += LockMarkerPosition;
        if(onDeliveryFinished != null) onDeliveryFinished.OnEventRaised += ResetMarker;
    }

    void OnDestroy() {
        onMoveMarker.OnEventRaised -= SetMoveDirection;
        if(onMarkerLock != null) onMarkerLock.OnEventRaised -= LockMarkerPosition;
        if(onDeliveryFinished != null) onDeliveryFinished.OnEventRaised -= ResetMarker;
    }

    void Update() {
        if(_isMarkerActive) {
            MoveMarker();
        }
    }

    public void SetMoveDirection(Vector2 direction) {
        if(!_isMarkerActive) return;
        _moveDirection = direction;
    }

    private void MoveMarker() {
        if(_moveDirection.sqrMagnitude > 0.01f) {
            Vector3 movement = new Vector3(_moveDirection.x, 0, _moveDirection.y) * moveSpeed * Time.deltaTime;
            markerVisual.position += movement;

            Vector3 clampedPosition = markerVisual.position;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, minBounds.x, maxBounds.x);
            clampedPosition.z = Mathf.Clamp(clampedPosition.z, minBounds.z, maxBounds.z);
            markerVisual.position = clampedPosition;
        }
    }

    private void LockMarkerPosition() {
        if(!_isMarkerActive) return;

        _isMarkerActive = false;
        // Raise event to tell StateManager where we locked
        onBounceMarkerSet.RaiseEvent(markerVisual.position);
    }

    public void ResetMarker() {
        _isMarkerActive = true;
    }
}