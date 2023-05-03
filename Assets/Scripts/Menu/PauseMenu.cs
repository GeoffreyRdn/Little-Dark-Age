using Photon.Pun;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private string lobbyScene;

    public void Resume()
    {
        Debug.Log("Resume");
        InputManager.Instance.ClosePauseMenu();
        OpenOrClosePauseMenu();
            
        Cursor.visible = true;
    }

    public void ReturnToLobby()
    {
        Debug.Log("Return to lobby");
        if (PhotonNetwork.IsMasterClient && SceneManager.GetActiveScene().name != lobbyScene)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;
            PhotonNetwork.LoadLevel(lobbyScene);
            Resume();
        }
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("Return to main menu");
    }

    public void ExitGame()
    {
        Debug.Log("Exiting ...");

        
        Application.Quit();
        
        #if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
        #endif
    }

    public void OpenOrClosePauseMenu()
    {
        Debug.Log("Opening Pause Menu -> " + !gameObject.activeInHierarchy);
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
}
