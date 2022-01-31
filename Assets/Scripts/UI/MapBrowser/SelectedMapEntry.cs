using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace NotReaper.MapBrowser
{
    public class SelectedMapEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI songName;
        [SerializeField] private TextMeshProUGUI artistMapper;
        [SerializeField] private Image progress;
        [SerializeField] private GameObject successAnimation;
        [SerializeField] private GameObject failedAnimation;
        [SerializeField] private Button button;
        public MapData Data { get; private set; }
        public void SetData(MapData data)
        {
            this.Data = data;
            button.interactable = true;
            songName.text = data.SongName;
            artistMapper.text = $"{data.Artist} ・ {data.Mapper}".ToLower();       
            progress.fillAmount = 0f;
            successAnimation.SetActive(Data.Downloaded);
            failedAnimation.SetActive(false);
        }

        public void ClearData()
        {
            progress.fillAmount = 0f;
            successAnimation.SetActive(false);
            failedAnimation.SetActive(false);
            MapBrowser.Instance.DeselectCachedEntry(Data.RequestUrl, Data.ID);
            Data = null;
        }

        public void OnDeselected()
        {
            successAnimation.SetActive(false);
            MapBrowser.Instance.DeselectCachedEntry(Data.RequestUrl, Data.ID);
            MapEntrySpawnManager.Instance.RemoveSelectedEntry(this);
        }

        public void OnDownloaded(bool success)
        {
            if (success) successAnimation.SetActive(true);
            else failedAnimation.SetActive(true);
            Data.BrowserEntry.UpdateDownloadedIcon();
        }

        public void ResetAnimation()
        {
            successAnimation.SetActive(false);
            failedAnimation.SetActive(false);
        }

        public void SetButtonInteractable(bool interactable)
        {
            button.interactable = interactable;
        }
    }
}

