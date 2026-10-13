using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    public int perfectScore = 1000;
    public int goodScore = 700;
    public int badScore = 300;

    [Header("Runtime")]
    public int score = 0;
    public int combo = 0;
    public int maxCombo = 0;
    public int perfectCount = 0;
    public int goodCount = 0;
    public int badCount = 0;
    public int missCount = 0;
    public int counterweightCount = 0;
    public int counterweightBonusScore = 0;
    public int bestScore = 0;
    public int bestCombo = 0;
    public bool isNewBestScore = false;
    public bool isNewBestCombo = false;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text comboText;

    private void Start()
    {
        ResetScore();
    }

    public void ResetScore()
    {
        score = 0;
        combo = 0;
        maxCombo = 0;
        perfectCount = 0;
        goodCount = 0;
        badCount = 0;
        missCount = 0;
        counterweightCount = 0;
        counterweightBonusScore = 0;
        bestScore = 0;
        bestCombo = 0;
        isNewBestScore = false;
        isNewBestCombo = false;

        UpdateUI();
    }

    public void ApplyJudgeResult(JudgeResult result)
    {
        switch (result)
        {
            case JudgeResult.Perfect:
                perfectCount++;
                AddScore(perfectScore);
                AddCombo();
                break;

            case JudgeResult.Good:
                goodCount++;
                AddScore(goodScore);
                AddCombo();
                break;

            case JudgeResult.Bad:
                badCount++;
                AddScore(badScore);
                AddCombo();
                break;

            case JudgeResult.Miss:
                missCount++;
                BreakCombo();
                break;
        }

        UpdateUI();
    }

    public void FinalizeRun(string songId, string difficulty)
    {
        string safeSongId = string.IsNullOrEmpty(songId) ? "unknown" : songId;
        string safeDifficulty = string.IsNullOrEmpty(difficulty) ? "unknown" : difficulty;
        string keySuffix = $"{safeSongId}_{safeDifficulty}";
        string scoreKey = $"GB_BestScore_{keySuffix}";
        string comboKey = $"GB_BestCombo_{keySuffix}";

        bestScore = PlayerPrefs.GetInt(scoreKey, 0);
        bestCombo = PlayerPrefs.GetInt(comboKey, 0);
        isNewBestScore = false;
        isNewBestCombo = false;

        if (GetTotalJudgements() <= 0)
        {
            return;
        }

        bool changed = false;

        if (score > bestScore)
        {
            bestScore = score;
            isNewBestScore = true;
            PlayerPrefs.SetInt(scoreKey, bestScore);
            changed = true;
        }

        if (maxCombo > bestCombo)
        {
            bestCombo = maxCombo;
            isNewBestCombo = true;
            PlayerPrefs.SetInt(comboKey, bestCombo);
            changed = true;
        }

        if (changed)
        {
            PlayerPrefs.Save();
        }
    }

    private void AddScore(int amount)
    {
        score += amount;
    }

    private void AddCombo()
    {
        combo++;

        if (combo > maxCombo)
        {
            maxCombo = combo;
        }
    }

    private void BreakCombo()
    {
        combo = 0;
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score:N0}";
        }

        if (comboText != null)
        {
            comboText.text = combo > 0 ? $"Combo: {combo}" : "";
        }
    }

    public int GetTotalJudgements()
    {
        return perfectCount + goodCount + badCount + missCount;
    }

    public float GetAccuracy()
    {
        int total = GetTotalJudgements();
        if (total == 0 || perfectScore <= 0)
        {
            return 0f;
        }

        float earned = perfectCount * perfectScore + goodCount * goodScore + badCount * badScore;
        float possible = total * perfectScore;
        return Mathf.Clamp01(earned / possible) * 100f;
    }

    public string GetGrade()
    {
        if (GetTotalJudgements() == 0)
        {
            return "--";
        }

        float accuracy = GetAccuracy();
        if (accuracy >= 99f) return "S";
        if (accuracy >= 95f) return "A";
        if (accuracy >= 90f) return "B";
        if (accuracy >= 80f) return "C";
        return "D";
    }

    public int GetScore()
    {
        return score;
    }

    public int GetMaxCombo()
    {
        return maxCombo;
    }

    public void RegisterCounterweightBonus(int bonus)
    {
        if (bonus <= 0)
        {
            return;
        }

        counterweightCount++;
        counterweightBonusScore += bonus;
        score += bonus;
        UpdateUI();
    }
}
