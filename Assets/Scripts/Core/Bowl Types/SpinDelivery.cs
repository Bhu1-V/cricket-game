using UnityEngine;

public class SpinDelivery : IBowlingType {
    public string DeliveryName => "Spin";
    private readonly BowlingConfig _config;

    public SpinDelivery(BowlingConfig config) {
        _config = config;
    }

    public void ApplyInitialForce(Rigidbody ball, Vector3 initialVelocity, Vector3 pitchUpVector, float strengthFactor) {
        // Standard drag logic or 0 if you want it identical to swing's purity
        ball.linearDamping = _config.airDrag;
    }

    public void ApplyMidAirPhysics(Rigidbody ball, float strengthFactor, Vector3 swingDirection) {
        // SNIPPET LOGIC: "Phase II: Spin effect happens only at bounce"
        // No forces in air.
    }

    public Vector3 HandleBouncePhysics(Vector3 reflectedVelocity, Vector3 collisionNormal, float strengthFactor, Vector3 spinDirection) {
        // SNIPPET LOGIC: ApplySpinImpulse()
        // "Calculate instant velocity change... ForceMode.VelocityChange is critical here."

        // 1. Calculate Impulse (The "Cut")
        // spinDirection.x is -1 (Off) to 1 (Leg)
        // We use maxSpinTurnAngle as the velocity magnitude scalar here for simplicity.
        float spinMagnitude = spinDirection.x * _config.maxSpinTurnAngle * strengthFactor;
        Vector3 spinImpulse = Vector3.right * spinMagnitude;

        // 2. Apply Base Friction to the reflection
        Vector3 flatVel = new Vector3(reflectedVelocity.x, 0, reflectedVelocity.z);
        flatVel *= (1f - _config.pitchFriction);
        Vector3 finalVelocity = new Vector3(flatVel.x, reflectedVelocity.y, flatVel.z);

        // 3. Add Impulse directly (Equivalent to ForceMode.VelocityChange)
        finalVelocity += spinImpulse;

        return finalVelocity;
    }
}