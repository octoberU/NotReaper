using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Modifiers
{
    public class PreviousValueTracker : MonoBehaviour
    {
        [SerializeField]
        private GameObject prevValueIndicator;
        
        [SerializeField]
        private TextMeshProUGUI prevText;

        [SerializeField]
        private Image prevColorLeft;

        [SerializeField]
        private Image prevColorRight;

        [NRInject]
        private ModifierManager manager;

        private void Start()
        {
            ModifierManager.onContentSelected += UpdatePreviousValue;
            DisableIndicator();
        }

        private void UpdatePreviousValue(Content currentContent)
        {
            var currentModifier = currentContent as Modifier;
            var type = currentModifier.ModifierType is ModifierType.ColorChange or ModifierType.ColorUpdate ? Type.LeftRightColor :
                currentModifier.ModifierType is ModifierType.SkyboxColor ? Type.SingleColor :
                currentModifier.ModifierType is ModifierType.ArenaChange or ModifierType.OverlaySetter or ModifierType.HiddenTelegraphs or ModifierType.InvisibleGuns ? Type.None : 
                Type.Amount;

            if (type is Type.None)
            {
                DisableIndicator();
                return;
            }
            
            prevText.gameObject.SetActive(type is Type.Amount);
            prevValueIndicator.SetActive(type is Type.Amount);
            prevColorLeft.gameObject.SetActive(type is Type.LeftRightColor);
            prevColorRight.gameObject.SetActive(type is not Type.Amount);
            var currentIndex = manager.Content.IndexOf(currentModifier);
            for (int i = currentIndex - 1; i >= 0; i--)
            {
                var modifier = manager.Content[i] as Modifier;
                if (modifier.Type != currentModifier.Type) continue;
                
                switch (type)
                {
                    case Type.Amount:
                        prevText.text = modifier.Data.amount.ToString();
                        return;
                    case Type.SingleColor:
                        prevColorRight.color = ToColor(modifier.LeftHandColor);
                        return;
                    case Type.LeftRightColor:
                        prevColorRight.color = HSVToRgb(modifier.RightHandColor);
                        prevColorLeft.color = HSVToRgb(modifier.LeftHandColor);
                        return;
                }
            }

            //there's no previous modifier of this type => disable the indicator
            DisableIndicator();
        }

        private void DisableIndicator()
        {
            prevValueIndicator.SetActive(false);
            prevColorLeft.gameObject.SetActive(false);
            prevColorRight.gameObject.SetActive(false);
            prevText.gameObject.SetActive(false);
        }

        private Color HSVToRgb(float[] array) => Color.HSVToRGB(array[0], array[1], array[2]);
        private Color ToColor(float[] array) => new(array[0], array[1], array[2]);

        private enum Type
        {
            None,
            Amount,
            LeftRightColor,
            SingleColor
        }
    }
}