using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class TankInputUser : MonoBehaviour
{
    /// <summary>
    /// The InputUser for this tank.
    /// </summary>
    public InputUser InputUser => _inputUser;

    /// <summary>
    /// The local Input Action Asset copy only binded to the right device
    /// </summary>
    public InputActionAsset ActionAsset => _localActionAsset;

    private InputUser _inputUser;
    private InputActionAsset _localActionAsset;

    private void Awake()
    {
        _localActionAsset = InputActionAsset.FromJson(InputSystem.actions.ToJson());
        SetNewInputUser(InputUser.PerformPairingWithDevice(Keyboard.current));
    }

    /// <summary>
    /// Activate the given control scheme on the Input User.
    /// </summary>
    /// <param name="name">The name of the ControlScheme to activate.</param>
    public void ActivateScheme(string name)
    {
        _inputUser.ActivateControlScheme(name);
    }

    /// <summary>
    /// Replace the input user contained in this component by the given one.
    /// </summary>
    /// <param name="user">The new InputUser.</param>
    public void SetNewInputUser(InputUser user)
    {
        if (!user.valid)
            return;

        _inputUser = user;
        _inputUser.AssociateActionsWithUser(_localActionAsset);

        // If this user have an associated controlScheme (e.g. in this project KeyboardRight or KeyboardLeft) we
        // re-activate this scheme on the input user. This is necessary as we changed the associated actions in the above
        // line, so those new action haven't had their control scheme set, and this will set it.
        if (_inputUser.controlScheme.HasValue)
            _inputUser.ActivateControlScheme(_inputUser.controlScheme.Value);
    }
}