using UnityEngine;

public class SwingDelivery : IBowlingType {
    public string DeliveryName => "Swing";
    private readonly BowlingConfig _config;

    public SwingDelivery(BowlingConfig config) {
        _config = config;
    }

    public void ApplyInitialForce(Rigidbody ball, Vector3 initialVelocity, Vector3 pitchUpVector, float accuracy) {
        // For swing, ensure minimal or no initial spin
        ball.angularVelocity = Vector3.zero;
        ball.linearVelocity = initialVelocity;

        // Add realistic drag
        ball.linearDamping = _config.airDragMultiplier;
    }

    // Mid-Air: Applies a consistent lateral force based on swing direction and accuracy.
    public void ApplyMidAirPhysics(Rigidbody ball, float accuracy, Vector3 swingDirection) {
        // Calculate the maximum possible swing force based on the current speed
        float speedFactor = ball.linearVelocity.magnitude / 30f; // Normalize speed
        float maxForce = _config.maxSwingForce * speedFactor;

        // Force strength is proportional to the player's accuracy (closer to 1.0 is better)
        // We use accuracy to modify the effective magnitude of the force.
        float effectiveAccuracy = Mathf.Lerp(0.2f, 1.0f, accuracy);

        // Swing force is perpendicular to the velocity and proportional to accuracy
        Vector3 swingForce = Vector3.Cross(ball.linearVelocity.normalized, swingDirection).normalized * maxForce * effectiveAccuracy;

        ball.AddForce(swingForce, ForceMode.Acceleration);
    }

    // Post-Bounce: Swing relies mostly on the physics engine and typically has minimal manual post-bounce adjustment.
    public void HandleBouncePhysics(Rigidbody ball, Vector3 collisionNormal, float accuracy, Vector3 spinDirection) {
        // The bounce physics are mainly handled by the Rigidbody and the Physics Material.
        // The result of a good swing is movement in the air, not a dramatic change after bounce.
    }
}