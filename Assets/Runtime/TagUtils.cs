namespace com.karabaev.gameplayTags
{
  public static class TagUtils
  {
    /// <summary>
    /// Validates a dot-separated tag path string.
    /// Returns null on success, or a human-readable error message on failure.
    /// </summary>
    public static string? ValidatePath(string path)
    {
      if(string.IsNullOrEmpty(path))
        return "Path must not be empty.";

      if(path.Length > 256)
        return $"Path is too long ({path.Length} chars). Maximum is 256.";

      if(path[0] == Tag.Separator || path[^1] == Tag.Separator)
        return $"Path must not start or end with '{Tag.Separator}'.";

      var segments = path.Split(Tag.Separator);
      foreach(var segment in segments)
      {
        if(segment.Length == 0) return "Path must not contain consecutive dots ('..').";

        foreach(var ch in segment)
        {
          if (!char.IsLetterOrDigit(ch) && ch != '_')
          {
            return $"Segment '{segment}' contains invalid character '{ch}'. Only letters, digits, and underscores are allowed.";
          }
        }
      }

      return null;
    }
  }
}
