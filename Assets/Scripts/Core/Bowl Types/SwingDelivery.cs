using UnityEngine;

public class SwingDelivery : IBowlingType {
    public string DeliveryName => "Swing";
    private readonly BowlingConfig _config;

    public SwingDelivery(BowlingConfig config) {
        _config = config;
    }

    public void ApplyInitialForce(Rigidbody ball, Vector3 initialVelocity, Vector3 pitchUpVector, float strengthFactor) {
        ball.linearDamping = 0f;
    }

    public void ApplyMidAirPhysics(Rigidbody ball, float strengthFactor, Vector3 swingDirection) {
        // Logic moved to Deterministic Kinematic Solver in BallPhysicsController.
        // We no longer apply forces per-frame to avoid integration errors.
    }

    public Vector3 HandleBouncePhysics(Vector3 reflectedVelocity, Vector3 collisionNormal, float strengthFactor, Vector3 swingDirection) {
        // Pure Friction Bounce
        Vector3 flatVel = new Vector3(reflectedVelocity.x, 0, reflectedVelocity.z);
        flatVel *= (1f - _config.pitchFriction);
        return new Vector3(flatVel.x, reflectedVelocity.y, flatVel.z);
    }
}