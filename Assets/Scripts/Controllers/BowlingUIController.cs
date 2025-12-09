using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// If using TextMeshPro, uncomment the line below and change 'Text' to 'TMP_Text' in the variables
// using TMPro; 

public class BowlingUIController : MonoBehaviour {
    [Header("Controllers")]
    public BowlingStateManager stateManager;
    public AccuracyMeterController accuracyMeter;

    [Header("UI Panels")]
    public GameObject selectionPanel;
    public GameObject preBowlPanel;
    public Button bowlButton;

    [Tooltip("Assign ONLY the Accuracy Meter object here.")]
    public CanvasGroup accuracyCanvasGroup;

    [Header("Sliders")]
    public Slider speedSlider;
    public Slider angleSlider;

    [Header("Slider Labels (Assign Text Objects)")]
    public TextMeshProUGUI speedMinText;
    public TextMeshProUGUI speedMaxText;
    public TextMeshProUGUI angleMinText;
    public TextMeshProUGUI angleMaxText;

    [Header("Event Channels")]
    public VoidEventChannelSO onBowlTriggered;
    public FloatEventChannelSO onAccuracyValueSet;
    public VoidEventChannelSO onDeliveryFinished;

    // Internal flag to prevent loop when updating sliders via code
    private bool _isUpdatingUI = false;

    void OnEnable() {
        if(speedSlider != null) speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
        if(angleSlider != null) angleSlider.onValueChanged.AddListener(OnAngleSliderChanged);

        if(onAccuracyValueSet != null) onAccuracyValueSet.OnEventRaised += OnAccuracyLocked;
        if(onDeliveryFinished != null) onDeliveryFinished.OnEventRaised += OnDeliveryFinished;
    }

    void OnDisable() {
        if(speedSlider != null) speedSlider.onValueChanged.RemoveListener(OnSpeedSliderChanged);
        if(angleSlider != null) angleSlider.onValueChanged.RemoveListener(OnAngleSliderChanged);

        if(onAccuracyValueSet != null) onAccuracyValueSet.OnEventRaised -= OnAccuracyLocked;
        if(onDeliveryFinished != null) onDeliveryFinished.OnEventRaised -= OnDeliveryFinished;
    }

    void Start() {
        ShowSelectionUI();
        if(bowlButton != null) bowlButton.gameObject.SetActive(false);
    }

    public void OnSelectSwing() {
        stateManager.SetDeliveryType(true);
        RefreshSliders();
        GoToPreBowlState();
    }

    public void OnSelectSpin() {
        stateManager.SetDeliveryType(false);
        RefreshSliders();
        GoToPreBowlState();
    }

    public void OnChangeSideClicked() {
        // 1. Toggle Side
        stateManager.ToggleBowlingSide();
        // 2. Refresh UI to show new Effective Values and Ranges
        RefreshSliders();
    }

    public void OnBowlButtonClicked() {
        if(onBowlTriggered != null) onBowlTriggered.RaiseEvent();
        preBowlPanelSetActive(false);
        bowlButton.gameObject.SetActive(false);
    }

    // --- Slider Callbacks ---
    private void OnSpeedSliderChanged(float val) {
        if(_isUpdatingUI) return;
        stateManager.SetCurrentSpeed(val);
    }

    private void OnAngleSliderChanged(float val) {
        if(_isUpdatingUI) return;
        stateManager.SetCurrentDirectionFromUI(val);
    }

    // --- State Management ---

    private void RefreshSliders() {
        _isUpdatingUI = true; // Block callbacks while we setup limits

        // 1. Speed (Changes based on Swing vs Spin)
        float speedMin, speedMax, speedCurrent;
        if(stateManager.isSwingDelivery) {
            speedMin = BowlingStateManager.SwingSpeedMin;
            speedMax = BowlingStateManager.SwingSpeedMax;
            speedCurrent = stateManager.swingReleaseSpeed;
        } else {
            speedMin = BowlingStateManager.SpinSpeedMin;
            speedMax = BowlingStateManager.SpinSpeedMax;
            speedCurrent = stateManager.spinReleaseSpeed;
        }
        SetupSlider(speedSlider, speedMin, speedMax, speedCurrent);
        UpdateMinMaxLabels(speedMinText, speedMaxText, speedMin, speedMax);

        // 2. Angle (Changes based on Swing vs Spin AND Over/Around Wicket)
        float angleMin = stateManager.GetMinDirection();
        float angleMax = stateManager.GetMaxDirection();
        float angleCurrent = stateManager.GetEffectiveDirection();

        SetupSlider(angleSlider, angleMin, angleMax, angleCurrent);
        UpdateMinMaxLabels(angleMinText, angleMaxText, angleMin, angleMax);

        _isUpdatingUI = false;
    }

    private void SetupSlider(Slider s, float min, float max, float current) {
        if(s == null) return;
        s.minValue = min;
        s.maxValue = max;
        s.value = current;
    }

    private void UpdateMinMaxLabels(TextMeshProUGUI minText, TextMeshProUGUI maxText, float minVal, float maxVal) {
        if(minText != null) minText.text = minVal.ToString("F1"); // 1 Decimal place (e.g. "17.5")
        if(maxText != null) maxText.text = maxVal.ToString("F1");
    }

    private void ShowSelectionUI() {
        selectionPanel.SetActive(true);
        preBowlPanelSetActive(false);
        bowlButton.gameObject.SetActive(false);
        accuracyMeter.SetMeterActive(false);
    }

    private void GoToPreBowlState() {
        selectionPanel.SetActive(false);
        preBowlPanelSetActive(true);
        accuracyMeter.SetMeterActive(true);
    }

    private void preBowlPanelSetActive(bool v) {
        if(preBowlPanel != null) preBowlPanel.SetActive(v);
        if(accuracyCanvasGroup != null) {
            accuracyCanvasGroup.alpha = v ? 1f : 0f;
            accuracyCanvasGroup.interactable = false;
            accuracyCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnAccuracyLocked(float val) {
        if(bowlButton != null) {
            bowlButton.gameObject.SetActive(true);
            bowlButton.Select();
        }
    }

    private void OnDeliveryFinished() {
        ShowSelectionUI();
    }
}