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
    public const float SpinSpeedMin = 17.5f;
    public const float SpinSpeedMax = 20f;
    public const float SpinDirMin = -1f;
    public const float SpinDirMax = 1f;

    [Header("Current Internal Settings")]
    public float spinReleaseSpeed = 18f;

    [Range(-100f, 100f)] public float rawDirectionInput = 0f;

    // REMOVED manual ballRadius field. We fetch it from the BallPhysicsController.

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
        if(!isSwingDelivery) spinReleaseSpeed = val;
    }

    public void SetRawDirectionFromUI(float val) {
        rawDirectionInput = val;
    }

    public float GetEffectiveDirection() {
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
        float desiredSpeed = isSwingDelivery ? config.maxBallSpeed : spinReleaseSpeed;
        float directionMultiplier = GetEffectiveDirection();

        Vector3 startPos = isOverTheWicket ? overTheWicketPos.position : aroundTheWicketPos.position;
        Vector3 targetPos = _lockedTargetPosition;

        // DYNAMIC RADIUS FETCH
        float currentRadius = ballController.BallRadius;

        // Target Logic: We want the BOTTOM to be at the marker.
        // So the Center target is Marker + Radius.
        targetPos.y += currentRadius;

        Vector3 displacement = targetPos - startPos;

        // 2. Calculate Acceleration
        Vector3 acceleration = CalculateAcceleration(directionMultiplier, _lockedStrength);

        // 3. Shared Solver
        TrajectoryResult result = SolveTrajectory(displacement, acceleration, desiredSpeed);

        // 4. Launch
        Vector3 controlInput = new Vector3(directionMultiplier, 0, 0);

        // Pass the Start BOTTOM position so the controller can drive the bottom along the path
        Vector3 startBottom = startPos - (Vector3.up * currentRadius);

        // Wait! The 'startPos' is usually the hand position (Center of ball).
        // If 'startPos' IS the center, then the path calculation logic:
        // displacement = targetCenter - startCenter
        // This is correct.
        // But the Controller's 'StartDelivery' asks for 'startBottom'.
        // So we calculate startBottom based on startPos (Center).

        ballController.StartDelivery(_currentDeliveryType, _lockedStrength, controlInput, result.velocity, acceleration, startBottom);

        // Keep state locked for preview during flight
    }

    // --- Shared Physics Logic ---

    private Vector3 CalculateAcceleration(float dirMultiplier, float strength) {
        Vector3 acc = Vector3.up * config.gravity;
        if(isSwingDelivery) {
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

        ResetState();
        ResetBallToStart();

        onDeliveryFinished.RaiseEvent();
    }

    private void OnDrawGizmos() {
        if(markerController != null && ballController != null) {
            // Get radius dynamically (works in Editor via helper)
            float r = Application.isPlaying ? ballController.BallRadius : ballController.GetRadiusForGizmos();

            Vector3 groundPos = _hasTargetPosition ? _lockedTargetPosition : markerController.transform.position;
            Vector3 targetBallCenter = groundPos + Vector3.up * r;

            Gizmos.color = Color.red; Gizmos.DrawWireSphere(groundPos, 0.15f);
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(targetBallCenter, r);

            if(overTheWicketPos != null && aroundTheWicketPos != null) {
                Vector3 startPos = isOverTheWicket ? overTheWicketPos.position : aroundTheWicketPos.position;
                Gizmos.color = Color.green; Gizmos.DrawWireSphere(startPos, 0.15f);

                if(config != null) DrawPathPreview(startPos, targetBallCenter, r);
            }
        }
    }

    private void DrawPathPreview(Vector3 startCenter, Vector3 endCenter, float radius) {
        float speed = isSwingDelivery ? config.maxBallSpeed : spinReleaseSpeed;
        float dir = GetEffectiveDirection();
        float strength = (_lockedStrength >= 0) ? _lockedStrength : 1f;

        Vector3 disp = endCenter - startCenter;
        Vector3 acc = CalculateAcceleration(dir, strength);
        TrajectoryResult result = SolveTrajectory(disp, acc, speed);

        Gizmos.color = Color.cyan;

        // We draw the path of the BOTTOM of the ball
        Vector3 bottomOffset = Vector3.down * radius;
        Vector3 prev = startCenter + bottomOffset;

        int res = Mathf.Max(100, Mathf.CeilToInt(result.time * 60));

        for(int i = 1; i <= res; i++) {
            float ct = (result.time / res) * i;
            Vector3 pointCenter = startCenter + (result.velocity * ct) + (0.5f * acc * ct * ct);
            Vector3 pointBottom = pointCenter + bottomOffset;

            Gizmos.DrawLine(prev, pointBottom);
            prev = pointBottom;
        }
    }
}