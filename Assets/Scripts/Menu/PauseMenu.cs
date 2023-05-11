using Photon.Pun;
using Photon.Realtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviourPunCallbacks
{
    [SerializeField] private string lobbyScene;
    [SerializeField] private string mainMenuScene;
    [SerializeField] private GameObject lobbyGO;

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient) lobbyGO.SetActive(true);
        else lobbyGO.SetActive(false);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        if (PhotonNetwork.IsMasterClient) lobbyGO.SetActive(true);
        else lobbyGO.SetActive(false);
        
    }

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

        Cursor.visible = true;

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

    private void ClosePauseMenu()
        => gameObject.SetActive(false);

}
