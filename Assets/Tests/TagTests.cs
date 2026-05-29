using System;
using NUnit.Framework;

namespace com.karabaev.gameplayTags.tests
{
  public class TagTests : TagTestBase
  {
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
