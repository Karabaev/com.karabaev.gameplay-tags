using System;
using UnityEngine;

namespace com.karabaev.gameplayTags
{
  [Serializable]
  public class TagAuthoring
  {
    public string Name = string.Empty;

    public Tag Author() => Tag.From(Name);
  }
}
