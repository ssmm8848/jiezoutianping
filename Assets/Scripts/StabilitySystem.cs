using UnityEngine;
using UnityEngine.UI;

public class StabilitySystem : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public JudgeLineController judgeLineController;

    [Header("Stability")]
    public float maxStability = 100f;
    public float stability = 100f;

    [Header("Judge Change")]
    public float perfectGain = 1f;
    public float goodGain = 0f;
    public float badLoss = 3f;
    public float missLoss = 8f;
    public float heavyMissLoss = 15f;
    public float stabilizePerfectGain = 8f;
    public float stabilizeGoodGain = 4f;

    [Header("Offset Penalty")]
    public float warningOffset = 0.8f;
    public float dangerOffset = 1.2f;
    public float warningLossPerSecond = 5f;
    public float dangerLossPerSecond = 12f;

    [Header("UI")]
    public Slider stabilitySlider;

    private bool hasFailed = false;

    private void Start()
    {
        ResetStability();
    }

    private void Update()
    {
        if (gameManager == null || !gameManager.IsPlaying())
        {
            return;
        }

        ApplyOffsetPenalty();
    }

    public void ResetStability()
    {
        stability = maxStability;
        hasFailed = false;
        UpdateUI();
    }

    public void ApplyJudgeResult(JudgeResult result, WeightType weightType)
    {
        switch (result)
        {
            case JudgeResult.Perfect:
                if (weightType == WeightType.Stabilize)
                {
                    ChangeStability(stabilizePerfectGain);
                }
                else
                {
                    ChangeStability(perfectGain);
                }
                break;

            case JudgeResult.Good:
                if (weightType == WeightType.Stabilize)
                {
                    ChangeStability(stabilizeGoodGain);
                }
                else
                {
                    ChangeStability(goodGain);
                }
                break;

            case JudgeResult.Bad:
                ChangeStability(-badLoss);
                break;

            case JudgeResult.Miss:
                if (weightType == WeightType.Heavy)
                {
                    ChangeStability(-heavyMissLoss);
                }
                else
                {
                    ChangeStability(-missLoss);
                }
                break;
        }
    }

    public void ChangeStability(float amount)
    {
        if (hasFailed)
        {
            return;
        }

        stability += amount;
        stability = Mathf.Clamp(stability, 0f, maxStability);

        UpdateUI();

        if (stability <= 0f)
        {
            Fail();
        }
    }

    private void ApplyOffsetPenalty()
    {
        if (judgeLineController == null)
        {
            return;
        }

        float absOffset = judgeLineController.GetAbsOffset();

        if (absOffset >= dangerOffset)
        {
            ChangeStability(-dangerLossPerSecond * Time.deltaTime);
        }
        else if (absOffset >= warningOffset)
        {
            ChangeStability(-warningLossPerSecond * Time.deltaTime);
        }
    }

    private void Fail()
    {
        if (hasFailed)
        {
            return;
        }

        hasFailed = true;

        if (gameManager != null)
        {
            gameManager.FailGame();
        }
    }

    private void UpdateUI()
    {
        if (stabilitySlider != null)
        {
            stabilitySlider.maxValue = maxStability;
            stabilitySlider.value = stability;
        }
    }

    public float GetStability()
    {
        return stability;
    }
}