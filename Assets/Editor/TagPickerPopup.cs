using System;
using System.Collections.Generic;
using com.karabaev.gameplayTags;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace com.karabaev.gameplayTags.editor
{
  public sealed class TagPickerPopup : PopupWindowContent
  {
    private const float DefaultWidth = 300f;
    private const float PopupHeight = 360f;
    private const int IndentPerDepth = 16;

    private readonly float _windowWidth;
    private readonly bool _multiSelect;
    private readonly HashSet<string> _selected;
    private readonly Action<List<string>> _changed;
    private readonly TagDatabase _db;

    private readonly Dictionary<string, bool> _expanded = new();

    private string _searchText = string.Empty;
    private VisualElement _treeContainer = null!;

    /// <param name="currentPaths">Paths that are currently selected.</param>
    /// <param name="multiSelect">
    /// true  — checkboxes, changes flushed on popup close.<br/>
    /// false — radio, popup closes immediately on selection.
    /// </param>
    /// <param name="changed">Called with the new selection list.</param>
    /// <param name="db">Tag database to display.</param>
    /// <param name="windowWidth">
    /// Width of the popup window. Pass the trigger button's width to match it.
    /// Defaults to <see cref="DefaultWidth"/> when zero or negative.
    /// </param>
    public TagPickerPopup(
      IReadOnlyList<string> currentPaths,
      bool multiSelect,
      Action<List<string>> changed,
      TagDatabase db,
      float windowWidth = 0f)
    {
      _windowWidth = windowWidth > 0f ? windowWidth : DefaultWidth;
      _multiSelect = multiSelect;
      _changed = changed;
      _db = db;
      _selected = new HashSet<string>();
      
      foreach (var p in currentPaths)
      {
        if (!string.IsNullOrEmpty(p))
          _selected.Add(p);
      }
    }

    public override Vector2 GetWindowSize() => new(_windowWidth, PopupHeight);

    public override void OnOpen()
    {
      // GetWindowSize() is only an initial hint when CreateGUI() is used —
      // UI Toolkit can auto-resize the window after layout. Lock min/max to
      // prevent that and enforce the desired dimensions.
      if(editorWindow == null) return;
      editorWindow.minSize = new Vector2(_windowWidth, PopupHeight);
      editorWindow.maxSize = new Vector2(_windowWidth, PopupHeight);
    }

    public override VisualElement CreateGUI()
    {
      var root = new VisualElement();
      root.style.flexDirection = FlexDirection.Column;
      root.style.flexGrow = 1;
      root.style.width = _windowWidth;
      // Search field
      var searchField = new TextField
      {
        textEdition = { placeholder = "Search..." },
        style =
        {
          marginLeft = 4,
          marginRight = 4,
          marginTop = 4,
          marginBottom = 4
        }
      };
      searchField.RegisterValueChangedCallback(evt =>
      {
        _searchText = evt.newValue ?? string.Empty;
        RebuildTree();
      });
      root.Add(searchField);

      // Separator
      var sep = new VisualElement();
      sep.style.height = 1;
      sep.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
      root.Add(sep);

      // Scrollable tree
      var scroll = new ScrollView();
      scroll.style.flexGrow = 1;
      _treeContainer = new VisualElement();
      scroll.Add(_treeContainer);
      root.Add(scroll);

      RebuildTree();
      return root;
    }

    public override void OnClose()
    {
      if(_multiSelect)
        _changed(new List<string>(_selected));
    }

    private void RebuildTree()
    {
      _treeContainer.Clear();

      // Orphaned entries: selected paths that no longer exist in the database.
      BuildOrphanedSection();

      var roots = TagTreeBuilder.Build(_db);
      var searching = !string.IsNullOrEmpty(_searchText);

      if(searching)
        BuildSearchResults(roots);
      else
        foreach(var node in roots)
          BuildNodeElement(_treeContainer, node, depth: 0);
    }

    // Tags that are selected but no longer exist in the database.
    private void BuildOrphanedSection()
    {
      var dbPaths = new HashSet<string>();
      foreach(var tag in _db.Tags)
        if(!string.IsNullOrEmpty(tag.Name))
          dbPaths.Add(tag.Name);

      var orphans = new List<string>();
      foreach(var path in _selected)
        if(!dbPaths.Contains(path))
          orphans.Add(path);

      if(orphans.Count == 0) return;

      // Section header
      var header = new Label("Not in database:");
      header.style.color = new StyleColor(new Color(0.9f, 0.6f, 0.2f));
      header.style.fontSize = 10;
      header.style.paddingLeft = 4;
      header.style.paddingTop = 4;
      header.style.paddingBottom = 2;
      _treeContainer.Add(header);

      foreach(var path in orphans)
      {
        var capturedPath = path;
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.paddingLeft = 4;
        row.style.minHeight = 22;

        // Spacer matching expand-button column
        var spacer = new VisualElement();
        spacer.style.width = 20;
        row.Add(spacer);

        var toggle = new Toggle { value = true };
        toggle.style.marginRight = 4;
        toggle.RegisterValueChangedCallback(evt =>
        {
          if(!evt.newValue)
            _selected.Remove(capturedPath);
          else
            _selected.Add(capturedPath);
          RebuildTree();
        });
        row.Add(toggle);

        var nameLabel = new Label(capturedPath);
        nameLabel.style.flexGrow = 1;
        nameLabel.style.color = new StyleColor(new Color(0.9f, 0.6f, 0.2f));
        nameLabel.RegisterCallback<ClickEvent>(_ =>
        {
          var next = !_selected.Contains(capturedPath);
          if(next) _selected.Add(capturedPath); else _selected.Remove(capturedPath);
          RebuildTree();
        });
        row.Add(nameLabel);

        AddHover(row);
        _treeContainer.Add(row);
      }

      // Separator after orphaned section
      var sep = new VisualElement();
      sep.style.height = 1;
      sep.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
      sep.style.marginTop = 4;
      sep.style.marginBottom = 2;
      _treeContainer.Add(sep);
    }

    // Flat list of defined nodes whose full path contains the search string.
    private void BuildSearchResults(List<TagTreeBuilder.TagNode> roots)
    {
      var query = _searchText.ToLowerInvariant();
      var matches = new List<TagTreeBuilder.TagNode>();
      CollectMatches(roots, query, matches);

      if(matches.Count == 0)
      {
        var empty = new Label("No tags match.");
        empty.style.color = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
        empty.style.paddingLeft = 8;
        empty.style.paddingTop = 6;
        _treeContainer.Add(empty);
        return;
      }

      foreach(var node in matches)
        _treeContainer.Add(MakeRow(node, depth: 0, showExpand: false));
    }

    private static void CollectMatches(
      List<TagTreeBuilder.TagNode> nodes,
      string query,
      List<TagTreeBuilder.TagNode> result)
    {
      foreach(var node in nodes)
      {
        if(node.IsDefined && node.FullPath.ToLowerInvariant().Contains(query))
          result.Add(node);
        CollectMatches(node.Children, query, result);
      }
    }

    private void BuildNodeElement(VisualElement container, TagTreeBuilder.TagNode node, int depth)
    {
      container.Add(MakeRow(node, depth, showExpand: node.Children.Count > 0));
      if(node.Children.Count == 0) return;

      var expanded = _expanded.GetValueOrDefault(node.FullPath, true);
      if(!expanded) return;

      foreach(var child in node.Children)
        BuildNodeElement(container, child, depth + 1);
    }

    private VisualElement MakeRow(TagTreeBuilder.TagNode node, int depth, bool showExpand)
    {
      var row = new VisualElement();
      row.style.flexDirection = FlexDirection.Row;
      row.style.alignItems = Align.Center;
      row.style.paddingLeft = depth * IndentPerDepth + 4;
      row.style.minHeight = 22;

      // Expand / collapse button or spacer
      if(showExpand)
      {
        var isExpanded = _expanded.GetValueOrDefault(node.FullPath, true);
        var expandBtn = new Button { text = isExpanded ? "▼" : "▶" };
        StyleExpandButton(expandBtn);
        expandBtn.clicked += () =>
        {
          var cur = _expanded.GetValueOrDefault(node.FullPath, true);
          _expanded[node.FullPath] = !cur;
          RebuildTree();
        };
        row.Add(expandBtn);
      }
      else
      {
        var spacer = new VisualElement();
        spacer.style.width = 20;
        row.Add(spacer);
      }

      // Toggle (defined nodes only) or spacer
      if(node.IsDefined)
      {
        var isChecked = _selected.Contains(node.FullPath);
        var toggle = new Toggle { value = isChecked };
        toggle.style.marginRight = 4;
        toggle.RegisterValueChangedCallback(evt => OnToggleChanged(node.FullPath, evt.newValue));
        row.Add(toggle);
      }
      else
      {
        var spacer = new VisualElement();
        spacer.style.width = 18;
        row.Add(spacer);
      }

      var nameLabel = new Label(node.Segment);
      nameLabel.style.flexGrow = 1;
      if(!node.IsDefined)
        nameLabel.style.color = new StyleColor(new Color(0.55f, 0.55f, 0.55f));

      if(node.IsDefined)
      {
        nameLabel.style.cursor = new StyleCursor(new Cursor());
        nameLabel.RegisterCallback<ClickEvent>(_ =>
          OnToggleChanged(node.FullPath, !_selected.Contains(node.FullPath)));
      }

      row.Add(nameLabel);
      AddHover(row);
      return row;
    }

    private void OnToggleChanged(string path, bool nowChecked)
    {
      if(_multiSelect)
      {
        if(nowChecked)
          _selected.Add(path);
        else
          _selected.Remove(path);

        RebuildTree();
      }
      else
      {
        _selected.Clear();
        if(nowChecked)
          _selected.Add(path);

        _changed(new List<string>(_selected));
        editorWindow.Close();
      }
    }

    private static void AddHover(VisualElement element)
    {
      element.RegisterCallback<MouseEnterEvent>(_ =>
        element.style.backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f)));
      element.RegisterCallback<MouseLeaveEvent>(_ =>
        element.style.backgroundColor = StyleKeyword.None);
    }

    private static void StyleExpandButton(Button button)
    {
      button.style.width = 20;
      button.style.height = 18;
      button.style.paddingLeft = 0;
      button.style.paddingRight = 0;
      button.style.marginLeft = 0;
      button.style.marginRight = 2;
    }
  }
}
