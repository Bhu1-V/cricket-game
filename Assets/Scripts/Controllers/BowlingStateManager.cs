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

    // --- Constants ---
    // Removed Swing Min/Max constants as requested.
    // Spin still needs ranges if variable speed is allowed for spin.
    public const float SpinSpeedMin = 17.5f;
    public const float SpinSpeedMax = 20f;
    // Spin Direction could also be unified, but keeping as per current logic for now:
    public const float SpinDirMin = -1f;
    public const float SpinDirMax = 1f;

    [Header("Current Internal Settings")]
    // Swing Speed is now CONSTANT (config.maxBallSpeed), so this var is only for Spin or debug override
    public float spinReleaseSpeed = 18f;

    [Range(-100f, 100f)] public float rawDirectionInput = 0f;

    [Header("Physics Tweaks")]
    [Tooltip("The physical radius of the ball.")]
    public float ballRadius = 0.05f;

    [Header("Events")]
    public Vector3EventChannelSO onBounceMarkerSet;
    public FloatEventChannelSO onStrengthValueSet;
    public VoidEventChannelSO onBowlTriggered;
    public VoidEventChannelSO onDeliveryFinished;

    private IBowlingType _currentDeliveryType;
    private Dictionary<string, IBowlingType> _deliveryTypes;

    private Vector3 _lockedTargetPosition;
    private bool _hasTargetPosition = false;
    private float _lockedStrength = -1f;
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
        if(onStrengthValueSet == null) Debug.LogError("Missing Strength Event Channel!");
    }

    void OnDestroy() => UnsubscribeEvents();

    private void SubscribeEvents() {
        if(onBounceMarkerSet != null) onBounceMarkerSet.OnEventRaised += SetBounceTargetPosition;
        if(onStrengthValueSet != null) onStrengthValueSet.OnEventRaised += SetStrengthValue;
        if(onBowlTriggered != null) onBowlTriggered.OnEventRaised += ExecuteBowl;
    }

    private void UnsubscribeEvents() {
        if(onBounceMarkerSet != null) onBounceMarkerSet.OnEventRaised -= SetBounceTargetPosition;
        if(onStrengthValueSet != null) onStrengthValueSet.OnEventRaised -= SetStrengthValue;
        if(onBowlTriggered != null) onBowlTriggered.OnEventRaised -= ExecuteBowl;
    }

    // --- UI Helpers ---
    public void SetCurrentSpeed(float val) {
        // Only affects Spin now, as Swing is constant
        if(!isSwingDelivery) spinReleaseSpeed = val;
    }

    public void SetRawDirectionFromUI(float val) {
        rawDirectionInput = val;
    }

    public float GetEffectiveDirection() {
        // Maps -100 to 100  ->  -1.0 to 1.0
        return rawDirectionInput / 100f;
    }

    public void SetDeliveryType(bool isSwing) {
        isSwingDelivery = isSwing;
        _currentDeliveryType = isSwingDelivery ? _deliveryTypes["Swing"] : _deliveryTypes["Spin"];
        if(ballController != null) ballController.ClearDebugLine();
        ResetBallToStart();
    }

    public void ToggleBowlingSide() {
        isOverTheWicket = !isOverTheWicket;
        ResetBallToStart();
    }

    private void ResetBallToStart() {
        rawDirectionInput = 0f;
        if(ballController == null) return;
        Vector3 startPos = isOverTheWicket ? overTheWicketPos.position : aroundTheWicketPos.position;
        ballController.ResetBall(startPos);
    }

    // --- Bowling Logic ---
    public void SetStrengthValue(float value) {
        _lockedStrength = value;
        CheckReadiness();
    }

    private void SetBounceTargetPosition(Vector3 targetPosition) {
        _lockedTargetPosition = targetPosition;
        _hasTargetPosition = true;
        CheckReadiness();
    }

    private void CheckReadiness() {
        if(_lockedStrength >= 0f && _hasTargetPosition) {
            _isReadyToBowl = true;
        }
    }

    private void ExecuteBowl() {
        if(ballController.CurrentState != BallPhysicsController.BallState.Idle) return;
        if(!_isReadyToBowl) return;

        // 1. Gather Inputs
        // UNIFICATION CHANGE: Swing uses Constant Config Speed
        float desiredSpeed = isSwingDelivery ? config.maxBallSpeed : spinReleaseSpeed;

        float directionMultiplier = GetEffectiveDirection(); // -1.0 to 1.0

        Vector3 startPos = isOverTheWicket ? overTheWicketPos.position : aroundTheWicketPos.position;
        Vector3 targetPos = _lockedTargetPosition;
        targetPos.y += ballRadius;

        Vector3 displacement = targetPos - startPos;

        // 2. Calculate Acceleration
        Vector3 acceleration = CalculateAcceleration(directionMultiplier, _lockedStrength);

        // 3. Shared Solver
        TrajectoryResult result = SolveTrajectory(displacement, acceleration, desiredSpeed);

        // 4. Launch
        Vector3 controlInput = new Vector3(directionMultiplier, 0, 0);
        ballController.StartDelivery(_currentDeliveryType, _lockedStrength, controlInput, result.velocity, acceleration);

        ResetState();
    }

    // --- Shared Physics Logic ---

    private Vector3 CalculateAcceleration(float dirMultiplier, float strength) {
        Vector3 acc = Vector3.up * config.gravity;
        if(isSwingDelivery) {
            // UNIFICATION CHANGE: 
            // -100 UI Input -> -1.0 Multiplier
            // Multiplier * MaxSwingForce = Actual Force applied
            float swingAcc = dirMultiplier * config.maxSwingForce * strength;
            acc.x += swingAcc;
        }
        return acc;
    }

    private struct TrajectoryResult {
        public Vector3 velocity;
        public float time;
    }

    private TrajectoryResult SolveTrajectory(Vector3 displacement, Vector3 acceleration, float desiredSpeed) {
        float t = displacement.magnitude / desiredSpeed;
        Vector3 v0 = Vector3.zero;
        int iterations = 20;

        for(int i = 0; i < iterations; i++) {
            v0 = (displacement - (0.5f * acceleration * t * t)) / t;
            float calculatedSpeed = v0.magnitude;

            if(Mathf.Abs(calculatedSpeed - desiredSpeed) < 0.001f) break;

            float ratio = calculatedSpeed / desiredSpeed;
            t = t * Mathf.Lerp(1f, ratio, 0.5f);
        }

        return new TrajectoryResult { velocity = v0, time = t };
    }

    private void ResetState() {
        _isReadyToBowl = false;
        _lockedTargetPosition = Vector3.zero;
        _hasTargetPosition = false;
        _lockedStrength = -1f;
    }

    public void OnBallBounce() { }

    public void OnDeliveryFinished() {
        ballController.CurrentState = BallPhysicsController.BallState.Idle;
        markerController.ResetMarker();
        if(inputReader != null) inputReader.ResetInputState();
        ResetBallToStart();
        onDeliveryFinished.RaiseEvent();
    }

    private void OnDrawGizmos() {
        if(markerController != null) {
            Vector3 groundPos = _hasTargetPosition ? _lockedTargetPosition : markerController.transform.position;
            Vector3 targetBallCenter = groundPos + Vector3.up * ballRadius;

            Gizmos.color = Color.red; Gizmos.DrawWireSphere(groundPos, 0.15f);
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(targetBallCenter, ballRadius);

            if(overTheWicketPos != null && aroundTheWicketPos != null) {
                Vector3 startPos = isOverTheWicket ? overTheWicketPos.position : aroundTheWicketPos.position;
                Gizmos.color = Color.green; Gizmos.DrawWireSphere(startPos, 0.15f);

                if(config != null) DrawPathPreview(startPos, targetBallCenter);
            }
        }
    }

    private void DrawPathPreview(Vector3 start, Vector3 end) {
        // UNIFICATION CHANGE: Use Constant Config Speed for Preview too
        float speed = isSwingDelivery ? config.maxBallSpeed : spinReleaseSpeed;

        float dir = GetEffectiveDirection();
        float strength = (_lockedStrength >= 0) ? _lockedStrength : 1f;

        Vector3 disp = end - start;
        Vector3 acc = CalculateAcceleration(dir, strength);
        TrajectoryResult result = SolveTrajectory(disp, acc, speed);

        Gizmos.color = Color.cyan;
        Vector3 prev = start;
        int res = 30;
        for(int i = 1; i <= res; i++) {
            float ct = (result.time / res) * i;
            Vector3 point = start + (result.velocity * ct) + (0.5f * acc * ct * ct);
            Gizmos.DrawLine(prev, point);
            prev = point;
        }
    }
}