using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This class is responsible for controlling the pause menu, including the buttons, the text, and the controls.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>
    /// The character physics that will be paused when the pause menu is open.
    /// </summary>
    public CharacterPhysics characterPhysics;

    #region Components of the base Pause Menu
    /// <summary>
    /// The text that will be displayed when the pause menu is open.
    /// </summary>
    public TextMeshProUGUI pauseText;
    /// <summary>
    /// The button that will quit the game.
    /// </summary>
    public GameObject quitButton;
    /// <summary>
    /// The button that will resume the game.
    /// </summary>
    public GameObject resumeButton;
    /// <summary>
    /// The button that will open the controls menu.
    /// </summary>
    public GameObject controlsButton;
    /// <summary>
    /// The button that will open the keyboard controls menu.
    /// </summary>
    public GameObject keyboardButton;
    /// <summary>
    /// The controls that will be displayed when the controls menu is open.
    /// </summary>
    #endregion

    #region Sub Menus    
    /// <summary>
    /// The controller controls menu, and components that will be displayed when this controls menu is open.
    /// </summary>
    public GameObject controllerControls;
    /// <summary>
    /// The keyboard controls menu, and components that will be displayed when this controls menu is open.
    /// </summary>
    public GameObject keyboardControls;
    #endregion

    /// <summary>
    /// The first button that will be selected when the pause menu is opened.
    /// </summary>
    [Header("First Selected Buttons")]
    /// <summary>
    /// The first button that will be selected when the base pause menu is opened.
    /// </summary>
    [SerializeField] private GameObject firstMenuButton;
    /// <summary>
    /// The first button that will be selected when the controller controls menu is opened.
    /// </summary>
    [SerializeField] private GameObject firstControlsButton;
    /// <summary>
    /// The first button that will be selected when the keyboard controls menu is opened.
    /// </summary>
    [SerializeField] private GameObject firstKeyboardButton;

    /// <summary>
    /// The ID of the menu that is currently open. Currently this is vestigial! But it feels like a good idea to have a variable tracking
    /// the menu ID, so I'm keeping it in for now.
    /// </summary>
    private int menuID = 0;

    /// <summary>
    /// Opens to the base pause menu.
    /// </summary>
    public void OpenMenu() {
        menuID = 0;
        SetMenuActive(menuID);
    }

    /// <summary>
    /// Sets the menu active based on the ID of the menu that is passed in.
    /// </summary>
    /// <param name="ID">The ID of the sub menu to be opened.</param>
    public void SetMenuActive(int ID)
    {
        if (ID == 0) {
            // the base pause menu components
            pauseText.gameObject.SetActive(true);
            quitButton.SetActive(true);
            resumeButton.SetActive(true);
            controlsButton.SetActive(true);
            keyboardButton.SetActive(true);
            // sub menus
            controllerControls.SetActive(false);
            keyboardControls.SetActive(false);
            // resets the selected button to the first button in the base pause menu
            EventSystem.current.SetSelectedGameObject(firstMenuButton);
        } else if (ID == 1) {
            // the base pause menu components
            pauseText.gameObject.SetActive(false);
            quitButton.SetActive(false);
            resumeButton.SetActive(false);
            controlsButton.SetActive(false);
            keyboardButton.SetActive(false);
            // sub menus
            controllerControls.SetActive(true);
            keyboardControls.SetActive(false);
            // resets the selected button to the first button in the base pause menu
            EventSystem.current.SetSelectedGameObject(firstControlsButton);
        } else {
            // the base pause menu components
            pauseText.gameObject.SetActive(false);
            quitButton.SetActive(false);
            resumeButton.SetActive(false);
            controlsButton.SetActive(false);
            keyboardButton.SetActive(false);
            // sub menus
            controllerControls.SetActive(false);
            keyboardControls.SetActive(true);
            // resets the selected button to the first button in the base pause menu
            EventSystem.current.SetSelectedGameObject(firstKeyboardButton);
        }
    }

    /// <summary>
    /// Closes the pause menu.
    /// </summary>
    public void CloseMenu() {
        // characterPhysics.Unpause();
        pauseText.gameObject.SetActive(false);
        quitButton.SetActive(false);
        resumeButton.SetActive(false);
        controlsButton.SetActive(false);
        keyboardButton.SetActive(false);
        controllerControls.SetActive(false);
        keyboardControls.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null); 
    }

    #region Button Functions That Could Be Replaced By One Function That Takes In The ID As A Parameter
    /// <summary>
    /// Opens the controller controls menu.
    /// </summary>
    public void OpenControls() {
        menuID = 1;
        SetMenuActive(menuID);
    }

    /// <summary>
    /// Opens the keyboard controls menu.
    /// </summary>
    public void OpenKeyboardControls() {
        menuID = 2;
        SetMenuActive(menuID);
    }

    /// <summary> 
    /// Closes the controls menu, goes back to main pause menu.
    /// </summary>
    public void BackToPauseMenu() {
        menuID = 0;
        SetMenuActive(menuID);
    }
    #endregion

    /// <summary>
    /// Quits the game.
    /// </summary>
    public void QuitGame() {
        Application.Quit();
    }
}
