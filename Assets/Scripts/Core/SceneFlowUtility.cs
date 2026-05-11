using UnityEngine.SceneManagement;

public static class SceneFlowUtility
{
    public static int ResolveNextSceneIndex(int currentBuildIndex, string titleSceneName)
    {
        int nextIndex = currentBuildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            return nextIndex;
        }

        int titleIndex = FindSceneIndexByName(titleSceneName);
        return titleIndex >= 0 ? titleIndex : 0;
    }

    public static int ResolveGameplaySceneIndex(string titleSceneName)
    {
        int titleIndex = FindSceneIndexByName(titleSceneName);
        int candidate = titleIndex >= 0 ? titleIndex + 1 : 0;

        if (candidate < SceneManager.sceneCountInBuildSettings)
        {
            return candidate;
        }

        return 0;
    }

    public static int FindSceneIndexByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return -1;
        }

        for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(index);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);

            if (name == sceneName)
            {
                return index;
            }
        }

        return -1;
    }
}
