using System;
using NotReaper.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NotReaper.Genres
{
    public class GenreMenu : NRMenu
    {
        [SerializeField] private GenrePicker genrePicker;
        [SerializeField] private GameObject menu;

        private void Start()
        {
            menu.SetActive(false);
        }

        public override void Show()
        {
            genrePicker.LoadData(EditorFile.AudicaFile.desc.genre, EditorFile.AudicaFile.desc.tags);
            menu.SetActive(true);
            OnActivated();
        }

        public override void Hide()
        {
            EditorFile.AudicaFile.desc.genre = genrePicker.GetGenre();
            EditorFile.AudicaFile.desc.tags = genrePicker.GetTags();
            EditorIO.SaveMap();
            OnDeactivated();
            menu.SetActive(false);
        }

        public override void ShowHelp() => NRHelp.Instance.ShowGenrePicker();
        protected override void OnEscPressed(InputAction.CallbackContext context) => Hide();
        public void SubmitTag() => genrePicker.SubmitTag();
    }
}