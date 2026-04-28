using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace com.karabaev.gameplayTags.editor
{
  [CustomPropertyDrawer(typeof(TagContainerAuthoring))]
  public class TagContainerAuthoringDrawer : PropertyDrawer
  {
    private SerializedProperty _tagNamesProperty = null!;
    private Button _button = null!;
    private VisualElement _chipsArea = null!;
    private float _popupWidth;
    
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
      _tagNamesProperty = property.FindPropertyRelative(nameof(TagContainerAuthoring.TagNames));

      var root = new VisualElement();
      root.RegisterCallback<GeometryChangedEvent>(e => _popupWidth = e.newRect.width);
      
      var headerRow = TagDrawerUtils.MakeTagRow();
      root.Add(headerRow);
      
      _button = TagDrawerUtils.MakeDropdownButton();
      _button.clicked += OnButtonClicked;
      headerRow.Add(TagDrawerUtils.MakeFieldLabel(property.displayName));
      headerRow.Add(_button);
      
      _chipsArea = new VisualElement();
      _chipsArea.style.marginLeft = TagDrawerUtils.LabelWidth;
      root.Add(_chipsArea);

      Refresh(_button, _chipsArea, _tagNamesProperty);
      return root;
    }

    private void OnButtonClicked()
    {
      var current = new List<string>();
      for (var i = 0; i < _tagNamesProperty.arraySize; i++)
      {
        current.Add(_tagNamesProperty.GetArrayElementAtIndex(i).stringValue);
      }

      var popup = new TagPickerPopup(
        current,
        multiSelect: true,
        changed: updated =>
        {
          _tagNamesProperty.ClearArray();
          for(var i = 0; i < updated.Count; i++)
          {
            _tagNamesProperty.InsertArrayElementAtIndex(i);
            _tagNamesProperty.GetArrayElementAtIndex(i).stringValue = updated[i];
          }
          _tagNamesProperty.serializedObject.ApplyModifiedProperties();
          Refresh(_button, _chipsArea, _tagNamesProperty);
        },
        db: TagDatabase.instance,
        windowWidth: _popupWidth);

      TagDrawerUtils.ShowTagPopup(_button, popup);
    }
    
    private static void Refresh(Button button, VisualElement chipsArea, SerializedProperty pathsProp)
    {
      var tagsCount = pathsProp.arraySize;
      button.text = tagsCount == 0 
        ? "(None)" 
        : $"▼ {tagsCount} tag{(tagsCount == 1 ? string.Empty : "s")}";

      chipsArea.Clear();
      for(var i = 0; i < tagsCount; i++)
      {
        var capturedIndex = i;
        var path = pathsProp.GetArrayElementAtIndex(i).stringValue;

        var chip = new VisualElement();
        chip.style.flexDirection = FlexDirection.Row;
        chip.style.alignItems = Align.Center;
        chip.style.minHeight = 20;

        var nameLabel = new Label(path);
        nameLabel.style.flexGrow = 1;
        nameLabel.style.paddingLeft = 4;
        chip.Add(nameLabel);

        chip.Add(TagDrawerUtils.MakeRemoveButton(() =>
        {
          pathsProp.DeleteArrayElementAtIndex(capturedIndex);
          pathsProp.serializedObject.ApplyModifiedProperties();
          Refresh(button, chipsArea, pathsProp);
        }));

        chipsArea.Add(chip);
      }
    }
  }
}
