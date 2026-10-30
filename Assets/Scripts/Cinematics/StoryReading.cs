using UnityEngine;

public static class StoryReading
{
    public static float Duration(string line)
    {
        return Mathf.Clamp(1.1f + (string.IsNullOrEmpty(line) ? 0 : line.Length) * .038f, 2.2f, 7.2f);
    }
}
