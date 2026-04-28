namespace com.karabaev.gameplayTags
{
  /// <summary>
  /// A fixed-capacity, unmanaged collection of Tags. Safe to use in Burst-compiled code.
  /// Stores ancestor data inline so hierarchy queries (Has) require no external registry.
  /// </summary>
  public unsafe struct TagContainer
  {
    public const int MaxTags = 8;

    // Parallel arrays — slot i holds _hashes[i], _depths[i], and
    // ancestors at _ancestors[i * Tag.MaxAncestors .. + _depths[i]].
    private fixed long _hashes[MaxTags];
    private fixed long _ancestors[MaxTags * Tag.MaxAncestors];
    private fixed int _depths[MaxTags];
    private int _count;

    public int Count => _count;
    public bool IsFull => _count >= MaxTags;

    /// <summary>
    /// Adds the tag. Duplicate and full-container additions are silently ignored.
    /// </summary>
    public void Add(in Tag tag)
    {
      if(tag.IsNone || HasExact(in tag) || IsFull)
        return;

      var slot = _count;
      _hashes[slot] = tag.Value;
      _depths[slot] = tag.Depth;

      fixed(long* ancestorsDest = _ancestors)
        tag.CopyAncestorsTo(ancestorsDest + slot * Tag.MaxAncestors, Tag.MaxAncestors);

      _count++;
    }

    /// <summary>Removes the first tag that exactly matches. No-op if not present.</summary>
    public void Remove(in Tag tag)
    {
      for(var i = 0; i < _count; i++)
      {
        if(_hashes[i] != tag.Value)
          continue;

        // Shift subsequent slots down.
        for(var j = i; j < _count - 1; j++)
        {
          _hashes[j] = _hashes[j + 1];
          _depths[j] = _depths[j + 1];

          var src = j + 1;
          var dst = j;
          for(var k = 0; k < Tag.MaxAncestors; k++)
            _ancestors[dst * Tag.MaxAncestors + k] = _ancestors[src * Tag.MaxAncestors + k];
        }

        _count--;
        return;
      }
    }

    /// <summary>Returns true only if the exact tag hash is present.</summary>
    public bool HasExact(in Tag tag)
    {
      for(var i = 0; i < _count; i++)
        if(_hashes[i] == tag.Value)
          return true;
      return false;
    }

    /// <summary>
    /// Returns true if this container holds <paramref name="parentTag"/> itself
    /// OR any tag that is a child (direct or indirect) of <paramref name="parentTag"/>.
    /// </summary>
    public bool Has(in Tag parentTag)
    {
      if(parentTag.IsNone)
        return false;
      return HasByHash(parentTag.Value);
    }

    /// <summary>
    /// Returns true if, for every tag in <paramref name="other"/>, this container
    /// satisfies <see cref="Has"/> for that tag.
    /// </summary>
    public bool HasAll(in TagContainer other)
    {
      for(var i = 0; i < other._count; i++)
        if(!HasByHash(other._hashes[i]))
          return false;
      return true;
    }

    // Checks whether any stored tag IS or IS A CHILD OF the tag identified by parentHash.
    // A stored tag T is a child of parentHash if parentHash appears in T's ancestor list.
    private bool HasByHash(long parentHash)
    {
      for(var i = 0; i < _count; i++)
      {
        if(_hashes[i] == parentHash)
          return true;

        var ancestorBase = i * Tag.MaxAncestors;
        for(var k = 0; k < _depths[i]; k++)
          if(_ancestors[ancestorBase + k] == parentHash)
            return true;
      }
      return false;
    }
  }
}
