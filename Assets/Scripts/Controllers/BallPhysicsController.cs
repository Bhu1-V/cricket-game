using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class BallPhysicsController : MonoBehaviour {
    private Rigidbody _rb;
    private LineRenderer _lineRenderer;
    private BowlingStateManager _stateManager;
    private IBowlingType _currentDeliveryType;

    private Vector3 _currentControlInput;
    private float _strengthFactor;

    // DETERMINISTIC STATE
    private Vector3 _startPosition;
    private Vector3 _initialVelocity;
    private Vector3 _constantAcceleration;
    private float _timeAlive;

    private Vector3 _lastFrameVelocity;

    [Header("Collision Settings")]
    public float ballRadius = 0.05f; // Should match your visual ball size
    public LayerMask groundLayer = ~0; // Default to Everything, ensure Pitch is on this layer

    [Header("Debug Settings")]
    public bool debugMode = true;
    public Color debugLineColor = Color.red;
    [Range(0.05f, 0.5f)] public float debugLineWidth = 0.1f;

    public enum BallState { Idle, MidAir, Bounced, Finished }
    public BallState CurrentState { get; set; }

    void Awake() {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.isKinematic = true;

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

    public void ResetBall(Vector3 startPosition) {
        CurrentState = BallState.Idle;
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        _lastFrameVelocity = Vector3.zero;
        // Debug line persists until ClearDebugLine is called
    }

    public void ClearDebugLine() {
        if(_lineRenderer != null) _lineRenderer.positionCount = 0;
    }

    public void StartDelivery(IBowlingType deliveryType, float strength, Vector3 controlInput, Vector3 initialVelocity, Vector3 acceleration) {
        _currentDeliveryType = deliveryType;
        _strengthFactor = strength;
        _currentControlInput = controlInput;

        if(_lineRenderer != null) _lineRenderer.positionCount = 0;

        CurrentState = BallState.MidAir;
        _rb.isKinematic = true; // IMPORTANT: Kinematic Driver

        _startPosition = transform.position;
        _initialVelocity = initialVelocity;
        _constantAcceleration = acceleration;
        _timeAlive = 0f;

        _currentDeliveryType.ApplyInitialForce(_rb, initialVelocity, Vector3.up, _strengthFactor);

        _rb.linearVelocity = initialVelocity;
        _lastFrameVelocity = initialVelocity;
        _rb.angularVelocity = Vector3.zero;
    }

    void Update() {
        // Move Debug Point Generation here for smoother curves
        if(CurrentState == BallState.MidAir || CurrentState == BallState.Bounced) {
            if(debugMode) AddDebugPoint();
        }
    }

    void FixedUpdate() {
        if(CurrentState == BallState.Idle || CurrentState == BallState.Finished) return;

        if(CurrentState == BallState.MidAir) {
            // DETERMINISTIC KINEMATIC UPDATE
            _timeAlive += Time.fixedDeltaTime;

            // P(t) = P0 + V0*t + 0.5*a*t^2
            Vector3 nextPos = _startPosition
                            + (_initialVelocity * _timeAlive)
                            + (0.5f * _constantAcceleration * _timeAlive * _timeAlive);

            // V(t) = V0 + a*t
            Vector3 nextVel = _initialVelocity + (_constantAcceleration * _timeAlive);

            // ROBUST COLLISION CHECK (Sweep Test)
            Vector3 currentPos = _rb.position;
            Vector3 displacement = nextPos - currentPos;
            float dist = displacement.magnitude;

            if(dist > 0 && Physics.SphereCast(currentPos, ballRadius, displacement.normalized, out RaycastHit hit, dist, groundLayer, QueryTriggerInteraction.Ignore)) {

                if(hit.collider.TryGetComponent(out IPitch pitch)) {
                    // Snap to surface
                    Vector3 snapPos = hit.point + (hit.normal * (ballRadius + 0.001f));
                    _rb.position = snapPos;
                    if(debugMode) AddDebugPoint();

                    _lastFrameVelocity = nextVel;
                    _timeAlive = _timeAlive; // Keep sync

                    HandlePitchBounce();
                    return;
                }
            }

            _rb.MovePosition(nextPos);
            _rb.linearVelocity = nextVel;
            _lastFrameVelocity = nextVel;

            //Debug.Break();
        } else if(CurrentState == BallState.Bounced) {
            // Fallback to manual gravity after bounce
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

        // Fallback: If Physics Engine catches a collision we missed
        if(CurrentState == BallState.MidAir && collision.gameObject.TryGetComponent(out IPitch pitch)) {
            HandlePitchBounce();
        }
    }

    private void HandlePitchBounce() {
        CurrentState = BallState.Bounced;
        _rb.isKinematic = false; // Switch to dynamic physics

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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, ballRadius);
    }
}