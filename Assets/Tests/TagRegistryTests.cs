using NUnit.Framework;

namespace com.karabaev.gameplayTags.tests
{
  public class TagRegistryTests : TagTestBase
  {
    [Test]
    public void Register_ReturnsCorrectTag()
    {
      using var registry = new TagRegistry();
      Assert.AreEqual(MakeTag("Damage.Fire.Burning"), registry.Register("Damage.Fire.Burning"));
    }

    [Test]
    public void TryGetName_ReturnsPath()
    {
      using var registry = new TagRegistry();
      registry.Register("Damage.Fire.Burning");

      Assert.IsTrue(registry.TryGetName(MakeTag("Damage.Fire.Burning"), out var name));
      Assert.AreEqual("Damage.Fire.Burning", name);
    }

    [Test]
    public void Register_AlsoRegistersAncestors()
    {
      using var registry = new TagRegistry();
      registry.Register("Damage.Fire.Burning");

      Assert.IsTrue(registry.IsKnown(MakeTag("Damage.Fire")));
      Assert.IsTrue(registry.IsKnown(MakeTag("Damage")));
    }

    [Test]
    public void DuplicateRegister_IsIdempotent()
    {
      using var registry = new TagRegistry();
      var first = registry.Register("Damage.Fire");
      var second = registry.Register("Damage.Fire");
      Assert.AreEqual(first, second);
    }

    [Test]
    public void FreeContainer_ZeroesContainer()
    {
      using var registry = new TagRegistry();
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
      using var registry = new TagRegistry();
      registry.Register("Damage");
      var container = registry.CreateContainer(2);

      registry.FreeContainer(ref container);
      Assert.DoesNotThrow(() => registry.FreeContainer(ref container));
    }

    [Test]
    public void FreeContainer_DefaultContainer_IsNoOp()
    {
      using var registry = new TagRegistry();
      var container = default(TagContainer);
      Assert.DoesNotThrow(() => registry.FreeContainer(ref container));
    }

    [Test]
    public void FreeContainer_ContainerIsNotValid()
    {
      using var registry = new TagRegistry();
      var tag = registry.Register("Damage.Fire");
      var container = registry.CreateContainer(4);
      container.Add(tag);
      registry.FreeContainer(ref container);

      Assert.IsFalse(container.IsValid);
    }

    [Test]
    public void FreeContainer_ThenDispose_DoesNotThrow()
    {
      var registry = new TagRegistry();
      registry.Register("Damage.Fire");
      var c1 = registry.CreateContainer(2);
      var c2 = registry.CreateContainer(2);

      registry.FreeContainer(ref c1);
      Assert.DoesNotThrow(() => registry.Dispose());
    }
  }
}
