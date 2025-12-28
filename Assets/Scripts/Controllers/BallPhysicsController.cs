using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))] // Enforce SphereCollider
[RequireComponent(typeof(LineRenderer))]
public class BallPhysicsController : MonoBehaviour {
    private Rigidbody _rb;
    private SphereCollider _col;
    private LineRenderer _lineRenderer;
    private BowlingStateManager _stateManager;
    private IBowlingType _currentDeliveryType;

    private Vector3 _currentControlInput;
    private float _strengthFactor;

    // DETERMINISTIC STATE
    private Vector3 _startBottomPosition;
    private Vector3 _initialVelocity;
    private Vector3 _constantAcceleration;
    private float _timeAlive;

    private Vector3 _lastFrameVelocity;

    [Header("Collision Settings")]
    public LayerMask groundLayer = ~0;

    // AUTO-CALCULATED RADIUS
    public float BallRadius { get; private set; }

    [Header("Debug Settings")]
    public bool debugMode = true;
    public Color debugLineColor = Color.red;
    [Range(0.05f, 0.5f)] public float debugLineWidth = 0.1f;

    public enum BallState { Idle, MidAir, Bounced, Finished }
    public BallState CurrentState { get; set; }

    void Awake() {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<SphereCollider>(); // Get the collider
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.isKinematic = true;

        // CALCULATE TRUE WORLD RADIUS
        BallRadius = _col.radius * transform.lossyScale.x;

        _stateManager = FindAnyObjectByType<BowlingStateManager>();
        _lineRenderer = GetComponent<LineRenderer>();
        if(_lineRenderer != null) {
            _lineRenderer.startWidth = debugLineWidth;
            _lineRenderer.endWidth = debugLineWidth;
            _lineRenderer.positionCount = 0;
            Material lineMat = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.material = lineMat;
            _lineRenderer.startColor = debugLineColor;
            _lineRenderer.endColor = debugLineColor;
        }
    }

    public float GetRadiusForGizmos() {
        if(_col == null) _col = GetComponent<SphereCollider>();
        if(_col != null) return _col.radius * transform.lossyScale.x;
        return 0.05f;
    }

    public void ResetBall(Vector3 startPosition) {
        CurrentState = BallState.Idle;
        _rb.isKinematic = true;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        _lastFrameVelocity = Vector3.zero;
    }

    public void ClearDebugLine() {
        if(_lineRenderer != null) _lineRenderer.positionCount = 0;
    }

    public void StartDelivery(IBowlingType deliveryType, float strength, Vector3 controlInput, Vector3 initialVelocity, Vector3 acceleration, Vector3 startBottom) {
        _currentDeliveryType = deliveryType;
        _strengthFactor = strength;
        _currentControlInput = controlInput;

        if(_lineRenderer != null) _lineRenderer.positionCount = 0;

        CurrentState = BallState.MidAir;
        _rb.isKinematic = true;

        _startBottomPosition = startBottom;
        _initialVelocity = initialVelocity;
        _constantAcceleration = acceleration;
        _timeAlive = 0f;

        _currentDeliveryType.ApplyInitialForce(_rb, initialVelocity, Vector3.up, _strengthFactor);
        _lastFrameVelocity = initialVelocity;
    }

    void Update() {
        if(CurrentState == BallState.MidAir || CurrentState == BallState.Bounced) {
            if(debugMode) AddDebugPoint();
        }
    }

