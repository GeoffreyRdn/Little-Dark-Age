using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.IO;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private string lobbyScene;
    [SerializeField] private string dungeonScene;
    [SerializeField] private string playerManagerPath;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name == lobbyScene || scene.name == dungeonScene)
        {
            PhotonNetwork.Instantiate(playerManagerPath, Vector3.zero, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}