using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace NotReaper.Modifiers
{
    public class ModifierTemplateTrack : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        private ModifierTemplateEditor _editor;

        public ModifierType Type { get; private set; }
        public int TypeIndex { get; private set; }
        public int Order { get; set; }

        public void Init(ModifierTemplateEditor editor, TrackManager.TrackOrder trackOrder)
        {
            _editor = editor;
            
            Type = (ModifierType)trackOrder.type;
            TypeIndex = trackOrder.typeIndex;
            Order = trackOrder.order;
            
            text.SetText(Type.ToDisplayName());
        }

        public void MoveUp()
        {
            var index = transform.GetSiblingIndex();
            if (index == 0)
                return;
            
            transform.SetSiblingIndex(index - 1);
            Order--;
        }

        public void MoveDown()
        {
            var index = transform.GetSiblingIndex();
            if (index == transform.parent.childCount - 1)
                return;
            
            transform.SetSiblingIndex(index + 1);
            Order++;
        }

        public void Remove()
        {
            _editor.RemoveTrack(this);
        }

        public TrackManager.TrackOrder GetData() 
            => new ((int)Type, transform.GetSiblingIndex(), TypeIndex);
    }
}
