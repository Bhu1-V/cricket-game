using UnityEngine;

// Manages the ball's Rigidbody and applies continuous physics updates.
[RequireComponent(typeof(Rigidbody))]
public class BallPhysicsController : MonoBehaviour {
    private Rigidbody _rb;
    private BowlingStateManager _stateManager;
    private IBowlingType _currentDeliveryType;
    private Vector3 _currentSwingDirection;
    private Vector3 _currentSpinDirection;
    private float _accuracy;

    // Enum to represent the ball's current flight state
    public enum BallState { Idle, MidAir, Bounced, Finished }
    public BallState CurrentState { get; set; }

    void Awake() {
        _rb = GetComponent<Rigidbody>();
        _stateManager = FindObjectsByType<BowlingStateManager>(FindObjectsSortMode.None)[0]; // Get the manager

        // Ensure Rigidbody is configured for physics
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        CurrentState = BallState.Idle;
    }

    // Public method called by the State Manager to start the bowl.
    public void StartDelivery(IBowlingType deliveryType, float accuracy, Vector3 swingDirection, Vector3 spinDirection, Vector3 initialVelocity) {
        _currentDeliveryType = deliveryType;
        _accuracy = accuracy;
        _currentSwingDirection = swingDirection;
        _currentSpinDirection = spinDirection;

        // Reset and prepare the ball
        CurrentState = BallState.MidAir;
        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // Apply initial forces using the specific bowling type implementation
        _currentDeliveryType.ApplyInitialForce(_rb, initialVelocity, Vector3.up, _accuracy);
    }

    void FixedUpdate() {
        if(CurrentState == BallState.MidAir) {
            // OCP: Call the current delivery type's mid-air logic.
            _currentDeliveryType.ApplyMidAirPhysics(_rb, _accuracy, _currentSwingDirection);
        } else if(CurrentState == BallState.Bounced) {
            // Apply continuous post-bounce effects if needed (e.g., continued friction/spin decay)
        }

        // Simple simulation of gravity for the ball (always on unless kinematic)
        // _rb.AddForce(Vector3.down * 9.81f, ForceMode.Acceleration); // Already handled by default physics settings
    }

    void OnCollisionEnter(Collision collision) {
        if(CurrentState != BallState.MidAir) return;

        // Assume the pitch tag is set correctly
        if(collision.gameObject.CompareTag("Pitch")) {
            CurrentState = BallState.Bounced;

            // Apply bounce physics (restitution/friction)
            ContactPoint contact = collision.contacts[0];

            // OCP: Call the current delivery type's bounce logic.
            _currentDeliveryType.HandleBouncePhysics(_rb, contact.normal, _accuracy, _currentSpinDirection);

            // Notify the State Manager that the bounce occurred
            _stateManager.OnBallBounce();
        } else if(collision.gameObject.CompareTag("Stumps") || collision.gameObject.CompareTag("Boundary")) {
            CurrentState = BallState.Finished;
            _stateManager.OnDeliveryFinished();
        }
    }
}