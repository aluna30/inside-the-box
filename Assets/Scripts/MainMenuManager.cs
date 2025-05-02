using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu GameObjects")]
    [SerializeField]
    private GameObject mainMenu;
    [SerializeField]
    private GameObject tutorialMain;
    [SerializeField]
    private GameObject tutorialSubmenuOne;
    [SerializeField]
    private GameObject tutorialSubmenuTwo;
    [SerializeField]
    private GameObject levelsMenu;
    [SerializeField]
    private GameObject levelsSubmenuOne;
    [SerializeField]
    private GameObject levelsSubmenuTwo;
    [SerializeField]
    private GameObject creditsMenu;

    [Header("Menu TextMeshPro Elements")]
    [SerializeField]
    private TextMeshProUGUI customLevelButtonTxt;
    [SerializeField]
    private TextMeshProUGUI customLevelErrorTxt;

    [Header("Locked Level Buttons And Text Input")]
    [SerializeField]
    private TMP_InputField customLevelSizeInput;
    [SerializeField]
    private List<Button> levelButtons;

    // Non-Serialized Fields
    private List<GameObject> navigationHistory = new List<GameObject>();
    private List<GameObject> decisionStack = new List<GameObject>();
    private GameObject activeMenu;

    public void SwichActiveMenuGroup()
    {
        GameObject activeMenuGroup = activeMenu;
        GameObject menuGroupToActive = activeMenu;
        string buttonName = EventSystem.current.currentSelectedGameObject.name;

        switch (buttonName)
        {
            case "Levels_Btn":
                activeMenuGroup = mainMenu;
                menuGroupToActive = levelsMenu;
                break;
            case "Custom_Level_Btn":
                if (decisionStack[decisionStack.Count - 1] != levelsMenu)
                    decisionStack.Add(levelsMenu);
                activeMenuGroup = levelsSubmenuOne;
                menuGroupToActive = levelsSubmenuTwo;
                break;
            case "Tutorial_Btn":
                activeMenuGroup = mainMenu;
                menuGroupToActive = tutorialMain;
                break;
            case "Tutorial_Next_Btn":
                if (decisionStack[decisionStack.Count - 1] != tutorialMain)
                    decisionStack.Add(tutorialMain);
                activeMenuGroup = tutorialSubmenuOne;
                menuGroupToActive = tutorialSubmenuTwo;
                break;
            case "Credits_Btn":
                activeMenuGroup = mainMenu;
                menuGroupToActive = creditsMenu;
                break;
            default:
                Debug.LogError("What happened? Seems like button " + buttonName + " is not recognized?");
                break;
        }

        decisionStack.Add(activeMenuGroup);
        activeMenuGroup.SetActive(false);
        menuGroupToActive.SetActive(true);
        activeMenu = menuGroupToActive;

        // Cheat Code Tracker
        if (navigationHistory.Count < 9)
        {
            navigationHistory.Add(activeMenu);
        }
        else
        {
            navigationHistory.RemoveAt(0);
            navigationHistory.Add(activeMenu);
            CheckCheatCode();
        }
    }

    public void BackButtonAction()
    {
        if (activeMenu == tutorialSubmenuOne || activeMenu == levelsSubmenuOne) // Lazy Solution...
        {
            activeMenu = decisionStack[decisionStack.Count - 1];
            decisionStack.RemoveAt(decisionStack.Count - 1);
        }

        activeMenu.SetActive(false);
        activeMenu = decisionStack[decisionStack.Count - 1];
        decisionStack.RemoveAt(decisionStack.Count - 1);
        activeMenu.SetActive(true);

    }

    public void PlayLevel(int mazeSize)
    {
        string buttonName = EventSystem.current.currentSelectedGameObject.name;

        // Set SessionData shared data and load level
        SessionData.mazeSize = mazeSize;
        SessionData.isCustomLevel = false;
        SessionData.nextLevelIdx = SessionData.levelButtons.FindIndex((button) => button.gameObject.name == buttonName) + 1;

        SceneManager.LoadScene(SceneKeys.SCENE_NAME_PUZZLE_LEVEL, LoadSceneMode.Single);
    }

    public void PlayCustomLevel(bool singleMazeLevel)
    {
        if (int.TryParse(customLevelSizeInput.text, out int mazeSize) && mazeSize > 1)
        {
            // Set SessionData shared data and load level
            SessionData.mazeSize = mazeSize;
            SessionData.isCustomLevel = true;
            SessionData.isSingleMazeLevel = singleMazeLevel;

            SceneManager.LoadScene(SceneKeys.SCENE_NAME_PUZZLE_LEVEL, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Error encountered while trying to parse provided input.");
            customLevelErrorTxt.enabled = true;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        Time.timeScale = 1.0f;
        activeMenu = mainMenu;
        SessionData.levelButtons = levelButtons;

        // Load Save Data and enable unlocked levels
        SessionData.unlockedLevelButtonsIdxs = PersistentDataManager.LoadData();
        SessionData.unlockedLevelButtonsIdxs.ForEach((idx) => { levelButtons[idx].interactable = true; });

        if (levelButtons.FindAll((button) => !button.interactable).Count == 0)
        {
            customLevelButtonTxt.text = "Custom Level";
        }
    }

    private void CheckCheatCode()
    {
        // Start fresh
        if (navigationHistory[0] == creditsMenu && navigationHistory[1] == creditsMenu
            && navigationHistory[2] == levelsMenu && navigationHistory[3] == levelsMenu
            && navigationHistory[4] == tutorialMain && navigationHistory[5] == tutorialMain
            && navigationHistory[6] == creditsMenu && navigationHistory[7] == levelsMenu && navigationHistory[8] == tutorialMain)
        {
            PersistentDataManager.DeleteData();

            SessionData.unlockedLevelButtonsIdxs.Clear();
            SessionData.unlockedLevelButtonsIdxs.AddRange(new List<int>() { 0 });

            List<int> lockedLevels = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            lockedLevels.ForEach((idx) => { levelButtons[idx].interactable = false; });
            
            customLevelButtonTxt.text = (levelButtons.FindAll((button) => !button.interactable).Count == 0) ? "Custom Level" : "?";
        }
        // Unlock all levels
        else if (navigationHistory[0] == tutorialMain && navigationHistory[1] == tutorialMain
            && navigationHistory[2] == levelsMenu && navigationHistory[3] == levelsMenu
            && navigationHistory[4] == creditsMenu && navigationHistory[5] == creditsMenu
            && navigationHistory[6] == tutorialMain && navigationHistory[7] == levelsMenu && navigationHistory[8] == creditsMenu)
        {
            SessionData.unlockedLevelButtonsIdxs.Clear();
            SessionData.unlockedLevelButtonsIdxs.AddRange(new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            PersistentDataManager.SaveData(SessionData.unlockedLevelButtonsIdxs);

            SessionData.unlockedLevelButtonsIdxs.ForEach((idx) => { levelButtons[idx].interactable = true; });
            if (levelButtons.FindAll((button) => !button.interactable).Count == 0)
            {
                customLevelButtonTxt.text = "Custom Level";
            }
        }
    }
}
