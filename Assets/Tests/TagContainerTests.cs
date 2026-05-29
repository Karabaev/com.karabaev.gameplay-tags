using System;
using NUnit.Framework;

namespace com.karabaev.gameplayTags.tests
{
  public class TagContainerTests : TagTestBase
  {
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
    public void AddContainer_NoDuplicatesInResult()
    {
      var dst = MakeContainer();
      dst.Add(MakeTag("Damage.Fire"));

      var src = MakeContainer();
      src.Add(MakeTag("Damage.Fire")); // already in dst
      src.Add(MakeTag("Ability.Heal"));

      dst.Add(src);

      Assert.AreEqual(2, dst.Count);
      Assert.IsTrue(dst.HasExact(MakeTag("Damage.Fire")));
      Assert.IsTrue(dst.HasExact(MakeTag("Ability.Heal")));
    }

    [Test]
    public void AddContainer_TooSmall_ThrowsInvalidOperationException()
    {
      var dst = MakeContainer(capacity: 1);
      dst.Add(MakeTag("Damage.Fire"));

      var src = MakeContainer();
      src.Add(MakeTag("Ability.Heal")); // one new tag, no free slots

      Assert.Throws<InvalidOperationException>(() => dst.Add(src));
    }

    [Test]
    public void RemoveContainer_RemovesPresentTags()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));
      c.Add(MakeTag("Ability.Heal"));
      c.Add(MakeTag("Status.Burn"));

      var toRemove = MakeContainer();
      toRemove.Add(MakeTag("Damage.Fire"));
      toRemove.Add(MakeTag("Ability.Heal"));

      c.Remove(toRemove);

      Assert.AreEqual(1, c.Count);
      Assert.IsFalse(c.HasExact(MakeTag("Damage.Fire")));
      Assert.IsFalse(c.HasExact(MakeTag("Ability.Heal")));
      Assert.IsTrue(c.HasExact(MakeTag("Status.Burn")));
    }

    [Test]
    public void RemoveContainer_AbsentTags_AreIgnored()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire"));

      var toRemove = MakeContainer();
      toRemove.Add(MakeTag("Ability.Heal")); // not in c

      Assert.DoesNotThrow(() => c.Remove(toRemove));
      Assert.AreEqual(1, c.Count);
      Assert.IsTrue(c.HasExact(MakeTag("Damage.Fire")));
    }

    [Test]
    public void ToArray_ReturnsAllTags()
    {
      var c = MakeContainer();
      var fire = MakeTag("Damage.Fire");
      var heal = MakeTag("Ability.Heal");
      c.Add(fire);
      c.Add(heal);

      var result = c.ToArray();

      Assert.AreEqual(2, result.Length);
      Assert.IsTrue(System.Array.Exists(result, t => t == fire));
      Assert.IsTrue(System.Array.Exists(result, t => t == heal));
    }

    [Test]
    public void ToArray_EmptyContainer_ReturnsEmptyArray()
    {
      var c = MakeContainer();
      Assert.AreEqual(0, c.ToArray().Length);
    }

    [Test]
    public void ToArray_TagsPreserveHierarchy()
    {
      var c = MakeContainer();
      c.Add(MakeTag("Damage.Fire.Burning"));

      var tag = c.ToArray()[0];

      Assert.IsTrue(tag.IsChildOf(MakeTag("Damage.Fire")));
      Assert.IsTrue(tag.IsChildOf(MakeTag("Damage")));
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
  }
}
