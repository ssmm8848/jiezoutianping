using UnityEngine;
using UnityEngine.UI;

public class JudgeLineController : MonoBehaviour
{
    [Header("References")]
    public Transform judgeLine;
    public Transform judgePointRoot;
    public HitEffectSystem hitEffectSystem;
    public GameManager gameManager;

    [Header("Offset")]
    public float offsetY = 0f;
    public float velocityY = 0f;
    public float maxOffset = 1.5f;

    [Header("Weight Effects")]
    public float heavyOffset = -0.42f;
    public float lightOffset = 0.34f;
    public float stabilizeRate = 0.3f;

    [Header("Gravity Visuals")]
    public float visualDangerOffset = 1.2f;

    [Header("Spring Back")]
    public float springForce = 12f;
    [Range(0.8f, 0.999f)]
    public float damping = 0.97f;

    [Header("Calibration Energy")]
    public float calibrationEnergy = 0f;
    public float maxCalibrationEnergy = 100f;
    public float calibrationCost = 30f;

    public float perfectEnergyGain = 3f;
    public float goodEnergyGain = 1f;
    public float stabilizePerfectEnergyGain = 8f;

    [Header("Calibration Effect")]
    public float calibrationOffsetRate = 0.5f;
    public float calibrationVelocityRate = 0.3f;

    [Header("Gravity Lock")]
    [Tooltip("Remaining duration during which Heavy and Light impulses are suppressed.")]
    private float gravityLockTimer = 0f;

    [Header("Counterweight Link")]
    [Tooltip("A clean Heavy/Light reversal within this many beats triggers a balancing bonus.")]
    public float counterweightWindowBeats = 4f;
    public float counterweightOffsetRate = 0.22f;
    public float counterweightVelocityRate = 0.3f;

    private bool hasPendingCounterweight;
    private WeightType pendingCounterweightType;
    private float pendingCounterweightTime;

    [Header("Calibration UI")]
    public Slider calibrationSlider;

    [Header("Shake")]
    public float shakeOffsetY = 0f;
    public float shakeTimer = 0f;
    public float shakeDuration = 0.15f;
    public float shakeStrength = 0.12f;

    private Vector3 judgeLineBasePosition;
    private Vector3 judgePointRootBasePosition;
    private Vector3 judgeLineBaseScale;
    private Renderer judgeLineRenderer;
    private MaterialPropertyBlock judgeLineProperties;

    private void Start()
    {
        if (judgeLine != null)
        {
            judgeLineBasePosition = judgeLine.position;
            judgeLineBaseScale = judgeLine.localScale;
            judgeLineRenderer = judgeLine.GetComponent<Renderer>();
            judgeLineProperties = new MaterialPropertyBlock();
        }

        if (judgePointRoot != null)
        {
            judgePointRootBasePosition = judgePointRoot.position;
        }
        UpdateCalibrationUI();
    }

    private void Update()
    {
        if (gameManager != null && !gameManager.IsPlaying())
        {
            return;
        }

        if (gravityLockTimer > 0f)
        {
            gravityLockTimer = Mathf.Max(0f, gravityLockTimer - Time.deltaTime);
        }

        ExpireCounterweightWindow();

        UpdateSpringBack();
        UpdateShake();
        ApplyPosition();
    }

    private void UpdateShake()
    {
        if (shakeTimer <= 0f)
        {
            shakeOffsetY = 0f;
            return;
        }

        shakeTimer -= Time.deltaTime;

        float randomValue = Random.Range(-1f, 1f);
        float fade = shakeTimer / shakeDuration;

        shakeOffsetY = randomValue * shakeStrength * fade;
    }

