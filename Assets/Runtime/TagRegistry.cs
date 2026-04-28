using System;
using System.Collections.Generic;

namespace com.karabaev.gameplayTags
{
  /// <summary>
  /// Managed name-lookup table built from tag path strings.
  /// Provides human-readable names for Tags at edit/debug time.
  /// Not required for runtime hierarchy queries — those are self-contained in Tag.
  /// </summary>
  public class TagRegistry
  {
    private readonly Dictionary<long, string> _namesByHash = new();

    /// <summary>
    /// Registers a full dot-separated path and all its implicit ancestor paths.
    /// Returns the Tag for the given path.
    /// Duplicate registrations are silently ignored.
    /// </summary>
    public Tag Register(string path)
    {
      if(string.IsNullOrEmpty(path))
        return Tag.None;

      var tag = Tag.From(path);
      _namesByHash.TryAdd(tag.Value, path);

      // Walk up the path and register each ancestor so TryGetName works for them too.
      var remaining = path.AsSpan();
      var dotIndex = remaining.LastIndexOf('.');
      while(dotIndex > 0)
      {
        remaining = remaining.Slice(0, dotIndex);
        var ancestorPath = remaining.ToString();
        var ancestorTag = Tag.From(ancestorPath);
        _namesByHash.TryAdd(ancestorTag.Value, ancestorPath);
        dotIndex = remaining.LastIndexOf('.');
      }

      return tag;
    }

    public bool TryGetName(in Tag tag, out string name) =>
      _namesByHash.TryGetValue(tag.Value, out name!);

    public bool IsKnown(in Tag tag) => _namesByHash.ContainsKey(tag.Value);
  }
}
