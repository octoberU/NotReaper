using System.Collections;
using System.Collections.Generic;
using NotReaper.Models;
using NotReaper.UserInput;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NotReaper.UI {


    public class SoundSelect : MonoBehaviour {
        // Start is called before the first frame update

        //public Image ddBG;
        //public Image arrow;
        [Header("Hitsound Sprites")]
        [SerializeField] private SpriteRenderer kick;
        [SerializeField] private SpriteRenderer snare;
        [SerializeField] private SpriteRenderer percussion;
        [SerializeField] private SpriteRenderer chainStart;
        [SerializeField] private SpriteRenderer chainNode;
        [SerializeField] private SpriteRenderer melee;
        [SerializeField] private SpriteRenderer silent;


        [Space, Header("Configuration")]
        public float fadeAmount = 0.4f;

        private float fadeDuration;

        public UIInput uiInput;
        [NRInject] private Timeline timeline;

        private void Start()
        {
            fadeDuration = (float)NRSettings.config.UIFadeDuration;
        }

        /*public void LoadUIColors() 
        {
            Color rColor = NRSettings.config.rightColor;
            ddBG.color = new Color(rColor.r, rColor.g, rColor.b, 0.9f);

            arrow.color = rColor;
        }
        */

        public void ValueChanged(int value) 
        {
            EditorState.SelectHitsound((TargetHitsound)value);
            //uiInput.SelectHitsound((TargetHitsound)value);
        }

        [NRListener]
        private void OnHandUpdated(TargetHandType _)
        {
            SelectHitsound(EditorState.Hitsound.Current);
        }

        private void FadeoutBehaviors()
        {
            kick.DOColor(Color.white, fadeDuration);
            snare.DOColor(Color.white, fadeDuration);
            percussion.DOColor(Color.white, fadeDuration);
            chainStart.DOColor(Color.white, fadeDuration);
            chainNode.DOColor(Color.white, fadeDuration);
            melee.DOColor(Color.white, fadeDuration);
            silent.DOColor(Color.white, fadeDuration);

            kick.DOFade(fadeAmount, fadeDuration);
            snare.DOFade(fadeAmount, fadeDuration);
            percussion.DOFade(fadeAmount, fadeDuration);
            chainStart.DOFade(fadeAmount, fadeDuration);
            chainNode.DOFade(fadeAmount, fadeDuration);
            melee.DOFade(fadeAmount, fadeDuration);
            silent.DOFade(fadeAmount, fadeDuration);
        }

        [NRListener]
        private void SelectHitsound(TargetHitsound velocity)
        {
            if (kick == null)
                return;

            Color color = NRSettings.GetSelectedColor();
            FadeoutBehaviors();
            SpriteRenderer selected = null;
            switch (velocity)
            {
                case TargetHitsound.Standard:
                    selected = kick;
                    break;
                case TargetHitsound.Snare:
                    selected = snare;
                    break;
                case TargetHitsound.Percussion:
                    selected = percussion;
                    break;
                case TargetHitsound.ChainStart:
                    selected = chainStart;
                    break;
                case TargetHitsound.ChainNode:
                    selected = chainNode;
                    break;
                case TargetHitsound.Melee:
                    selected = melee;
                    break;
                case TargetHitsound.Silent:
                    selected = silent;
                    break;
                default:
                    break;
            }
            if (selected != null)
            {
                selected.DOKill();
                selected.DOFade(1, fadeDuration);
                selected.DOColor(color, fadeDuration);
            }
        }
    }

}