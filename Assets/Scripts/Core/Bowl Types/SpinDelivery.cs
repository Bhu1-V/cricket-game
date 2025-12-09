using UnityEngine;

public class SpinDelivery : IBowlingType {
    public string DeliveryName => "Spin";
    private readonly BowlingConfig _config;

    public SpinDelivery(BowlingConfig config) {
        _config = config;
    }

    public void ApplyInitialForce(Rigidbody ball, Vector3 initialVelocity, Vector3 pitchUpVector, float accuracy) {
        ball.linearDamping = _config.airDrag * 1.2f;
    }

    public void ApplyMidAirPhysics(Rigidbody ball, float accuracy, Vector3 swingDirection) {
        // Spin travels straight in air
    }

    public Vector3 HandleBouncePhysics(Vector3 reflectedVelocity, Vector3 collisionNormal, float accuracy, Vector3 spinDirection) {
        // Friction
        reflectedVelocity.z *= (1f - _config.pitchFriction);

        // PENALTY LOGIC:
        // If Accuracy is 0, Turn Force is 0. Ball continues straight (reflected X).
        // If Accuracy is 1, Full Turn.

        float dampener = accuracy;

        float turnForce = spinDirection.x * _config.maxSpinTurnAngle * dampener;

        reflectedVelocity.x += turnForce;

        return reflectedVelocity;
    }
}