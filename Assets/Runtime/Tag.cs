using System;

namespace com.karabaev.gameplayTags
{
  public readonly unsafe struct Tag : IEquatable<Tag>
  {
    public const char Separator = '.';
    public static readonly Tag None = default;

    internal readonly long Value;
    private readonly long* _ancestors;
    internal readonly int Depth;

    private Tag(long value, long* ancestors, int depth)
    {
      Value = value;
      _ancestors = ancestors;
      Depth = depth;
    }

    /// <summary>
    /// Creates a Tag from a dot-separated hierarchical path (e.g. "Damage.Fire.Burning").
    /// Ancestor hashes are written into <paramref name="ancestors"/> (caller-owned buffer,
    /// must have at least as many elements as there are separators in <paramref name="name"/>).
    /// Pass null for root-level tags (no separator). The buffer must outlive all uses of the Tag.
    /// </summary>
    public static Tag From(string name, long* ancestors)
    {
      if(string.IsNullOrEmpty(name)) throw new InvalidOperationException("Tag name cannot be null or empty");

      var value = ComputeHash(name.AsSpan());
      var depth = 0;

      var remaining = name.AsSpan();
      var dotIndex = remaining.LastIndexOf(Separator);
      while(dotIndex >= 0)
      {
        remaining = remaining[..dotIndex];
        ancestors[depth++] = ComputeHash(remaining);
        dotIndex = remaining.LastIndexOf(Separator);
      }

      return new Tag(value, ancestors, depth);
    }

    public bool IsNone => Value == 0;

    /// <summary>
    /// Returns true if this tag is a direct or indirect child of <paramref name="ancestor"/>.
    /// Returns false for equal tags — use <see cref="IsOrIsChildOf"/> for that.
    /// </summary>
    public bool IsChildOf(in Tag ancestor)
    {
      if(ancestor.IsNone) return false;

      for(var i = 0; i < Depth; i++)
      {
        if(_ancestors[i] == ancestor.Value) return true;
      }
      return false;
    }

    /// <summary>Returns true if this tag equals <paramref name="other"/> or is a child of it.</summary>
    public bool IsOrIsChildOf(in Tag other) => this == other || IsChildOf(in other);

    internal void CopyAncestorsTo(long* dest, int maxCount)
    {
      var count = Depth < maxCount ? Depth : maxCount;
      for(var i = 0; i < count; i++)
        dest[i] = _ancestors[i];
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
