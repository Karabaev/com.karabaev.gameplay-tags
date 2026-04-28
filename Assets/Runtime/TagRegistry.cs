using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace com.karabaev.gameplayTags
{
  /// <summary>
  /// Manages Tag and TagContainer creation, owns all backing buffers, and provides
  /// human-readable name lookup at edit/debug time.
  ///
  /// All Tags and TagContainers produced by this registry remain valid until
  /// <see cref="Dispose"/> is called. Hierarchy queries on those values are safe
  /// within that lifetime.
  /// </summary>
  public class TagRegistry : IDisposable
  {
    private readonly Dictionary<long, string> _namesByHash = new();
    private readonly List<GCHandle> _handles = new();
    private int _maxDepth;

    /// <summary>
    /// Registers a full dot-separated name and all its implicit ancestor names.
    /// Returns the Tag for the given name. Duplicate registrations are silently ignored.
    /// </summary>
    public Tag Register(string tagName)
    {
      if(string.IsNullOrEmpty(tagName)) throw new InvalidOperationException("Tag name cannot be null or empty");

      var tag = CreateTag(tagName);
      _namesByHash.TryAdd(tag.Value, tagName);

      var remaining = tagName.AsSpan();
      var dotIndex = remaining.LastIndexOf(Tag.Separator);
      while(dotIndex > 0)
      {
        remaining = remaining[..dotIndex];
        var ancestorPath = remaining.ToString();
        _namesByHash.TryAdd(CreateTag(ancestorPath).Value, ancestorPath);
        dotIndex = remaining.LastIndexOf(Tag.Separator);
      }

      return tag;
    }

    /// <summary>
    /// Allocates backing buffers for a TagContainer of the given capacity and returns it.
    /// The ancestor stride is sized to the deepest tag registered so far.
    /// Register all tags before creating containers that will hold them.
    /// The buffers are owned by this registry and freed on <see cref="Dispose"/>.
    /// </summary>
    public unsafe TagContainer CreateContainer(int capacity)
    {
      var stride = _maxDepth;
      var hashes = Pin(new long[capacity]);
      var depths = Pin(new int[capacity]);
      long* ancestors = null;
      if (stride > 0)
      {
        ancestors = (long*)Pin(new long[capacity * stride]).AddrOfPinnedObject().ToPointer();
      }
      return new TagContainer(
        (long*)hashes.AddrOfPinnedObject().ToPointer(),
        ancestors,
        (int*)depths.AddrOfPinnedObject().ToPointer(),
        capacity, stride);
    }

    public bool TryGetName(in Tag tag, out string name) =>
      _namesByHash.TryGetValue(tag.Value, out name!);

    public bool IsKnown(in Tag tag) => _namesByHash.ContainsKey(tag.Value);

    public void Dispose()
    {
      foreach (var h in _handles)
      {
        h.Free();
      }
      
      _handles.Clear();
    }

    private unsafe Tag CreateTag(string name)
    {
      var depth = CountSeparators(name);
      if (depth > _maxDepth) _maxDepth = depth;
      
      long* ancestors = null;
      if (depth > 0)
      {
        ancestors = (long*)Pin(new long[depth]).AddrOfPinnedObject().ToPointer();
      }

      return Tag.From(name, ancestors);
    }

    private GCHandle Pin(object array)
    {
      var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
      _handles.Add(handle);
      return handle;
    }

    private static int CountSeparators(string name)
    {
      var count = 0;
      foreach (var c in name)
      {
        if(c == Tag.Separator) count++;
      }
      return count;
    }
  }
}
