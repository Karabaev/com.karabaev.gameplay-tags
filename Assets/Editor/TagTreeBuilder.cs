using System;
using System.Collections.Generic;

namespace com.karabaev.gameplayTags.editor
{
  /// <summary>
  /// Builds a hierarchical tree model from a <see cref="TagDatabase"/>.
  /// Shared between <see cref="TagSettingsProvider"/> and <see cref="TagPickerPopup"/>.
  /// </summary>
  public static class TagTreeBuilder
  {
    public static List<TagNode> Build(TagDatabase db)
    {
      var nodesByPath = new Dictionary<string, TagNode>();
      var roots = new List<TagNode>();

      // Collect all defined paths plus their implicit structural ancestors.
      var allPaths = new SortedSet<string>(StringComparer.Ordinal);
      foreach (var tag in db.Tags)
      {
        if (string.IsNullOrEmpty(tag.Name)) continue;
        allPaths.Add(tag.Name);

        var span = tag.Name.AsSpan();
        var dot = span.LastIndexOf(Tag.Separator);
        while (dot > 0)
        {
          span = span[..dot];
          allPaths.Add(span.ToString());
          dot = span.LastIndexOf(Tag.Separator);
        }
      }

      foreach(var path in allPaths)
      {
        var lastDot = path.LastIndexOf(Tag.Separator);
        var segment = lastDot >= 0 ? path.Substring(lastDot + 1) : path;
        var comment = db.FindTag(path)?.Comment ?? string.Empty;
        var node = new TagNode(path, segment, db.ContainsPath(path), comment);
        nodesByPath[path] = node;

        if (lastDot < 0)
          roots.Add(node);
        else if (nodesByPath.TryGetValue(path.Substring(0, lastDot), out var parent))
          parent.Children.Add(node);
      }

      return roots;
    }

    public sealed class TagNode
    {
      public readonly string FullPath;
      public readonly string Segment;
      public readonly bool IsDefined;
      public readonly string Comment;
      public readonly List<TagNode> Children = new();

      public TagNode(string fullPath, string segment, bool isDefined, string comment)
      {
        FullPath = fullPath;
        Segment = segment;
        IsDefined = isDefined;
        Comment = comment;
      }
    }
  }
}
