using UnityEngine;
using UnityEngine.InputSystem;

// This script acts as the centralized gateway between the Unity Input System 
// and the rest of the game logic via Scriptable Object Event Channels.
public class InputReaderChannel : MonoBehaviour, BallControls.IBallActions {
    [Header("Event Output Channels")]
    [Tooltip("Vector2 for continuous marker movement (X/Z axes). Consumed by BounceMarkerController.")]
    public Vector2EventChannelSO onMoveMarker;

    [Tooltip("Raised on Tap 1: Signals the Accuracy Meter to stop and capture its value.")]
    public VoidEventChannelSO onAccuracyCapture;

    [Tooltip("Raised on Tap 2: Signals the State Manager to execute the bowl. Consumed by BowlingStateManager.")]
    public VoidEventChannelSO onBowlTriggered;

    [Header("Event Input Channels (Listening)")]
    [Tooltip("Listens for the final accuracy value after Tap 1. Raised by the Accuracy Meter.")]
    public FloatEventChannelSO onAccuracyValueSet;

    private BallControls _controls;
    private bool _isAccuracyCaptured = false;
    private float _capturedAccuracyValue = 0f;

    // --- Unity Lifecycle ---
    private void OnEnable() {
        if(_controls == null) {
            _controls = new BallControls();
            // Implement the IBallActions interface callbacks
            _controls.Ball.SetCallbacks(this);
        }
        _controls.Ball.Enable();

        // Subscribe to the accuracy result from the (simulated) Accuracy Meter
        onAccuracyValueSet.OnEventRaised += HandleAccuracyCaptured;
    }

    private void OnDisable() {
        _controls.Ball.Disable();
        onAccuracyValueSet.OnEventRaised -= HandleAccuracyCaptured;
    }

    // --- IBallActions Implementation ---

    // 1. Movement Input Handler (Maps Input System to SO Event)
    public void OnMove(InputAction.CallbackContext context) {
        // Continuous input reading for the marker position
        if(context.performed || context.started || context.canceled) {
            // Raise the Vector2 event channel for the BounceMarkerController to consume
            onMoveMarker.RaiseEvent(context.ReadValue<Vector2>());
        }
    }

    // 2. Bowl Input Handler (Manages Two-Tap State)
    public void OnBowl(InputAction.CallbackContext context) {
        // We only care about the performed (tap/button press) phase
        if(!context.performed) return;

        if(!_isAccuracyCaptured) {
            // --- TAP 1: Capture Accuracy ---
            Debug.Log("Tap 1: Capturing Accuracy. Meter should stop now.");
            onAccuracyCapture.RaiseEvent();
            // State waits for HandleAccuracyCaptured event before allowing Tap 2
        } else {
            // --- TAP 2: Execute Bowl ---
            Debug.Log($"Tap 2: Executing Bowl with Accuracy: {_capturedAccuracyValue:F2}");
            onBowlTriggered.RaiseEvent();

            // Reset state for the next delivery
            _isAccuracyCaptured = false;
            _capturedAccuracyValue = 0f;
        }
    }

    // Called when the FloatEventChannelSO is raised by the Accuracy Meter
    private void HandleAccuracyCaptured(float accuracy) {
        _capturedAccuracyValue = accuracy;
        _isAccuracyCaptured = true;
        Debug.Log($"Accuracy captured: {accuracy:F2}. Waiting for Tap 2.");
    }
}