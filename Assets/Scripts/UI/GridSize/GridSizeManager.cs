using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using NotReaper.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NotReaper.UI
{
    public class GridSizeManager : NRMenu
    {
        [SerializeField] private DisplaySliderCombo gridSizeX;
        [SerializeField] private DisplaySliderCombo gridSizeY;
        [SerializeField] private Material gridMaterial;

        private const int MinSizeX = 6;
        private const int MinSizeY = 4;

        private const int MaxSizeX = 20;
        private const int MaxSizeY = 15;

        public delegate void GridSizeChangedEventHandler(Vector2 size);

        public static event GridSizeChangedEventHandler onSizeChanged;
        
        public bool IsActive { get; private set; }

        private void Start()
        {
            NRSettings.OnLoad(LoadValues);
            gameObject.SetActive(false);
        }

        internal void LoadValues()
        {
            var size = ClampSize(NRSettings.config.gridSize);
            UpdateInputFields(size);
            UpdateGrid(size);
        }

        private Vector2 ClampSize(Vector2 size) => new(Math.Clamp(size.x, MinSizeX, MaxSizeX), Math.Clamp(size.y, MinSizeY, MaxSizeY));

        private void UpdateGrid(Vector2 size) => gridMaterial.SetVector("Grid",size);

        private void UpdateInputFields(Vector2 size)
        {
            gridSizeX.SetValueWithoutNotify(size.x);
            gridSizeY.SetValueWithoutNotify(size.y);
        }

        public void ApplyValues()
        {
            var x = (int)gridSizeX.value;
            var y = (int)gridSizeY.value;
            var size = ClampSize(new(x, y));
            
            UpdateGrid(size);
            UpdateInputFields(size);
            
            NRSettings.config.gridSize = size;
            NRSettings.SaveSettingsJson();
            
            onSizeChanged?.Invoke(size);
        }

        public void ResetValues()
        {
            Vector2 size = new(11, 6);
            UpdateGrid(size);
            UpdateInputFields(size);
            NRSettings.config.gridSize = size;
            NRSettings.SaveSettingsJson();
            onSizeChanged?.Invoke(size);
        }

        public override void Show()
        {
            IsActive = true;
            OnActivated();
        }

        public override void Hide()
        {
            IsActive = false;
            ApplyValues();
            OnDeactivated();
        }

        public override void ShowHelp() => NRHelp.Instance.ShowGridSize();

        protected override void OnEscPressed(InputAction.CallbackContext context) => Hide();
    }
}
