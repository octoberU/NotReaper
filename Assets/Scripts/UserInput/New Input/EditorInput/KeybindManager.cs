using NotReaper;
using NotReaper.UserInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
public static class KeybindManager
{
    #region Fields
    private static List<InputActionAsset> inputAssets = new List<InputActionAsset>();
    private static Dictionary<InputActionAsset, RebindConfiguration> registeredAssets = new Dictionary<InputActionAsset, RebindConfiguration>();
    private static InputActionAsset editorKeybinds;
    private static GlobalKeybinds globalKeybinds;

    private static Dictionary<InputActionAsset, KeybindOverrides> activeAssets = new Dictionary<InputActionAsset, KeybindOverrides>();
    private static KeybindOverrides activeUIOverrides;
    private static int activeUiElements = 0;
    #endregion

    #region Events
    public delegate void OnCtrlDown();
    public static event OnCtrlDown onCtrlDown;

    public delegate void OnCtrlUp();
    public static event OnCtrlUp onCtrlUp;

    public delegate void OnShiftDown();
    public static event OnShiftDown onShiftDown;

    public delegate void OnAltDown();
    public static event OnAltDown onAltDown;

    public delegate void OnMouseDown(bool down);
    public static event OnMouseDown onMouseDown;

    public delegate void OnScrolled(bool forward);
    public static event OnScrolled onScrolled;

    public delegate void OnTabPressed();
    public static event OnTabPressed onTabPressed;

    public delegate void OnEnterPressed();
    public static event OnEnterPressed onEnterPressed;
    #endregion

    #region Initialization
    static KeybindManager()
    {        
        globalKeybinds = new GlobalKeybinds();
        RegisterCallbacks();
        globalKeybinds.Enable();
    }

    private static void RegisterCallbacks()
    {
        globalKeybinds.Global.Control.performed += _ => UpdateCtrl(true);
        globalKeybinds.Global.Alt.performed += _ => UpdateAlt(true);
        globalKeybinds.Global.Shift.performed += _ => UpdateShift(true);

        globalKeybinds.Global.Control.canceled += _ => UpdateCtrl(false);
        globalKeybinds.Global.Alt.canceled += _ => UpdateAlt(false);
        globalKeybinds.Global.Shift.canceled += _ => UpdateShift(false);

        globalKeybinds.Global.MouseDown.started += _ => onMouseDown?.Invoke(true);
        globalKeybinds.Global.MouseDown.canceled += _ => onMouseDown?.Invoke(false);

        globalKeybinds.Global.Scroll.performed += ctx => onScrolled?.Invoke(ctx.ReadValue<float>() > 0);

        globalKeybinds.Global.Tab.started += _ => onTabPressed?.Invoke();

        globalKeybinds.Global.Enter.started += _ => onEnterPressed?.Invoke();
    } 

    private static void UpdateCtrl(bool down)
    {
        if (down)
        {
            SetFlag(Global.Modifiers.Ctrl);
            onCtrlDown?.Invoke();
        }
        else
        {
            RemoveFlag(Global.Modifiers.Ctrl);
            onCtrlUp?.Invoke();
        }
    }
    private static void UpdateShift(bool down)
    {
        if (down)
        {
            SetFlag(Global.Modifiers.Shift);
            onShiftDown?.Invoke();
        }
        else
        {
            RemoveFlag(Global.Modifiers.Shift);
        }
    }
    private static void UpdateAlt(bool down)
    {
        if (down)
        {
            SetFlag(Global.Modifiers.Alt);
            onAltDown?.Invoke();
        }
        else
        {
            RemoveFlag(Global.Modifiers.Alt);
        }
    }

