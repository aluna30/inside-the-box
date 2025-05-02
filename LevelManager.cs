using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource bgm;

    // GUI Elements
    [SerializeField]
    private TextMeshProUGUI activeUITimerText;
    [SerializeField]
    private TextMeshProUGUI pauseMenuTimerText;
    [SerializeField]
    private TextMeshProUGUI levelCompletionTimeText;
    [SerializeField]
    private GameObject activeUI;
    [SerializeField]
    private GameObject pauseMenu;
    [SerializeField]
    private GameObject levelCompleteMenu;
    [SerializeField]
    private GameObject levelFailedMenu;
    [SerializeField]
    private GameObject nextLevelButton;

    private bool GameOver = false;
    private bool GamePaused = false;
    private bool startCountdown = false;

    private float TimeToSolve = 0;
    private float timeLeftToSolve = 0;

    private PlayerController playerController;
    private PlayerControls playerInputs;

    public void StartCountdown(bool state)
    {
        startCountdown = state;
    }

    public void UIUnpause()
    {
        PauseResumeGame(new InputAction.CallbackContext());
    }

    public void NextLevel()
    {
        SessionData.mazeSize = SessionData.levelSizes[SessionData.nextLevelIdx];
        SessionData.nextLevelIdx += 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(SceneKeys.SCENE_NAME_MAIN_MENU, LoadSceneMode.Single);
    }

    public void ToggleActiveUI(bool state)
    {
        activeUI.SetActive(state);
    }

    private void Awake()
    {
        playerInputs = new PlayerControls();
    }

    private void OnEnable()
    {
        PlayerController.LevelCompleted += LevelComplete;
        playerInputs.Player.Pause.started += PauseResumeGame;

        playerInputs.Player.Pause.Enable();
    }

    private void OnDisable()
    {
        PlayerController.LevelCompleted -= LevelComplete;
        playerInputs.Player.Pause.started -= PauseResumeGame;

        playerInputs.Player.Pause.Disable();
    }

    private void Start()
    {
        playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        Time.timeScale = 1.0f;

        // Generate Maze and get time to complete from it
        TimeToSolve = SessionData.isSingleMazeLevel ? MazeManager.BuildSingleLevelCustomMaze(SessionData.mazeSize) : MazeManager.BuildMaze(SessionData.mazeSize);
        timeLeftToSolve = SessionData.isCustomLevel ? 0 : TimeToSolve;
        activeUITimerText.text = "Time Remaining: " + TimeSpan.FromSeconds(timeLeftToSolve).ToString("mm':'ss':'fff");

        playerController.StartLevel();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameOver && startCountdown && !GamePaused)
        {
            timeLeftToSolve = SessionData.isCustomLevel ? (timeLeftToSolve + Time.deltaTime) : (timeLeftToSolve - Time.deltaTime);
            if (!SessionData.isCustomLevel && timeLeftToSolve <= 0)
            {
                GameOver = true;
                timeLeftToSolve = 0;
                playerController.GameOverState(GameOver);
                activeUI.SetActive(!GameOver);
                levelFailedMenu.SetActive(GameOver);
            }

            activeUITimerText.text = (SessionData.isCustomLevel ? "Elapsed Time: " : "Time Remaining: ") + TimeSpan.FromSeconds(timeLeftToSolve).ToString("mm':'ss':'fff");
        }
    }

    // Pauses/Unpauses the Game and stops/starts progressive processes
    public void PauseResumeGame(InputAction.CallbackContext context)
    {
        // Pause Game if running
        if (!GameOver && !GamePaused && startCountdown)
        {
            GamePaused = !GamePaused;
            Time.timeScale = 0;
            bgm.volume = 0.3f;
            pauseMenuTimerText.text = (SessionData.isCustomLevel ? "Elapsed Time\n" : "Time Remaining\n") + TimeSpan.FromSeconds(timeLeftToSolve).ToString("mm':'ss':'fff");
        }
        // Resume Game if paused
        else if (!GameOver && GamePaused)
        {
            GamePaused = !GamePaused;
            Time.timeScale = 1;
            bgm.volume = 1f;
        }

        activeUI.SetActive(!GamePaused);
        pauseMenu.SetActive(GamePaused);
        playerController.GamePausedState(GamePaused);
    }

    private void LevelComplete()
    {
        float completionTime = SessionData.isCustomLevel ? timeLeftToSolve : TimeToSolve - timeLeftToSolve;

        SessionData.unlockedLevelButtonsIdxs.Add(SessionData.nextLevelIdx);
        PersistentDataManager.SaveData(SessionData.unlockedLevelButtonsIdxs);

        GameOver = true;
        levelCompletionTimeText.text = "Completion Time\n" + TimeSpan.FromSeconds(completionTime).ToString("mm':'ss':'fff");
        activeUI.SetActive(!GameOver);
        levelCompleteMenu.SetActive(GameOver);

        if (SessionData.isCustomLevel || SessionData.nextLevelIdx == SessionData.levelButtons.Count - 1)
            nextLevelButton.SetActive(false);
    }
}
