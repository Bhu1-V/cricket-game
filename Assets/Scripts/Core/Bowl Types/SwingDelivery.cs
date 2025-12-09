using UnityEngine;

public class SwingDelivery : IBowlingType {
    public string DeliveryName => "Swing";
    private readonly BowlingConfig _config;

    public SwingDelivery(BowlingConfig config) {
        _config = config;
    }

    public void ApplyInitialForce(Rigidbody ball, Vector3 initialVelocity, Vector3 pitchUpVector, float accuracy) {
        ball.linearDamping = _config.airDrag;
    }

    public void ApplyMidAirPhysics(Rigidbody ball, float accuracy, Vector3 swingDirection) {
        // PENALTY LOGIC:
        // Multiply force by Accuracy (0.0 to 1.0).
        // If Accuracy is 0.1, we only apply 10% of the Swing Force.

        float dampener = accuracy;
        // Optional: curve it so it's not too punishing? 
        // dampener = Mathf.Sqrt(accuracy); // Less punishing
        // For now, linear is exactly what you asked for.

        float swingMag = swingDirection.x * _config.maxSwingForce * dampener;
        ball.AddForce(new Vector3(swingMag, 0, 0), ForceMode.Force);
    }

    public Vector3 HandleBouncePhysics(Vector3 reflectedVelocity, Vector3 collisionNormal, float accuracy, Vector3 swingDirection) {
        float bounceY = reflectedVelocity.y;
        Vector3 flatVelocity = new Vector3(reflectedVelocity.x, 0, reflectedVelocity.z);

        float currentAngle = Vector3.SignedAngle(Vector3.forward, flatVelocity, Vector3.up);
        float nipFactor = 1.5f;

        // Bounce Nip is also dependent on accuracy
        float newAngle = currentAngle * (1f + (nipFactor * accuracy));

        Quaternion rotation = Quaternion.Euler(0, newAngle - currentAngle, 0);
        Vector3 finalFlatVelocity = rotation * flatVelocity;
        finalFlatVelocity *= (1f - _config.pitchFriction);

        return new Vector3(finalFlatVelocity.x, bounceY, finalFlatVelocity.z);
    }
}