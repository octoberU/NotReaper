using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using NotReaper.Audio;
using UnityEngine.InputSystem;
using NotReaper.Models;

namespace NotReaper.UI.Components
{
    [ExecuteAlways]
    public class NRSlider : NRThemeable
    {
        [Header("Skin")]
        [SerializeField] private NRSliderSkin skin;

        [Space, Header("Fill Bar")]
        [SerializeField] private Theme fillTheme = Theme.fillColor;
        [Space, Header("Animation")]
        [SerializeField, Range(.1f, 1f)] private float animationDuration = .3f;
        // [SerializeField] private Theme sliderBGTheme = Theme.sliderBGColor;

        [SerializeField, HideInInspector] private Image sliderBG;
        [SerializeField, HideInInspector] private GameObject fillArea;
        [SerializeField, HideInInspector] private Image fill;

        private bool initialized = false;

        protected override void Awake()
        {
            base.Awake();
        }
        private void Start()
        {
            if (Application.isPlaying)
            {
                UpdateVisuals();
            }
        }
        public override void Initialize()
        {
            initialized = true;
            sliderBG = transform.GetChild(0).GetComponent<Image>();
            fillArea = transform.GetChild(1).gameObject;
            fill = fillArea.transform.GetChild(0).GetComponent<Image>();
        }

        protected override void OnValidate()
        {
            if (Application.isPlaying) return;

            if (!initialized)
            {
                Initialize();
            }
            UpdateVisuals();
        }

        public override void UpdateVisuals()
        {
            sliderBG.color = skin.sliderBGColor;

            fill.color = fillTheme == Theme.fillColor ? skin.sliderFillColor : fillTheme == Theme.LeftHand ? NRSettings.config.leftColor : fillTheme == Theme.RightHand ? NRSettings.config.rightColor : NRSettings.config.selectedHighlightColor;
            if(fillTheme == Theme.CurrentHand || fillTheme == Theme.OppositeHand)
            {
                fill.color = GetColorForFill();
            }
        }

        [NRListener]
        private void OnHandChanged(TargetHandType hand)
        {
            if(fillTheme == Theme.CurrentHand || fillTheme == Theme.OppositeHand)
            {
                fill.DOColor(GetColorForFill(), animationDuration);
            }
        }

        private Color GetColorForFill()
        {
            if(fillTheme == Theme.CurrentHand)
            {
                return EditorState.Hand.Current == TargetHandType.Left ? NRSettings.config.leftColor : NRSettings.config.rightColor;
            }
            else
            {
                return EditorState.Hand.Current == TargetHandType.Left ? NRSettings.config.rightColor : NRSettings.config.leftColor;
            }
        }

        public override void ApplyLightTheme(ThemeData theme)
        {
            skin = theme.slider.light;
        }

        public override void ApplyDarkTheme(ThemeData theme)
        {
            skin = theme.slider.dark;
        }

        public enum Theme
        {
            fillColor,
            sliderBGColor,
            LeftHand,
            RightHand,
            CurrentHand,
            OppositeHand
        }
    }
}

