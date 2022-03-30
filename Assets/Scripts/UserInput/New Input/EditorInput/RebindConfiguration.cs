using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
namespace NotReaper.UserInput
{
    public class RebindConfiguration
    {
        public string AssetName
        {
            get
            {
                return string.IsNullOrEmpty(customAssetTitle) ? RemoveKeybinds(SplitCamelCase(asset.name)) : customAssetTitle;
            }
        }
        public int Priority { get { return priority; } }



        private InputActionAsset asset;
        private bool isAssetRebindable = true;
        private int priority = 0;
        private string customAssetTitle;
        private Dictionary<InputActionMap, string> customMapTitles = new Dictionary<InputActionMap, string>();
        private Dictionary<InputAction, string> customKeybindNames = new Dictionary<InputAction, string>();
        private List<InputAction> nonRebindableKeybinds = new List<InputAction>();
        private List<InputAction> hiddenKeybinds = new List<InputAction>();
        private List<InputActionMap> hiddenMaps = new List<InputActionMap>();
        private KeybindManager.KeybindOverrides overrides = new KeybindManager.KeybindOverrides();

        public RebindConfiguration(InputActionAsset asset, KeybindManager.KeybindOverrides overrides)
        {
            this.asset = asset;
            this.overrides = overrides;
        }
        /// <summary>
        /// Sets the entire action asset rebindable.
        /// </summary>
        /// <param name="rebindable">True if you want it to be rebindable.</param>
        /// <returns></returns>
        public RebindConfiguration SetRebindable(bool rebindable)
        {
            isAssetRebindable = rebindable;
            return this;
        }
        /// <summary>
        /// Sets the priority of this action asset in the rebind menu.
        /// </summary>
        /// <param name="priority">The priority you want to give it. Higher number = higher priority.</param>
        /// <returns></returns>
        public RebindConfiguration SetPriority(int priority)
        {
            this.priority = priority;
            return this;
        }
        /// <summary>
        /// Sets a custom title for the action asset.
        /// </summary>
        /// <param name="title">The custom title.</param>
        /// <returns></returns>
        public RebindConfiguration SetAssetTitle(string title)
        {
            customAssetTitle = title;
            return this;
        }
        /// <summary>
        /// Sets a custom title for an action map.
        /// </summary>
        /// <param name="map">The map you want to set a custom title for.</param>
        /// <param name="title">The title you want to display for the map.</param>
        /// <returns></returns>
        public RebindConfiguration AddCustomMapTitle(InputActionMap map, string title)
        {
            if (!customMapTitles.ContainsKey(map))
                customMapTitles.Add(map, title);

            return this;
        }
        /// <summary>
        /// Sets custom titles for multiple action maps.
        /// </summary>
        /// <param name="customMapTitles">A dictionary of action maps and titles.</param>
        /// <returns></returns>
        public RebindConfiguration AddCustomMapTitles(Dictionary<InputActionMap, string> customMapTitles)
        {
            foreach (var entry in customMapTitles)
            {
                if (!this.customMapTitles.ContainsKey(entry.Key))
                {
                    this.customMapTitles.Add(entry.Key, entry.Value);
                }
            }
            return this;
        }
        /// <summary>
        /// Sets individual keybinds to be non-rebindable.
        /// </summary>
        /// <param name="keybinds">The keybinds you want to make non-rebindable.</param>
        /// <returns></returns>
        public RebindConfiguration AddNonRebindableKeybinds(params InputAction[] keybinds)
        {
            foreach (var keybind in keybinds)
            {
                if (!nonRebindableKeybinds.Contains(keybind))
                    nonRebindableKeybinds.Add(keybind);
            }
            return this;
        }
        /// <summary>
        /// Adds keybinds that should be hidden on the rebind menu.
        /// </summary>
        /// <param name="keybinds">The keybinds to hide.</param>
        /// <returns></returns>
        public RebindConfiguration AddHiddenKeybinds(params InputAction[] keybinds)
        {
            foreach (var keybind in keybinds)
            {
                if (!hiddenKeybinds.Contains(keybind))
                    hiddenKeybinds.Add(keybind);
            }
            return this;
        }
        /// <summary>
        /// Adds maps that should be hidden on the rebind menu.
        /// </summary>
        /// <param name="maps">The mpas to hide.</param>
        /// <returns></returns>
        public RebindConfiguration AddHiddenMaps(params InputActionMap[] maps)
        {
            foreach(var map in maps)
            {
                if (!hiddenMaps.Contains(map))
                    hiddenMaps.Add(map);
            }
            return this;
        }
        /// <summary>
        /// Sets a custom name for a keybind.
        /// </summary>
        /// <param name="keybind">The keybind you want to set a custom name for.</param>
        /// <param name="name">The name you want to display for the keybind.</param>
        /// <returns></returns>
        public RebindConfiguration AddCustomKeybindName(InputAction keybind, string name)
        {
            if (!customKeybindNames.ContainsKey(keybind))
                customKeybindNames.Add(keybind, name);

            return this;
        }
        /// <summary>
        /// Sets custom names for multiple keybinds.
        /// </summary>
        /// <param name="customNames">A dictionary of keybinds and names.</param>
        /// <returns></returns>
        public RebindConfiguration AddCustomKeybindNames(Dictionary<InputAction, string> customNames)
        {
            foreach (var entry in customNames)
            {
                if (!customKeybindNames.ContainsKey(entry.Key))
                {
                    customKeybindNames.Add(entry.Key, entry.Value);
                }
            }
            return this;
        }

        public string GetMapName(InputActionMap map)
        {
            if (customMapTitles.ContainsKey(map))
                return customMapTitles[map];
            else return map.name;
        }

        public string GetKeybindName(InputAction keybind)
        {
            if (customKeybindNames.ContainsKey(keybind))
                return customKeybindNames[keybind];
            else return SplitCamelCase(keybind.name);
        }

        public Dictionary<InputAction, string> GetKeybindDisplayNames()
        {
            return customKeybindNames;
        }

        public KeybindManager.KeybindOverrides GetOverrides()
        {
            return overrides;
        }
        public bool IsRebindable(InputAction keybind)
        {
            bool rebindable = !nonRebindableKeybinds.Contains(keybind) && isAssetRebindable;
            bool isModifier = false;
            bool isMouse = false;
            foreach (var bind in keybind.bindings)
            {
                if (bind.isPartOfComposite || bind.isComposite) break;
                string path = bind.effectivePath.ToLower();
                if (path.Contains("ctrl") || path.Contains("alt") || path.Contains("shift") || path.Contains("control")) isModifier = true;
            }
            if (keybind.bindings.Any(b => b.effectivePath.ToLower().Contains("mouse"))) isMouse = true;
            return rebindable && !isModifier && !isMouse;
        }

        public bool IsHidden(InputAction keybind)
        {
            return hiddenKeybinds.Contains(keybind);
        }

        public bool IsHidden(InputActionMap map)
        {
            return hiddenMaps.Contains(map);
        }

        private string SplitCamelCase(string str)
        {
            return Regex.Replace(
                Regex.Replace(
                    str,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                ),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }

        private string RemoveKeybinds(string str)
        {
            return str.Replace("Keybinds", "");
        }
    }

}
