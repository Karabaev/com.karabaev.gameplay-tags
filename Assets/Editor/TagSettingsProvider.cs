using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.karabaev.gameplayTags.editor
{
  public static class TagSettingsProvider
  {
    [SettingsProvider]
    public static SettingsProvider CreateProvider() =>
      new("Project/Gameplay/Tags", SettingsScope.Project) { activateHandler = OnActivated };

    private static void OnActivated(string _, VisualElement root)
    {
      BuildPanel(root, TagDatabase.instance);
    }
    
    private static void BuildPanel(VisualElement root, TagDatabase db)
    {
      root.Clear();
      root.style.paddingLeft = 10;
      root.style.paddingRight = 10;
      root.style.paddingTop = 10;

      var title = new Label("Gameplay Tags");
      title.style.fontSize = 16;
      title.style.unityFontStyleAndWeight = FontStyle.Bold;
      title.style.marginBottom = 8;
      root.Add(title);

      var addRootBtn = new Button { text = "+  Add Root Tag" };
      addRootBtn.style.alignSelf = Align.FlexStart;
      addRootBtn.style.marginBottom = 6;
      root.Add(addRootBtn);

      var scrollView = new ScrollView();
      scrollView.style.flexGrow = 1;
      root.Add(scrollView);

      var treeContainer = new VisualElement();
      scrollView.Add(treeContainer);

      // Root-level inline form sits below the tree inside the scroll view.
      var rootFormContainer = new VisualElement();
      scrollView.Add(rootFormContainer);

      void Rebuild()
      {
        rootFormContainer.Clear();
        treeContainer.Clear();
        foreach(var node in TagTreeBuilder.Build(db))
          treeContainer.Add(BuildNodeElement(node, db, Rebuild));
      }

      addRootBtn.clicked += () => ShowAddForm("", rootFormContainer, db, Rebuild);
      Rebuild();
    }

    private static VisualElement BuildNodeElement(TagTreeBuilder.TagNode node, TagDatabase db, Action rebuild)
    {
      var depth = CountDepth(node.FullPath);

      var nodeContainer = new VisualElement();

      // --- Header row ---
      var header = new VisualElement();
      header.style.flexDirection = FlexDirection.Row;
      header.style.alignItems = Align.Center;
      header.style.paddingLeft = depth * 16;
      header.style.minHeight = 22;
      header.style.marginBottom = 1;

      var childrenContainer = new VisualElement();
      if(node.Children.Count > 0)
      {
        childrenContainer.style.display = DisplayStyle.Flex;

        var expandBtn = new Button { text = "▼" };
        StyleIconButton(expandBtn);
        expandBtn.clicked += () =>
        {
          var isVisible = childrenContainer.style.display == DisplayStyle.Flex;
          childrenContainer.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
          expandBtn.text = isVisible ? "▶" : "▼";
        };
        header.Add(expandBtn);
      }
      else
      {
        var spacer = new VisualElement { style = { width = 22 } };
        header.Add(spacer);
      }

      var nameLabel = new Label(node.Segment);
      nameLabel.style.flexGrow = 1;
      if(!node.IsDefined)
      {
        nameLabel.style.color = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
        nameLabel.tooltip = "Implicit parent (not a defined tag)";
      }
      header.Add(nameLabel);

      var formContainer = new VisualElement();

      var addBtn = new Button { text = "+", tooltip = "Add subtag" };
      StyleIconButton(addBtn);
      addBtn.clicked += () => ShowAddForm(node.FullPath, formContainer, db, rebuild);
      header.Add(addBtn);

      if(node.IsDefined)
      {
        var renameBtn = new Button { text = "✎", tooltip = "Rename" };
        StyleIconButton(renameBtn);
        renameBtn.clicked += () => ShowRenameForm(node, formContainer, db, rebuild);
        header.Add(renameBtn);

        var removeBtn = new Button { text = "✕", tooltip = "Remove tag and all subtags" };
        StyleIconButton(removeBtn);
        removeBtn.style.color = new StyleColor(new Color(0.9f, 0.4f, 0.4f));
        removeBtn.clicked += () =>
        {
          var hasChildren = node.Children.Count > 0;
          var message = hasChildren
            ? $"Remove '{node.FullPath}' and all its subtags?"
            : $"Remove '{node.FullPath}'?";

          if(EditorUtility.DisplayDialog("Remove Tag", message, "Remove", "Cancel"))
          {
            Undo.RecordObject(db, "Remove Gameplay Tag");
            db.RemoveTag(node.FullPath);
            AssetDatabase.SaveAssetIfDirty(db);
            rebuild();
          }
        };
        header.Add(removeBtn);
      }

      nodeContainer.Add(header);
      nodeContainer.Add(formContainer);

      if(node.Children.Count > 0)
      {
        foreach(var child in node.Children)
          childrenContainer.Add(BuildNodeElement(child, db, rebuild));
        nodeContainer.Add(childrenContainer);
      }

      return nodeContainer;
    }

    private static void ShowAddForm(
      string parentPath,
      VisualElement formContainer,
      TagDatabase db,
      Action rebuild)
    {
      formContainer.Clear();

      var depth = string.IsNullOrEmpty(parentPath) ? 0 : CountDepth(parentPath) + 1;
      var form = CreateFormBox(depth);

      var hint = new Label(string.IsNullOrEmpty(parentPath)
        ? "New root tag"
        : $"Under: {parentPath}");
      hint.style.color = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
      hint.style.fontSize = 10;
      hint.style.marginBottom = 4;
      form.Add(hint);

      var nameField = AddFormField(form, "Name");
      var commentField = AddFormField(form, "Comment");

      var errorLabel = CreateErrorLabel();
      form.Add(errorLabel);

      var btnRow = new VisualElement();
      btnRow.style.flexDirection = FlexDirection.Row;
      btnRow.style.marginTop = 4;

      var applyBtn = new Button(() =>
      {
        var segment = nameField.value.Trim();
        if(string.IsNullOrEmpty(segment))
        {
          SetError(errorLabel, "Name must not be empty.");
          return;
        }

        var fullPath = string.IsNullOrEmpty(parentPath)
          ? segment
          : parentPath + Tag.Separator + segment;

        var err = TagUtils.ValidatePath(fullPath);
        if(err != null) { SetError(errorLabel, err); return; }

        if(db.ContainsPath(fullPath))
        {
          SetError(errorLabel, $"Tag '{fullPath}' already exists.");
          return;
        }

        Undo.RecordObject(db, "Add Gameplay Tag");
        db.AddTag(fullPath, commentField.value.Trim());
        AssetDatabase.SaveAssetIfDirty(db);
        rebuild();
      }) { text = "Apply" };

      var cancelBtn = new Button(() => formContainer.Clear()) { text = "Cancel" };

      btnRow.Add(applyBtn);
      btnRow.Add(cancelBtn);
      form.Add(btnRow);
      formContainer.Add(form);
      nameField.Focus();
    }

    private static void ShowRenameForm(
      TagTreeBuilder.TagNode node,
      VisualElement formContainer,
      TagDatabase db,
      Action rebuild)
    {
      formContainer.Clear();

      var form = CreateFormBox(CountDepth(node.FullPath));

      var nameField = AddFormField(form, "New name");
      nameField.value = node.Segment;

      var errorLabel = CreateErrorLabel();
      form.Add(errorLabel);

      var btnRow = new VisualElement();
      btnRow.style.flexDirection = FlexDirection.Row;
      btnRow.style.marginTop = 4;

      var applyBtn = new Button(() =>
      {
        var newSegment = nameField.value.Trim();
        if(string.IsNullOrEmpty(newSegment))
        {
          SetError(errorLabel, "Name must not be empty.");
          return;
        }

        var err = TagUtils.ValidatePath(newSegment);
        if(err != null) { SetError(errorLabel, $"Invalid name: {err}"); return; }

        var dotIndex = node.FullPath.LastIndexOf(Tag.Separator);
        var newFullPath = dotIndex >= 0
          ? node.FullPath.Substring(0, dotIndex + 1) + newSegment
          : newSegment;

        if(newFullPath != node.FullPath && db.ContainsPath(newFullPath))
        {
          SetError(errorLabel, $"Tag '{newFullPath}' already exists.");
          return;
        }

        Undo.RecordObject(db, "Rename Gameplay Tag");
        db.RenameTag(node.FullPath, newSegment);
        AssetDatabase.SaveAssetIfDirty(db);
        rebuild();
      }) { text = "Apply" };

      var cancelBtn = new Button(() => formContainer.Clear()) { text = "Cancel" };

      btnRow.Add(applyBtn);
      btnRow.Add(cancelBtn);
      form.Add(btnRow);
      formContainer.Add(form);
      nameField.SelectAll();
      nameField.Focus();
    }

    // ------------------------------------------------------------------ //
    // Form helpers
    // ------------------------------------------------------------------ //

    private static VisualElement CreateFormBox(int depth)
    {
      var form = new VisualElement();
      form.style.paddingLeft = depth * 16 + 22;
      form.style.paddingTop = 6;
      form.style.paddingBottom = 6;
      form.style.paddingRight = 6;
      form.style.marginTop = 2;
      form.style.marginBottom = 2;
      form.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));
      form.style.borderTopLeftRadius = 3;
      form.style.borderTopRightRadius = 3;
      form.style.borderBottomLeftRadius = 3;
      form.style.borderBottomRightRadius = 3;
      return form;
    }

    private static TextField AddFormField(VisualElement parent, string labelText)
    {
      var row = new VisualElement();
      row.style.flexDirection = FlexDirection.Row;
      row.style.alignItems = Align.Center;
      row.style.marginBottom = 3;

      var label = new Label(labelText) { style = { width = 72 } };
      row.Add(label);

      var field = new TextField();
      field.style.flexGrow = 1;
      row.Add(field);

      parent.Add(row);
      return field;
    }

    private static Label CreateErrorLabel()
    {
      var lbl = new Label();
      lbl.style.color = new StyleColor(new Color(1f, 0.4f, 0.4f));
      lbl.style.display = DisplayStyle.None;
      lbl.style.marginTop = 2;
      return lbl;
    }

    private static void SetError(Label errorLabel, string message)
    {
      errorLabel.text = message;
      errorLabel.style.display = DisplayStyle.Flex;
    }

    private static void StyleIconButton(Button btn)
    {
      btn.style.width = 22;
      btn.style.height = 18;
      btn.style.paddingLeft = 0;
      btn.style.paddingRight = 0;
      btn.style.marginLeft = 2;
    }
    
    private static int CountDepth(string path)
    {
      var count = 0;
      foreach(var c in path)
        if(c == Tag.Separator) count++;
      return count;
    }
  }
}
