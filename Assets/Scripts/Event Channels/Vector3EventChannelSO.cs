using UnityEngine;
using UnityEngine.Events;
// --- VECTOR3 EVENT (For the Bounce Marker Position) ---
[CreateAssetMenu(menuName = "Cricket/Events/Vector3 Event Channel")]
public class Vector3EventChannelSO : ScriptableObject {
    [HideInInspector] public UnityAction<Vector3> OnEventRaised;

    public void RaiseEvent(Vector3 value) {
        OnEventRaised?.Invoke(value);
    }
}
