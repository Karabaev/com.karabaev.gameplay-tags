using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace com.karabaev.gameplayTags.editor.Baking
{
  internal class TagListBakerBuildPreprocessor : IPreprocessBuildWithReport
  {
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report) => ListBaker.Bake(TagDatabase.instance.Tags);
  }
}
