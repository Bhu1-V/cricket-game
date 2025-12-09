using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class AccuracyMeterController : MonoBehaviour {
    [Header("Dependencies")]
    public Image sliderBackground;

    [Header("Events")]
    public VoidEventChannelSO onAccuracyCapture;
    public FloatEventChannelSO onAccuracyValueSet;

    [Header("Configuration")]
    [Range(0.1f, 5f)] public float oscillationSpeed = 1.5f;
    [Range(0f, 0.5f)] public float perfectThreshold = 0.05f;
    [Range(0f, 0.5f)] public float goodThreshold = 0.15f;
    [Range(0f, 0.5f)] public float okayThreshold = 0.30f;

    [Header("Colors")]
    public Color perfectColor = new Color(0f, 0.4f, 1f);
    public Color goodColor = Color.green;
    public Color okayColor = new Color(1f, 0.8f, 0f);
    public Color badColor = Color.red;

    private Slider _slider;
    private bool _isRunning = false;

    void Awake() {
        _slider = GetComponent<Slider>();
    }

    void Start() {
        GenerateMeterVisuals();
        // Default to not running until UI Controller activates it
        _isRunning = false;

        if(onAccuracyCapture != null) onAccuracyCapture.OnEventRaised += StopAndCapture;
    }

    void OnDestroy() {
        if(onAccuracyCapture != null) onAccuracyCapture.OnEventRaised -= StopAndCapture;
    }

    // Called by BowlingUIController to start/stop the logic
    public void SetMeterActive(bool isActive) {
        if(isActive) {
            _isRunning = true;
            _slider.value = 0; // Reset position
        } else {
            _isRunning = false;
        }
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

        // Calculate Score
        float distanceFromCenter = Mathf.Abs(_slider.value - 0.5f);
        float accuracyScore = 0f;

        if(distanceFromCenter <= perfectThreshold) accuracyScore = 1.0f;
        else if(distanceFromCenter <= goodThreshold) accuracyScore = 0.70f;
        else if(distanceFromCenter <= okayThreshold) accuracyScore = 0.40f;
        else accuracyScore = 0.10f;

        // Notify Listeners (UI Controller will see this and show Bowl button)
        if(onAccuracyValueSet != null) {
            onAccuracyValueSet.RaiseEvent(accuracyScore);
        }
    }

    [ContextMenu("Refresh Visuals")]
    public void GenerateMeterVisuals() {
        if(sliderBackground == null) return;

        int height = 256;
        int width = 1;
        Texture2D texture = new Texture2D(width, height);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for(int y = 0; y < height; y++) {
            float t = (float)y / (height - 1);
            float dist = Mathf.Abs(t - 0.5f);
            Color col = badColor;

            if(dist <= perfectThreshold) col = perfectColor;
            else if(dist <= goodThreshold) col = goodColor;
            else if(dist <= okayThreshold) col = okayColor;

            texture.SetPixel(0, y, col);
        }

        texture.Apply();
        sliderBackground.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}