    private static void SetFlag(Global.Modifiers flag)
    {
        Global.Modifier |= flag;
    }
    private static void RemoveFlag(Global.Modifiers flag)
    {
        Global.Modifier &= ~flag;
    }
    /// <summary>
    /// Set the Standard Editor-Keybind asset.
    /// </summary>
    /// <param name="standard">The standard keybind asset.</param>
    public static void SetStandardKeybinds(InputActionAsset standard)
    {
        editorKeybinds = standard;
    }
    /// <summary>
    /// Register an InputActionAsset so it can be handled for keybind remapping.
    /// </summary>
    /// <param name="asset">The asset to register.</param>
    public static void RegisterAsset(InputActionAsset asset, RebindConfiguration options)
    {
        if (!registeredAssets.ContainsKey(asset))
        {
            registeredAssets.Add(asset, options);
        }
    }
    #endregion

    #region Enable/Disable Assets
    /// <summary>
    /// Enables the asset and applies it's overrides while disabling all other keybinds.
    /// </summary>
    /// <param name="asset">The asset to enable.</param>
    /// <param name="overrides">The keybind overrides to apply.</param>
    public static void EnableAsset(InputActionAsset asset, KeybindOverrides overrides)
    {
        if (overrides == null) overrides = new();
        if (overrides.maps == null) overrides.maps = new List<Map>();
        if (overrides.keybinds == null) overrides.keybinds = new List<string>();
        if(asset != null)
        {
            if (activeAssets.Count > 0) 
                DisableKeybinds(activeAssets.Last().Value.keybinds);

            asset.Enable();
            if (!activeAssets.ContainsKey(asset))
            {
                activeAssets.Add(asset, overrides);
            }
        }
        else
        {
            activeUIOverrides = overrides;
            activeUiElements++;
            if (activeAssets.Count > 0)
            {
                var lastAsset = activeAssets.Last();
                DisableKeybinds(lastAsset.Value.keybinds);
                lastAsset.Key.Disable();
                //DisableAsset(activeAssets.Last().Key);
                //DisableKeybinds(activeAssets.Last().Value.keybinds);
            }
        }
        ApplyOverrides(overrides);
    }
    /// <summary>
    /// Disables the asset and enables the previously active asset.
    /// </summary>
    /// <param name="asset">The asset to disable.</param>
    public static void DisableAsset(InputActionAsset asset)
    {
        if(asset != null)
        {
            asset.Disable();

            if (activeAssets.ContainsKey(asset))
            {
                if(activeAssets.Count > 1 || activeUiElements > 0)
                {
                    ///we only disable the asset's override keybinds if there are other active assets in the list. 
                    ///If we don't, it re-enables those override keybinds while they're being disabled, resulting in
                    ///an error.
                    DisableKeybinds(activeAssets[asset].keybinds);
                }
                activeAssets.Remove(asset);
            }
        }

        if(activeAssets.Count > 0)
        {
            ApplyOverrides(activeAssets.Last().Value);
        }
        else
        {         
            if(activeUiElements == 0)
            {
                EnableEditorKeybinds();
            }
            else
            {
                ApplyOverrides(activeUIOverrides);
            }
        }
    }
    /// <summary>
    /// Adds + 1 to active UI elements without interfering with keybinds.
    /// </summary>
    public static void EnableUIMenu() => activeUiElements++;

    /// <summary>
    /// Call this if your object is a UI element without it's own keybinds.
    /// </summary>
    public static void DisableUIMenu()
    {
        if (activeUiElements == 0) return;
        activeUiElements--;
        if (activeUiElements == 0)
        {
            activeUIOverrides = null;
            EditorState.SetIsInUI(false);
            if(activeAssets.Count > 0)
            {
                var lastAsset = activeAssets.Last();
                //lastAsset.Key.Enable();
                //ApplyOverrides(lastAsset.Value);
                EnableAsset(lastAsset.Key, lastAsset.Value);
            }
            else
            {
                EnableEditorKeybinds();           
            }
            
        }
    }
    /// <summary>
    /// Applies keybind overrides to standard keybinds.
    /// </summary>
    private static void ApplyOverrides(KeybindOverrides overrides)
    {
        if (editorKeybinds == null) return;
        if (overrides.maps == null || overrides.maps.Count == 0)
        {
            editorKeybinds.Disable();
        }
        else
        {
            var names = Enum.GetNames(typeof(Map));
            foreach (var name in names)
            {
                var m = (Map)Enum.Parse(typeof(Map), name);
                if (overrides.maps.Any(map => map == m))
                {
                    var map = editorKeybinds.FindActionMap(m.ToString());
                    if(map != null && !map.enabled)
                        map.Enable();
                }
                else
                {
                    var map = editorKeybinds.FindActionMap(m.ToString());
                    if (map != null && map.enabled)
                        map.Disable();
                }
            }
        }

        if (overrides.keybinds != null && overrides.keybinds.Count > 0)
        {
            foreach (string keybind in overrides.keybinds)
            {
                var key = editorKeybinds.FindAction(keybind);
                if (key != null && !key.enabled)
                    key.Enable();
            }
        }
    }
    #endregion

