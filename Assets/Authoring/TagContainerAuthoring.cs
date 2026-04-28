using System;

namespace com.karabaev.gameplayTags
{
  [Serializable]
  public class TagContainerAuthoring
  {
    public string[] TagNames = Array.Empty<string>();

    public TagContainer Author(TagRegistry registry)
    {
      var container = registry.CreateContainer(TagNames.Length);
      foreach (var path in TagNames)
      {
        container.Add(registry.Register(path));
      }
      return container;
    }
  }
}
