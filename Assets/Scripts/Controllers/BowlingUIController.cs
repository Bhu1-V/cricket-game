using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Labels (Assign Text Objects)")]
    public TextMeshProUGUI speedMinText;
    public TextMeshProUGUI speedMaxText;
    public TextMeshProUGUI angleMinText;
    public TextMeshProUGUI angleMaxText;

    [Header("New UI Elements")]
    public TextMeshProUGUI effectiveStrengthText;

    [Header("Event Channels")]
    public VoidEventChannelSO onBowlTriggered;
    public FloatEventChannelSO onStrengthValueSet;
    public VoidEventChannelSO onDeliveryFinished;

    private bool _isUpdatingUI = false;

    void OnEnable() {
        if(speedSlider != null) speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
        if(angleSlider != null) angleSlider.onValueChanged.AddListener(OnDirectionSliderChanged);

        if(onStrengthValueSet != null) onStrengthValueSet.OnEventRaised += OnStrengthLocked;
        if(onDeliveryFinished != null) onDeliveryFinished.OnEventRaised += OnDeliveryFinished;
    }

    void OnDisable() {
        if(speedSlider != null) speedSlider.onValueChanged.RemoveListener(OnSpeedSliderChanged);
        if(angleSlider != null) angleSlider.onValueChanged.RemoveListener(OnDirectionSliderChanged);

        if(onStrengthValueSet != null) onStrengthValueSet.OnEventRaised -= OnStrengthLocked;
        if(onDeliveryFinished != null) onDeliveryFinished.OnEventRaised -= OnDeliveryFinished;
    }

    void Start() {
        ShowSelectionUI();
        if(bowlButton != null) bowlButton.gameObject.SetActive(false);
        if(effectiveStrengthText != null) effectiveStrengthText.text = "";
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
        stateManager.ToggleBowlingSide();
        RefreshSliders();
    }

    public void OnBowlButtonClicked() {
        if(onBowlTriggered != null) onBowlTriggered.RaiseEvent();
        preBowlPanelSetActive(false);
        bowlButton.gameObject.SetActive(false);
        if(effectiveStrengthText != null) effectiveStrengthText.text = "";
    }

    // --- Slider Callbacks ---
    private void OnSpeedSliderChanged(float val) {
        if(_isUpdatingUI) return;
        stateManager.SetCurrentSpeed(val);
    }

    private void OnDirectionSliderChanged(float val) {
        if(_isUpdatingUI) return;
        stateManager.SetRawDirectionFromUI(val);
    }

    // --- State Management ---

    private void RefreshSliders() {
        _isUpdatingUI = true;

        // 1. Speed Slider Logic
        if(stateManager.isSwingDelivery) {
            // Swing has Constant Speed -> Disable Speed Slider
            if(speedSlider != null) speedSlider.interactable = false;

            // Just show the fixed value
            float fixedSpeed = stateManager.config.maxBallSpeed;
            SetupSlider(speedSlider, fixedSpeed, fixedSpeed, fixedSpeed);
            UpdateMinMaxLabels(speedMinText, speedMaxText, fixedSpeed, fixedSpeed);
        } else {
            // Spin has Variable Speed -> Enable Speed Slider
            if(speedSlider != null) speedSlider.interactable = true;

            float speedMin = BowlingStateManager.SpinSpeedMin;
            float speedMax = BowlingStateManager.SpinSpeedMax;
            float speedCurrent = stateManager.spinReleaseSpeed;

            SetupSlider(speedSlider, speedMin, speedMax, speedCurrent);
            UpdateMinMaxLabels(speedMinText, speedMaxText, speedMin, speedMax);
        }

        // 2. Angle/Direction (Always -100 to 100)
        SetupSlider(angleSlider, -100f, 100f, 0f);
        UpdateMinMaxLabels(angleMinText, angleMaxText, -100f, 100f);

        stateManager.SetRawDirectionFromUI(0f);

        _isUpdatingUI = false;
    }

    private void SetupSlider(Slider s, float min, float max, float current) {
        if(s == null) return;
        s.minValue = min;
        s.maxValue = max;
        s.value = current;
    }

    private void UpdateMinMaxLabels(TextMeshProUGUI minText, TextMeshProUGUI maxText, float minVal, float maxVal) {
        if(minText != null) minText.text = minVal.ToString("F0");
        if(maxText != null) maxText.text = maxVal.ToString("F0");
    }

    private void ShowSelectionUI() {
        selectionPanel.SetActive(true);
        preBowlPanelSetActive(false);
        bowlButton.gameObject.SetActive(false);
        accuracyMeter.SetMeterActive(false);
        if(effectiveStrengthText != null) effectiveStrengthText.text = "";
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

    private void OnStrengthLocked(float meterVal) {
        if(bowlButton != null) {
            bowlButton.gameObject.SetActive(true);
            bowlButton.Select();
        }

        if(effectiveStrengthText != null) {
            float directionMagnitude = Mathf.Abs(angleSlider.value);
            float effectiveStrength = meterVal * directionMagnitude;

            effectiveStrengthText.text = $"Effective Strength: {effectiveStrength:F0}%";
        }
    }

    private void OnDeliveryFinished() {
        ShowSelectionUI();
    }
}