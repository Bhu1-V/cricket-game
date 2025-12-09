using UnityEngine;
using UnityEngine.Events;
// --- FLOAT EVENT (For the Accuracy Meter Value) ---
[CreateAssetMenu(menuName = "Cricket/Events/Float Event Channel")]
public class FloatEventChannelSO : ScriptableObject {
    [HideInInspector] public UnityAction<float> OnEventRaised;

    public void RaiseEvent(float value) {
        OnEventRaised?.Invoke(value);
    }
}