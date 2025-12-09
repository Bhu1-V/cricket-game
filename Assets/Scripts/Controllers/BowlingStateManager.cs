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

    // --- Base Constants (Over the Wicket) ---
    public const float SwingSpeedMin = 25f;
    public const float SwingSpeedMax = 40f;
    public const float SwingDirMin = -4f;
    public const float SwingDirMax = 2f;

    public const float SpinSpeedMin = 15f;
    public const float SpinSpeedMax = 20f;
    public const float SpinDirMin = -0.4f;
    public const float SpinDirMax = 0.2f;

    [Header("Current Internal Settings (Base Values)")]
    // We keep these stored as "Base" values (relative to the bowler's perspective basically)
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

    // --- EFFECTIVE GETTERS (For UI) ---
    // These calculate the Range and Value based on the current Side (Over/Around)

    public float GetMinDirection() {
        float baseMin = isSwingDelivery ? SwingDirMin : SpinDirMin;
        float baseMax = isSwingDelivery ? SwingDirMax : SpinDirMax;
        // If Around Wicket, the range flips: [-4, 2] becomes [-2, 4]
        return isOverTheWicket ? baseMin : -baseMax;
    }

    public float GetMaxDirection() {
        float baseMin = isSwingDelivery ? SwingDirMin : SpinDirMin;
        float baseMax = isSwingDelivery ? SwingDirMax : SpinDirMax;
        return isOverTheWicket ? baseMax : -baseMin;
    }

    public float GetEffectiveDirection() {
        float baseVal = isSwingDelivery ? swingDirection : spinDirection;
        // If Around Wicket, effective direction is opposite of base
        return isOverTheWicket ? baseVal : -baseVal;
    }

    // --- UI SETTERS ---

    public void SetCurrentSpeed(float val) {
        if(isSwingDelivery) swingReleaseSpeed = val;
        else spinReleaseSpeed = val;
    }

    public void SetCurrentDirectionFromUI(float uiVal) {
        // uiVal is the "Effective" direction. We need to store the "Base" direction.
        // Over: Base = UI
        // Around: Base = -UI
        float baseVal = isOverTheWicket ? uiVal : -uiVal;

        if(isSwingDelivery) {
            swingDirection = baseVal;
            Debug.Log($"[Settings] Swing Effective: {uiVal} | Stored Base: {swingDirection}");
        } else {
            spinDirection = baseVal;
            Debug.Log($"[Settings] Spin Effective: {uiVal} | Stored Base: {spinDirection}");
        }
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

        // 1. Get Params
        float currentSpeed = isSwingDelivery ? swingReleaseSpeed : spinReleaseSpeed;

        // 2. Calculate Effective Direction (Physics uses this directly now)
        float effectiveDirection = GetEffectiveDirection();

        Vector3 startPos = isOverTheWicket ? overTheWicketPos.position : aroundTheWicketPos.position;

        // 3. Trajectory Calculation
        Vector3 displacement = _lockedTargetPosition - startPos;
        float time = displacement.magnitude / currentSpeed;

        float vY = (displacement.y - (0.5f * config.gravity * Mathf.Pow(time, 2))) / time;
        float vZ = displacement.z / time;
        float vX = 0f;

        if(isSwingDelivery) {
            Rigidbody rb = ballController.GetComponent<Rigidbody>();
            float mass = rb != null ? rb.mass : 1f;
            // Use effectiveDirection directly
            float swingForceX = effectiveDirection * config.maxSwingForce * _lockedAccuracy;
            float accX = swingForceX / mass;
            vX = (displacement.x - (0.5f * accX * Mathf.Pow(time, 2))) / time;
        } else {
            vX = displacement.x / time;
        }

        Vector3 initialVelocity = new Vector3(vX, vY, vZ);
        Vector3 controlInput = new Vector3(effectiveDirection, 0, 0);

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