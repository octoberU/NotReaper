using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Notifications
{
    public class Puller : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image badge;
        [SerializeField] private Image pullerIcon;
        [Space, Header("Settings")]
        [Range(0f, 1f), SerializeField] private float openedPullerAlpha = .7f;
        [Range(0f, 1f), SerializeField] private float closedPullerAlpha = .5f;

        private Button puller;

        private void Awake()
        {
            puller = GetComponent<Button>();
        }

        public void UpdateAlpha(bool isOpen)
        {
            float alpha = isOpen ? openedPullerAlpha : closedPullerAlpha;
            ColorBlock block = puller.colors;
            block.normalColor = SetAlpha(block.normalColor, alpha);
            block.disabledColor = SetAlpha(block.disabledColor, alpha);
            block.selectedColor = SetAlpha(block.selectedColor, alpha);
            puller.colors = block;
        }

        public Image GetBadge()
        {
            return badge;
        }

        public Transform GetPullerIconTransform()
        {
            return pullerIcon.transform;
        }

        public void SetPullerInteractable(bool interactable)
        {
            puller.interactable = interactable;
        }

        private Color SetAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}

