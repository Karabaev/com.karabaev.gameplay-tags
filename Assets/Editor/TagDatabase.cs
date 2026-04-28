using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.karabaev.gameplayTags.editor
{
  [FilePath("ProjectSettings/Gameplay/Tags.asset", FilePathAttribute.Location.ProjectFolder)]
  public class TagDatabase : ScriptableSingleton<TagDatabase>
  {
    [SerializeField] private List<EditorTag> _tags = new();

    public IReadOnlyList<EditorTag> Tags => _tags;

    public void AddTag(string path, string comment = "")
    {
      if(ContainsPath(path)) return;
      
      _tags.Add(new EditorTag(path, comment));
      Save(true);
    }

    /// <summary>Removes the tag and all its descendants.</summary>
    public void RemoveTag(string path)
    {
      var childPrefix = path + Tag.Separator;
      _tags.RemoveAll(t => t.Name == path || t.Name.StartsWith(childPrefix));
      Save(true);
    }

    /// <summary>
    /// Renames the last segment of <paramref name="oldName"/> to <paramref name="newSegment"/>.
    /// All descendants are updated to reflect the new prefix.
    /// </summary>
    public void RenameTag(string oldName, string newSegment)
    {
      var dotIndex = oldName.LastIndexOf(Tag.Separator);
      var newFullName = dotIndex >= 0
        ? oldName[..(dotIndex + 1)] + newSegment
        : newSegment;

      var oldPrefix = $"{oldName}{Tag.Separator}";
      var newPrefix = $"{newFullName}{Tag.Separator}";

      foreach(var tag in _tags)
      {
        if (tag.Name == oldName)
        {
          tag.Name = newFullName;
        }
        else if (tag.Name.StartsWith(oldPrefix))
        {
          tag.Name = $"{newPrefix}{tag.Name[oldPrefix.Length..]}";
        }
      }

      Save(true);
    }

    public bool ContainsPath(string tagName)
    {
      foreach (var tag in _tags)
      {
        if (tag.Name == tagName) return true;
      }
      return false;
    }

    public bool TryGetTag(string tagName, out EditorTag? foundTag)
    {
      foreach (var tag in _tags)
      {
        if (tag.Name != tagName) continue;
        
        foundTag = tag;
        return true;
      }

      foundTag = null;
      return false;
    }
    
    public EditorTag? FindTag(string path)
    {
      foreach (var tag in _tags)
      {
        if (tag.Name == path) return tag;
      }
      return null;
    }

    public TagRegistry BuildRegistry()
    {
      var registry = new TagRegistry();
      foreach(var tag in _tags)
      {
        if (!string.IsNullOrEmpty(tag.Name))
        {
          registry.Register(tag.Name);
        }
      }
      return registry;
    }
  }
}
