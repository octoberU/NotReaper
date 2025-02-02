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
        [SerializeField] private NRToggle explicitToggle;
        [SerializeField] private List<TagEntry> tags = new();
        
        private const string EXPLICIT_TAG = "explicit";

        private void Awake()
        {
            foreach (var tag in tags)
                tag.gameObject.SetActive(false);
        }

        public void AddTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }
            
            if(tags.All(t => t.IsSet))
            {
                NotificationCenter.SendNotification($"Can't add more than {tags.Count} tags.");
                return;
            }

            tag = tag.ToLower();

            if (tag == EXPLICIT_TAG)
            {
                explicitToggle.Select();
                return;
            }
            
            //check if the same tag is already set
            if(tags.Any(t => t.IsSet && t.Tag.Equals(tag, System.StringComparison.InvariantCultureIgnoreCase)))
            {
                NotificationCenter.SendNotification($"Tag {tag} is already set.");
                return;
            }

            tags.First(t => !t.IsSet).SetTag(tag);
        }

        public List<string> GetTags()
        {
            List<string> selectedTags = new();
            foreach(var tag in tags)
            {
                if (tag.IsSet)
                    selectedTags.Add(tag.Tag);
            }

            if (explicitToggle.isOn)
            {
                selectedTags.Add(EXPLICIT_TAG);
            }
            
            return selectedTags;
        }

        public void ResetTags()
        {
            foreach (var tag in tags)
                tag.RemoveTag();
        }

        public void SetSuggestions(List<string> suggestions) => suggestionDrop.RepopulateDropdownList(suggestions);

        public void OnInputFieldSubmit()
        {
            AddTag(inputField.text);
            inputField.text = "";
            inputField.Focus();
        }

        public void OnSuggestionDropdownChanged() => AddTag(suggestionDrop.valueString);
    }
}
