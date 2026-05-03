using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hub singleton for loading application scenes by name. Add matching scenes to Build Settings.
/// </summary>
public class FPVSceneManager : MonoBehaviour
{
    [SerializeField] GameObject menuGameObject;

    /// <summary>Disables the menu root GameObject.</summary>
    private void DisableMenuGameObject() 
    {
        if (menuGameObject != null)
            menuGameObject.SetActive(false);
    }

    public void LoadTutorial() {
        DisableMenuGameObject();
    }

}
