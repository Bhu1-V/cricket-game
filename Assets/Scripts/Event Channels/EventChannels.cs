using UnityEngine;
using UnityEngine.Events;

// --- VOID EVENT (For simple triggers like Tap 2: Bowl or Tap 1: Stop Meter) ---
[CreateAssetMenu(menuName = "Cricket/Events/Void Event Channel")]
public class VoidEventChannelSO : ScriptableObject {
    [HideInInspector] public UnityAction OnEventRaised;

    public void RaiseEvent() {
        OnEventRaised?.Invoke();
    }
}

// --- VECTOR2 EVENT (For continuous marker movement input) ---
[CreateAssetMenu(menuName = "Cricket/Events/Vector2 Event Channel")]
public class Vector2EventChannelSO : ScriptableObject {
    [HideInInspector] public UnityAction<Vector2> OnEventRaised;

    public void RaiseEvent(Vector2 value) {
        OnEventRaised?.Invoke(value);
    }
}

// --- VECTOR3 EVENT (For the Bounce Marker Position) ---
[CreateAssetMenu(menuName = "Cricket/Events/Vector3 Event Channel")]
public class Vector3EventChannelSO : ScriptableObject {
    [HideInInspector] public UnityAction<Vector3> OnEventRaised;

    public void RaiseEvent(Vector3 value) {
        OnEventRaised?.Invoke(value);
    }
}

// --- FLOAT EVENT (For the Accuracy Meter Value) ---
[CreateAssetMenu(menuName = "Cricket/Events/Float Event Channel")]
public class FloatEventChannelSO : ScriptableObject {
    [HideInInspector] public UnityAction<float> OnEventRaised;

    public void RaiseEvent(float value) {
        OnEventRaised?.Invoke(value);
    }
}