    #region Enable/Disable Editor Keybinds
    /// <summary>
    /// Enables standard keybinds.
    /// </summary>
    public static void EnableEditorKeybinds()
    {
        if (editorKeybinds != null)
        {
            editorKeybinds.Enable();
            foreach (var map in editorKeybinds.actionMaps) map.Enable();
        }
        //EnableAsset(editorKeybinds);
    }
    /// <summary>
    /// Disables standard keybinds.
    /// </summary>
    public static void DisableEditorKeybinds()
    {
        if(editorKeybinds != null)
        {
            editorKeybinds.Disable();
        }
    }

    public static void EnableKeybind(string name)
    {
        var action = editorKeybinds.FindAction(name);
        if(action != null)
        {
            if (!action.enabled)
            {
                action.Enable();
            }
        }
    }

    public static void DisableKeybind(string name)
    {
        var action = editorKeybinds.FindAction(name);
        if(action != null)
        {
            if (action.enabled)
            {
                action.Disable();
            }
        }
    }
    #endregion

    #region Enable/Disable Specific Maps/Keybinds
    /// <summary>
    /// Enables a specific map.
    /// </summary>
    /// <param name="map">The map to enable.</param>
    public static void EnableMap(Map map)
    {
        editorKeybinds.FindActionMap(map.ToString())?.Enable();
    }
    /// <summary>
    /// Disables a specific map.
    /// </summary>
    /// <param name="map">The map to disable.</param>
    public static void DisableMap(Map map)
    {
        editorKeybinds.FindActionMap(map.ToString())?.Disable();
    }
    /// <summary>
    /// Enables the given list of maps and disables all others.
    /// </summary>
    /// <param name="maps">The maps to enable.</param>
    private static void EnableMaps(List<Map> maps)
    {
        var names = Enum.GetNames(typeof(Map));
        foreach (var name in names)
        {
            var m = (Map)Enum.Parse(typeof(Map), name);
            if (maps.Any(map => map == m))
            {
                editorKeybinds.FindActionMap(m.ToString())?.Enable();
            }
            else
            {
                editorKeybinds.FindActionMap(m.ToString())?.Disable();
            }
        }
    }
    /// <summary>
    /// Enables the given list of keybinds.
    /// </summary>
    /// <param name="keybinds">The keybinds to enable.</param>
    private static void EnableKeybinds(List<string> keybinds)
    {
        foreach (string keybind in keybinds)
        {
            editorKeybinds.FindAction(keybind)?.Enable();
        }
    }
    /// <summary>
    /// Disables the given list of keybinds.
    /// </summary>
    /// <param name="keybinds">The keybinds to disable.</param>
    private static void DisableKeybinds(List<string> keybinds)
    {
        foreach(string keybind in keybinds)
        {
            editorKeybinds.FindAction(keybind)?.Disable();
        }
    }
    #endregion

    #region Getter
    public static Dictionary<InputActionAsset, RebindConfiguration> GetRegisteredKeybinds()
    {
        return registeredAssets;
    }

    public static List<InputActionMap> ConvertEnumToActionMap(List<Map> maps)
    {
        List<InputActionMap> foundMaps = new List<InputActionMap>();

        foreach(var map in maps)
        {
            var m = editorKeybinds.FindActionMap(map.ToString());
            if (m != null) foundMaps.Add(m);
        }

        return foundMaps;
    }
    public static List<InputAction> ConvertStringToAction(List<string> actions)
    {
        List<InputAction> foundActions = new List<InputAction>();
        foreach(var action in actions)
        {
            var a = editorKeybinds.FindAction(action);
            if (a != null) foundActions.Add(a);
        }
        return foundActions;
    }

