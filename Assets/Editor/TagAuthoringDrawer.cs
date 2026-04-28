using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace com.karabaev.gameplayTags.editor
{
  [CustomPropertyDrawer(typeof(TagAuthoring))]
  public class TagAuthoringDrawer : PropertyDrawer
  {
    private SerializedProperty _tagNameProperty = null!;
    private Button _button = null!;
    private float _popupWidth;

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
      _tagNameProperty = property.FindPropertyRelative(nameof(TagAuthoring.Name));

      var root = TagDrawerUtils.MakeTagRow();
      root.RegisterCallback<GeometryChangedEvent>(e => _popupWidth = e.newRect.width);
      root.Add(TagDrawerUtils.MakeFieldLabel(property.displayName));

      _button = TagDrawerUtils.MakeDropdownButton();
      _button.clicked += OnButtonClicked;
      root.Add(_button);

      RefreshCurrentValueText(_button, _tagNameProperty);
      return root;
    }

    private void OnButtonClicked()
    {
      var popup = new TagPickerPopup(
        new List<string> { _tagNameProperty.stringValue },
        multiSelect: false,
        changed: selected =>
        {
          _tagNameProperty.stringValue = selected.Count > 0 ? selected[0] : string.Empty;
          _tagNameProperty.serializedObject.ApplyModifiedProperties();
          RefreshCurrentValueText(_button, _tagNameProperty);
        },
        db: TagDatabase.instance, _popupWidth);

      TagDrawerUtils.ShowTagPopup(_button, popup);
    }
    
    private static void RefreshCurrentValueText(Button button, SerializedProperty pathProp)
    {
      button.text = string.IsNullOrEmpty(pathProp.stringValue) ? "(None)" : pathProp.stringValue;
    }
  }
}
