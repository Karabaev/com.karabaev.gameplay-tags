using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace com.karabaev.gameplayTags.tests
{
  public abstract unsafe class TagTestBase
  {
    private readonly List<IntPtr> _allocations = new();

    [TearDown]
    public void TearDown()
    {
      foreach (var p in _allocations) Marshal.FreeHGlobal(p);
      _allocations.Clear();
    }

    protected Tag MakeTag(string path)
    {
      var depth = CountSeparators(path);
      long* ancestors = null;
      if (depth > 0)
      {
        var mem = (long*)Marshal.AllocHGlobal(depth * sizeof(long)).ToPointer();
        _allocations.Add(new IntPtr(mem));
        ancestors = mem;
      }

      return Tag.From(path, ancestors);
    }

    protected TagContainer MakeContainer(int capacity = 8, int ancestorStride = 8)
    {
      var hashBytes = capacity * sizeof(long);
      var ancestorBytes = capacity * ancestorStride * sizeof(long);
      var depthBytes = capacity * sizeof(int);
      var mem = (byte*)Marshal.AllocHGlobal(hashBytes + ancestorBytes + depthBytes).ToPointer();
      _allocations.Add(new IntPtr(mem));
      new System.Span<byte>(mem, hashBytes + ancestorBytes + depthBytes).Clear();
      var h = (long*)mem;
      var a = (long*)(mem + hashBytes);
      var d = (int*)(mem + hashBytes + ancestorBytes);
      return new TagContainer(0, h, a, d, capacity, ancestorStride);
    }

    private static int CountSeparators(string path)
    {
      var count = 0;
      foreach (var character in path)
      {
        if (character == Tag.Separator) count++;
      }

      return count;
    }
  }
}