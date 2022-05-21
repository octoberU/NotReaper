using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotReaper.Maudica;
using UnityEngine.UI;
using DG.Tweening;
using NotReaper.Models;
using System;
using TMPro;
using NotReaper.MapBrowser;
using NotReaper.UI;

namespace NotReaper.Maudica
{
    public class MaudicaUI : View
    {
        [Header("Curation")]
        [SerializeField] private GameObject maudicaPanel;
        [SerializeField] private TextMeshProUGUI curationStatus;
        [Space, Header("Voting")]
        [SerializeField] private TextMeshProUGUI voteCount;
        [Space, Header("Hint")]
        [SerializeField] private GameObject hintPanel;
        [SerializeField] private TextMeshProUGUI hint;

        private void Start()
        {
            NRSettings.OnLoad(() => 
            {
                StartCoroutine(MaudicaHandler.CheckPermissions());
            });

            EditorFile.onAudicaFileLoaded += (file) => InitializeUI();
        }

        private void InitializeUI()
        {
            if (!MaudicaHandler.HasToken)
            {
                hint.text = "enter your maudica token in settings to access maudica features";
                hintPanel.SetActive(true);
                return;
            }
            if(!MaudicaHandler.IsTokenValid)
            {
                hint.text = "invalid token";
                return;
            }
            if (!EditorFile.IsAudicaFileLoaded)
            {
                return;
            }

            StartCoroutine(MaudicaHandler.GetMap(EditorFile.AudicaFile.filepath, new Action<Song>((song) =>
            {
                bool exists = song.id > 0;
                if (!exists)
                {
                    hint.text = "map isn't available on maudica";
                    hintPanel.SetActive(true);
                    maudicaPanel.SetActive(false);
                    return;
                }
                maudicaPanel.SetActive(true);
                hintPanel.SetActive(false);
                UpdateCurationStatus(song.curated);
                UpdateVoteCount(song.score);
            })));
        }

        private void UpdateCurationStatus(float percentage)
        {
            curationStatus.text = $"{percentage * 100f}%";
        }

        private void UpdateVoteCount(int count)
        {
            voteCount.text = count.ToString();
        }

        public void OnApproveClicked()
        {
            StartCoroutine(MaudicaHandler.ApproveMap(new Action(() => 
            {
                StartCoroutine(MaudicaHandler.GetMap(EditorFile.AudicaFile.filepath, new Action<Song>( (song) => UpdateCurationStatus(song.curated))));
            })));
        }

        public void OnUnapproveClicked()
        {
            StartCoroutine(MaudicaHandler.UnapproveMap(new Action(() =>
            {
                StartCoroutine(MaudicaHandler.GetMap(EditorFile.AudicaFile.filepath, new Action<Song>((song) => UpdateCurationStatus(song.curated))));
            })));
        }

        public void OnVoteUpClicked()
        {
            StartCoroutine(MaudicaHandler.VoteMapUp(new Action(() =>
            {
                StartCoroutine(MaudicaHandler.GetMap(EditorFile.AudicaFile.filepath, new Action<Song>((song) => UpdateVoteCount(song.score))));
            })));
        }
        public void OnVoteDownClicked()
        {
            StartCoroutine(MaudicaHandler.VoteMapDown(new Action(() =>
            {
                StartCoroutine(MaudicaHandler.GetMap(EditorFile.AudicaFile.filepath, new Action<Song>((song) => UpdateVoteCount(song.score))));
            })));
        }

        public override void Show()
        {
        }

        public override void Hide()
        {
        }
    }
}

