using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NavigatorController
{
    private static Dictionary<string, object> sceneArguments = new Dictionary<string, object>();

    public static void Navigate(string sceneName, object args = null)
    {
        if (args != null)
        {
            sceneArguments[sceneName] = args;
        }
        SceneManager.LoadScene(sceneName);
    }

    public static T GetArguments<T>(string sceneName)
    {
        if (sceneArguments.TryGetValue(sceneName, out var args) && args is T typedArgs)
        {
            return typedArgs;
        }
        return default;
    }

    public static void ClearArguments(string sceneName)
    {
        if (sceneArguments.ContainsKey(sceneName))
        {
            sceneArguments.Remove(sceneName);
        }
    }
}
