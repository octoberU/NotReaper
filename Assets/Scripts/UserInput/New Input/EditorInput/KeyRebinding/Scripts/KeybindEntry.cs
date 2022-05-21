using NotReaper.CustomComposites;
using NotReaper.Keyboard;
using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.UserInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NotReaper.Keybinds
{
    public class KeybindEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI keybindName;
        [SerializeField] private GameObject pressAnykeyLabel;
        [SerializeField] private Image keyImage;
        [SerializeField] private Image modifier1Image;
        [SerializeField] private Image modifier2Image;
        [SerializeField] private GameObject modifierPlus;
        [SerializeField] private Button button;

        [Space, Header("Tint")]
        [SerializeField] private Tint nameTint;
        [SerializeField] private Tint modifierTint;
        [SerializeField] private Tint keybindTint;

        private InputAction action;
        private InputIcons icons;
        private Action<InputActionAsset> onSaveCallback;

        private Action<bool> onRebindCallback;

        private int bindingIndex;
        private InputBinding previousBinding;
        private KeybindManager.KeybindOverrides overrides;

        private KeybindManager.Global.Modifiers modifier1Key, modifier2Key;

        private string modifier1Path, modifier2Path;
        private string mapName;

        private TextMeshProUGUI pressAnykeyLabelText;
        private string keybindLabel = "";
        private bool useLabel => keybindLabel.Length > 0;

        private List<string> controlsToExclude = new List<string>()
        {
            "/mouse/position",
            "/mouse/scroll",
            "/mouse/leftButton",
            "/mouse/rightButton",
            "/mouse/middleButton",
            "/mouse/press",
            "/pointer",
            "/keyboard/anyKey",
            "/keyboard/leftCtrl",
            "/keyboard/rightCtrl",
            "/keyboard/leftAlt",
            "/keyboard/rightAlt",
            "/keyboard/leftShift",
            "/keyboard/rightShift",
            "/keyboard/ctrl",
            "/keyboard/shift",
            "/keyboard/alt",
            "/keyboard/control"
        };

        internal void Initialize(InputIcons icons, InputAction action, string keybindName, string mapName, bool rebindable, KeybindManager.KeybindOverrides overrides, TargetHandType hand, Action<InputActionAsset> onSaveCallback, Action<bool> enableScrollCallback)
        {
            this.icons = icons;
            this.action = action;
            this.onSaveCallback = onSaveCallback;
            this.onRebindCallback = enableScrollCallback;
            this.mapName = mapName;
            modifier1Image.gameObject.SetActive(false);
            modifier2Image.gameObject.SetActive(false);
            modifierPlus.gameObject.SetActive(false);

            pressAnykeyLabelText = pressAnykeyLabel.GetComponent<TextMeshProUGUI>();

            this.keybindName.text = keybindName;
            button.interactable = rebindable;
            this.overrides = overrides;

            nameTint.SetTint(hand);
            modifierTint.SetTint(hand);
            keybindTint.SetTint(hand);
        }

        public void SetFirstModifier(InputBinding modifier)
        {
            modifier1Image.gameObject.SetActive(true);
            modifier1Image.sprite = icons.GetIcon(modifier.effectivePath, out modifier1Key, out _);
            modifier1Path = modifier.effectivePath;
        }

        public void SetSecondModifier(InputBinding modifier)
        {
            modifierPlus.SetActive(true);
            modifier2Image.gameObject.SetActive(true);
            modifier2Image.sprite = icons.GetIcon(modifier.effectivePath, out modifier2Key, out _);
            modifier2Path = modifier.effectivePath;
        }

        public void SetKeybind(InputBinding keybind)
        {
            keyImage.sprite = icons.GetIcon(keybind.effectivePath, out _, out bool hasIcon);
            if (!hasIcon)
            {
                UseLabel(keybind.effectivePath);
            }
            else
            {
                UseIcon();
            }
        }

        public void UpdateUI()
        {
            keyImage.sprite = icons.GetIcon(action.bindings[bindingIndex].effectivePath, out _, out bool hasIcon);
            if (!hasIcon)
            {
               UseLabel(action.bindings[bindingIndex].effectivePath);
            }
            else
            {
                UseIcon();
            }
            
            ShortcutKeyboardHandler.Instance.UpdateKey(keybindName.text, mapName, action.bindings[bindingIndex].effectivePath, modifier1Key, modifier2Key);
            
        }

        private void UseLabel(string keybindName)
        {
            keybindName = keybindName.Replace("<Keyboard>/", "");
            keybindName = keybindName.Replace("<Mouse>/", "(Mouse) ");
            if (keybindName.StartsWith("#("))
            {
                keybindName = keybindName.Replace("#(", "");
                keybindName = keybindName.Replace(")", "");
            }
            keyImage.gameObject.SetActive(false);
            pressAnykeyLabel.SetActive(true);
            pressAnykeyLabelText.text = keybindName;
            keybindLabel = keybindName;
        }

        private void UseIcon()
        {
            pressAnykeyLabel.SetActive(false);
            keyImage.gameObject.SetActive(true);
            keybindLabel = "";
        }

        public void SetBindingIndex(int bindingIndex)
        {
            this.bindingIndex = bindingIndex;
            pressAnykeyLabel.SetActive(false);
            UpdateUI();
        }

        public void OnClick()
        {
            var rebind = action.PerformInteractiveRebinding();

            //foreach (var exclude in controlsToExclude) 
            // rebind.WithControlsExcluding(exclude);
            onRebindCallback.Invoke(true);
            previousBinding = action.bindings[bindingIndex];
            pressAnykeyLabelText.text = "press any key..";
            pressAnykeyLabel.SetActive(true);
            keyImage.gameObject.SetActive(false);
            rebind.OnComplete(OnFinishedRebind);
            //rebind.OnCancel(OnFinishedRebind);
            rebind.OnMatchWaitForAnother(.1f);
            //rebind.WithCancelingThrough("<Keyboard>/escape");
            rebind.WithTargetBinding(bindingIndex);
            rebind.Start();
            button.interactable = false;
        }

        private void OnFinishedRebind(InputActionRebindingExtensions.RebindingOperation obj)
        {
            ///There is currently a bug where <Keyboard>escape</Keyboard> would be interpreted as both "escape" and "e".
            ///The same is true for every othere exclude or cancel key - <Keyboard>leftShift</Keyboard> => "leftShift" and "l".
            ///This is why we don't set up exclude or cancel keys, and manually check if one of those keys was pressed instead.
            ///Should apparently be fixed in upcoming 1.4
            #region Manual Exclude
            bool isExcludedKey = false;
            foreach (var candidate in obj.candidates)
            {
                if (candidate.path.ToLower() == "/keyboard/anykey") continue;
                foreach (var exclude in controlsToExclude)
                {
                    if (candidate.path.ToLower() == exclude.ToLower())
                    {
                        isExcludedKey = true;
                        break;
                    }

                }
                if (isExcludedKey) break;
            }
            if (isExcludedKey)
            {
                action.ApplyBindingOverride(bindingIndex, previousBinding);
                obj.Dispose();
                OnClick();
                return;
            }
            #endregion

            #region Manual Cancel
            bool canceled = obj.canceled;
            if (!canceled)
            {
                foreach(var candidate in obj.candidates)
                {
                    if(candidate.path.ToLower() == "/keyboard/escape")
                    {
                        canceled = true;
                        break;
                    }
                }
            }
            if (canceled)
            {
                action.ApplyBindingOverride(bindingIndex, previousBinding);
            }
            #endregion
            else
            {
                if (HasDoubleBindings(out InputBinding doubledBinding))
                {
                    NotificationCenter.SendNotification($"{action.bindings[bindingIndex].ToDisplayString()} is already used by {doubledBinding.action}", NotificationType.Error, true);
                    action.RemoveBindingOverride(bindingIndex);
                    obj.Dispose();
                    OnClick();
                    return;
                }
            }
            if (useLabel)
            {
                pressAnykeyLabelText.text = keybindLabel;
            }
            else
            {
                pressAnykeyLabel.SetActive(false);
            }
            keyImage.gameObject.SetActive(true);
            UpdateUI();
            button.interactable = true;
            if (!canceled)
            {
                onSaveCallback.Invoke(action.actionMap.asset);
                onRebindCallback.Invoke(false);
            }
            obj.Dispose();
        }

        private bool HasDoubleBindings(out InputBinding doubledBinding)
        {
            var newBinding = action.bindings[bindingIndex];
            doubledBinding = new InputBinding();
            if(CheckActionMap(action.actionMap.asset.actionMaps, newBinding, ref doubledBinding))
            {
                return true;
            }
            if(CheckActionMap(KeybindManager.ConvertEnumToActionMap(overrides.maps), newBinding, ref doubledBinding))
            {
                return true;
            }
            foreach(var action in KeybindManager.ConvertStringToAction(overrides.keybinds))
            {
                if(CheckBindings(new List<InputAction>() { action }, newBinding, ref doubledBinding))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckActionMap(IEnumerable<InputActionMap> maps, InputBinding newBinding, ref InputBinding doubledBinding)
        {
            foreach (var map in maps)
            {
                if(CheckBindings(map.actions, newBinding, ref doubledBinding))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckBindings(IEnumerable<InputAction> actions, InputBinding newBinding, ref InputBinding doubledBinding)
        {
            foreach(var otherAction in actions)
            {
                //skip if current action is our new action.
                if (this.action == otherAction)
                {
                    continue;
                }

                foreach (var binding in otherAction.bindings)
                {                    
                    //if the binding we check against is a composite, we can skip, since this indicates the parent binding, which doesn't actually hold a path.
                    if (binding.isComposite)
                    {
                        continue;
                    }
                    //if our new binding is not part of a composite, we can allow double binds if the binding we're checking against is part of one, and vice versa.
                    if ((!newBinding.isPartOfComposite && binding.isPartOfComposite) || (newBinding.isPartOfComposite && !binding.isPartOfComposite))
                    {
                        continue;
                    }
                    //if both are part of a composite, we have to check if they use the same modifier keys.
                    if (binding.isPartOfComposite && newBinding.isPartOfComposite)
                    {
                        if (this.action.bindings.Count != otherAction.bindings.Count)
                        {
                            //different amount of bindings means different amount of modifier keys -> skip
                            continue;
                        }
                        if (binding.name.ToLower().Contains("modifier"))
                        {
                            //we don't need to check modifier keys against our new binding, since our new binding can't ever be a modifier key.
                            continue;
                        }
                        //get list of modifiers on the binding we check against
                        List<string> modifiers = new List<string>();
                        for(int i = 1; i < otherAction.bindings.Count - 1; i++)
                        {
                            modifiers.Add(otherAction.bindings[i].effectivePath);
                        }
                        bool hasSameModifiers = true;
                        foreach (string m in modifiers)
                        {
                            //see if this action's modifier keys are present on our action
                            if(!this.action.bindings.Any(b => b.effectivePath == m))
                            {
                                hasSameModifiers = false;
                                break;
                            }
                        }
                        //skip if we don't have the same modifier keys
                        if (!hasSameModifiers) continue;

                    }
                    if (binding.effectivePath == newBinding.effectivePath)
                    {
                        doubledBinding = binding;
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
}

