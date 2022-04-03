using NotReaper;
using NotReaper.MenuBrowser;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Disables all keybinds when this object gets enabled. Enables standard keybinds again when this object gets disabled. Creates an input catcher zone that prevents any input to go through the object.
/// </summary>
namespace NotReaper
{
    public abstract class NRMenu : MonoBehaviour
    {
        /// /// <summary>
        /// Keybinds to exclude from being disabled from editor keybinds.
        /// </summary>
        [SerializeField] private List<InputActionReference> actionsToEnable = new List<InputActionReference>();
        [SerializeField] private List<KeybindManager.Map> mapsToEnable = new List<KeybindManager.Map>();
        /// <summary>
        /// Set to true if you want an input catcher zone to be created for you. It prevents any and all mouse clicks from going through to any UI elements behind this one.
        /// </summary>
        [SerializeField] protected bool useInputCatcher = true;
        /// <summary>
        /// Set to true if you want your window to remain active.
        /// </summary>
        [SerializeField] protected bool persistent = false;
        /// <summary>
        /// The display name for the menu in Menu Browser
        /// </summary>
        [SerializeField] protected string menuName = "";
        /// <summary>
        /// Set to false if you don't want this menu to show up in the Menu Browser.
        /// </summary>
        [SerializeField] protected bool browsable = true;
        private List<string> keybindsToEnable = new List<string>();
        private GameObject inputCatcher;
        private bool hasBeenInitialized = false;
        protected virtual void Awake()
        {
            if (useInputCatcher)
            {
                inputCatcher = Instantiate(Resources.Load<GameObject>("InputCatcher"), transform);
                inputCatcher.layer = gameObject.layer;
                var canvas = GetComponent<Canvas>();
                if (canvas != null)
                {
                    var catcherCanvas = inputCatcher.GetComponent<Canvas>();
                    catcherCanvas.sortingOrder = canvas.sortingOrder + 100;//canvas.sortingOrder - 1;                   
                }
                inputCatcher.SetActive(false);
            }
            if (browsable)
            {
                MenuRegistration.RegisterMenu(menuName, this);
            }
            keybindsToEnable.Clear();
            foreach (var key in actionsToEnable) keybindsToEnable.Add(key.action.name);
        }

        public abstract void Show();
        public abstract void Hide();
        public abstract void ShowHelp();
        /// <summary>
        /// Callback function that gets called when Esc/Start gets pressed. Use this to close your window, or leave it empty if you don't need to do anything.
        /// </summary>
        /// <param name="context">1 if ESC pressed, 0 if not.</param>
        protected abstract void OnEscPressed(InputAction.CallbackContext context);

        /// <summary>
        /// Disables all keybinds when this object gets enabled. Call this whenever this object gets activated/shown/enabled.
        /// </summary>
        protected void OnActivated()
        {
            if (!hasBeenInitialized) hasBeenInitialized = true;
            gameObject.SetActive(true);
            KeybindManager.Global.RegisterEscCallback(OnEscPressed);
            KeybindManager.EnableAsset(null, new KeybindManager.KeybindOverrides(mapsToEnable, keybindsToEnable));
            EditorState.SetIsInUI(true);
            if (useInputCatcher)
            {
                inputCatcher.transform.SetParent(null);
                inputCatcher.transform.position = Vector3.zero;
                inputCatcher.SetActive(true);
            }

        }
        /// <summary>
        /// Enables standard keybinds when this object gets disabled. Call this whenever this object gets deactivated/hidden/disabled.
        /// </summary>
        protected void OnDeactivated()
        {
            if (!hasBeenInitialized) return;

            KeybindManager.Global.UnregisterEscCallback(OnEscPressed);
            KeybindManager.DisableUIMenu();
            if (useInputCatcher)
            {
                inputCatcher.transform.SetParent(transform);
                inputCatcher.SetActive(false);
            }
            if(!persistent) gameObject.SetActive(false);
        }
        /// <summary>
        /// Get the List of actions that get enabled when this menu is activated.
        /// </summary>
        /// <returns>The list of actions.</returns>
        internal List<InputActionReference> GetEnabledActions()
        {
            return actionsToEnable;
        }
        /// <summary>
        /// Get the List of maps that get enabled when this menu is activated.
        /// </summary>
        /// <returns>The list of maps.</returns>
        internal List<KeybindManager.Map> GetEnabledMaps()
        {
            return mapsToEnable;
        }
    }
}

