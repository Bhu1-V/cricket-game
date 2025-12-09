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
        // Continuous Force in global X (Magnus Effect)
        // This causes the ball to curve IN THE AIR.
        float swingMag = swingDirection.x * _config.maxSwingForce * accuracy;
        ball.AddForce(new Vector3(swingMag, 0, 0), ForceMode.Force);
    }

    public Vector3 HandleBouncePhysics(Vector3 reflectedVelocity, Vector3 collisionNormal, float accuracy, Vector3 swingDirection) {
        // TANGENTIAL CONTINUATION LOGIC

        // 1. Separate Vertical (Y) and Horizontal (XZ) components
        // We preserve Y explicitly so the BallPhysicsController can handle bounce height via Restitution.
        // If we apply friction to Y here, it double-dampens the bounce, causing the "rolling" issue.
        float bounceY = reflectedVelocity.y;
        Vector3 flatVelocity = new Vector3(reflectedVelocity.x, 0, reflectedVelocity.z);

        // 2. Calculate the current angle of travel relative to straight forward
        // (If ball is moving Right, angle is positive)
        float currentAngle = Vector3.SignedAngle(Vector3.forward, flatVelocity, Vector3.up);

        // 3. Exaggerate this angle (The "Cut")
        // If the ball was swinging Right (positive angle), we want it to go MORE Right.
        // We multiply the angle to sharpen the path based on the incoming trajectory arc.
        float nipFactor = 1.5f; // How much sharper the bounce is
        float newAngle = currentAngle * (1f + (nipFactor * accuracy));

        // 4. Create new direction from this angle
        Quaternion rotation = Quaternion.Euler(0, newAngle - currentAngle, 0); // Rotate by the difference
        Vector3 finalFlatVelocity = rotation * flatVelocity;

        // 5. Apply standard friction ONLY to the horizontal speed (Skidding)
        finalFlatVelocity *= (1f - _config.pitchFriction);

        // 6. Recombine with the original vertical bounce
        return new Vector3(finalFlatVelocity.x, bounceY, finalFlatVelocity.z);
    }
}