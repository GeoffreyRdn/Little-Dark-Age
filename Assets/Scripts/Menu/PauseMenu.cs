using Photon.Pun;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviourPunCallbacks
{
    [SerializeField] private string lobbyScene;
    [SerializeField] private string mainMenuScene;

    public void Resume()
    {
        Debug.Log("Resume");
        InputManager.Instance.ClosePauseMenu();
        ClosePauseMenu();
            
        Cursor.visible = false;
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
        InputManager.Instance.CloseInventory();
        InputManager.Instance.CloseShop();
        InputManager.Instance.ClosePauseMenu();

        PhotonNetwork.Destroy(PhotonNetwork.LocalPlayer.TagObject as GameObject);
        PhotonNetwork.LocalPlayer.TagObject = null;
        Destroy(InputManager.Instance.gameObject);

        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(mainMenuScene);
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

    public void ClosePauseMenu()
        => gameObject.SetActive(false);
    
    public void OpenPauseMenu()
        => gameObject.SetActive(true);
    
}
