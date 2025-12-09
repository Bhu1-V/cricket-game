using UnityEngine;
using UnityEngine.Events;
// --- VECTOR2 EVENT (For continuous marker movement input) ---
[CreateAssetMenu(menuName = "Cricket/Events/Vector2 Event Channel")]
public class Vector2EventChannelSO : ScriptableObject {
    [HideInInspector] public UnityAction<Vector2> OnEventRaised;

    public void RaiseEvent(Vector2 value) {
        OnEventRaised?.Invoke(value);
    }
}
