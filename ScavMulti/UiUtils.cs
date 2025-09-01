using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace ScavMulti;

public enum ContentAlignment
{
	TopLeft,
	TopCenter,
	TopRight,
	MiddleLeft,
	MiddleCenter,
	MiddleRight,
	BottomLeft,
	BottomCenter,
	BottomRight
}

// UiUtils is the family friendly version of oh god it hurts no more Unity UI through code please make it stop
static class UiUtils
{
	public static Sprite DefaultBackgroundSprite = null;

	/// <summary>
    /// its properties will be copied to text labels
    /// </summary>
	public static TextMeshProUGUI ReferenceText = null;

	/// <summary>
	/// makes a canvas with a background image that grows according to its children content
	/// </summary>
	public static GameObject CreateAutoGrowingUI(
		Transform parent,
		string name,
		ContentAlignment contentAlignment,
		Vector2 positionRelativeToAlignment,
		float gapBetweenElements,
		RectOffset padding,
		Sprite overrideBackgroundSprite = null)
	{
		var resultGO = new GameObject(name);
		resultGO.transform.SetParent(parent, false);
		
		resultGO.AddComponent<CanvasRenderer>();
		var image = resultGO.AddComponent<Image>();
		image.sprite = overrideBackgroundSprite ?? DefaultBackgroundSprite;
		image.type = Image.Type.Sliced; // makes it so edges don't stretch

		var rectTransform = resultGO.GetComponent<RectTransform>();
		var direction = GetNormalizedAnchorDirection(contentAlignment);
		rectTransform.anchorMin = direction;
		rectTransform.anchorMax = direction;
		rectTransform.pivot = direction; // makes it grow in the direction of the anchor, eg to the right on ContentAlignment.*Left
		rectTransform.anchoredPosition = positionRelativeToAlignment;

		var layoutGroup = resultGO.AddComponent<VerticalLayoutGroup>();
		layoutGroup.childAlignment = TextAnchor.MiddleCenter;
		layoutGroup.childControlWidth = true;
		layoutGroup.childControlHeight = true;
		layoutGroup.padding = padding;
		layoutGroup.spacing = gapBetweenElements;

		var fitter = resultGO.AddComponent<ContentSizeFitter>();
		fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		return resultGO;
	}

	public static TextMeshProUGUI CreateTMPLabel(Transform parent, string name, string text, bool keepDefault = false, Color? overrideColor = null)
	{
		var result = new GameObject(name);
		result.transform.SetParent(parent, false);
		var textComponent = result.AddComponent<TextMeshProUGUI>();
		if (!keepDefault && ReferenceText)
		{
			// lazy ? yeah
			ReflectionUtils.ShallowCopyPropsOnly(ReferenceText, textComponent);
			if (overrideColor.HasValue)
				textComponent.color = overrideColor.Value;
		}
		else
		{
			textComponent.fontSize = 14;
			textComponent.color = overrideColor ?? Color.white;
		}
		textComponent.text = text;
		return textComponent;
	}

    public static TMP_InputField CreateInputField(
		Transform parent,
		string objectName = "InputField",
		string prompt = "Enter text...",
		float fontSize = 14,
		float? customWidth = null,
		Sprite overrideBackgroundSprite = null)
    {
		// input field hierarchy (as shown when doing Create -> UI -> Input Field - TextMeshPro):
		// InputField
		//    | TextArea (RectMask2D)
		//       | Placeholder (TextMeshProUGUI)
		//       | Text (TextMeshProUGUI)
		// ill be honest i don't understand everything i do here since usually you don't do it through code
		// it just works trademark
        var resultGO = new GameObject(objectName);
        resultGO.transform.SetParent(parent, false);
        resultGO.AddComponent<RectTransform>();
		if (customWidth.HasValue)
		{
			var layoutElement = resultGO.AddComponent<LayoutElement>();
			layoutElement.minWidth = layoutElement.preferredWidth = customWidth.Value;
		}
		resultGO.AddComponent<CanvasRenderer>();

        TMP_InputField inputField = resultGO.AddComponent<TMP_InputField>();

        // Text Area
        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(resultGO.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 6);
        textAreaRect.offsetMax = new Vector2(-10, -6);
        textArea.AddComponent<RectMask2D>();
        inputField.textViewport = textAreaRect;

        // Placeholder
        var placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(textArea.transform, false);
        RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 6);
        placeholderRect.offsetMax = new Vector2(-10, -6);

        TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
        placeholderText.text = prompt;
        placeholderText.fontSize = fontSize;
        placeholderText.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        placeholderText.alignment = TextAlignmentOptions.Left;
        inputField.placeholder = placeholderText;

        // Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textArea.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 6);
        textRect.offsetMax = new Vector2(-10, -6);

        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = "";
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.enableWordWrapping = false;
		textComponent.overflowMode = TextOverflowModes.ScrollRect;
		inputField.textComponent = textComponent;

        // Set InputField background
        Image background = resultGO.AddComponent<Image>();
        background.color = Color.white;
		background.sprite = overrideBackgroundSprite ?? DefaultBackgroundSprite;
		background.type = Image.Type.Sliced;
        inputField.targetGraphic = background;

        inputField.transition = Selectable.Transition.ColorTint;

        // Set up color transitions
        ColorBlock colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.95f, 0.95f, 0.95f),
            pressedColor = new Color(0.9f, 0.9f, 0.9f),
            selectedColor = new Color(0.95f, 0.95f, 0.95f),
            disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f),
            colorMultiplier = 1,
            fadeDuration = 0.1f
        };
        inputField.colors = colors;

        return inputField;
    }

	public static void ForceRealodObject(GameObject obj)
	{
		// only way i found to properly reload the ui. LayoutRebuilder.ForceRebuildLayoutImmediate does nothing. i hate the unity UI system.
		obj.SetActive(false);
		obj.SetActive(true);
	}

	public static Button CreateButton(Transform parent, string name, string content, UnityAction action)
	{
		var resultGO = new GameObject(name);
		resultGO.transform.SetParent(parent, false);

		var image = resultGO.AddComponent<Image>();
		image.sprite = UiUtils.DefaultBackgroundSprite;
		image.type = Image.Type.Sliced;
		var layout = resultGO.AddComponent<VerticalLayoutGroup>();
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		var button = resultGO.AddComponent<Button>();
		if (action != null)
			button.onClick.AddListener(action);
		UiUtils.CreateTMPLabel(resultGO.transform, "Button text", content);
		return button;
	}

	public static Vector2 GetNormalizedAnchorDirection(ContentAlignment alignment) => alignment switch
	{
		ContentAlignment.TopLeft => Vector2.up,
		ContentAlignment.TopCenter => new Vector2(0.5f, 1),
		ContentAlignment.TopRight => Vector2.one,
		ContentAlignment.MiddleLeft => new Vector2(0, 0.5f),
		ContentAlignment.MiddleCenter => new Vector2(0.5f, 0.5f),
		ContentAlignment.MiddleRight => new Vector2(1, 0.5f),
		ContentAlignment.BottomLeft => Vector2.zero,
		ContentAlignment.BottomCenter => new Vector2(0.5f, 0),
		ContentAlignment.BottomRight => Vector2.right,
		_ => throw new ArgumentException("alignment")
	};
}
