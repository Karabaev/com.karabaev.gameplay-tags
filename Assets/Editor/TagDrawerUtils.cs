using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace com.karabaev.gameplayTags.editor
{
  internal static class TagDrawerUtils
  {
    internal const float LabelWidth = 150f;
    internal const float RemoveBtnWidth = 20f;

    internal static VisualElement MakeTagRow()
    {
      var row = new VisualElement();
      row.style.flexDirection = FlexDirection.Row;
      row.style.alignItems = Align.Center;
      row.style.minHeight = 20;
      return row;
    }
    
    internal static Label MakeFieldLabel(string displayName)
    {
      var label = new Label(displayName);
      label.style.width = LabelWidth;
      label.style.minWidth = LabelWidth;
      label.AddToClassList("unity-base-field__label");
      return label;
    }

    /// <summary>Creates a flex-grow button used as the dropdown trigger.</summary>
    internal static Button MakeDropdownButton()
    {
      var btn = new Button();
      btn.style.flexGrow = 1;
      btn.style.unityTextAlign = TextAnchor.MiddleLeft;
      btn.style.paddingLeft = 4;
      return btn;
    }

    /// <summary>
    /// Creates a small red × button. <paramref name="onClick"/> is called when pressed.
    /// </summary>
    internal static Button MakeRemoveButton(Action onClick)
    {
      var btn = new Button(onClick) { text = "×" };
      btn.style.width = RemoveBtnWidth;
      btn.style.height = 18;
      btn.style.paddingLeft = 0;
      btn.style.paddingRight = 0;
      btn.style.color = new StyleColor(new Color(0.9f, 0.4f, 0.4f));
      btn.style.marginLeft = 2;
      return btn;
    }

    /// <summary>
    /// Opens a <see cref="TagPickerPopup"/> anchored below <paramref name="anchor"/>,
    /// with popup width matching the anchor's width.
    /// </summary>
    internal static void ShowTagPopup(
      VisualElement anchor,
      TagPickerPopup popup)
    {
      PopupWindow.Show(anchor.worldBound, popup);
    }
  }
}
