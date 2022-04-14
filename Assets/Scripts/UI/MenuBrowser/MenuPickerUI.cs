using DG.Tweening;
using NotReaper.Keybinds;
using NotReaper.Models;
using NotReaper.Overlays;
using NotReaper.UI;
using NotReaper.UI.Components;
using NotReaper.UserInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static NotReaper.Keybinds.RebindManager;

namespace NotReaper.MenuBrowser
{
    public class MenuPickerUI : NRMenu
    {
        [Header("References")]
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private NRButton menuEntryPrefab;
        [SerializeField] private KeybindEntry keybindEntryPrefab;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private CanvasScaler scaler;
        [Space, Header("Parents")]
        [SerializeField] private GameObject menuParent;
        [SerializeField] private GameObject keybindParent;

        private Dictionary<string, GameObject> menuEntries = new();
        private Dictionary<string, List<KeybindEntry>> keybindEntries = new();

        private View activeView = View.Menu;

        private InputIcons icons;
        public static MenuPickerUI Instance { get; private set; } = null;
        private bool initialized;
        protected override void Awake()
        {
            if (Instance != null)
            {
                Debug.Log("MenuBrowser already exists.");
                return;
            }
            Instance = this;
            base.Awake();
        }

        private void Start()
        {
            icons = NRDependencyInjector.Get<InputIcons>();
            EditorFile.onAudicaFileLoaded += OnAudicaLoaded;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            menuParent.SetActive(true);
            keybindParent.SetActive(false);
            gameObject.SetActive(false);
        }

        private void OnAudicaLoaded(AudicaFile _)
        {
            if (initialized) return;

            initialized = true;
            var entries = new Dictionary<string, MonoBehaviour>();
            foreach(var menu in MenuRegistration.menuEntries)
            {
                entries.Add(menu.Key, menu.Value);
            }
            foreach(var overlay in MenuRegistration.overlayEntries)
            {
                entries.Add(overlay.Key, overlay.Value);
            }
            foreach(var entry in entries.OrderBy(entry => entry.Key))
            {
                if (entry.Value.GetType().BaseType == typeof(NROverlay))
                {
                    CreateOverlayEntry(entry.Key, entry.Value as NROverlay);
                }
                else
                {
                    CreateMenuEntry(entry.Key, entry.Value as NRMenu);
                } 
            }
            foreach(var entry in MenuRegistration.keybindEntries)
            {
                CreateKeybindEntry(entry);
            }
        }

        private void CreateKeybindEntry(KeybindDisplayData data)
        {
            var keybind = Instantiate(keybindEntryPrefab, keybindParent.transform);
            keybind.Initialize(icons, null, data.displayName, "", false, null, Models.TargetHandType.Either, null, null);
            keybind.SetKeybind(new(data.keybind));
            if (!string.IsNullOrEmpty(data.modifier1)) keybind.SetFirstModifier(new(data.modifier1));
            if (!string.IsNullOrEmpty(data.modifier2)) keybind.SetSecondModifier(new(data.modifier2));
            if (keybindEntries.ContainsKey(data.displayName))
            {
                keybindEntries[data.displayName].Add(keybind);
            }
            else
            {
                keybindEntries.Add(data.displayName, new List<KeybindEntry>() { keybind });
            }
        }

        private void CreateMenuEntry(string name, NRMenu menu)
        {
            if (name.ToLower().Contains("downmap"))
            {
                if (!PlayerPrefs.HasKey("l_diffs"))
                {
                    return;
                }

                if (PlayerPrefs.GetInt("l_diffs") != 1)
                {
                    return;
                }
            }
            var button = Instantiate(menuEntryPrefab, menuParent.transform);
            button.SetText(name);
            UnityAction listener = new UnityAction(() =>
            {
                Hide();
                menu.Show();
            });
            button.UpdateVisuals();
            button.onClick.AddListener(listener);
            menuEntries.Add(name, button.gameObject);
        }
        private void CreateOverlayEntry(string name, NROverlay overlay)
        {
            var button = Instantiate(menuEntryPrefab, menuParent.transform);
            button.SetText(name);
            UnityAction listener = new UnityAction(() =>
            {
                Hide();
                overlay.Show();
            });
            button.UpdateVisuals();
            button.onClick.AddListener(listener);
            menuEntries.Add(name, button.gameObject);
        }


        public void OnMenusClicked()
        {
            ChangeView(true);
        }

        public void OnKeybindsClicked()
        {
            ChangeView(false);
        }

        private void ChangeView(bool menuView)
        {
            searchInput.SetTextWithoutNotify("");
            menuParent.SetActive(menuView);
            keybindParent.SetActive(!menuView);
            activeView = menuView ? View.Menu : View.Keybind;
            SetEntriesActive("");
        }

        private void SetEntriesActive(string search)
        {
            if(activeView == View.Menu)
            {
                foreach(var entry in menuEntries)
                {

                    entry.Value.gameObject.SetActive(entry.Key.ToLower().Contains(search.ToLower()));
                }
            }
            else
            {
                foreach(var entry in keybindEntries)
                {
                    foreach(var keybind in entry.Value)
                    {
                        keybind.gameObject.SetActive(entry.Key.ToLower().Contains(search.ToLower()));
                    }
                }
            }
        }

        public void OnSearchChanged()
        {
            SetEntriesActive(searchInput.text);
        }

        public override void Show()
        {
            OnActivated();
            canvasGroup.DOFade(1f, .3f);
        }

        public override void Hide()
        {
            canvasGroup.DOFade(0f, .3f).OnComplete(() =>
            {
                OnDeactivated();
            });
        }

        public override void ShowHelp() 
        {
            NRHelp.Instance.ShowMenuBrowser();
        }
        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }

        private enum View
        {
            Menu,
            Keybind
        }
    }
}