    public static List<string> GetBindingPaths(InputAction action)
    {
        List<string> paths = new List<string>();
        foreach(var binding in action.bindings)
        {
            paths.Add(binding.effectivePath);
        }
        return paths;
    }
    #endregion

    #region Classes and Enums
    /// <summary>
    /// The list of available InputActionMaps in EditorKeybinds.
    /// </summary>
    /// <remarks>Has to be updated manually. CAREFUL WHEN ADDING NEW MAPS - Append them to the end of this list. 
    /// If you add them somewhere in the middle, or delete an existing one, all scripts defining keybind overrides will have to re-set appropriate maps.</remarks>
    public enum Map
    {
        Mapping = 0,
        Utility = 1,
        Menus = 2,
        Timeline = 3,
        DragSelect = 4,
        Grid = 5,
        BPM = 6,
        SpacingSnap = 7,
        Pathbuilder = 8,
        Modifiers = 9,
        HitsoundSelect = 10,
        HitsoundConvert = 11,
        BehaviorSelect = 12,
        BehaviorConvert = 13
    }
    /// <summary>
    /// Describes keybinds that should stay enabled when a new asset gets enabled.
    /// </summary>
    public class KeybindOverrides
    {
        public List<Map> maps;
        public List<string> keybinds;

        public KeybindOverrides()
        {
            maps = new List<Map>();
            keybinds = new List<string>();
        }

        public KeybindOverrides(List<Map> maps, List<string> keybinds)
        {
            this.maps = maps;
            this.keybinds = keybinds;
        }
    }

    /// <summary>
    /// Describes common, global keybinds that are always active and accessible.
    /// </summary>
    public class Global
    {
     
        public static Modifiers Modifier;

        public static InputAction MousePosition
        {
            get { return globalKeybinds.Global.MousePosition; }
        }

        public static void RegisterEscCallback(Action<InputAction.CallbackContext> callback)
        {
            globalKeybinds.Global.Close.performed += callback;
        }
        public static void UnregisterEscCallback(Action<InputAction.CallbackContext> callback)
        {
            globalKeybinds.Global.Close.performed -= callback;
        }

        [Flags]
        public enum Modifiers
        {
            None = 0,
            Ctrl = 1,
            Alt = 2,
            Shift = 4,

            CtrlShift = Ctrl | Shift,
            CtrlAlt = Ctrl | Alt,
            ShiftAlt = Alt | Shift,
            All = Ctrl | Alt | Shift
        }
       
    }
    #endregion
}

#region Modifier Extenstions
static class ModifiersExtension
{
    public static bool IsShiftDown(this KeybindManager.Global.Modifiers modifier)
    {
        return (modifier & KeybindManager.Global.Modifiers.Shift) == KeybindManager.Global.Modifiers.Shift;
    }
    public static bool IsCtrlDown(this KeybindManager.Global.Modifiers modifier)
    {
        return (modifier & KeybindManager.Global.Modifiers.Ctrl) == KeybindManager.Global.Modifiers.Ctrl;
    }
    public static bool IsAltDown(this KeybindManager.Global.Modifiers modifier)
    {
        return (modifier & KeybindManager.Global.Modifiers.Alt) == KeybindManager.Global.Modifiers.Alt;
    }
    public static bool IsCtrlShiftDown(this KeybindManager.Global.Modifiers modifier)
    {
        return modifier == KeybindManager.Global.Modifiers.CtrlShift;
    }
    public static bool IsCtrlAltDown(this KeybindManager.Global.Modifiers modifier)
    {
        return modifier == KeybindManager.Global.Modifiers.CtrlAlt;
    }
    public static bool IsShiftAltDown(this KeybindManager.Global.Modifiers modifier)
    {
        return modifier == KeybindManager.Global.Modifiers.ShiftAlt;
    }
}
#endregion
