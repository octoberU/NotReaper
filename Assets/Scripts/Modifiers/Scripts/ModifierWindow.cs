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
        [NRInject] private ModifierInfo help;

        public override void Show() => OnActivated();
        public override void Hide() => OnDeactivated();
        public void OnPointerEnter(PointerEventData eventData) => modifierCreator.isHovering = true;
        public void OnPointerExit(PointerEventData eventData) => modifierCreator.isHovering = false;

        [NRListener]
        protected override void OnEditorModeChanged(EditorMode mode)
        {
            if (!gameObject.activeInHierarchy) return;

            if (mode != EditorMode.Compose)
            {
                modifierCreator.Activate(false);
            }
        }
        public override void ShowHelp()
        {
            switch (modifierCreator.DropdownIndex)
            {
                case 0:
                    help.ShowAimAssist();
                    break;
                case 1:
                    help.ShowArenaChange();
                    break;
                case 2:
                case 3:
                case 4:
                    help.ShowColor();
                    break;
                case 5:
                    help.ShowHiddenTelegraphs();
                    break;
                case 6:
                    help.ShowInvisibleGuns();
                    break;
                case 7:
                    help.ShowOverlaySetter();
                    break;
                case 8:
                    help.ShowParticles();
                    break;
                case 9:
                case 10:
                    help.ShowPsychedelia();
                    break;
                case 11:
                    help.ShowAutoLight();
                    break;
                case 12:
                    help.ShowSkyboxColor();
                    break;
                case 13:
                    help.ShowSkyboxBrightness();
                    break;
                case 14:
                    help.ShowFader();
                    break;
                case 15:
                    help.ShowLimiter();
                    break;
                case 16:
                    help.ShowSkyboxRotation();
                    break;
                case 17:
                    help.ShowSpeed();
                    break;
                case 18:
                    help.ShowTextPopup();
                    break;
                case 19:
                    help.ShowZOffset();
                    break;
                default:
                    help.ShowGeneral();
                    break;
            }

            modifierCreator.OnInputFocused("");
        }
    }
}

