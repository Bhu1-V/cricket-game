using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))] // Added LineRenderer dependency
public class BallPhysicsController : MonoBehaviour {
    private Rigidbody _rb;
    private LineRenderer _lineRenderer; // Reference to the LineRenderer
    private BowlingStateManager _stateManager;
    private IBowlingType _currentDeliveryType;

    private Vector3 _currentControlInput;
    private float _accuracy;

    [Header("Debug Settings")]
    public bool debugMode = true; // Toggle this in Inspector
    public Color debugLineColor = Color.red;
    [Range(0.05f, 0.5f)] public float debugLineWidth = 0.1f;

    public enum BallState { Idle, MidAir, Bounced, Finished }
    public BallState CurrentState { get; set; }

    void Awake() {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false; // We use manual gravity
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _stateManager = FindAnyObjectByType<BowlingStateManager>();

        // --- Setup LineRenderer Defaults ---
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.startWidth = debugLineWidth;
        _lineRenderer.endWidth = debugLineWidth;
        _lineRenderer.positionCount = 0;

        // Simple default material for the line so it is visible immediately
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.material = lineMat;
        _lineRenderer.startColor = debugLineColor;
        _lineRenderer.endColor = debugLineColor;
    }

    public void StartDelivery(IBowlingType deliveryType, float accuracy, Vector3 controlInput, Vector3 initialVelocity) {
        _currentDeliveryType = deliveryType;
        _accuracy = accuracy;
        _currentControlInput = controlInput;

        // Clear the debug path for the new delivery
        if(_lineRenderer != null) {
            _lineRenderer.positionCount = 0;
        }

        CurrentState = BallState.MidAir;
        _rb.isKinematic = false;
        _rb.linearVelocity = initialVelocity;
        _rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate() {
        if(CurrentState == BallState.MidAir) {
            // Track Path
            if(debugMode) AddDebugPoint();

            // 1. Manual Gravity
            _rb.AddForce(Vector3.up * _stateManager.config.gravity, ForceMode.Acceleration);

            // 2. Apply Mid-Air Swing/Drift
            _currentDeliveryType.ApplyMidAirPhysics(_rb, _accuracy, _currentControlInput);

        } else if(CurrentState == BallState.Bounced) {
            // Track Path
            if(debugMode) AddDebugPoint();

            // Continue applying gravity after bounce
            _rb.AddForce(Vector3.up * _stateManager.config.gravity, ForceMode.Acceleration);
        }
    }

    private void AddDebugPoint() {
        if(_lineRenderer == null) return;

        // Add a new point to the end of the line
        _lineRenderer.positionCount++;
        _lineRenderer.SetPosition(_lineRenderer.positionCount - 1, transform.position);
    }

    void OnCollisionEnter(Collision collision) {
        // Prevent double-bounce logic or hitting erroneous triggers
        if(CurrentState == BallState.Bounced && collision.gameObject.TryGetComponent(out IWicket w) || collision.gameObject.TryGetComponent(out IBoundary b)) {
                Debug.Log("BallPhysicsController: Ball Stopped on Either Wicket or Boundary");
                StopBall();
        } 
        
        if(CurrentState != BallState.MidAir) return;

        // Check for Pitch
        if(collision.gameObject.TryGetComponent(out IPitch pitch)) {
            Debug.Log("BallPhysicsController: Ball bounced on pitch.");
            CurrentState = BallState.Bounced;

            // --- MANUAL BOUNCE CALCULATION ---
            Vector3 incomingVel = collision.relativeVelocity * -1f;
            Vector3 normal = collision.contacts[0].normal;

            // 1. Standard Reflection
            Vector3 reflected = Vector3.Reflect(incomingVel, normal);

            // 2. Delegate to Delivery Type (Apply Swing Tangent / Spin Turn)
            Vector3 finalVel = _currentDeliveryType.HandleBouncePhysics(reflected, normal, _accuracy, _currentControlInput);

            // 3. Apply Global Bounciness (Y-axis only)
            finalVel.y *= _stateManager.config.restitution;

            // 4. Apply
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