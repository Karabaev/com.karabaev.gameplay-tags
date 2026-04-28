using System;

namespace com.karabaev.gameplayTags
{
  [Serializable]
  public class TagAuthoring
  {
    public string Name = string.Empty;

    public Tag Author(TagRegistry registry) => registry.Register(Name);
  }
}
