using System;
using NUnit.Framework;

namespace com.karabaev.gameplayTags.tests
{
  public class TagRegistryTests : TagTestBase
  {
    private const int DefaultMaxDepth = 8;

    [Test]
    public void Register_ReturnsCorrectTag()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);
      Assert.AreEqual(MakeTag("Damage.Fire.Burning"), registry.Register("Damage.Fire.Burning"));
    }

    [Test]
    public void TryGetName_ReturnsPath()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);
      registry.Register("Damage.Fire.Burning");

      Assert.IsTrue(registry.TryGetName(MakeTag("Damage.Fire.Burning"), out var name));
      Assert.AreEqual("Damage.Fire.Burning", name);
    }

    [Test]
    public void Register_AlsoRegistersAncestors()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);
      registry.Register("Damage.Fire.Burning");

      Assert.IsTrue(registry.IsKnown(MakeTag("Damage.Fire")));
      Assert.IsTrue(registry.IsKnown(MakeTag("Damage")));
    }

    [Test]
    public void DuplicateRegister_IsIdempotent()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);
      var first = registry.Register("Damage.Fire");
      var second = registry.Register("Damage.Fire");
      Assert.AreEqual(first, second);
    }

    [Test]
    public void FreeContainer_ZeroesContainer()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);
      registry.Register("Damage.Fire");
      var container = registry.CreateContainer(4);
      container.Add(registry.Register("Damage.Fire"));

      registry.FreeContainer(ref container);

      Assert.AreEqual(0, container.Count);
      Assert.AreEqual(0, container.Id);
    }

    [Test]
    public void FreeContainer_DoubleFree_IsNoOp()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);
      registry.Register("Damage");
      var container = registry.CreateContainer(2);

      registry.FreeContainer(ref container);
      Assert.DoesNotThrow(() => registry.FreeContainer(ref container));
    }

    [Test]
    public void FreeContainer_DefaultContainer_IsNoOp()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);
      var container = default(TagContainer);
      Assert.DoesNotThrow(() => registry.FreeContainer(ref container));
    }

    [Test]
    public void FreeContainer_ContainerIsNotValid()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);
      var tag = registry.Register("Damage.Fire");
      var container = registry.CreateContainer(4);
      container.Add(tag);
      registry.FreeContainer(ref container);

      Assert.IsFalse(container.IsValid);
    }

    [Test]
    public void FreeContainer_ThenDispose_DoesNotThrow()
    {
      var registry = new TagRegistry(DefaultMaxDepth);
      registry.Register("Damage.Fire");
      var c1 = registry.CreateContainer(2);
      var c2 = registry.CreateContainer(2);

      registry.FreeContainer(ref c1);
      Assert.DoesNotThrow(() => registry.Dispose());
    }

    [Test]
    public void Constructor_NegativeMaxDepth_Throws()
    {
      Assert.Throws<ArgumentOutOfRangeException>(() => new TagRegistry(-1));
    }

    [Test]
    public void Register_DepthExceedsMaxDepth_Throws()
    {
      using var registry = new TagRegistry(1);
      Assert.Throws<InvalidOperationException>(() => registry.Register("Damage.Fire.Burning"));
    }

    [Test]
    public void Register_DepthEqualsMaxDepth_Succeeds()
    {
      using var registry = new TagRegistry(2);
      Assert.DoesNotThrow(() => registry.Register("Damage.Fire.Burning"));
    }

    [Test]
    public void CreateContainer_BeforeAnyRegister_StillUsesConfiguredMaxDepth()
    {
      using var registry = new TagRegistry(2);
      var container = registry.CreateContainer(4);

      container.Add(registry.Register("Damage.Fire.Burning"));

      Assert.IsTrue(container.Has(MakeTag("Damage.Fire")));
      Assert.IsTrue(container.Has(MakeTag("Damage")));
    }

    [Test]
    public void TryGetTag_RegisteredTag_ReturnsTrueAndTag()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);
      var registered = registry.Register("Damage.Fire");

      Assert.IsTrue(registry.TryGetTag("Damage.Fire", out var tag));
      Assert.AreEqual(registered, tag);
    }

    [Test]
    public void TryGetTag_AncestorOfRegisteredTag_ReturnsTrue()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);
      registry.Register("Damage.Fire.Burning");

      Assert.IsTrue(registry.TryGetTag("Damage.Fire", out _));
      Assert.IsTrue(registry.TryGetTag("Damage", out _));
    }

    [Test]
    public void TryGetTag_UnregisteredTag_ReturnsFalse()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);
      registry.Register("Damage.Fire");

      Assert.IsFalse(registry.TryGetTag("Ability.Heal", out _));
    }

    [Test]
    public void TryGetTag_DoesNotRegisterTag()
    {
      using var registry = new TagRegistry(DefaultMaxDepth);

      registry.TryGetTag("Ability.Heal", out _);

      Assert.IsFalse(registry.IsKnown(MakeTag("Ability.Heal")));
    }
  }
}
