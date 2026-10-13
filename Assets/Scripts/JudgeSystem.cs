using UnityEngine;
using TMPro;

public class JudgeSystem : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public ScoreManager scoreManager;
    public JudgeLineController judgeLineController;
    public StabilitySystem stabilitySystem;
    public HitEffectSystem hitEffectSystem;

    [Header("Judge Windows")]
    public float perfectWindow = 0.05f;
    public float goodWindow = 0.10f;
    public float badWindow = 0.15f;

    [Header("Input Offset")]
    public float inputOffsetSeconds = 0f;

    [Header("Counterweight Bonus")]
    public int counterweightScoreBonus = 250;
    public float counterweightStabilityGain = 3f;
    public float counterweightEnergyGain = 5f;

    [Header("UI")]
    public TMP_Text judgeText;
    public float judgeTextShowTime = 0.3f;

    private float judgeTextTimer = 0f;
    private const string InputOffsetKey = "GB_InputOffsetMs";

    private void Update()
    {
        UpdateJudgeTextTimer();
    }

    private void Start()
    {
        LoadSavedInputOffset();
    }

    private void UpdateJudgeTextTimer()
    {
        if (judgeText == null)
        {
            return;
        }

        if (judgeTextTimer <= 0f)
        {
            return;
        }

        judgeTextTimer -= Time.deltaTime;

        if (judgeTextTimer <= 0f)
        {
            judgeText.text = "";
        }
    }

    public JudgeResult Judge(Note note)
    {
        if (gameManager == null || note == null)
        {
            return JudgeResult.Miss;
        }

        float currentMusicTime = GetAdjustedMusicTime();
        float timeDiff = Mathf.Abs(currentMusicTime - note.hitTime);

        JudgeResult result = GetJudgeResult(timeDiff);

        ApplyJudgeResult(note, result);

        return result;
    }

    public void SetInputOffsetMilliseconds(float milliseconds, bool save = true)
    {
        milliseconds = Mathf.Clamp(milliseconds, -200f, 200f);
        inputOffsetSeconds = milliseconds / 1000f;

        if (save)
        {
            PlayerPrefs.SetFloat(InputOffsetKey, milliseconds);
            PlayerPrefs.Save();
        }

        Debug.Log($"设置按键延迟：{milliseconds:F0} ms");
    }

    public float GetInputOffsetMilliseconds()
    {
        return inputOffsetSeconds * 1000f;
    }

    public float GetAdjustedMusicTime()
    {
        if (gameManager == null)
        {
            return 0f;
        }

        return gameManager.GetMusicTime() + inputOffsetSeconds;
    }

    public float GetGravityOffset()
    {
        return judgeLineController != null ? judgeLineController.offsetY : 0f;
    }

    private void LoadSavedInputOffset()
    {
        if (PlayerPrefs.HasKey(InputOffsetKey))
        {
            float savedOffset = PlayerPrefs.GetFloat(InputOffsetKey, 0f);
            SetInputOffsetMilliseconds(savedOffset, false);
        }
    }


    public void ApplyAutoMiss(Note note)
    {
        if (note == null)
        {
            return;
        }

        ShowJudgeText(JudgeResult.Miss);

        if (scoreManager != null)
        {
            scoreManager.ApplyJudgeResult(JudgeResult.Miss);
        }
        if (stabilitySystem != null)
        {
            stabilitySystem.ApplyJudgeResult(JudgeResult.Miss, note.weightType);
        }

        if (judgeLineController != null)
        {
            judgeLineController.BreakCounterweightChain();
        }


        note.hasMissed = true;

        if (hitEffectSystem != null)
        {
            hitEffectSystem.PlayMissEffect(note.transform.position);
        }

        Debug.Log($"Auto Miss: lane={note.lane}, time={note.hitTime}");

        Destroy(note.gameObject);
    }

    public JudgeResult GetJudgeResult(float timeDiff)
    {
        if (timeDiff <= perfectWindow)
        {
            return JudgeResult.Perfect;
        }

        if (timeDiff <= goodWindow)
        {
            return JudgeResult.Good;
        }

        if (timeDiff <= badWindow)
        {
            return JudgeResult.Bad;
        }

        return JudgeResult.Miss;
    }

    private void ApplyJudgeResult(Note note, JudgeResult result)
    {
        ShowJudgeText(result);

        if (scoreManager != null)
        {
            scoreManager.ApplyJudgeResult(result);
        }

        if (stabilitySystem != null)
        {
            stabilitySystem.ApplyJudgeResult(result, note.weightType);
        }

        if (judgeLineController != null)
        {
            judgeLineController.AddCalibrationEnergy(result, note.weightType);
        }

        if (result == JudgeResult.Miss)
        {
            if (judgeLineController != null)
            {
                judgeLineController.BreakCounterweightChain();
            }

            note.hasMissed = true;
            Debug.Log($"Miss: lane={note.lane}, time={note.hitTime}");
            Destroy(note.gameObject);
            return;
        }
        if (hitEffectSystem != null)
        {
            hitEffectSystem.PlayHitEffect(note.transform.position, note.weightType);
        }

        note.hasBeenHit = true;

        bool counterweightResolved = false;
        if (judgeLineController != null)
        {
            bool canTriggerCounterweight = result == JudgeResult.Perfect || result == JudgeResult.Good;
            counterweightResolved = judgeLineController.ApplyWeight(note.weightType, canTriggerCounterweight);
        }

        if (counterweightResolved)
        {
            if (scoreManager != null)
            {
                scoreManager.RegisterCounterweightBonus(counterweightScoreBonus);
            }

            if (stabilitySystem != null)
            {
                stabilitySystem.ChangeStability(counterweightStabilityGain);
            }

            if (judgeLineController != null)
            {
                judgeLineController.AddCounterweightEnergy(counterweightEnergyGain);
            }

            if (hitEffectSystem != null)
            {
                hitEffectSystem.PlayCounterweightEffect(note.transform.position);
            }

            ShowCounterweightText(result);
        }

        if (judgeLineController != null)
        {
            if (counterweightResolved)
            {
                judgeLineController.PlayShake(0.09f, 0.14f);
            }
            else
            {
                switch (note.weightType)
                {
                    case WeightType.Heavy:
                        judgeLineController.PlayShake(0.16f, 0.18f);
                        break;

                    case WeightType.Light:
                        judgeLineController.PlayShake(0.06f, 0.10f);
                        break;

                    case WeightType.Stabilize:
                        judgeLineController.PlayShake(0.08f, 0.12f);
                        break;
                }
            }
        }

        if (!counterweightResolved)
        {
            ShowWeightFeedback(note.weightType, result);
        }

        Debug.Log($"{result}: lane={note.lane}, time={note.hitTime}, weight={note.weightType}");

        Destroy(note.gameObject);
    }

    private void ShowJudgeText(JudgeResult result)
    {
        if (judgeText == null)
        {
            return;
        }

        judgeText.text = result.ToString();
        judgeText.color = Color.white;
        judgeTextTimer = judgeTextShowTime;
    }

    private void ShowWeightFeedback(WeightType weightType, JudgeResult result)
    {
        if (judgeText == null || weightType == WeightType.Normal)
        {
            return;
        }

        string forceLabel;
        Color forceColor;
        switch (weightType)
        {
            case WeightType.Heavy:
                forceLabel = "重力下坠";
                forceColor = new Color32(255, 173, 94, 255);
                break;
            case WeightType.Light:
                forceLabel = "反重力上浮";
                forceColor = new Color32(105, 195, 255, 255);
                break;
            case WeightType.Stabilize:
                forceLabel = "稳定场·配平";
                forceColor = new Color32(112, 235, 187, 255);
                break;
            default:
                return;
        }

        judgeText.text = result + "\n" + forceLabel;
        judgeText.color = forceColor;
        judgeTextTimer = Mathf.Max(judgeTextShowTime, 0.55f);
    }

    private void ShowCounterweightText(JudgeResult result)
    {
        if (judgeText == null)
        {
            return;
        }

        judgeText.text = "重力配平成功\n" + result + "  +" + counterweightScoreBonus;
        judgeText.color = new Color32(115, 239, 195, 255);
        judgeTextTimer = Mathf.Max(judgeTextShowTime, 0.72f);
    }
}
