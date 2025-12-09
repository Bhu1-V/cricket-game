using UnityEngine;

public interface IBowlingType {
    string DeliveryName { get; }

    void ApplyInitialForce(Rigidbody ball, Vector3 initialVelocity, Vector3 pitchUpVector, float accuracy);

    void ApplyMidAirPhysics(Rigidbody ball, float accuracy, Vector3 swingDirection);

    Vector3 HandleBouncePhysics(Vector3 reflectedVelocity, Vector3 collisionNormal, float accuracy, Vector3 spinDirection);
}