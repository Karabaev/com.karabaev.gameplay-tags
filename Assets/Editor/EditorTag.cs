using System;
using UnityEngine;

namespace com.karabaev.gameplayTags.editor
{
  [Serializable]
  public class EditorTag
  {
    [field: SerializeField] public string Name { get; set; } = string.Empty;
    [field: SerializeField] public string Comment { get; set; } = string.Empty;

    public EditorTag() { }

    public EditorTag(string name, string comment = "")
    {
      Name = name;
      Comment = comment;
    }
  }
}