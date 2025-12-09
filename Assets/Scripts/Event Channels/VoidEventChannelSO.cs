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
