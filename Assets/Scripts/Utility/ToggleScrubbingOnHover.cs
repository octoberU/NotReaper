using System;
using System.Linq;
using NotReaper.UI;
using UnityEngine;

namespace Utility
{
    [RequireComponent(typeof(OnHover))]
    public class ToggleScrubbingOnHover : MonoBehaviour
    {
        [SerializeField] private bool _rememberPreviousScrubbingState = true;
        private OnHover _hover;

        private bool _previousScrubbingState;

        private void Awake()
        {
            _hover = GetComponent<OnHover>();
            _hover.onHover.AddListener(ToggleScrubbing);
        }

        private void ToggleScrubbing(bool isHovering)
        {
            if (_rememberPreviousScrubbingState)
            {
                if (isHovering)
                {
                    _previousScrubbingState = KeybindManager.WasScrubbingEnabled;
                }
                else
                {
                    KeybindManager.EnableScrubbing(_previousScrubbingState);
                    return;
                }
            }
            
            KeybindManager.EnableScrubbing(!isHovering);
        }
    }
}