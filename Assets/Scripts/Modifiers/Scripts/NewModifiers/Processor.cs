using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NotReaper.Statistics;
using TMPro;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public abstract class Processor : MonoBehaviour
    {
        [NRInject] protected ModifierUIHandler uiHandler;
        protected abstract ModifierType Type { get; }

        private DisplayData displayData = new();

        protected Modifier Modifier { get; private set; }
        protected Data Data => Modifier.Data;

        internal FieldData<float> Amount { get; private set; }
        internal FieldData<string> Value1 { get; private set; }
        internal FieldData<string> Value2 { get; private set; }
        internal FieldData<string> Value3 { get; private set; }
        internal FieldData<string> Value4 { get; private set; }
        internal FieldData<string> Value5 { get; private set; }
        internal FieldData<bool> Option1 { get; private set; }
        internal FieldData<bool> Option2 { get; private set; }
        internal FieldData<bool> Option3 { get; private set; }
        internal FieldData<float[]> ColorLeft { get; private set; }
        internal FieldData<float[]> ColorRight { get; private set; }
        internal virtual string Hint => "";
        internal virtual Vector2 AmountMinMax => new(0, 100);
        internal virtual ColorPickerType ColorPickerType => ColorPickerType.None;
        internal virtual bool RefreshOnSelect => false;
        
        internal DisplayData.DisplayField ExtraButton { get; private set; }

        protected virtual void Start()
        {
            Amount = new(value =>
            {
                Debug.Log("Set amount to " + value + " in " + GetType());
                Data.amount = value;
                
            });
            
            Value1 = new(value => { Data.value1 = value; });
            Value2 = new(value => { Data.value2 = value; });
            Value3 = new(value => { Data.xoffset = value; });
            Value4 = new(value => { Data.yoffset = value; });
            Value5 = new(value => { Data.zoffset = value; });

            Option1 = new(value => { Data.option1 = value; });
            Option2 = new(value => { Data.option2 = value; });
            Option3 = new(value => { Data.independantBool = value; });

            ColorLeft = new(value => { Data.leftHandColor = value; });
            ColorRight = new(value => { Data.rightHandColor = value; });

            ExtraButton = new();
            
            ProcessorManager.RegisterProcessor(Type, this);
            ModifierUIHandler.onBeforeUIUpdate += OnModifierSelected;
            InitializeFields();
        }

        private void InitializeFields()
        {
            InitializeDisplayData(ref displayData);
            Amount.Init(displayData.amount.Show, displayData.amount.DisplayName);

            Value1.Init(displayData.value1.Show, displayData.value1.DisplayName, displayData.value1.ContentType);
            Value2.Init(displayData.value2.Show, displayData.value2.DisplayName, displayData.value2.ContentType);
            Value3 .Init(displayData.value3.Show, displayData.value3.DisplayName, displayData.value3.ContentType);
            Value4 .Init(displayData.value4.Show, displayData.value4.DisplayName, displayData.value4.ContentType);
            Value5 .Init(displayData.value5.Show, displayData.value5.DisplayName, displayData.value5.ContentType);

            Option1 .Init(displayData.option1.Show, displayData.option1.DisplayName);
            Option2 .Init(displayData.option2.Show, displayData.option2.DisplayName);
            Option3 .Init(displayData.option3.Show, displayData.option3.DisplayName);

            ExtraButton = displayData.extraButton;
        }

        internal void RefreshFields()
        {
            if (Modifier == null) return;
            
            InitializeFields();
            OnModifierSelected(Modifier);
        }

        private void OnModifierSelected(Modifier modifier)
        {
            Modifier = modifier;

            Amount.Set(Data.amount);

            Value1.Set(Data.value1);
            Value2.Set(Data.value2);
            Value3.Set(Data.xoffset);
            Value4.Set(Data.yoffset);
            Value5.Set(Data.zoffset);

            Option1.Set(Data.option1);
            Option2.Set(Data.option2);
            Option3.Set(Data.independantBool);

            ColorLeft.Set(Data.leftHandColor);
            ColorRight.Set(Data.rightHandColor);
        }

        /// <summary>
        /// Set the display name for every field you need to show up on the UI. "Show" will automatically be set to true when you do.
        /// </summary>
        /// <param name="displayData"></param>
        protected abstract void InitializeDisplayData(ref DisplayData displayData);

        internal virtual void OnOption1Changed()
        {
        }

        internal virtual void OnOption2Changed()
        {
            
        }

        internal virtual void OnExtraButtonPressed()
        {
            
        }

        public class FieldData<T>
        {
            public string DisplayName { get; private set; }
            public bool Show { get; private set; }

            public TMP_InputField.ContentType ContentType { get; set; } = TMP_InputField.ContentType.Standard;

            private T value;

            private readonly Action<T> onValueSet;

            public FieldData(Action<T> onValueSet)
            {
                this.onValueSet = onValueSet;
            }

            public FieldData(bool show, string displayName)
            {
                Show = show;
                DisplayName = displayName;
            }

            public FieldData(bool show, string displayName, TMP_InputField.ContentType contentType)
            {
                Show = show;
                DisplayName = displayName;
                ContentType = contentType;
            }

            public void Init(bool show, string displayName)
            {
                Show = show;
                DisplayName = displayName;
            }

            public void Init(bool show, string displayName, TMP_InputField.ContentType contentType)
            {
                Init(show, displayName);
                ContentType = contentType;
            }

            public T Get() => value;

            public void Set(T value)
            {
                this.value = value;
                onValueSet?.Invoke(value);
            }
        }

        public class DisplayData
        {
            public DisplayField amount = new();

            public DisplayField value1 = new();
            public DisplayField value2 = new();
            public DisplayField value3 = new();
            public DisplayField value4 = new();
            public DisplayField value5 = new();

            public DisplayField option1 = new();
            public DisplayField option2 = new();
            public DisplayField option3 = new();

            public DisplayField extraButton = new();

            public class DisplayField
            {
                public bool Show { get; private set; }

                public TMP_InputField.ContentType ContentType { get; set; } = TMP_InputField.ContentType.Standard;

                private string displayName;

                public string DisplayName
                {
                    get => displayName;
                    set
                    {
                        displayName = value;
                        Show = true;
                    }
                }

                public void Hide() => Show = false;
            }
        }
    }
}