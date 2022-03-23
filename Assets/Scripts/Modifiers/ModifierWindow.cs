using NotReaper.Tools.ChainBuilder;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using NotReaper.UI;
using NotReaper.Overlays;
using NotReaper.Models;

namespace NotReaper.Modifier
{
    public class ModifierWindow : NROverlay, IPointerEnterHandler, IPointerExitHandler
    {
        [NRInject] private ModifierHandler modifierCreator;

        public override void Show()
        {
            OnActivated();
        }

        public override void Hide()
        {
            OnDeactivated();
        }

        public override void ShowHelp()
        {
            NRHelp.Instance.ShowModifiers();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            modifierCreator.isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            modifierCreator.isHovering = false;
        }
        [NRListener]
        protected override void OnEditorModeChanged(EditorMode mode)
        {
            if (!gameObject.activeInHierarchy) return;

            if (mode != EditorMode.Compose)
            {
                modifierCreator.Activate(false);
            }
        }
    }
}

