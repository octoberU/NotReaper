using NotReaper.Models;
using NotReaper.Overlays;
using NotReaper.Tools.ChainBuilder;
using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace NotReaper.Tools.ChainBuilder
{
    public class ChainBuilderWindow : NROverlay, IPointerEnterHandler, IPointerExitHandler
    {

        [NRInject] private ChainBuilder chainBuilder;
        [SerializeField] internal GameObject chainBuilderWindowSelectedControls;
        [SerializeField] internal GameObject chainBuilderWindowUnselectedControls;

        [SerializeField] internal Michsky.UI.ModernUIPack.HorizontalSelector pathBuilderInterval;
        [SerializeField] internal NRInputSliderCombo angleIncrement;
        [SerializeField] internal NRInputSliderCombo angleIncrementIncrement;
        [SerializeField] internal NRInputSliderCombo stepDistance;
        [SerializeField] internal NRInputSliderCombo stepIncrement;

        public void OnPointerEnter(PointerEventData eventData)
        {
            chainBuilder.isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            chainBuilder.isHovering = false;
        }

        public override void Show()
        {
            OnActivated();
        }

        public override void Hide()
        {
            OnDeactivated();
        }

        public void OnIntervalChange()
        {
            ChainBuilder.Instance.OnIntervalChange();
        }

        public void OnGeneratePathClicked()
        {
            ChainBuilder.Instance.GeneratePathFromSelectedNote();
        }

        public void OnBakeClicked()
        {
            ChainBuilder.Instance.BakePathFromSelectedNote();
        }

        [NRListener]
        protected override void OnEditorModeChanged(EditorMode mode)
        {
            if (!gameObject.activeInHierarchy) return;

            if(mode != EditorMode.Compose)
            {
                chainBuilder.Activate(false);
            }
        }
    }
}

