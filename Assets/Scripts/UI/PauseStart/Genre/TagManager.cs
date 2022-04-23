using NotReaper.Notifications;
using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.Genres
{
    public class TagManager : MonoBehaviour
    {
        [SerializeField] private NRDropdown suggestionDrop;
        [SerializeField] private NRInputField inputField;
        [SerializeField] private List<TagEntry> tags = new();

        private void Awake()
        {
            foreach (var tag in tags)
                tag.gameObject.SetActive(false);
        }

        public void AddTag(string tag)
        {
            if(tags.All(t => t.IsSet))
            {
                NotificationCenter.SendNotification($"Can't add more than {tags.Count} tags.", NotificationType.Info);
                return;
            }

            tags.First(t => !t.IsSet).SetTag(tag);
        }

        public List<string> GetTags()
        {
            List<string> _tags = new();
            foreach(var tag in tags)
            {
                if (tag.IsSet)
                    _tags.Add(tag.Tag);
            }
            return _tags;
        }

        public void SetSuggestions(List<string> suggestions) => suggestionDrop.RepopulateDropdownList(suggestions);

        public void OnInputFieldSubmit()
        {
            AddTag(inputField.text);
            inputField.text = "";
        }

        public void OnSuggestionDropdownChanged() => AddTag(suggestionDrop.valueString);
    }
}
