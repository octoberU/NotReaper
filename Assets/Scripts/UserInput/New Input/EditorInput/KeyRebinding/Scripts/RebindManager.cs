using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using NotReaper.UserInput;
using NotReaper.Models;
using UnityEngine.UI;
using NotReaper.MenuBrowser;

namespace NotReaper.Keybinds
{
    public class RebindManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject rebindWindow;
        [SerializeField] private ScrollRect scroller;
        [Space, Header("Rebind References")]
        [SerializeField] private KeybindTitle keybindTitle;
        [SerializeField] private KeybindMap keybindMap;
        [SerializeField] private KeybindEntry keybindPrefab;
        [SerializeField] private Transform content;

        [NRInject] private InputIcons icons;

        private TargetHandType hand = TargetHandType.Left;
        public bool isRebinding = false;

        public static Dictionary<string, KeybindDisplayData> displayKeybindData { get; set; } = new Dictionary<string, KeybindDisplayData>();

        private void Start()
        {
            PopulateKeybindsMenu();

            transform.localPosition = Vector3.zero;
            rebindWindow.SetActive(false);
        }

        private bool LoadKeybinds(InputActionAsset asset)
        {
            try
            {
                asset.LoadBindingOverridesFromJson(PlayerPrefs.GetString($"{asset.name}_keybinds", ""));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Something went wrong when loading keybind {asset.name}: {ex.Message}");
            }

            return false;
        }

        private void SaveKeybinds(InputActionAsset asset)
        {            
            var json = asset.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString($"{asset.name}_keybinds", json);
        }

        private void OnRebind(bool isRebinding)
        {
            scroller.enabled = !isRebinding;
            this.isRebinding = isRebinding;
        }

        public void Close()
        {
            rebindWindow.SetActive(false);
        }

        public void Show()
        {
            rebindWindow.SetActive(true);
        }

        private void ResetKeybinds(InputActionMap map)
        {
            map.RemoveAllBindingOverrides();
            SaveKeybinds(map.asset);
        }

        private void ResetKeybinds(InputActionAsset asset)
        {
            asset.RemoveAllBindingOverrides();
            SaveKeybinds(asset);
        }

        public static KeybindDisplayData GetKeybindDisplayData(InputAction action)
        {
            if (displayKeybindData.ContainsKey(action.name))
            {
                return displayKeybindData[action.name];
            }
            else
            {
                KeybindDisplayData paths = new KeybindDisplayData();
                paths.keybind = action.name;
                return paths;
            } 
        }

        public static KeybindDisplayData? GetNullableKeybindDisplayData(InputAction action)
        {
            if (displayKeybindData.ContainsKey(action.name))
                return displayKeybindData[action.name];

            return null;
        }

        private void PopulateKeybindsMenu()
        {
            var sets = KeybindManager.GetRegisteredKeybinds();
            var orderedSet = sets.OrderByDescending(x => x.Value.Priority);
            foreach(var entry in orderedSet)
            {
                LoadKeybinds(entry.Key);

                var title = Instantiate(keybindTitle, content);
                var rebindOptions = entry.Value;
                title.Initialize(rebindOptions.AssetName, hand, entry.Key);
                hand = hand == TargetHandType.Left ? TargetHandType.Right : TargetHandType.Left;
                foreach(var map in entry.Key.actionMaps)
                {
                    if (rebindOptions.IsHidden(map))
                        continue;

                    KeybindMap mapTitle = null;
                    if(entry.Key.actionMaps.Count > 1)
                    {
                        mapTitle = Instantiate(keybindMap, content);
                        mapTitle.Initialize(rebindOptions.GetMapName(map), hand, map, ResetKeybinds);
                    }
                    List<KeybindEntry> entries = new List<KeybindEntry>();
                    bool hasRebindableKeys = false;
                    foreach(var keybind in map.actions)
                    {
                        if (rebindOptions.IsHidden(keybind))
                        {
                            continue;
                        }
                        
                        KeybindEntry key = Instantiate(keybindPrefab, content);
                        KeybindDisplayData paths = new KeybindDisplayData();
                        entries.Add(key);
                        bool isRebindable = rebindOptions.IsRebindable(keybind);
                        if (isRebindable) hasRebindableKeys = true;
                        string displayName = rebindOptions.GetKeybindName(keybind);
                        paths.displayName = displayName;
                        key.Initialize(icons, keybind, displayName, rebindOptions.GetMapName(map), isRebindable, rebindOptions.GetOverrides(), hand, SaveKeybinds, OnRebind);
                        bool hasModifier1 = false;
                        if (keybind.bindings[0].isComposite)
                        {
                            for(int i = 1; i < keybind.bindings.Count; i++)
                            {
                                var bind = keybind.bindings[i];
                                if (!bind.isPartOfComposite) break;
                                if (bind.name.ToLower().Contains("modifier"))
                                {
                                    if (!hasModifier1)
                                    {
                                        hasModifier1 = true;
                                        key.SetFirstModifier(bind);
                                        paths.modifier1 = bind.effectivePath;
                                    }
                                    else
                                    {
                                        key.SetSecondModifier(bind);
                                        paths.modifier2 = bind.effectivePath;
                                    }
                                }
                                else
                                {
                                    key.SetBindingIndex(i);
                                    paths.keybind = keybind.bindings[i].effectivePath;
                                }
                            }
                        }
                        else
                        {
                            key.SetBindingIndex(0);
                            paths.keybind = keybind.bindings[0].effectivePath;
                        }

                        if(!displayKeybindData.ContainsKey(keybind.name))
                        {
                            displayKeybindData.Add(keybind.name, paths);
                            MenuRegistration.RegisterKeybind(paths);
                        }
                        
                    }
                    if (hasRebindableKeys)
                    {
                        if (mapTitle == null)
                        {
                            title.SetKeys(entries, ResetKeybinds);
                        }
                        else
                        {
                            mapTitle.SetKeys(entries);
                        }
                    }             
                    hand = hand == TargetHandType.Left ? TargetHandType.Right : TargetHandType.Left;

                }
            }
        }

        public struct KeybindDisplayData
        {
            public string keybind;
            public string displayName;
            public string modifier1;
            public string modifier2;
        }
    } 
}

