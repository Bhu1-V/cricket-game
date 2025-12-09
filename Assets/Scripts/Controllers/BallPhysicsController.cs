using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class BallPhysicsController : MonoBehaviour {
    private Rigidbody _rb;
    private LineRenderer _lineRenderer;
    private BowlingStateManager _stateManager;
    private IBowlingType _currentDeliveryType;

    private Vector3 _currentControlInput;
    private float _accuracy;

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
        _rb.isKinematic = true; // Start kinematic (in hand)

        _stateManager = FindAnyObjectByType<BowlingStateManager>();

        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.startWidth = debugLineWidth;
        _lineRenderer.endWidth = debugLineWidth;
        _lineRenderer.positionCount = 0;

        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.material = lineMat;
        _lineRenderer.startColor = debugLineColor;
        _lineRenderer.endColor = debugLineColor;
    }

    // --- Called by State Manager when UI changes side ---
    public void ResetBall(Vector3 startPosition) {
        CurrentState = BallState.Idle;
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;

        if(_lineRenderer != null) _lineRenderer.positionCount = 0;
    }

    public void StartDelivery(IBowlingType deliveryType, float accuracy, Vector3 controlInput, Vector3 initialVelocity) {
        _currentDeliveryType = deliveryType;
        _accuracy = accuracy;
        _currentControlInput = controlInput;

        if(_lineRenderer != null) _lineRenderer.positionCount = 0;

        CurrentState = BallState.MidAir;
        _rb.isKinematic = false;
        _rb.linearVelocity = initialVelocity;
        _rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate() {
        if(CurrentState == BallState.MidAir) {
            if(debugMode) AddDebugPoint();
            _rb.AddForce(Vector3.up * _stateManager.config.gravity, ForceMode.Acceleration);
            _currentDeliveryType.ApplyMidAirPhysics(_rb, _accuracy, _currentControlInput);
        } else if(CurrentState == BallState.Bounced) {
            if(debugMode) AddDebugPoint();
            _rb.AddForce(Vector3.up * _stateManager.config.gravity, ForceMode.Acceleration);
        }
    }

    private void AddDebugPoint() {
        if(_lineRenderer == null) return;
        _lineRenderer.positionCount++;
        _lineRenderer.SetPosition(_lineRenderer.positionCount - 1, transform.position);
    }

    void OnCollisionEnter(Collision collision) {
        if(CurrentState == BallState.Bounced && (collision.gameObject.TryGetComponent(out IWicket w) || collision.gameObject.TryGetComponent(out IBoundary b))) {
            StopBall();
        }

        if(CurrentState != BallState.MidAir) return;

        if(collision.gameObject.TryGetComponent(out IPitch pitch)) {
            CurrentState = BallState.Bounced;
            Vector3 incomingVel = collision.relativeVelocity * -1f;
            Vector3 normal = collision.contacts[0].normal;
            Vector3 reflected = Vector3.Reflect(incomingVel, normal);
            Vector3 finalVel = _currentDeliveryType.HandleBouncePhysics(reflected, normal, _accuracy, _currentControlInput);
            finalVel.y *= _stateManager.config.restitution;
            _rb.linearVelocity = finalVel;
            _stateManager.OnBallBounce();
        }
    }

    private void StopBall() {
        CurrentState = BallState.Finished;
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;
        _stateManager.OnDeliveryFinished();
    }
}