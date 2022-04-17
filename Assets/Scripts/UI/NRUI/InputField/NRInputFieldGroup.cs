using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.UI.Components
{
    public class NRInputFieldGroup : MonoBehaviour
    {
        [SerializeField] private NRInputField[] fields;

        private int activeIndex = -1;

        private void Awake()
        {
            if (fields != null)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i].index = i;
                }
            }
        }

        private void SwitchInput()
        {
            if (fields.All(field => !field.gameObject.activeInHierarchy))
                return;

            if (fields != null)
            {
                activeIndex = -1;
                if (fields != null)
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (fields[i].isFocused)
                        {
                            activeIndex = i;
                            break;
                        }
                    }
                }

                for(int i = 0; i < fields.Length; i++)
                {
                    var tempNext = (activeIndex + 1 + i) % fields.Length;
                    if (fields[tempNext].gameObject.activeInHierarchy)
                    {
                        activeIndex = tempNext;
                        break;
                    }
                }

                //activeIndex = (activeIndex + 1) % fields.Length;
                fields[activeIndex].Select();
            }
        }


        private void OnEnable()
        {
            KeybindManager.onTabPressed += SwitchInput;
        }

        private void OnDisable()
        {
            KeybindManager.onTabPressed -= SwitchInput;
        }
    }
}
