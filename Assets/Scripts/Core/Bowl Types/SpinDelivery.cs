using UnityEngine;

public class SpinDelivery : IBowlingType {
    public string DeliveryName => "Spin";
    private readonly BowlingConfig _config;

    public SpinDelivery(BowlingConfig config) {
        _config = config;
    }

    public void ApplyInitialForce(Rigidbody ball, Vector3 initialVelocity, Vector3 pitchUpVector, float accuracy) {
        // Higher drag for spin to allow for loopier trajectories if needed
        ball.linearDamping = _config.airDrag * 1.2f;
    }

    public void ApplyMidAirPhysics(Rigidbody ball, float accuracy, Vector3 swingDirection) {
        // NO Mid-Air Drift for Spin (Per user request: "Thrown straight onto the bounce Marker")
        // We leave this empty. The ball travels in a straight line (projectile motion).
    }

    public Vector3 HandleBouncePhysics(Vector3 reflectedVelocity, Vector3 collisionNormal, float accuracy, Vector3 spinDirection) {
        // SHARP CUT LOGIC (Ported from previous Swing logic)

        // 1. Friction (Spinners lose speed off the pitch)
        reflectedVelocity.z *= (1f - _config.pitchFriction);

        // 2. The "Snap" Turn
        // Since the ball came in straight, reflectedVelocity.x is near 0.
        // We inject the turn Velocity here based on spin input.

        float turnForce = spinDirection.x * _config.maxSpinTurnAngle * accuracy;
        // Note: Reusing maxSpinTurnAngle variable as a velocity multiplier for simplicity here, 
        // or you can add a specific force variable in Config.

        // Add direct lateral velocity (The Cut)
        reflectedVelocity.x += turnForce;

        return reflectedVelocity;
    }
}