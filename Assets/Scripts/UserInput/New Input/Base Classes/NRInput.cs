using NotReaper.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NotReaper.UserInput
{
    /// <summary>
    /// Registers the input asset to the InputManager to automatically handle key rebinding.
    /// </summary>
    /// <typeparam name="T">The Scriptable Object created when you create your InputActionAsset.</typeparam>
    public abstract class NRInput<T> : MonoBehaviour where T : new()
    {
        protected T actions;
        private RebindConfiguration configuration;
        private InputActionAsset asset;

        /// <summary>
        /// Maps and keybinds to exclude from being disabled from editor keybinds.
        /// </summary>
        [Header("Keybinds"), SerializeField]
        private List<KeybindManager.Map> mapsToEnable = new List<KeybindManager.Map>();
        [SerializeField]
        private List<InputActionReference> actionsToEnable = new List<InputActionReference>();

        private List<string> keybindsToEnable = new List<string>();
        /// <summary>
        /// Register callbacks to all of your defined keybindings in actions here.
        /// </summary>
        protected abstract void RegisterCallbacks();

        /// <summary>
        /// Callback function that gets called when Esc/Start gets pressed. Use this when closing your window/disabling your function.
        /// </summary>
        /// <param name="context">The value of the esc key.</param>
        protected abstract void OnEscPressed(InputAction.CallbackContext context);
        /// <summary>
        /// Set rebinding and display options for your keybinds.
        /// </summary>
        /// <param name="options">Configure your options using this.</param>
        /// <param name="myKeybinds">A reference to your keybinds. It's the same as <see cref="actions"/></param>
        protected abstract void SetRebindConfiguration(ref RebindConfiguration options, T myKeybinds);

        protected virtual void Awake()
        {
            actions = new T();
            asset = ((dynamic)actions).asset;
            RegisterCallbacks();
            keybindsToEnable.Clear();
            foreach (var key in actionsToEnable) keybindsToEnable.Add(key.action.name);
            configuration = new RebindConfiguration(asset, new KeybindManager.KeybindOverrides(mapsToEnable, keybindsToEnable));
            SetRebindConfiguration(ref configuration, actions);
            KeybindManager.RegisterAsset(asset, configuration);
        }

        /// <summary>
        /// Enable this object's keybinds when it gets activated/enabled/shown.
        /// </summary>
        protected virtual void OnActivated()
        {
            KeybindManager.Global.RegisterEscCallback(OnEscPressed);
            //KeybindManager.EnableSpecificEditorKeybinds(mapsToEnable, keybindsToEnable);
            KeybindManager.EnableAsset(asset, new KeybindManager.KeybindOverrides(mapsToEnable, keybindsToEnable));
        }
        /// <summary>
        /// Disables this object's keybinds and enables standard keybinds again when this object gets deactivated/disabled/hidden.
        /// </summary>
        protected virtual void OnDeactivated()
        {
            KeybindManager.DisableAsset(asset);
            KeybindManager.Global.UnregisterEscCallback(OnEscPressed);
        }
    }
}

