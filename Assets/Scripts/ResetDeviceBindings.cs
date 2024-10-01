using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Basically a whole class/script for one button even though the other buttons are all in one script, so that only one control scheme is
/// reset by the reset all button.
/// </summary>
public class NewBehaviourScript : MonoBehaviour
{
    /// <summary>
    /// All the input actions/controls schemes.
    /// </summary>
    [SerializeField] private InputActionAsset inputActions;
    /// <summary>
    /// The control scheme that will be reset.
    /// </summary>
    [SerializeField] private string targetControlScheme;

    public void ResetControlSchemeBinding() {
        foreach (InputActionMap map in inputActions.actionMaps) {
            foreach (InputAction action in map.actions) {
                action.RemoveBindingOverride(InputBinding.MaskByGroup(targetControlScheme));
            }
        }
    }
}
