using UnityEngine;
using System.Collections.Generic;

// Manages the overall bowling process, state transitions, and type injection.
public class BowlingStateManager : MonoBehaviour {
    // --- Dependencies & Injection ---
    [Header("Dependencies")]
    public BowlingConfig config;
    public BallPhysicsController ballController;
    public BounceMarkerController markerController;
    public Transform bowlerStartPosition;

    [Header("Event Listeners (Inputs)")]
    // Subscribed to: BounceMarkerController (Final locked position after Tap 1)
    public Vector3EventChannelSO onBounceMarkerSet;
    // Subscribed to: InputReaderChannel (Final bowl trigger after Tap 2)
    public VoidEventChannelSO onBowlTriggered;
    // Subscribed to: Accuracy Meter / InputReader (Accuracy value captured on Tap 1)
    public FloatEventChannelSO onAccuracyValueSet;

    [Header("Event Emitters (Outputs)")]
    public VoidEventChannelSO onDeliveryFinished; // For resetting UI/Game State

    // --- Delivery Configuration Fields (Set by UI/Input) ---
    [Header("Current Delivery Settings (UI Input Simulation)")]
    public bool isSwingDelivery = true; // Simulating UI toggle for now
    public Vector3 swingDirection = Vector3.right; // Right for out-swing, Left for in-swing
    public Vector3 spinDirection = new Vector3(1, 0, 1); // e.g., Top-right spin for Off-spinner

    // --- State and Runtime Data ---
    private IBowlingType _currentDeliveryType;
    private Vector3 _bounceTargetPosition;
    private float _accuracyValue = 1.0f; // Default full accuracy
    private float _currentPower = 1.0f; // Simulating power meter

    private Dictionary<string, IBowlingType> _deliveryTypes;

    void Awake() {
        // DIP: Initialize all concrete delivery types and store them by name.
        _deliveryTypes = new Dictionary<string, IBowlingType>
        {
            { "Swing", new SwingDelivery(config) },
            { "Spin", new SpinDelivery(config) }
        };

        // Subscribe to input events
        onBounceMarkerSet.OnEventRaised += SetBounceTargetPosition;
        onAccuracyValueSet.OnEventRaised += SetAccuracyValue; // Captures value after Tap 1
        onBowlTriggered.OnEventRaised += ExecuteBowl; // Final bowl trigger on Tap 2
    }

    void OnDestroy() {
        // Unsubscribe to prevent memory leaks
        onBounceMarkerSet.OnEventRaised -= SetBounceTargetPosition;
        onAccuracyValueSet.OnEventRaised -= SetAccuracyValue;
        onBowlTriggered.OnEventRaised -= ExecuteBowl;
    }

    // Called by the Accuracy Meter component (or InputReader in the new flow after Tap 1)
    public void SetAccuracyValue(float value) {
        _accuracyValue = value; // Should be between 0.0 and 1.0
        Debug.Log($"Accuracy received: {_accuracyValue:F2}. Ready for Tap 2.");
    }

    // Called by the Bounce Marker Controller after the first tap locks the marker.
    private void SetBounceTargetPosition(Vector3 targetPosition) {
        _bounceTargetPosition = targetPosition;

        // OCP/DIP: Select the delivery type based on the UI selection (isSwingDelivery).
        _currentDeliveryType = isSwingDelivery
            ? _deliveryTypes["Swing"]
            : _deliveryTypes["Spin"];

        Debug.Log($"Bounce target set. Waiting for final bowl trigger (Tap 2).");
    }

    // Called by the InputReaderChannel on the second tap (Bowl Event).
    private void ExecuteBowl() {
        if(ballController.CurrentState != BallPhysicsController.BallState.Idle) return; // Only bowl when idle
        if(_bounceTargetPosition == Vector3.zero) {
            Debug.LogError("Bowl executed without a locked bounce target! Tap 1 likely didn't occur.");
            return;
        }

        // 1. Calculate Initial Velocity (Parabola physics for aiming)
        Vector3 startPos = bowlerStartPosition.position;
        Vector3 displacement = _bounceTargetPosition - startPos;

        // Use projectile motion formula to find initial velocity
        // v = d / t + 0.5 * g * t
        Vector3 gravity = Physics.gravity;
        Vector3 initialVelocity = (displacement / config.timeToTarget) - (0.5f * gravity * config.timeToTarget);

        // 2. Start the delivery in the Ball Physics Controller
        ballController.StartDelivery(
            _currentDeliveryType,
            _accuracyValue,
            swingDirection,
            spinDirection,
            initialVelocity
        );

        Debug.Log($"Bowling started: {_currentDeliveryType.DeliveryName} to target {_bounceTargetPosition} with Accuracy: {_accuracyValue:F2}");
        // Reset bounce target to prevent accidental double-bowling without setting a new target
        _bounceTargetPosition = Vector3.zero;
    }

    // Called by the BallPhysicsController on collision with the pitch.
    public void OnBallBounce() {
        Debug.Log("Ball Bounced! Post-bounce physics applied.");
    }

    // Called by the BallPhysicsController when the ball stops or hits the boundary/stumps.
    public void OnDeliveryFinished() {
        Debug.Log("Delivery Finished. Resetting state.");
        ballController.CurrentState = BallPhysicsController.BallState.Idle;
        markerController.ResetMarker();
        // Reset accuracy for the next delivery attempt
        _accuracyValue = 1.0f;
        onDeliveryFinished.RaiseEvent();
    }
}