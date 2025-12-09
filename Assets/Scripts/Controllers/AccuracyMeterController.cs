using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class AccuracyMeterController : MonoBehaviour {
    [Header("Dependencies")]
    [Tooltip("The background image component of the Slider (the part that will change colors).")]
    public Image sliderBackground;

    [Header("Events")]
    [Tooltip("Listen for SPACE (Lock) to stop the meter.")]
    public VoidEventChannelSO onAccuracyCapture;

    [Tooltip("Send the calculated accuracy to the State Manager.")]
    public FloatEventChannelSO onAccuracyValueSet;

    [Tooltip("Reset the meter when delivery is done.")]
    public VoidEventChannelSO onDeliveryFinished;

    [Header("Configuration")]
    [Range(0.1f, 5f)]
    public float oscillationSpeed = 1.5f;

    // Define the "Half-Width" of the zones from the center (0.5)
    // 0.5 is Center. 
    // If Perfect is 0.05, it covers 0.45 to 0.55.
    [Header("Zones (Half-Width from Center)")]
    [Range(0f, 0.5f)] public float perfectThreshold = 0.05f; // Blue
    [Range(0f, 0.5f)] public float goodThreshold = 0.15f;    // Green
    [Range(0f, 0.5f)] public float okayThreshold = 0.30f;    // Yellow
    // Anything > okayThreshold is Red

    [Header("Colors")]
    public Color perfectColor = new Color(0f, 0.4f, 1f); // Nice Blue
    public Color goodColor = Color.green;
    public Color okayColor = new Color(1f, 0.8f, 0f); // Gold/Yellow
    public Color badColor = Color.red;

    private Slider _slider;
    private bool _isRunning = false;

    void Awake() {
        _slider = GetComponent<Slider>();
    }

    void Start() {
        GenerateMeterVisuals();
        ResetMeter(); // Start running

        if(onAccuracyCapture != null) onAccuracyCapture.OnEventRaised += StopAndCapture;
        if(onDeliveryFinished != null) onDeliveryFinished.OnEventRaised += ResetMeter;
    }

    void OnDestroy() {
        if(onAccuracyCapture != null) onAccuracyCapture.OnEventRaised -= StopAndCapture;
        if(onDeliveryFinished != null) onDeliveryFinished.OnEventRaised -= ResetMeter;
    }

    void Update() {
        if(_isRunning) {
            // PingPong between 0 and 1
            _slider.value = Mathf.PingPong(Time.time * oscillationSpeed, 1.0f);
        }
    }

    private void StopAndCapture() {
        if(!_isRunning) return;

        _isRunning = false;

        // Calculate Accuracy Logic
        // 0.5 is the target. Calculate distance from 0.5
        float distanceFromCenter = Mathf.Abs(_slider.value - 0.5f);

        // Map distance to a 0-1 score (1 is perfect, 0 is furthest away)
        // We can use the thresholds to determine "tiers" or a raw float.
        // Here we return a raw float adjusted by difficulty, but snapped for logic.

        float accuracyScore = 0f;

        if(distanceFromCenter <= perfectThreshold) {
            accuracyScore = 1.0f; // Perfect
            Debug.Log("Accuracy: PERFECT (Blue)");
        } else if(distanceFromCenter <= goodThreshold) {
            accuracyScore = 0.70f; // Good
            Debug.Log("Accuracy: GOOD (Green)");
        } else if(distanceFromCenter <= okayThreshold) {
            accuracyScore = 0.40f; // Okay
            Debug.Log("Accuracy: OKAY (Yellow)");
        } else {
            accuracyScore = 0.10f; // Bad/No Ball
            Debug.Log("Accuracy: BAD (Red)");
        }

        // Raise the event so StateManager knows the value
        if(onAccuracyValueSet != null) {
            onAccuracyValueSet.RaiseEvent(accuracyScore);
        }
    }

    private void ResetMeter() {
        _isRunning = true;
        _slider.value = 0;
    }

    // --- Procedural Texture Generation ---
    [ContextMenu("Refresh Visuals")]
    public void GenerateMeterVisuals() {
        if(sliderBackground == null) return;

        int height = 256;
        int width = 1;
        Texture2D texture = new Texture2D(width, height);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for(int y = 0; y < height; y++) {
            // Normalized Y (0 to 1)
            float t = (float)y / (height - 1);
            float dist = Mathf.Abs(t - 0.5f);
            Color col = badColor;

            if(dist <= perfectThreshold) col = perfectColor;
            else if(dist <= goodThreshold) col = goodColor;
            else if(dist <= okayThreshold) col = okayColor;

            texture.SetPixel(0, y, col);
        }

        texture.Apply();

        Sprite gradientSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        sliderBackground.sprite = gradientSprite;
    }
}