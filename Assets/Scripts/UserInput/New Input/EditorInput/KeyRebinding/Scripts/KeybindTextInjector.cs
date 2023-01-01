using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace NotReaper.Keybinds
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class KeybindTextInjector : MonoBehaviour
    {
        [InfoBox("Add index of the keybind above to the text boxin squiggly brackets ({0}).")]
        [SerializeField] private List<InputActionReference> inputs = new();

        private TextMeshProUGUI textField;

        private const string StartColorTag = "<color=#FDA50F>";
        private const string CloseColorTag = "</color>";

        private void Awake()
        {
            textField = GetComponent<TextMeshProUGUI>();
            ParseText();
        }

        public void ParseText()
        {
            string text = textField.text;
            for (int i = 0; i < inputs.Count; i++)
                text = text.Replace($"{{{i}}}", $"{StartColorTag}{ParseInput(inputs[i].action.bindings)}{CloseColorTag}");
            
            
            textField.SetText(text);
        }

        private string ParseInput(ReadOnlyArray<InputBinding> bindings)
        {
            string keys = string.Empty;
            bool lastKeyWasStatic = false;
            foreach (var binding in bindings)
            {
                if (staticKeys.TryGetValue(binding.effectivePath, out var key))
                {
                    if (lastKeyWasStatic)
                        keys += " + ";
                    
                    keys += key;
                    lastKeyWasStatic = true;
                }
                else if (binding.effectivePath.StartsWith(MouseTag))
                {
                    return keys;
                }
                else if (binding.effectivePath.StartsWith(KeyboardTag))
                {
                    if (lastKeyWasStatic)
                    {
                        keys += " + ";
                        lastKeyWasStatic = false;
                    }

                    key = Sanitize(binding.effectivePath.Substring(binding.effectivePath.IndexOf('/') + 1));
                    keys += $"{key.SplitPascalCase().ToLower()}";
                }
            }
            return keys;
        }

        private string Sanitize(string input)
            => Regex.Replace(input, @"[#()\s]", string.Empty);

        private const string MouseTag = "<Mouse>";
        private const string KeyboardTag = "<Keyboard>";

        private static Dictionary<string, string> staticKeys = new()
        {
            { "<Mouse>/leftButton", "left click" },
            { "<Mouse>/rightButton", "right click" },
            { "<Mouse>/middleButton", "mouse wheel click" },
            { "<Mouse>/forwardButton", "mouse forward" },
            { "<Mouse>/backButton", "mouse back" },
            { "<Keyboard>/shift", "shift" },
            { "<Keyboard>/ctrl", "control" },
            { "<Keyboard>/alt", "alt" },
            { "<Mouse>/scroll/y", "scroll" },
        };
    }
}
