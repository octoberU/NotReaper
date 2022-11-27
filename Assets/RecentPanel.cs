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
using NotReaper.MapIO;

namespace NotReaper.UI
{
    public class RecentPanel : View
    {
        [SerializeField] List<NRButton> nrButtons = new();
        [NRInject] Timeline timeline;
        [NRInject] private SavingPrompt savingPrompt;
        [SerializeField] NewPauseMenu pauseMenu;
        [SerializeField] private GameObject loadingOverlay;
        private void Awake()
        {
            RecentAudicaFiles.onRecentsLoaded += FillRecentButtons;
        }

        public override void Show() { }
        public override void Hide() { }

        public void FillRecentButtons()
        {
            if(nrButtons.Count > 0)
            {
                for (int i = 0; i < nrButtons.Count; i++)
                {
                    nrButtons[i].gameObject.SetActive(false);
                }

                for (int i = 0; i < nrButtons.Count; i++)
                {
                    if (i > (RecentAudicaFiles.AudicaPaths.Count - 1)) return;
                    string path = RecentAudicaFiles.AudicaPaths[i];
                    if (!File.Exists(path))
                    {
                        continue;
                    }
                    nrButtons[i].onClick.RemoveAllListeners();
                    nrButtons[i].onClick.AddListener(() =>
                    {
                        savingPrompt.ShowPrompt(response =>
                        {
                            switch (response)
                            {
                                case SavingPrompt.Response.Cancel:
                                    return;
                                case SavingPrompt.Response.Accept:
                                    EditorIO.SaveMap(() =>
                                    {
                                        loadingOverlay.SetActive(true);
                                        EditorIO.LoadAudicaFile(path, OnLoaded, false);
                                    });
                                    break;
                                case SavingPrompt.Response.Decline:
                                    loadingOverlay.SetActive(true);
                                    //StartCoroutine(timeline.LoadAudicaFile(false, path, -1, OnLoaded));
                                    EditorIO.LoadAudicaFile(path, OnLoaded, false);
                                    break;
                            }
                        });
                    });
                    Audica file = new Audica(path);
                    var color = NRSettings.config.leftColor;
                    string text = $"{file.desc.title} - {file.desc.artist}\n<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{file.desc.author}".ToLower();
                    nrButtons[i].SetText(text);
                    nrButtons[i].gameObject.SetActive(true);
                }
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
            for (int i = 0; i < nrButtons.Count; i++)
            {
                nrButtons[i].gameObject.SetActive(false);
            }
            gameObject.SetActive(false);
        }
    }
}

