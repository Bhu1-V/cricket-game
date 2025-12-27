using UnityEngine;

[CreateAssetMenu(fileName = "BowlingConfig", menuName = "Cricket/Bowling Config")]
public class BowlingConfig : ScriptableObject {
    [Header("Base Settings")]
    public float gravity = -9.81f;
    public float maxBallSpeed = 30f; // NEW: The constant speed for Swing deliveries
    public float airDrag = 0.1f;
    public float pitchFriction = 0.2f;
    public float restitution = 0.6f;

    [Header("Swing Settings")]
    public float maxSwingForce = 15f;
    public AnimationCurve swingCurve;

    [Header("Spin Settings")]
    public float maxSpinTurnAngle = 20f;
}