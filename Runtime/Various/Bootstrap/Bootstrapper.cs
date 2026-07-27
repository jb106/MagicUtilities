using UnityEngine;

public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Run()
    {
        // Loads every BootstrapConfig placed anywhere under a Resources folder
        foreach (var config in Resources.LoadAll<BootstrapConfig>(""))
        foreach (var prefab in config.prefabs)
        {
            if (prefab == null) continue;

            var instance = Object.Instantiate(prefab);
            instance.name = prefab.name; // strip "(Clone)"
            Object.DontDestroyOnLoad(instance);
        }
    }
}