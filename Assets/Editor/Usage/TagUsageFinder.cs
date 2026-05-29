using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.karabaev.gameplayTags.editor.Usage
{
  internal static class TagUsageFinder
  {
    internal static List<UsageResult> FindUsages(string tagName)
    {
      var searchFolders = new[] { "Assets" };
      var results = new List<UsageResult>();
      var seen = new HashSet<string>();
      using var cts = new CancellationTokenSource();
      const int passCount = 2;

      try
      {
        void ShowProgressBar(string assetPath, int assetIndex, int assetCount, int passIndex)
        {
          var progress = (passIndex + (float)assetIndex / assetCount) / passCount;
          if (EditorUtility.DisplayCancelableProgressBar("Finding Tag Usages", assetPath, progress))
          {
            cts.Cancel();
          }
        }

        var passIndex = 0;
        SearchAssets("t:ScriptableObject", searchFolders, tagName, results, seen, passIndex++, ShowProgressBar, cts.Token);
        SearchAssets("t:Prefab", searchFolders, tagName, results, seen, passIndex++, ShowProgressBar, cts.Token);
        SearchScenes(tagName, searchFolders, results, seen, passIndex++, ShowProgressBar, cts.Token);
        return results;
      }
      catch(OperationCanceledException)
      {
        return results;
      }
    }

    private static void SearchAssets(
      string filter,
      string[] searchFolders,
      string tagName,
      List<UsageResult> outResults,
      HashSet<string> outSeen,
      int passIndex,
      Action<string, int, int, int> showProgressBarAction,
      CancellationToken ct)
    {
      var guids = AssetDatabase.FindAssets(filter, searchFolders);
      for(var i = 0; i < guids.Length; i++)
      {
        var guid = guids[i];
        if(!outSeen.Add(guid)) continue;

        var path = AssetDatabase.GUIDToAssetPath(guid);
        showProgressBarAction.Invoke(path, i, guids.Length, passIndex);
        
        var asset = AssetDatabase.LoadMainAssetAtPath(path);
        var found = asset is GameObject prefabRoot
          ? SearchGameObject(prefabRoot, tagName, ct)
          : ContainsTag(new SerializedObject(asset), tagName, ct);

        if (!found) continue;
          
        outResults.Add(new UsageResult(path));
      }
    }

    private static void SearchScenes(string tagName,
      string[] searchFolders,
      List<UsageResult> outResults,
      HashSet<string> outSeen,
      int passIndex,
      Action<string, int, int, int> showProgressBarAction,
      CancellationToken ct)
    {
      var guids = AssetDatabase.FindAssets("t:SceneAsset", searchFolders);
      for(var i = 0; i < guids.Length; i++)
      {
        var guid = guids[i];
        if (!outSeen.Add(guid)) continue;

        var path = AssetDatabase.GUIDToAssetPath(guid);
        showProgressBarAction.Invoke(path, i, guids.Length, passIndex);

        var alreadyLoaded = IsSceneLoaded(path);
        var scene = alreadyLoaded
          ? SceneManager.GetSceneByPath(path)
          : EditorSceneManager.OpenPreviewScene(path);

        try
        {
          if (SceneContainsTag(scene, tagName, ct)) outResults.Add(new UsageResult(path));
        }
        finally
        {
          if (!alreadyLoaded) EditorSceneManager.ClosePreviewScene(scene);
        }
      }
    }

    private static bool IsSceneLoaded(string scenePath)
    {
      for(var i = 0; i < SceneManager.sceneCount; i++)
      {
        if (SceneManager.GetSceneAt(i).path == scenePath) return true;
      }
      return false;
    }

    private static bool SceneContainsTag(Scene scene, string tagName, CancellationToken ct)
    {
      foreach(var root in scene.GetRootGameObjects())
      {
        if (SearchGameObject(root, tagName, ct)) return true;
      }
      return false;
    }

    private static bool SearchGameObject(GameObject go, string tagName, CancellationToken ct)
    {
      var components = go.GetComponentsInChildren<Component>(true);
      foreach (var component in components)
      {
        if (ContainsTag(new SerializedObject(component), tagName, ct)) return true;
      }

      return false;
    }

    private static bool ContainsTag(SerializedObject so, string tagName, CancellationToken ct)
    {
      var iter = so.GetIterator();
      var enterChildren = true;
      while(iter.NextVisible(enterChildren))
      {
        if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
        
        enterChildren = true;

        if(iter.propertyPath == "m_Script")
        {
          enterChildren = false;
          continue;
        }

        switch (iter.type)
        {
          case nameof(TagAuthoring):
          {
            var nameProp = iter.FindPropertyRelative(nameof(TagAuthoring.Name));
            if (nameProp != null && IsMatch(nameProp.stringValue, tagName)) return true;
            enterChildren = false;
            break;
          }
          case nameof(TagContainerAuthoring):
          {
            var namesProp = iter.FindPropertyRelative(nameof(TagContainerAuthoring.TagNames));
            if(namesProp != null)
            {
              for(var i = 0; i < namesProp.arraySize; i++)
              {
                if (IsMatch(namesProp.GetArrayElementAtIndex(i).stringValue, tagName)) return true;
              }
            }
            enterChildren = false;
            break;
          }
        }
      }
      return false;
    }
    
    private static bool IsMatch(string storedTag, string searchTag) =>
      storedTag == searchTag || storedTag.StartsWith(searchTag + Tag.Separator);

    internal readonly struct UsageResult
    {
      internal readonly string AssetPath;

      internal UsageResult(string assetPath)
      {
        AssetPath = assetPath;
      }
    }
  }
}
