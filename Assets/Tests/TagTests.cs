using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace com.karabaev.gameplayTags.tests
{
  public class TagTests
  {
    private readonly List<IntPtr> _allocs = new List<IntPtr>();

    [TearDown]
    public void TearDown()
    {
      foreach(var p in _allocs) Marshal.FreeHGlobal(p);
      _allocs.Clear();
    }

    private unsafe Tag MakeTag(string path)
    {
      var depth = CountSeparators(path);
      long* ancestors = null;
      if(depth > 0)
      {
        var mem = (long*)Marshal.AllocHGlobal(depth * sizeof(long)).ToPointer();
        _allocs.Add(new IntPtr(mem));
        ancestors = mem;
      }
      return Tag.From(path, ancestors);
    }

    private unsafe TagContainer MakeContainer(int capacity = 8, int ancestorStride = 8)
    {
      var hashBytes = capacity * sizeof(long);
      var ancestorBytes = capacity * ancestorStride * sizeof(long);
      var depthBytes = capacity * sizeof(int);
      var mem = (byte*)Marshal.AllocHGlobal(hashBytes + ancestorBytes + depthBytes).ToPointer();
      _allocs.Add(new IntPtr(mem));
      new Span<byte>(mem, hashBytes + ancestorBytes + depthBytes).Clear();
      var h = (long*)mem;
      var a = (long*)(mem + hashBytes);
      var d = (int*)(mem + hashBytes + ancestorBytes);
      return new TagContainer(h, a, d, capacity, ancestorStride);
    }

    private static int CountSeparators(string path)
    {
      var count = 0;
      foreach(var c in path)
        if(c == Tag.Separator) count++;
      return count;
    }
    
    [Test]
    public void From_SamePath_ProducesEqualTags()
    {
      Assert.AreEqual(MakeTag("Damage.Fire"), MakeTag("Damage.Fire"));
    }

    [Test]
    public void From_DifferentPaths_ProducesUnequalTags()
    {
      Assert.AreNotEqual(MakeTag("Damage.Fire"), MakeTag("Damage.Ice"));
    }

    [Test]
    public unsafe void From_EmptyString_ThrowsException()
    {
      long* anc = stackalloc long[1];
      Assert.Throws<InvalidOperationException>(() => Tag.From("", anc));
      Assert.Throws<InvalidOperationException>(() => Tag.From(null!, anc));
    }

    [Test]
    public void None_IsNone_IsTrue()
    {
      Assert.IsTrue(Tag.None.IsNone);
      Assert.IsFalse(MakeTag("Damage").IsNone);
    }

    [Test]
    public void Tags_AreCaseSensitive()
    {
      Assert.AreNotEqual(MakeTag("Damage.Fire"), MakeTag("damage.fire"));
    }
    
    [Test]
    public void IsChildOf_DirectChild_ReturnsTrue()
    {
      Assert.IsTrue(MakeTag("Damage.Fire.Burning").IsChildOf(MakeTag("Damage.Fire")));
    }

    [Test]
    public void IsChildOf_IndirectDescendant_ReturnsTrue()
    {
      Assert.IsTrue(MakeTag("Damage.Fire.Burning").IsChildOf(MakeTag("Damage")));
    }

    [Test]
    public void IsChildOf_EqualTags_ReturnsFalse()
    {
      Assert.IsFalse(MakeTag("Damage.Fire").IsChildOf(MakeTag("Damage.Fire")));
    }

    [Test]
    public void IsChildOf_ParentIsNotChildOfChild_ReturnsFalse()
    {
      Assert.IsFalse(MakeTag("Damage").IsChildOf(MakeTag("Damage.Fire")));
    }

    [Test]
    public void IsChildOf_UnrelatedTags_ReturnsFalse()
    {
      Assert.IsFalse(MakeTag("Ability.Heal").IsChildOf(MakeTag("Damage.Fire")));
    }

    [Test]
    public void IsChildOf_NoneAncestor_ReturnsFalse()
    {
      Assert.IsFalse(MakeTag("Damage.Fire").IsChildOf(Tag.None));
    }

    [Test]
    public void IsChildOf_FiveLevels_ReturnsTrue()
    {
      Assert.IsTrue(MakeTag("A.B.C.D.E").IsChildOf(MakeTag("A")));
      Assert.IsTrue(MakeTag("A.B.C.D.E").IsChildOf(MakeTag("A.B.C")));
    }
    
    [Test]
    public void IsOrIsChildOf_ExactMatch_ReturnsTrue()
    {
      Assert.IsTrue(MakeTag("Damage.Fire").IsOrIsChildOf(MakeTag("Damage.Fire")));
    }

    [Test]
    public void IsOrIsChildOf_Child_ReturnsTrue()
    {
      Assert.IsTrue(MakeTag("Damage.Fire.Burning").IsOrIsChildOf(MakeTag("Damage.Fire")));
    }

    [Test]
    public void IsOrIsChildOf_Unrelated_ReturnsFalse()
    {
      Assert.IsFalse(MakeTag("Ability").IsOrIsChildOf(MakeTag("Damage")));
    }
    
    [Test]
    public void Add_IncreasesCount()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));
      Assert.AreEqual(1, c.Count);
    }

    [Test]
    public void Add_Duplicate_DoesNotIncreaseCount()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));
      c.Add(MakeTag("Damage.Fire"));
      Assert.AreEqual(1, c.Count);
    }

    [Test]
    public void Add_None_IsIgnored()
    {
      var c = MakeContainer();
      c.Add(Tag.None);
      Assert.AreEqual(0, c.Count);
    }

    [Test]
    public void HasExact_PresentTag_ReturnsTrue()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));
      Assert.IsTrue(c.HasExact(MakeTag("Damage.Fire")));
    }

    [Test]
    public void HasExact_AbsentTag_ReturnsFalse()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));
      Assert.IsFalse(c.HasExact(MakeTag("Damage")));
      Assert.IsFalse(c.HasExact(MakeTag("Damage.Fire.Burning")));
    }
    
    [Test]
    public void Remove_PresentTag_RemovesIt()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));
      c.Remove(MakeTag("Damage.Fire"));
      Assert.AreEqual(0, c.Count);
      Assert.IsFalse(c.HasExact(MakeTag("Damage.Fire")));
    }

    [Test]
    public void Remove_AbsentTag_DoesNothing()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));
      c.Remove(MakeTag("Ability"));
      Assert.AreEqual(1, c.Count);
    }

    [Test]
    public void Remove_MiddleElement_ShiftsRemainingCorrectly()
    {
      var c = MakeContainer();
      c.Add(MakeTag("A"));
      c.Add(MakeTag("B"));
      c.Add(MakeTag("C"));
      c.Remove(MakeTag("B"));
      Assert.AreEqual(2, c.Count);
      Assert.IsTrue(c.HasExact(MakeTag("A")));
      Assert.IsTrue(c.HasExact(MakeTag("C")));
      Assert.IsFalse(c.HasExact(MakeTag("B")));
    }

    [Test]
    public void Has_ExactTagPresent_ReturnsTrue()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));
      Assert.IsTrue(c.Has(MakeTag("Damage.Fire")));
    }

    [Test]
    public void Has_ParentOfPresentTag_ReturnsTrue()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire.Burning"));
      Assert.IsTrue(c.Has(MakeTag("Damage.Fire")));
      Assert.IsTrue(c.Has(MakeTag("Damage")));
    }

    [Test]
    public void Has_ChildOfPresentTag_ReturnsFalse()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));
      Assert.IsFalse(c.Has(MakeTag("Damage.Fire.Burning")));
    }

    [Test]
    public void Has_UnrelatedTag_ReturnsFalse()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire.Burning"));
      Assert.IsFalse(c.Has(MakeTag("Ability")));
    }

    [Test]
    public void Has_None_ReturnsFalse()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));
      Assert.IsFalse(c.Has(Tag.None));
    }

    [Test]
    public void HasAll_AllPresent_ReturnsTrue()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire.Burning"));
      c.Add(MakeTag("Ability.Heal"));

      var required = MakeContainer();
      required.Add(MakeTag("Damage.Fire")); // ancestor of stored tag
      required.Add(MakeTag("Ability.Heal")); // exact match

      Assert.IsTrue(c.HasAll(required));
    }

    [Test]
    public void HasAll_OneMissing_ReturnsFalse()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire.Burning"));

      var required = MakeContainer();
      required.Add(MakeTag("Damage.Fire"));
      required.Add(MakeTag("Ability")); // not present

      Assert.IsFalse(c.HasAll(required));
    }

    [Test]
    public void HasAll_EmptyOther_ReturnsTrue()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));
      Assert.IsTrue(c.HasAll(new TagContainer()));
    }

    [Test]
    public void Registry_Register_ReturnsCorrectTag()
    {
      using var registry = new TagRegistry();
      Assert.AreEqual(MakeTag("Damage.Fire.Burning"), registry.Register("Damage.Fire.Burning"));
    }

    [Test]
    public void Registry_TryGetName_ReturnsPath()
    {
      using var registry = new TagRegistry();
      registry.Register("Damage.Fire.Burning");

      Assert.IsTrue(registry.TryGetName(MakeTag("Damage.Fire.Burning"), out var name));
      Assert.AreEqual("Damage.Fire.Burning", name);
    }

    [Test]
    public void Registry_Register_AlsoRegistersAncestors()
    {
      using var registry = new TagRegistry();
      registry.Register("Damage.Fire.Burning");

      Assert.IsTrue(registry.IsKnown(MakeTag("Damage.Fire")));
      Assert.IsTrue(registry.IsKnown(MakeTag("Damage")));
    }

    [Test]
    public void Registry_DuplicateRegister_IsIdempotent()
    {
      using var registry = new TagRegistry();
      var first = registry.Register("Damage.Fire");
      var second = registry.Register("Damage.Fire");
      Assert.AreEqual(first, second);
    }
    
    [Test]
    public void ValidatePath_ValidPath_ReturnsNull() =>
      Assert.IsNull(TagUtils.ValidatePath("Damage.Fire.Burning"));

    [Test]
    public void ValidatePath_ValidWithUnderscore_ReturnsNull() =>
      Assert.IsNull(TagUtils.ValidatePath("Damage.Fire_Storm.Burning"));

    [Test]
    public void ValidatePath_Empty_ReturnsError() =>
      Assert.IsNotNull(TagUtils.ValidatePath(""));

    [Test]
    public void ValidatePath_LeadingDot_ReturnsError() =>
      Assert.IsNotNull(TagUtils.ValidatePath(".Damage.Fire"));

    [Test]
    public void ValidatePath_TrailingDot_ReturnsError() =>
      Assert.IsNotNull(TagUtils.ValidatePath("Damage.Fire."));

    [Test]
    public void ValidatePath_ConsecutiveDots_ReturnsError() =>
      Assert.IsNotNull(TagUtils.ValidatePath("Damage..Fire"));

    [Test]
    public void ValidatePath_IllegalChar_ReturnsError() =>
      Assert.IsNotNull(TagUtils.ValidatePath("Damage.Fire!"));

    [Test]
    public void ValidatePath_TooLong_ReturnsError() =>
      Assert.IsNotNull(TagUtils.ValidatePath(new string('A', 257)));

    [Test]
    public void ValidatePath_MaxLength_ReturnsNull() =>
      Assert.IsNull(TagUtils.ValidatePath(new string('A', 256)));
  }
}
