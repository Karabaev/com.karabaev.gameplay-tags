using System;

namespace com.karabaev.gameplayTags
{
  public unsafe struct Tag : IEquatable<Tag>
  {
    public const char Separator = '.';
    public const int MaxAncestors = 6;
    public static readonly Tag None = default;

    internal readonly long Value;
    internal fixed long Ancestors[MaxAncestors];
    internal readonly int Depth;

    private Tag(long value, long* ancestors, int depth)
    {
      Value = value;
      Depth = depth < MaxAncestors ? depth : MaxAncestors;
      for (var i = 0; i < Depth; i++)
      {
        Ancestors[i] = ancestors[i];
      }
    }

    /// <summary>
    /// Creates a Tag from a dot-separated hierarchical path (e.g. "Damage.Fire.Burning").
    /// The full ancestor chain is embedded in the struct — no registry needed for hierarchy queries.
    /// </summary>
    public static Tag From(string path)
    {
      if(string.IsNullOrEmpty(path))
        return None;

      var value = ComputeHash(path.AsSpan());

      var depth = 0;
      var ancestorHashes = stackalloc long[MaxAncestors];

      var remaining = path.AsSpan();
      var dotIndex = remaining.LastIndexOf(Separator);
      while(dotIndex >= 0 && depth < MaxAncestors)
      {
        remaining = remaining.Slice(0, dotIndex);
        ancestorHashes[depth++] = ComputeHash(remaining);
        dotIndex = remaining.LastIndexOf(Separator);
      }

      return new Tag(value, ancestorHashes, depth);
    }

    public bool IsNone => Value == 0;

    /// <summary>
    /// Returns true if this tag is a direct or indirect child of <paramref name="ancestor"/>.
    /// Returns false for equal tags — use <see cref="IsOrIsChildOf"/> for that.
    /// </summary>
    public readonly bool IsChildOf(in Tag ancestor)
    {
      if(ancestor.IsNone)
        return false;

      fixed(Tag* self = &this)
      {
        for(var i = 0; i < self->Depth; i++)
          if(self->Ancestors[i] == ancestor.Value)
            return true;
      }
      return false;
    }

    /// <summary>Returns true if this tag equals <paramref name="other"/> or is a child of it.</summary>
    public readonly bool IsOrIsChildOf(in Tag other) => this == other || IsChildOf(in other);

    internal readonly void CopyAncestorsTo(long* dest, int maxCount)
    {
      fixed(Tag* self = &this)
      {
        var count = self->Depth < maxCount ? self->Depth : maxCount;
        for(var i = 0; i < count; i++)
          dest[i] = self->Ancestors[i];
      }
    }

    public bool Equals(Tag other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is Tag t && Equals(t);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value == 0 ? "None" : $"Tag(0x{Value:X16})";

    public static bool operator ==(in Tag a, in Tag b) => a.Value == b.Value;
    public static bool operator !=(in Tag a, in Tag b) => a.Value != b.Value;

    // FNV-1a 64-bit — deterministic, no external dependencies.
    // 0 is reserved for Tag.None; any hash collision with 0 is remapped to 1.
    private static long ComputeHash(ReadOnlySpan<char> path)
    {
      unchecked
      {
        const long fnvPrime = 0x100000001B3L;
        var hash = unchecked((long)0xCBF29CE484222325UL);
        foreach(var c in path)
        {
          hash ^= c;
          hash *= fnvPrime;
        }
        return hash == 0 ? 1 : hash;
      }
    }
  }
}
