using UnityEngine;

// Defines the contract for any type of bowling delivery (Swing, Spin, Fast, etc.).
public interface IBowlingType {
    // The name of the delivery type for debugging or UI display.
    string DeliveryName { get; }

    // 1. Applies initial velocity and spin, aiming at the bounce marker.
    void ApplyInitialForce(Rigidbody ball, Vector3 initialVelocity, Vector3 pitchUpVector, float accuracy);

    // 2. Applies mid-air forces (like swing bias). Called during FixedUpdate.
    void ApplyMidAirPhysics(Rigidbody ball, float accuracy, Vector3 swingDirection);

    // 3. Handles post-bounce effects (like friction-induced spin deviation).
    void HandleBouncePhysics(Rigidbody ball, Vector3 collisionNormal, float accuracy, Vector3 spinDirection);
}