using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager instance;

    private void Awake()
    {
        instance = this;
    }

    [SerializeField] ScreenTint screenTint;
    [SerializeField] CameraController cameraConfiner;
    [Tooltip("Loaded for a new game when the game starts from the main menu")]
    [SerializeField] string startScene = "MainHouse";
    string currentScene;
    AsyncOperation unload;
    AsyncOperation load;

    public string CurrentScene
    {
        get { return currentScene; }
    }

    void Start()
    {
        currentScene = SceneManager.GetActiveScene().name;

        // Coming from the main menu only Essentials is loaded, so bring in the gameplay scene.
        // In the editor a gameplay scene is usually open next to Essentials already.
        if (currentScene == gameObject.scene.name)
        {
            string sceneToLoad = startScene;
            if (SaveManager.instance != null && string.IsNullOrEmpty(SaveManager.instance.SavedScene) == false)
            {
                sceneToLoad = SaveManager.instance.SavedScene;
            }

            // set right away so SaveManager sees the player is already in the saved scene
            currentScene = sceneToLoad;
            StartCoroutine(LoadStartScene(sceneToLoad));
        }
    }

    IEnumerator LoadStartScene(string sceneName)
    {
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        cameraConfiner.UpdateBounds();
    }

    public void InitSwitchScene(string to, Vector3 targetPosition)
    {
        StartCoroutine(Transition(to, targetPosition));
    }

    IEnumerator Transition(string to, Vector3 targetPosition)
    {
        screenTint.Tint();

        yield return new WaitForSeconds(1f / screenTint.speed + 0.001f); //1 second divided by speed of tinting and small addition of time offset
        SwitchScene(to, targetPosition);

        while(load != null & unload != null)
        {
            if (load.isDone) { load = null; }
            if (unload.isDone) { unload = null; }
            yield return new WaitForSeconds(0.001f);
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(currentScene));

        cameraConfiner.UpdateBounds();
        screenTint.Untint();
    }

    public void SwitchScene(string to, Vector3 targetPosition)
    {
        load = SceneManager.LoadSceneAsync(to, LoadSceneMode.Additive);
        unload = SceneManager.UnloadSceneAsync(currentScene);
        currentScene = to;

        WarpPlayer(targetPosition);
    }

    public void WarpPlayer(Vector3 targetPosition)
    {
        Transform playerTransform = GameManager.instance.player.transform;

        CinemachineBrain currentCamera = Camera.main.GetComponent<CinemachineBrain>();

        currentCamera.ActiveVirtualCamera.OnTargetObjectWarped(
            playerTransform,
            targetPosition - playerTransform.position
            );

        playerTransform.position = new Vector3(
            targetPosition.x,
            targetPosition.y,
            playerTransform.position.z
            );
    }

}
