using UnityEngine;
using UnityEngine.InputSystem;

// This script acts as the centralized gateway between the Unity Input System 
// and the rest of the game logic via Scriptable Object Event Channels.
public class InputReaderChannel : MonoBehaviour, BallControls.IBallActions {
    [Header("Event Output Channels")]
    [Tooltip("Vector2 for continuous marker movement (X/Z axes). Consumed by BounceMarkerController.")]
    public Vector2EventChannelSO onMoveMarker;

    [Tooltip("Raised on SPACE: Signals the Accuracy Meter to stop and the Marker to lock.")]
    public VoidEventChannelSO onAccuracyCapture;

    [Tooltip("Raised on ENTER: Signals the State Manager to execute the bowl.")]
    public VoidEventChannelSO onBowlTriggered;

    [Header("Event Input Channels (Listening)")]
    [Tooltip("Listens for the final accuracy value. Raised by the Accuracy Meter.")]
    public FloatEventChannelSO onAccuracyValueSet;

    private BallControls _controls;
    private bool _isLocked = false;

    // --- Unity Lifecycle ---
    private void OnEnable() {
        if(_controls == null) {
            _controls = new BallControls();
            _controls.Ball.SetCallbacks(this);
        }
        _controls.Ball.Enable();

        // Listen for reset/cleanup if needed, or handle internal state locally
        if(onAccuracyValueSet != null)
            onAccuracyValueSet.OnEventRaised += HandleAccuracyCaptured;
    }

    private void OnDisable() {
        _controls.Ball.Disable();
        if(onAccuracyValueSet != null)
            onAccuracyValueSet.OnEventRaised -= HandleAccuracyCaptured;
    }

    // --- IBallActions Implementation ---

    // 1. Movement (WASD / Stick)
    public void OnMove(InputAction.CallbackContext context) {
        if(_isLocked) return; // Prevent movement if we have already locked the target

        if(context.performed || context.started || context.canceled) {
            onMoveMarker.RaiseEvent(context.ReadValue<Vector2>());
        }
    }

    // 2. Select Accuracy (SPACE) - Locks everything
    public void OnSelectAccuracy(InputAction.CallbackContext context) {
        if(!context.performed) return;

        if(!_isLocked) {
            Debug.Log("Input: SPACE pressed. Locking Accuracy and Marker.");
            onAccuracyCapture.RaiseEvent();
            // We set _isLocked to true locally, but usually we wait for the confirmation events
            // For responsiveness, we lock input immediately here.
            _isLocked = true;
        }
    }

    // 3. Bowl (ENTER) - Executes the delivery
    public void OnBowl(InputAction.CallbackContext context) {
        if(!context.performed) return;

        if(_isLocked) {
            Debug.Log("Input: ENTER pressed. Triggering Bowl.");
            onBowlTriggered.RaiseEvent();
        } else {
            Debug.LogWarning("Input: ENTER pressed, but Accuracy/Marker not locked yet. Press SPACE first.");
        }
    }

    // Reset state when the delivery cycle is effectively "confirmed" or finished
    // You might want to subscribe to onDeliveryFinished in a real scenario to reset _isLocked = false;
    private void HandleAccuracyCaptured(float val) {
        _isLocked = true;
    }

    // Optional: Call this from an external manager when the turn resets
    public void ResetInputState() {
        _isLocked = false;
    }
}