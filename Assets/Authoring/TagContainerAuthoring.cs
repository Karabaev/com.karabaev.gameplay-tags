using System;

namespace com.karabaev.gameplayTags
{
  [Serializable]
  public class TagContainerAuthoring
  {
    public string[] TagNames = Array.Empty<string>();

    public TagContainer Author()
    {
      var container = new TagContainer();
      foreach (var path in TagNames)
      {
        container.Add(Tag.From(path));
      }
      return container;
    }
  }
}
