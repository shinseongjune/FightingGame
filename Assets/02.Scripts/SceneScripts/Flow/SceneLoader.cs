using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
