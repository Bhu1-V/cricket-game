using UnityEngine;
using System.Collections.Generic;

public class BowlingStateManager : MonoBehaviour {
    [Header("Dependencies")]
    public BowlingConfig config;
    public BallPhysicsController ballController;
    public BounceMarkerController markerController;
    public Transform bowlerStartPosition;
    public InputReaderChannel inputReader;

    [Header("Delivery Type")]
    public bool isSwingDelivery = true;

    [Header("Swing Settings (Over the Wicket)")]
    [Range(25f, 40f)] public float swingReleaseSpeed = 30f;
    [Range(-4f, 2f)] public float swingDirection = -1f;

    [Header("Spin Settings (Over the Wicket)")]
    [Range(15f, 20f)] public float spinReleaseSpeed = 18f;
    [Range(-0.4f, 0.2f)] public float spinDirection = -0.1f;

    [Header("Events")]
    public Vector3EventChannelSO onBounceMarkerSet;
    public FloatEventChannelSO onAccuracyValueSet;
    public VoidEventChannelSO onBowlTriggered;
    public VoidEventChannelSO onDeliveryFinished;

    private IBowlingType _currentDeliveryType;
    private Dictionary<string, IBowlingType> _deliveryTypes;
    private Vector3 _lockedTargetPosition;
    private float _lockedAccuracy = -1f;
    private bool _isReadyToBowl = false;

    void Awake() {
        _deliveryTypes = new Dictionary<string, IBowlingType> {
            { "Swing", new SwingDelivery(config) },
            { "Spin", new SpinDelivery(config) }
        };
        SubscribeEvents();
    }

    void OnDestroy() => UnsubscribeEvents();

    private void SubscribeEvents() {
        onBounceMarkerSet.OnEventRaised += SetBounceTargetPosition;
        onAccuracyValueSet.OnEventRaised += SetAccuracyValue;
        onBowlTriggered.OnEventRaised += ExecuteBowl;
    }

    private void UnsubscribeEvents() {
        onBounceMarkerSet.OnEventRaised -= SetBounceTargetPosition;
        onAccuracyValueSet.OnEventRaised -= SetAccuracyValue;
        onBowlTriggered.OnEventRaised -= ExecuteBowl;
    }

    public void SetAccuracyValue(float value) {
        _lockedAccuracy = value;
        CheckReadiness();
    }

    private void SetBounceTargetPosition(Vector3 targetPosition) {
        _lockedTargetPosition = targetPosition;
        _currentDeliveryType = isSwingDelivery ? _deliveryTypes["Swing"] : _deliveryTypes["Spin"];
        CheckReadiness();
    }

    private void CheckReadiness() {
        if(_lockedAccuracy >= 0f && _lockedTargetPosition != Vector3.zero) {
            _isReadyToBowl = true;
        }
    }

    private void ExecuteBowl() {
        if(ballController.CurrentState != BallPhysicsController.BallState.Idle) return;
        if(!_isReadyToBowl) return;

        // --- DETERMINE SPEED AND DIRECTION BASED ON TYPE ---
        float currentSpeed = isSwingDelivery ? swingReleaseSpeed : spinReleaseSpeed;
        float currentDirection = isSwingDelivery ? swingDirection : spinDirection;

        // 1. Calculate Time of Flight based on simple distance/speed
        Vector3 startPos = bowlerStartPosition.position;
        Vector3 displacement = _lockedTargetPosition - startPos;
        float time = displacement.magnitude / currentSpeed;

        // 2. Vertical Velocity (Standard Projectile Motion for Gravity)
        float vY = (displacement.y - (0.5f * config.gravity * Mathf.Pow(time, 2))) / time;

        // 3. Forward Velocity (Z)
        float vZ = displacement.z / time;

        // 4. Horizontal Logic (X Axis)
        float vX = 0f;

        if(isSwingDelivery) {
            // SWING: Compensate for the curve.
            Rigidbody rb = ballController.GetComponent<Rigidbody>();
            float mass = rb != null ? rb.mass : 1f;

            // Calculate expected acceleration from swing force
            // Note: We use currentDirection (the slider value) here
            float swingForceX = currentDirection * config.maxSwingForce * _lockedAccuracy;
            float accX = swingForceX / mass;

            // Apply compensation
            vX = (displacement.x - (0.5f * accX * Mathf.Pow(time, 2))) / time;
        } else {
            // SPIN: Throw STRAIGHT at the marker.
            // No swing compensation needed for mid-air travel.
            vX = displacement.x / time;
        }

        Vector3 initialVelocity = new Vector3(vX, vY, vZ);
        // Pass the slider direction into the control input for the ball physics to use later
        Vector3 controlInput = new Vector3(currentDirection, 0, 0);

        // 5. Start Physics
        ballController.StartDelivery(
            _currentDeliveryType,
            _lockedAccuracy,
            controlInput,
            initialVelocity
        );

        ResetState();
    }

    private void ResetState() {
        _isReadyToBowl = false;
        _lockedTargetPosition = Vector3.zero;
        _lockedAccuracy = -1f;
    }

    public void OnBallBounce() { }

    public void OnDeliveryFinished() {
        ballController.CurrentState = BallPhysicsController.BallState.Idle;
        markerController.ResetMarker();
        if(inputReader != null) inputReader.ResetInputState();
        onDeliveryFinished.RaiseEvent();
        transform.localPosition = Vector3.zero;
    }
}