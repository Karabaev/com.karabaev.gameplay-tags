using com.karabaev.gameplayTags;
using NUnit.Framework;

namespace com.karabaev.gameplayTags.tests
{
  public class TagTests
  {
    // ------------------------------------------------------------------ //
    // Tag.From / equality
    // ------------------------------------------------------------------ //

    [Test]
    public void From_SamePath_ProducesEqualTags()
    {
      var a = Tag.From("Damage.Fire");
      var b = Tag.From("Damage.Fire");
      Assert.AreEqual(a, b);
    }

    [Test]
    public void From_DifferentPaths_ProducesUnequalTags()
    {
      var a = Tag.From("Damage.Fire");
      var b = Tag.From("Damage.Ice");
      Assert.AreNotEqual(a, b);
    }

    [Test]
    public void From_EmptyString_ReturnsNone()
    {
      Assert.AreEqual(Tag.None, Tag.From(""));
      Assert.AreEqual(Tag.None, Tag.From(null!));
    }

    [Test]
    public void None_IsNone_IsTrue()
    {
      Assert.IsTrue(Tag.None.IsNone);
      Assert.IsFalse(Tag.From("Damage").IsNone);
    }

    [Test]
    public void Tags_AreCaseSensitive()
    {
      Assert.AreNotEqual(Tag.From("Damage.Fire"), Tag.From("damage.fire"));
    }

    // ------------------------------------------------------------------ //
    // IsChildOf
    // ------------------------------------------------------------------ //

    [Test]
    public void IsChildOf_DirectChild_ReturnsTrue()
    {
      Assert.IsTrue(Tag.From("Damage.Fire.Burning").IsChildOf(Tag.From("Damage.Fire")));
    }

    [Test]
    public void IsChildOf_IndirectDescendant_ReturnsTrue()
    {
      Assert.IsTrue(Tag.From("Damage.Fire.Burning").IsChildOf(Tag.From("Damage")));
    }

    [Test]
    public void IsChildOf_EqualTags_ReturnsFalse()
    {
      Assert.IsFalse(Tag.From("Damage.Fire").IsChildOf(Tag.From("Damage.Fire")));
    }

    [Test]
    public void IsChildOf_ParentIsNotChildOfChild_ReturnsFalse()
    {
      Assert.IsFalse(Tag.From("Damage").IsChildOf(Tag.From("Damage.Fire")));
    }

    [Test]
    public void IsChildOf_UnrelatedTags_ReturnsFalse()
    {
      Assert.IsFalse(Tag.From("Ability.Heal").IsChildOf(Tag.From("Damage.Fire")));
    }

    [Test]
    public void IsChildOf_NoneAncestor_ReturnsFalse()
    {
      Assert.IsFalse(Tag.From("Damage.Fire").IsChildOf(Tag.None));
    }

    [Test]
    public void IsChildOf_FiveLevels_ReturnsTrue()
    {
      Assert.IsTrue(Tag.From("A.B.C.D.E").IsChildOf(Tag.From("A")));
      Assert.IsTrue(Tag.From("A.B.C.D.E").IsChildOf(Tag.From("A.B.C")));
    }

    // ------------------------------------------------------------------ //
    // IsOrIsChildOf
    // ------------------------------------------------------------------ //

    [Test]
    public void IsOrIsChildOf_ExactMatch_ReturnsTrue()
    {
      Assert.IsTrue(Tag.From("Damage.Fire").IsOrIsChildOf(Tag.From("Damage.Fire")));
    }

    [Test]
    public void IsOrIsChildOf_Child_ReturnsTrue()
    {
      Assert.IsTrue(Tag.From("Damage.Fire.Burning").IsOrIsChildOf(Tag.From("Damage.Fire")));
    }

    [Test]
    public void IsOrIsChildOf_Unrelated_ReturnsFalse()
    {
      Assert.IsFalse(Tag.From("Ability").IsOrIsChildOf(Tag.From("Damage")));
    }

    // ------------------------------------------------------------------ //
    // TagContainer.Add / HasExact
    // ------------------------------------------------------------------ //

    [Test]
    public void Add_IncreasesCount()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire"));
      Assert.AreEqual(1, c.Count);
    }

