using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI.Components
{


    public class NRTabGroup : MonoBehaviour
    {
        [SerializeField] private NRThemeable[] fields;

        private List<ITabbable> tabbables;

        private int activeIndex = -1;

        private Camera cam;
        private RectTransform rect;

        private void Awake()
        {
            if (fields != null)
            {
                tabbables = new();
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i] is ITabbable tab)
                    {
                        tabbables.Add(tab);
                        tab.Index = i;
                    }
                }
            }
            rect = GetComponent<RectTransform>();
        }

        private void Start() => cam = CameraProvider.menu;

        private void SwitchInput()
        {
            if (!IsVisible())
            {
                KeybindManager.onTabPressed -= SwitchInput;
                return;
            }

            if (tabbables != null)
            {
                activeIndex = -1;
                if (fields != null)
                {
                    for (int i = 0; i < tabbables.Count; i++)
                    {
                        if (tabbables[i].IsFocused)
                        {
                            activeIndex = i;
                            break;
                        }
                    }
                }
                activeIndex = (activeIndex + 1) % fields.Length;
                tabbables[activeIndex].Focus();
            }
        }

        private bool IsVisible()
        {
            if (rect != null)
                return rect.IsVisibleFrom(cam);

            return false;
        }

        private void OnEnable() => KeybindManager.onTabPressed += SwitchInput;
        private void OnDisable() => KeybindManager.onTabPressed -= SwitchInput;
    }
}

