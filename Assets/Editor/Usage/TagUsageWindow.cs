using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.karabaev.gameplayTags.editor.Usage
{
  internal class TagUsageWindow : EditorWindow
  {
    private string _tagName = string.Empty;
    private List<TagUsageFinder.UsageResult>? _results;

    internal static void Open(string tagName)
    {
      var window = GetWindow<TagUsageWindow>();
      window.titleContent = new GUIContent("Tag Usages");
      window._tagName = tagName;
      try
      {
        var results = TagUsageFinder.FindUsages(tagName);
        window._results = results;
        window.RebuildUI();
      }
      finally
      {
        EditorUtility.ClearProgressBar();
      }
    }

    private void CreateGUI() => RebuildUI();

    private void RebuildUI()
    {
      rootVisualElement.Clear();
      rootVisualElement.style.paddingLeft = 8;
      rootVisualElement.style.paddingRight = 8;
      rootVisualElement.style.paddingTop = 8;

      if(_results == null)
        return;

      var header = new Label($"Usages of \"{_tagName}\" — {_results.Count} found");
      header.style.fontSize = 13;
      header.style.unityFontStyleAndWeight = FontStyle.Bold;
      header.style.marginBottom = 8;
      rootVisualElement.Add(header);

      if(_results.Count == 0)
      {
        var empty = new Label("No usages found.");
        empty.style.color = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
        rootVisualElement.Add(empty);
        return;
      }

      var scroll = new ScrollView();
      scroll.style.flexGrow = 1;
      rootVisualElement.Add(scroll);

      foreach(var result in _results)
        scroll.Add(MakeResultRow(result));
    }

    private VisualElement MakeResultRow(TagUsageFinder.UsageResult result)
    {
      var row = new Button(() =>
      {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(result.AssetPath);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
      });
      row.style.flexDirection = FlexDirection.Row;
      row.style.alignItems = Align.Center;
      row.style.unityTextAlign = TextAnchor.MiddleLeft;
      row.style.paddingLeft = 4;
      row.style.paddingRight = 4;
      row.style.marginBottom = 1;

      var asset = AssetDatabase.LoadAssetAtPath<Object>(result.AssetPath);
      var icon = EditorGUIUtility.GetIconForObject(asset);
      var img = new Image { image = icon };
      img.style.width = 16;
      img.style.height = 16;
      img.style.marginRight = 4;
      img.style.flexShrink = 0;
      row.Add(img);

      var label = new Label(result.AssetPath);
      label.style.flexGrow = 1;
      label.style.unityTextAlign = TextAnchor.MiddleLeft;
      label.style.overflow = Overflow.Hidden;
      row.Add(label);

      return row;
    }
  }
}