    [Test]
    public void Add_Duplicate_DoesNotIncreaseCount()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire"));
      c.Add(Tag.From("Damage.Fire"));
      Assert.AreEqual(1, c.Count);
    }

    [Test]
    public void Add_None_IsIgnored()
    {
      var c = new TagContainer();
      c.Add(Tag.None);
      Assert.AreEqual(0, c.Count);
    }

    [Test]
    public void HasExact_PresentTag_ReturnsTrue()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire"));
      Assert.IsTrue(c.HasExact(Tag.From("Damage.Fire")));
    }

    [Test]
    public void HasExact_AbsentTag_ReturnsFalse()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire"));
      Assert.IsFalse(c.HasExact(Tag.From("Damage")));
      Assert.IsFalse(c.HasExact(Tag.From("Damage.Fire.Burning")));
    }

    // ------------------------------------------------------------------ //
    // TagContainer.Remove
    // ------------------------------------------------------------------ //

    [Test]
    public void Remove_PresentTag_RemovesIt()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire"));
      c.Remove(Tag.From("Damage.Fire"));
      Assert.AreEqual(0, c.Count);
      Assert.IsFalse(c.HasExact(Tag.From("Damage.Fire")));
    }

    [Test]
    public void Remove_AbsentTag_DoesNothing()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire"));
      c.Remove(Tag.From("Ability"));
      Assert.AreEqual(1, c.Count);
    }

    [Test]
    public void Remove_MiddleElement_ShiftsRemainingCorrectly()
    {
      var c = new TagContainer();
      c.Add(Tag.From("A"));
      c.Add(Tag.From("B"));
      c.Add(Tag.From("C"));
      c.Remove(Tag.From("B"));
      Assert.AreEqual(2, c.Count);
      Assert.IsTrue(c.HasExact(Tag.From("A")));
      Assert.IsTrue(c.HasExact(Tag.From("C")));
      Assert.IsFalse(c.HasExact(Tag.From("B")));
    }

    // ------------------------------------------------------------------ //
    // TagContainer.Has (hierarchy-aware)
    // ------------------------------------------------------------------ //

    [Test]
    public void Has_ExactTagPresent_ReturnsTrue()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire"));
      Assert.IsTrue(c.Has(Tag.From("Damage.Fire")));
    }

    [Test]
    public void Has_ParentOfPresentTag_ReturnsTrue()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire.Burning"));
      Assert.IsTrue(c.Has(Tag.From("Damage.Fire")));
      Assert.IsTrue(c.Has(Tag.From("Damage")));
    }

    [Test]
    public void Has_ChildOfPresentTag_ReturnsFalse()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire"));
      // Container has "Damage.Fire" but not "Damage.Fire.Burning"
      Assert.IsFalse(c.Has(Tag.From("Damage.Fire.Burning")));
    }

    [Test]
    public void Has_UnrelatedTag_ReturnsFalse()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire.Burning"));
      Assert.IsFalse(c.Has(Tag.From("Ability")));
    }

    [Test]
    public void Has_None_ReturnsFalse()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire"));
      Assert.IsFalse(c.Has(Tag.None));
    }

    // ------------------------------------------------------------------ //
    // TagContainer.HasAll
    // ------------------------------------------------------------------ //

    [Test]
    public void HasAll_AllPresent_ReturnsTrue()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire.Burning"));
      c.Add(Tag.From("Ability.Heal"));

      var required = new TagContainer();
      required.Add(Tag.From("Damage.Fire")); // ancestor of stored tag
      required.Add(Tag.From("Ability.Heal")); // exact match

      Assert.IsTrue(c.HasAll(required));
    }

    [Test]
    public void HasAll_OneMissing_ReturnsFalse()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire.Burning"));

      var required = new TagContainer();
      required.Add(Tag.From("Damage.Fire"));
      required.Add(Tag.From("Ability")); // not present

      Assert.IsFalse(c.HasAll(required));
    }

    [Test]
    public void HasAll_EmptyOther_ReturnsTrue()
    {
      var c = new TagContainer();
      c.Add(Tag.From("Damage.Fire"));
      Assert.IsTrue(c.HasAll(new TagContainer()));
    }

    // ------------------------------------------------------------------ //
    // TagRegistry
    // ------------------------------------------------------------------ //

    [Test]
    public void Registry_Register_ReturnsCorrectTag()
    {
      var registry = new TagRegistry();
      var tag = registry.Register("Damage.Fire.Burning");
      Assert.AreEqual(Tag.From("Damage.Fire.Burning"), tag);
    }

    [Test]
    public void Registry_TryGetName_ReturnsPath()
    {
      var registry = new TagRegistry();
      registry.Register("Damage.Fire.Burning");

      Assert.IsTrue(registry.TryGetName(Tag.From("Damage.Fire.Burning"), out var name));
      Assert.AreEqual("Damage.Fire.Burning", name);
    }

    [Test]
    public void Registry_Register_AlsoRegistersAncestors()
    {
      var registry = new TagRegistry();
      registry.Register("Damage.Fire.Burning");

      Assert.IsTrue(registry.IsKnown(Tag.From("Damage.Fire")));
      Assert.IsTrue(registry.IsKnown(Tag.From("Damage")));
    }

    [Test]
    public void Registry_DuplicateRegister_IsIdempotent()
    {
      var registry = new TagRegistry();
      var first = registry.Register("Damage.Fire");
      var second = registry.Register("Damage.Fire");
      Assert.AreEqual(first, second);
    }

    // ------------------------------------------------------------------ //
    // TagUtils.ValidatePath
    // ------------------------------------------------------------------ //

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
