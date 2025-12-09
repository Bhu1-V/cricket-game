using UnityEngine;

[CreateAssetMenu(fileName = "BowlingConfig", menuName = "Cricket/Bowling Config", order = 1)]
// Configuration data for all bowling physics constants.
public class BowlingConfig : ScriptableObject {
    [Header("General Physics")]
    [Tooltip("The time (in seconds) the ball takes to travel from the bowler to the bounce marker.")]
    public float timeToTarget = 0.6f;
    [Tooltip("Global air drag multiplier.")]
    public float airDragMultiplier = 0.5f;
    [Tooltip("How much the ball loses velocity after bouncing (0.0 to 1.0).")]
    public float restitution = 0.6f;
    [Tooltip("Friction applied to the ball on collision with the pitch.")]
    public float pitchFriction = 0.8f;

    [Header("Swing Delivery")]
    [Tooltip("Max lateral force applied during mid-air swing (proportional to velocity).")]
    public float maxSwingForce = 5.0f;

    [Header("Spin Delivery")]
    [Tooltip("Initial angular velocity applied to a spin ball (e.g., RPM).")]
    public float initialSpinRate = 80.0f;
    [Tooltip("Multiplier for the Magnuss effect (lift due to spin and velocity)")]
    public float magnusEffectMultiplier = 0.05f;
    [Tooltip("Max instantaneous deviation force applied upon bounce.")]
    public float maxPostBounceSpinForce = 15.0f;
}