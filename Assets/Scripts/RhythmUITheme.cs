using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared visual language for menus, overlays and song selection.
/// </summary>
public static class RhythmUITheme
{
    public static readonly Color Ink = new Color32(5, 12, 27, 255);
    public static readonly Color Panel = new Color32(12, 24, 43, 246);
    public static readonly Color PanelRaised = new Color32(20, 35, 57, 248);
    public static readonly Color Primary = new Color32(0, 190, 210, 255);
    public static readonly Color Text = new Color32(237, 245, 255, 255);
    public static readonly Color MutedText = new Color32(160, 181, 205, 255);
    public static readonly Color Gold = new Color32(255, 210, 125, 255);

    private const int RoundedTextureSize = 64;
    private const float RoundedCornerRadius = 14f;
    private static Sprite roundedSprite;
    private static readonly Dictionary<string, Sprite> ResourceSprites = new Dictionary<string, Sprite>();

    public static Transform FindDeep(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    public static void StylePanel(Image image, Color color, bool addShadow = true)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        if (addShadow)
        {
            Shadow shadow = image.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = image.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color32(0, 4, 14, 125);
            shadow.effectDistance = new Vector2(0f, -5f);
            shadow.useGraphicAlpha = true;
        }
    }

    public static void StyleButton(Button button, Color baseColor, Color labelColor)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.13f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.17f);
        colors.selectedColor = Color.Lerp(baseColor, Color.white, 0.09f);
        colors.disabledColor = new Color32(61, 72, 91, 170);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.14f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;

        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].color = labelColor;
            labels[i].raycastTarget = false;
        }
    }

    public static void StyleText(TMP_Text text, float size, Color color, bool bold = false)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = size;
        text.color = color;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12f, size - 4f);
        text.fontSizeMax = size;
        text.raycastTarget = false;
    }

    public static void ApplyBackdrop(Image image, string resourcePath, Color tint)
    {
        if (image == null)
        {
            return;
        }

        Sprite backdrop = GetResourceSprite(resourcePath);
        if (backdrop == null)
        {
            return;
        }

        image.sprite = backdrop;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = tint;
        image.raycastTarget = false;
    }

    public static Sprite RoundedSprite
    {
        get
        {
            if (roundedSprite != null)
            {
                return roundedSprite;
            }

            Texture2D texture = new Texture2D(RoundedTextureSize, RoundedTextureSize, TextureFormat.RGBA32, false);
            texture.name = "Runtime Rounded UI Sprite";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            for (int y = 0; y < RoundedTextureSize; y++)
            {
                for (int x = 0; x < RoundedTextureSize; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 nearestCornerCenter = new Vector2(
                        Mathf.Clamp(point.x, RoundedCornerRadius, RoundedTextureSize - RoundedCornerRadius),
                        Mathf.Clamp(point.y, RoundedCornerRadius, RoundedTextureSize - RoundedCornerRadius)
                    );
                    float outsideDistance = Vector2.Distance(point, nearestCornerCenter) - RoundedCornerRadius;
                    float alpha = 1f - Mathf.Clamp01(outsideDistance + 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            roundedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, RoundedTextureSize, RoundedTextureSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(20f, 20f, 20f, 20f)
            );
            roundedSprite.name = "Runtime Rounded UI Sprite";
            roundedSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedSprite;
        }
    }

    private static Sprite GetResourceSprite(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        if (ResourceSprites.TryGetValue(resourcePath, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        sprite.name = "Runtime " + resourcePath + " Backdrop";
        sprite.hideFlags = HideFlags.HideAndDontSave;
        ResourceSprites[resourcePath] = sprite;
        return sprite;
    }
}
