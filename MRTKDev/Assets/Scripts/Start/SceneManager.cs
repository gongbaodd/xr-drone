using UnityEngine;
using UnityEngine.SceneManagement;

public class FPVSceneManager : MonoBehaviour
{

    [Header("Scenes (must be in Build Settings)")]
    [SerializeField] string tutorialScene = "";
    [SerializeField] string playGroundScene = "";
    [SerializeField] string cityScene = "";

    public void LoadTutorialScene()
    {
        SceneManager.LoadScene(tutorialScene);
    }

    public void LoadPlayGroundScene()
    {
        SceneManager.LoadScene(playGroundScene);
    }

    public void LoadCityScene()
    {
        SceneManager.LoadScene(cityScene);
    }
}