    void FixedUpdate() {
        if(CurrentState == BallState.Idle || CurrentState == BallState.Finished) return;

        if(CurrentState == BallState.MidAir) {
            _timeAlive += Time.fixedDeltaTime;

            // 1. Calculate Frame Positions
            Vector3 nextBottomPos = _startBottomPosition
                            + (_initialVelocity * _timeAlive)
                            + (0.5f * _constantAcceleration * _timeAlive * _timeAlive);

            Vector3 nextCenterPos = nextBottomPos + (Vector3.up * BallRadius);
            Vector3 nextVel = _initialVelocity + (_constantAcceleration * _timeAlive);

            Vector3 currentCenterPos = _rb.position;

            // --- NEW: TIME REWIND & VERTICAL TRAP ---

            // 1. Check ground height at the DESTINATION
            Vector3 probeOrigin = nextCenterPos + Vector3.up;
            if(Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit groundHit, 5.0f, groundLayer, QueryTriggerInteraction.Ignore)) {

                float groundHeight = groundHit.point.y;
                float heightAtEnd = nextCenterPos.y - groundHeight; // Height of center above ground
                float radiusThreshold = BallRadius + 0.005f;

                // Did we penetrate the ground this frame?
                if(heightAtEnd <= radiusThreshold) {

                    // 2. Calculate "Time Rewind" Fraction
                    // Previous height (approximate using flat ground assumption for the step)
                    float heightAtStart = currentCenterPos.y - groundHeight;

                    // How much did we drop in total?
                    float totalVerticalStep = heightAtStart - heightAtEnd;

                    // How much of that drop happened BEFORE we hit the radius?
                    float distanceToImpact = heightAtStart - BallRadius;

                    // Fraction of the frame (0.0 to 1.0) where impact occurred
                    float t = Mathf.Clamp01(distanceToImpact / totalVerticalStep);

                    if(debugMode) Debug.Log($"[Time Rewind] Frame Hit Fraction: {t:P1} | Correcting X/Z Overshoot");

                    // 3. Interpolate Position
                    // We rewind X/Z to the exact moment of impact
                    Vector3 impactPos = Vector3.Lerp(currentCenterPos, nextCenterPos, t);

                    // Ensure Y is perfectly snapped to the surface
                    impactPos.y = groundHeight + BallRadius;

                    // 4. Teleport & Handover
                    transform.position = impactPos;
                    _rb.position = impactPos;

                    // 5. Update Velocity?
                    // Ideally, we should also rewind velocity, but using nextVel is safe enough for bounce physics
                    // unless extreme drag is present.
                    _lastFrameVelocity = nextVel;

                    HandlePitchBounce();
                    return;
                }
            }

            // Fallback: Inner Core Cast (For Walls/Wickets only now)
            Vector3 displacement = nextCenterPos - currentCenterPos;
            float dist = displacement.magnitude;
            float castRadius = BallRadius * 0.2f;

            if(dist > 0 && Physics.SphereCast(currentCenterPos, castRadius, displacement.normalized, out RaycastHit hit, dist, groundLayer, QueryTriggerInteraction.Ignore)) {
                if(hit.collider.TryGetComponent(out IPitch pitch)) {
                    // This catches grazing if the vertical trap somehow misses
                    Vector3 snapPos = hit.point + (hit.normal * (BallRadius + 0.001f));
                    transform.position = snapPos;
                    _rb.position = snapPos;
                    _lastFrameVelocity = nextVel;
                    HandlePitchBounce();
                    return;
                }
            }

            // Normal Movement
            _rb.MovePosition(nextCenterPos);
            _lastFrameVelocity = nextVel;

        } else if(CurrentState == BallState.Bounced) {
            _rb.AddForce(Vector3.up * _stateManager.config.gravity, ForceMode.Acceleration);
        }
    }

    private void AddDebugPoint() {
        if(_lineRenderer == null) return;
        _lineRenderer.positionCount++;
        _lineRenderer.SetPosition(_lineRenderer.positionCount - 1, transform.position);
    }

    void OnCollisionEnter(Collision collision) {
        if(collision.gameObject.TryGetComponent(out IWicket w) || collision.gameObject.TryGetComponent(out IBoundary b)) {
            StopBall();
            return;
        }
        if(CurrentState == BallState.MidAir && collision.gameObject.TryGetComponent(out IPitch pitch)) {
            HandlePitchBounce();
        }
    }

    private void HandlePitchBounce() {
        CurrentState = BallState.Bounced;

        _rb.isKinematic = false;
        _rb.angularVelocity = Vector3.zero;

        Vector3 incoming = _lastFrameVelocity;
        float bounceY = Mathf.Abs(incoming.y) * _stateManager.config.restitution;
        Vector3 naturalBounce = new Vector3(incoming.x, bounceY, incoming.z);

        Vector3 finalVel = _currentDeliveryType.HandleBouncePhysics(naturalBounce, Vector3.up, _strengthFactor, _currentControlInput);

        _rb.linearVelocity = finalVel;
        _stateManager.OnBallBounce();
    }

    private void StopBall() {
        CurrentState = BallState.Finished;
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;
        _stateManager.OnDeliveryFinished();
    }

    private void OnDrawGizmos() {
        Gizmos.color = new Color(0.5f, 0f, 0f);
        float r = (Application.isPlaying) ? BallRadius : GetRadiusForGizmos();
        Gizmos.DrawWireSphere(transform.position, r);
    }
}