    public void PlayShake(float strength, float duration)
    {
        shakeStrength = strength;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    public void AddCalibrationEnergy(JudgeResult result, WeightType weightType)
    {
        switch (result)
        {
            case JudgeResult.Perfect:
                if (weightType == WeightType.Stabilize)
                {
                    calibrationEnergy += stabilizePerfectEnergyGain;
                }
                else
                {
                    calibrationEnergy += perfectEnergyGain;
                }
                break;

            case JudgeResult.Good:
                calibrationEnergy += goodEnergyGain;
                break;
        }

        calibrationEnergy = Mathf.Clamp(calibrationEnergy, 0f, maxCalibrationEnergy);
        UpdateCalibrationUI();
    }

    public bool ApplyWeight(WeightType weightType, bool canTriggerCounterweight = true)
    {
        if (weightType == WeightType.Stabilize)
        {
            float elapsed = GetCurrentMusicTime() - pendingCounterweightTime;
            bool stabilizeCounterweightResolved = canTriggerCounterweight &&
                                                  hasPendingCounterweight &&
                                                  elapsed <= GetCounterweightWindowDuration();

            if (stabilizeCounterweightResolved)
            {
                offsetY *= counterweightOffsetRate;
                velocityY *= counterweightVelocityRate;
            }
            else
            {
                offsetY *= stabilizeRate;
                velocityY *= stabilizeRate;
            }

            BreakCounterweightChain();
            offsetY = Mathf.Clamp(offsetY, -maxOffset, maxOffset);
            return stabilizeCounterweightResolved;
        }

        if (weightType == WeightType.Normal)
        {
            return false;
        }

        if (gravityLockTimer > 0f)
        {
            BreakCounterweightChain();
            return false;
        }

        float currentMusicTime = GetCurrentMusicTime();
        bool isOppositeWeight =
            (pendingCounterweightType == WeightType.Heavy && weightType == WeightType.Light) ||
            (pendingCounterweightType == WeightType.Light && weightType == WeightType.Heavy);
        bool counterweightResolved = canTriggerCounterweight &&
                                     hasPendingCounterweight &&
                                     isOppositeWeight &&
                                     currentMusicTime - pendingCounterweightTime <= GetCounterweightWindowDuration();

        if (counterweightResolved)
        {
            offsetY *= counterweightOffsetRate;
            velocityY *= counterweightVelocityRate;
            BreakCounterweightChain();
        }
        else
        {
            if (weightType == WeightType.Heavy)
            {
                offsetY += heavyOffset;
                velocityY += heavyOffset * 2.2f;
            }
            else if (weightType == WeightType.Light)
            {
                offsetY += lightOffset;
                velocityY += lightOffset * 2.2f;
            }

            if (canTriggerCounterweight)
            {
                hasPendingCounterweight = true;
                pendingCounterweightType = weightType;
                pendingCounterweightTime = currentMusicTime;
            }
            else
            {
                BreakCounterweightChain();
            }
        }

        offsetY = Mathf.Clamp(offsetY, -maxOffset, maxOffset);
        return counterweightResolved;
    }

    public bool TryGetCounterweightTarget(out WeightType targetWeight, out float remainingSeconds)
    {
        targetWeight = WeightType.Normal;
        remainingSeconds = 0f;

        if (!hasPendingCounterweight)
        {
            return false;
        }

        remainingSeconds = GetCounterweightWindowDuration() - (GetCurrentMusicTime() - pendingCounterweightTime);
        if (remainingSeconds <= 0f)
        {
            BreakCounterweightChain();
            remainingSeconds = 0f;
            return false;
        }

        targetWeight = pendingCounterweightType == WeightType.Heavy
            ? WeightType.Light
            : WeightType.Heavy;
        return true;
    }

    public void BreakCounterweightChain()
    {
        hasPendingCounterweight = false;
    }

    public void AddCounterweightEnergy(float amount)
    {
        calibrationEnergy = Mathf.Clamp(calibrationEnergy + amount, 0f, maxCalibrationEnergy);
        UpdateCalibrationUI();
    }

    public void Calibrate(float offsetRate, float velocityRate)
    {
        offsetY *= offsetRate;
        velocityY *= velocityRate;
    }

    public bool TryUseCalibration(float gravityLockDuration = 2f)
    {
        if (calibrationEnergy < calibrationCost)
        {
            Debug.Log("校准能量不足");
            return false;
        }

        calibrationEnergy -= calibrationCost;
        UpdateCalibrationUI();

        Calibrate(calibrationOffsetRate, calibrationVelocityRate);
        BreakCounterweightChain();
        gravityLockTimer = Mathf.Max(gravityLockTimer, Mathf.Max(0f, gravityLockDuration));
        PlayShake(0.06f, 0.12f);

        if (hitEffectSystem != null && judgeLine != null)
        {
            hitEffectSystem.PlayCalibrationEffect(judgeLine.position);
        }

        Debug.Log("使用 Space 校准判定线");

        return true;
    }

    public float GetGravityLockRemaining()
    {
        return gravityLockTimer;
    }

    private void ExpireCounterweightWindow()
    {
        if (hasPendingCounterweight &&
            GetCurrentMusicTime() - pendingCounterweightTime > GetCounterweightWindowDuration())
        {
            BreakCounterweightChain();
        }
    }

    private float GetCounterweightWindowDuration()
    {
        float bpm = gameManager != null && gameManager.chartData != null
            ? gameManager.chartData.bpm
            : 0f;
        float beatDuration = bpm > 0f ? 60f / bpm : 0.5f;
        return beatDuration * Mathf.Max(1f, counterweightWindowBeats);
    }

    private float GetCurrentMusicTime()
    {
        return gameManager != null ? gameManager.GetMusicTime() : Time.time;
    }

    private void UpdateCalibrationUI()
    {
        if (calibrationSlider != null)
        {
            calibrationSlider.maxValue = maxCalibrationEnergy;
            calibrationSlider.value = calibrationEnergy;
        }
    }

    public float GetAbsOffset()
    {
        return Mathf.Abs(offsetY);
    }

    private void UpdateSpringBack()
    {
        velocityY += -offsetY * springForce * Time.deltaTime;
        velocityY *= Mathf.Pow(Mathf.Clamp(damping, 0.01f, 1f), Time.deltaTime * 60f);
        offsetY += velocityY * Time.deltaTime;

        offsetY = Mathf.Clamp(offsetY, -maxOffset, maxOffset);
    }

    private void ApplyPosition()
    {
        Vector3 offset = Vector3.up * (offsetY + shakeOffsetY);

        if (judgeLine != null)
        {
            judgeLine.position = judgeLineBasePosition + offset;
            float intensity = Mathf.Clamp01(Mathf.Abs(offsetY) / Mathf.Max(0.01f, maxOffset));
            judgeLine.localScale = new Vector3(
                judgeLineBaseScale.x,
                judgeLineBaseScale.y * (1f + intensity * 0.8f),
                judgeLineBaseScale.z);
            UpdateGravityVisuals(intensity);
        }

        if (judgePointRoot != null)
        {
            judgePointRoot.position = judgePointRootBasePosition + offset;
        }
    }

    private void UpdateGravityVisuals(float intensity)
    {
        if (judgeLineRenderer == null)
        {
            return;
        }

        Color stableColor = new Color32(95, 221, 226, 255);
        Color forceColor = offsetY < 0f
            ? new Color32(255, 166, 88, 255)
            : new Color32(97, 180, 255, 255);
        Color fieldColor = Color.Lerp(stableColor, forceColor, Mathf.Clamp01(intensity * 1.7f));
        if (Mathf.Abs(offsetY) >= visualDangerOffset)
        {
            fieldColor = Color.Lerp(fieldColor, new Color32(255, 73, 96, 255), 0.78f);
        }

        judgeLineRenderer.GetPropertyBlock(judgeLineProperties);
        judgeLineProperties.SetColor("_Color", fieldColor);
        judgeLineProperties.SetColor("_BaseColor", fieldColor);
        judgeLineProperties.SetColor("_EmissionColor", fieldColor * (0.25f + intensity * 1.4f));
        judgeLineRenderer.SetPropertyBlock(judgeLineProperties);
    }
}
