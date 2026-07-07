using UnityEditor;
using UnityEngine;

namespace com.karabaev.gameplayTags.editor
{
  [FilePath("ProjectSettings/Gameplay/TagSettings.asset", FilePathAttribute.Location.ProjectFolder)]
  public class TagSettings : ScriptableSingleton<TagSettings>
  {
    [SerializeField] private DefaultAsset _generatedCodeDirectory = null!;
    [SerializeField] private string _generatedNamespace = "com.karabaev.gameplayTags.Generated";
    [SerializeField] private string _generatedClassName = "GameplayTags";

    public DefaultAsset GeneratedCodeDirectory => _generatedCodeDirectory;
    public string GeneratedNamespace => _generatedNamespace;
    public string GeneratedClassName => _generatedClassName;

    public void SetGeneratedCodeDirectory(DefaultAsset directory)
    {
      _generatedCodeDirectory = directory;
      Save(true);
    }

    public void SetGeneratedNamespace(string generatedNamespace)
    {
      _generatedNamespace = generatedNamespace;
      Save(true);
    }

    public void SetGeneratedClassName(string generatedClassName)
    {
      _generatedClassName = generatedClassName;
      Save(true);
    }
  }
}
