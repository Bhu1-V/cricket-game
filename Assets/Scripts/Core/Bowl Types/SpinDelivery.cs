using UnityEngine;

public class SpinDelivery : IBowlingType {
    public string DeliveryName => "Spin";
    private readonly BowlingConfig _config;

    public SpinDelivery(BowlingConfig config) {
        _config = config;
    }

    public void ApplyInitialForce(Rigidbody ball, Vector3 initialVelocity, Vector3 pitchUpVector, float accuracy) {
        // Initial velocity is set by the State Manager
        ball.linearVelocity = initialVelocity;

        // Use a high initial angular velocity to simulate spin, slightly randomized by accuracy.
        float effectiveSpin = Mathf.Lerp(_config.initialSpinRate * 0.8f, _config.initialSpinRate * 1.2f, accuracy);

        // The spin direction is implicitly set by the spinDirection parameter passed in the HandleBouncePhysics, 
        // but here we set a generic topspin/sidespin to simulate the flight.
        // We need a pre-calculated spin vector here. For this example, we assume the ball rotates along its axis of travel.
        ball.angularVelocity = new Vector3(effectiveSpin, 0, 0);

        // Low air drag, as spin deliveries are often slower
        ball.linearDamping = _config.airDragMultiplier * 0.5f;
    }

    // Mid-Air: Applies the Magnus Effect (lift force due to spin).
    public void ApplyMidAirPhysics(Rigidbody ball, float accuracy, Vector3 swingDirection) {
        // Magnus force (F_mag = c * v x w) is perpendicular to both velocity (v) and angular velocity (w)
        Vector3 magnusForce = Vector3.Cross(ball.linearVelocity, ball.angularVelocity) * _config.magnusEffectMultiplier;

        // Apply the force proportional to accuracy (a more accurate bowl maintains better spin)
        ball.AddForce(magnusForce * accuracy, ForceMode.Acceleration);
    }

    // Post-Bounce: Applies a force proportional to accuracy and the intended spin direction.
    public void HandleBouncePhysics(Rigidbody ball, Vector3 collisionNormal, float accuracy, Vector3 spinDirection) {
        // Calculate the direction vector for the "turn" after bounce
        Vector3 deviationForce = spinDirection.normalized * _config.maxPostBounceSpinForce * accuracy;

        // Apply an instantaneous force (Impulse) to simulate the sharp deviation due to heavy spin friction.
        ball.AddForce(deviationForce, ForceMode.Impulse);
    }
}