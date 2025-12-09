using UnityEngine;

[CreateAssetMenu(fileName = "BowlingConfig", menuName = "Cricket/Bowling Config", order = 1)]
public class BowlingConfig : ScriptableObject {
    [Header("Speed Settings (m/s)")]
    public float minBallSpeed = 18f;
    public float maxBallSpeed = 45f;

    [Header("Physics Constants")]
    public float gravity = -9.81f;
    public float airDrag = 0.1f; // Low drag to keep speed up

    [Header("Bounciness")]
    [Range(0f, 1f)] public float restitution = 0.75f; // Good bounce
    [Range(0f, 1f)] public float pitchFriction = 0.1f; // Low friction for skidding

    [Header("Swing Settings")]
    [Tooltip("Force applied sideways. Higher = More Curve.")]
    public float maxSwingForce = 45.0f; // Increased for visibility

    [Header("Spin Settings")]
    public float maxSpinTurnAngle = 35.0f;
    public float driftForce = 8.0f;
}