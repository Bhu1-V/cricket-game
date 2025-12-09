using UnityEngine;
using System.Collections.Generic;

public class BowlingStateManager : MonoBehaviour {
    [Header("Dependencies")]
    public BowlingConfig config;
    public BallPhysicsController ballController;
    public BounceMarkerController markerController;
    public InputReaderChannel inputReader;

    [Header("Bowler Positions")]
    public Transform overTheWicketPos;
    public Transform aroundTheWicketPos;

    [Header("State")]
    public bool isSwingDelivery = true;
    public bool isOverTheWicket = true;

    // --- ACCURACY VARIANCE (Line & Speed) ---
    [Header("Accuracy Penalty Settings")]
    [Tooltip("Max variance in METERS from the target line (X-axis) when accuracy is 0.")]
    public float maxLineError = 1.5f;
    [Tooltip("Max variance in SPEED when accuracy is 0.")]
    public float maxSpeedError = 3.0f;

    // --- Constants ---
    public const float SwingSpeedMin = 25f;
    public const float SwingSpeedMax = 40f;
    public const float SwingDirMin = -4f;
    public const float SwingDirMax = 2f;

    public const float SpinSpeedMin = 17.5f;
    public const float SpinSpeedMax = 20f;
    public const float SpinDirMin = -0.2f;
    public const float SpinDirMax = 0.5f;

    [Header("Current Internal Settings")]
    [Range(SwingSpeedMin, SwingSpeedMax)] public float swingReleaseSpeed = 30f;
    [Range(SwingDirMin, SwingDirMax)] public float swingDirection = -1f;

    [Range(SpinSpeedMin, SpinSpeedMax)] public float spinReleaseSpeed = 18f;
    [Range(SpinDirMin, SpinDirMax)] public float spinDirection = -0.1f;

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

    void Start() {
        SetDeliveryType(true);
    }

    void OnDestroy() => UnsubscribeEvents();

    private void SubscribeEvents() {
        if(onBounceMarkerSet != null) onBounceMarkerSet.OnEventRaised += SetBounceTargetPosition;
        if(onAccuracyValueSet != null) onAccuracyValueSet.OnEventRaised += SetAccuracyValue;
        if(onBowlTriggered != null) onBowlTriggered.OnEventRaised += ExecuteBowl;
    }

    private void UnsubscribeEvents() {
        if(onBounceMarkerSet != null) onBounceMarkerSet.OnEventRaised -= SetBounceTargetPosition;
        if(onAccuracyValueSet != null) onAccuracyValueSet.OnEventRaised -= SetAccuracyValue;
        if(onBowlTriggered != null) onBowlTriggered.OnEventRaised -= ExecuteBowl;
    }

    // --- UI Helpers ---
    public float GetMinDirection() => isOverTheWicket ? (isSwingDelivery ? SwingDirMin : SpinDirMin) : -(isSwingDelivery ? SwingDirMax : SpinDirMax);
    public float GetMaxDirection() => isOverTheWicket ? (isSwingDelivery ? SwingDirMax : SpinDirMax) : -(isSwingDelivery ? SwingDirMin : SpinDirMin);
    public float GetEffectiveDirection() => isOverTheWicket ? (isSwingDelivery ? swingDirection : spinDirection) : -(isSwingDelivery ? swingDirection : spinDirection);

    public void SetCurrentSpeed(float val) {
        if(isSwingDelivery) swingReleaseSpeed = val;
        else spinReleaseSpeed = val;
    }

    public void SetCurrentDirectionFromUI(float uiVal) {
        float baseVal = isOverTheWicket ? uiVal : -uiVal;
        if(isSwingDelivery) swingDirection = Mathf.Clamp(baseVal, SwingDirMin, SwingDirMax);
        else spinDirection = Mathf.Clamp(baseVal, SpinDirMin, SpinDirMax);
    }

    public void SetDeliveryType(bool isSwing) {
        isSwingDelivery = isSwing;
        _currentDeliveryType = isSwingDelivery ? _deliveryTypes["Swing"] : _deliveryTypes["Spin"];
        ResetBallToStart();
    }

    public void ToggleBowlingSide() {
        isOverTheWicket = !isOverTheWicket;
        ResetBallToStart();
    }

    private void ResetBallToStart() {
        if(ballController == null) return;
        Vector3 startPos = isOverTheWicket ? overTheWicketPos.position : aroundTheWicketPos.position;
        ballController.ResetBall(startPos);
    }

    // --- Bowling Logic ---
    public void SetAccuracyValue(float value) {
        _lockedAccuracy = value;
        CheckReadiness();
    }

    private void SetBounceTargetPosition(Vector3 targetPosition) {
        _lockedTargetPosition = targetPosition;
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

        // 1. Get Base Inputs (The Player's Intent)
        float baseSpeed = isSwingDelivery ? swingReleaseSpeed : spinReleaseSpeed;
        float intendedDirection = GetEffectiveDirection(); // This is the Slider Value

        // 2. Calculate Variance Factor
        // Low Accuracy (0.0) -> high errorFactor (1.0)
        float errorFactor = 1f - _lockedAccuracy;

        // 3. Apply Penalty: NOISY SPEED
        float speedNoise = Random.Range(-maxSpeedError, maxSpeedError) * errorFactor;
        float finalSpeed = baseSpeed + speedNoise;

        // 4. Apply Penalty: NOISY LINE (Missing the target)
        // We add noise to the Target Position X, simulating a "Wide" or "Leg side" error
        float lineNoise = Random.Range(-maxLineError, maxLineError) * errorFactor;
        Vector3 noisyTargetPos = _lockedTargetPosition;
        noisyTargetPos.x += lineNoise;

        Debug.Log($"[Bowl] Acc: {_lockedAccuracy:F2} | Line Error: {lineNoise:F2}m | Speed Error: {speedNoise:F1}");

        // 5. Trajectory Calculation (Aiming for the NOISY target)
        Vector3 startPos = isOverTheWicket ? overTheWicketPos.position : aroundTheWicketPos.position;
        Vector3 displacement = noisyTargetPos - startPos;
        float time = displacement.magnitude / finalSpeed;

        float vY = (displacement.y - (0.5f * config.gravity * Mathf.Pow(time, 2))) / time;
        float vZ = displacement.z / time;

        // Horizontal Velocity (Aiming logic)
        // Note: For Swing, we still compensate based on the INTENDED direction so the ball *starts* on a line 
        // that would hit the target if it swung fully. But if accuracy is low, it won't swing fully, 
        // so it will drift off that calculated line.
        float vX = 0f;

        if(isSwingDelivery) {
            Rigidbody rb = ballController.GetComponent<Rigidbody>();
            float mass = rb != null ? rb.mass : 1f;

            // Expected Swing Force (Assuming Perfect Execution)
            float expectedSwingForceX = intendedDirection * config.maxSwingForce;
            float accX = expectedSwingForceX / mass;

            vX = (displacement.x - (0.5f * accX * Mathf.Pow(time, 2))) / time;
        } else {
            vX = displacement.x / time;
        }

        Vector3 initialVelocity = new Vector3(vX, vY, vZ);

        // 6. Pass INTENDED Direction to Physics
        // The Delivery scripts will use _lockedAccuracy to DAMPEN this value.
        Vector3 controlInput = new Vector3(intendedDirection, 0, 0);

        ballController.StartDelivery(_currentDeliveryType, _lockedAccuracy, controlInput, initialVelocity);

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
    }
}