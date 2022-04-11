using NotReaper;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using AudicaTools;
using NotReaper.UI.Components;

namespace NotReaper.UI
{
    public class RecentPanel : View
    {
        [SerializeField] Button[] buttons;
        [SerializeField] List<NRButton> nrButtons = new();
        [NRInject] Timeline timeline;
        [SerializeField] NewPauseMenu pauseMenu;
        [SerializeField] private GameObject loadingOverlay;
        private void Start()
        {
            if (RecentAudicaFiles.audicaPaths == null) RecentAudicaFiles.LoadRecents();
            FillRecentButtons();
        }

        public override void Show()
        {
            if (RecentAudicaFiles.audicaPaths == null) RecentAudicaFiles.LoadRecents();
            if (RecentAudicaFiles.audicaPaths != null)
            {
                if (RecentAudicaFiles.audicaPaths.Count >= 1)
                {
                    gameObject.SetActive(true);
                    FillRecentButtons();
                }
            }

        }

        public override void Hide() { }

        public void FillRecentButtons()
        {
            if(nrButtons.Count > 0)
            {
                for(int i = 0; i < nrButtons.Count; i++)
                {
                    if (i >= (RecentAudicaFiles.audicaPaths.Count - 1)) return;
                    string path = RecentAudicaFiles.audicaPaths[i];
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    nrButtons[i].onClick.AddListener(new UnityAction(() =>
                    {
                        loadingOverlay.SetActive(true);
                        StartCoroutine(timeline.LoadAudicaFile(false, path, -1, OnLoaded));
                    }));
                    Audica file = new Audica(path);
                    var color = NRSettings.config.leftColor;
                    string text = $"{file.desc.title} - {file.desc.artist}\n<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{file.desc.author}".ToLower();
                    //string filename = path.Split(Path.DirectorySeparatorChar).Last();
                    //filename = filename.Substring(0, filename.Length - 7);
                    nrButtons[i].SetText(text);
                    nrButtons[i].gameObject.SetActive(true);
                }

                return;
            }
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (i >= (RecentAudicaFiles.audicaPaths.Count - 1)) return;
                string path = RecentAudicaFiles.audicaPaths[i];
                /*buttons[i].Click = new UnityEvent();
                buttons[i].NROnClick.AddListener(new UnityAction(() =>
                {
                    if (!timeline.LoadAudicaFile(false, path)) return;
                    pauseMenu.Hide();
                }));*/
                buttons[i].onClick = new Button.ButtonClickedEvent();
                buttons[i].onClick.AddListener(new UnityAction(() =>
                {
                    loadingOverlay.SetActive(true);
                    StartCoroutine(timeline.LoadAudicaFile(false, path, -1, OnLoaded));
                    /*if (!timeline.LoadAudicaFile(false, path)) return;
                    loadingOverlay.SetActive(false);
                    pauseMenu.Hide();*/
                }));
                Audica file = new Audica(path);
                string filename = $"{file.desc.title} - {file.desc.artist}\n{file.desc.author}";
                //string filename = path.Split(Path.DirectorySeparatorChar).Last();
                //filename = filename.Substring(0, filename.Length - 7);
                buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = filename;
                buttons[i].gameObject.SetActive(true);
            }
        }

        private void OnLoaded(bool success)
        {
            if (success)
            {
                loadingOverlay.SetActive(false);
                pauseMenu.Hide();
            }
        }

        public void Clear()
        {
            RecentAudicaFiles.ClearRecents();
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].gameObject.SetActive(false);
            }
            gameObject.SetActive(false);
        }
    }
}

