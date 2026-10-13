using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ResumeCountdownUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text countdownText;
    public CanvasGroup canvasGroup;

    [Header("Timing")]
    public float numberDuration = 0.65f;
    public float goDuration = 0.45f;

    [Header("Colors RGB")]
    public Color numberColor = new Color32(0, 220, 255, 255);
    public Color goColor = new Color32(240, 248, 255, 255);

    [Header("Animation")]
    public float startScale = 1.35f;
    public float endScale = 1.0f;

    private Coroutine countdownCoroutine;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        HideImmediately();
    }

    public void PlayCountdown(Action onFinished)
    {
        gameObject.SetActive(true);

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("ResumeCountdownPanel 不能放在未启用的父对象下面。请把它放到 Canvas 根节点下，和 PausePanel 平级。");
            onFinished?.Invoke();
            return;
        }

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        countdownCoroutine = StartCoroutine(CountdownRoutine(onFinished));
    }

    private IEnumerator CountdownRoutine(Action onFinished)
    {
        SetVisible(true);

        string[] numbers =
        {
            "3",
            "2",
            "1"
        };

        for (int i = 0; i < numbers.Length; i++)
        {
            ShowText(numbers[i], numberColor);
            yield return AnimateText(numberDuration);
        }

        ShowText("GO", goColor);
        yield return AnimateText(goDuration);

        HideImmediately();
        countdownCoroutine = null;

        onFinished?.Invoke();
    }

    private void ShowText(string text, Color color)
    {
        if (countdownText != null)
        {
            countdownText.text = text;
            countdownText.color = color;
            countdownText.rectTransform.localScale = Vector3.one * startScale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private IEnumerator AnimateText(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(timer / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);

            if (countdownText != null)
            {
                float scale = Mathf.Lerp(startScale, endScale, eased);
                countdownText.rectTransform.localScale = Vector3.one * scale;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0.15f, progress);
            }

            yield return null;
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    private void HideImmediately()
    {
        SetVisible(false);

        if (countdownText != null)
        {
            countdownText.text = "";
        }
    }
}
