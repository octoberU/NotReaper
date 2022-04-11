using NotReaper.Timing;
using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TargetPreview.Targets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NotReaper.MapPreview
{
    public class PreviewCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera cam;
        [SerializeField] private NRToggle autoCamToggle;
        [SerializeField] private Slider fovSlider;
        [SerializeField] private PreviewSpawner spawner;
        [SerializeField] private TextMeshProUGUI fovText;
        [Space, Header("Camera Settings")]
        [SerializeField] private float manualRotationSpeed = .1f;
        [SerializeField] private float autoRotationSpeed = 1.5f;
        [SerializeField, Range(-10, 0)] private int autoCamBeatRangeFrom = 0;
        [SerializeField, Range(1, 10)] private int autoCamBeatRangeTo = 2;

        private Vector3 direction = Vector3.zero;      
        private InputAction mousePosition;

        internal bool isActive { get; set; }

        private void Awake()
        {
            autoCamToggle.isOn = true;
        }

        private void Start()
        {
            mousePosition = KeybindManager.Global.MousePosition;

            NRSettings.OnLoad(() =>
            {
                cam.fieldOfView = NRSettings.config.previewFOV;
                fovSlider.SetValueWithoutNotify(NRSettings.config.previewFOV);
                OnFOVChanged(NRSettings.config.previewFOV);
                fovSlider.onValueChanged.AddListener(OnFOVChanged);
            });
        }

        private void LateUpdate()
        {
            if (isActive)
            {
                if (autoCamToggle.selected)
                {
                    if (spawner.HasSpawnedTargets())
                    {
                        List<Vector3> positions = new();
                        var start = EditorTime.Time - Relative_QNT.FromBeatTime(autoCamBeatRangeFrom);
                        var end = EditorTime.Time + Relative_QNT.FromBeatTime(autoCamBeatRangeTo);
                        foreach (var target in spawner.GetSpawnedPreviewTargets(start, end))
                        {
                            if (target.TargetData.behavior == TargetBehavior.Melee || target.TargetData.behavior == TargetBehavior.Dodge)
                                continue;

                            positions.Add(target.TargetData.transformData.position);
                        }
                        var averagePosition = positions.Aggregate(Vector3.zero, (acc, v) => acc + v) / positions.Count;
                        direction = averagePosition - cam.transform.position;
                        direction.Normalize();
                    }

                    if (direction != Vector3.zero)
                    {
                        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * autoRotationSpeed);
                    }
                }
                else if (isMouseDown)
                {
                    cam.transform.eulerAngles += new Vector3(-Mouse.current.delta.y.ReadValue(), Mouse.current.delta.x.ReadValue(), 0) * manualRotationSpeed;
                }

            }
        }
        private bool isMouseDown;
        internal void MouseDown(bool down)
        {
            if (down)
            {
                var pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = mousePosition.ReadValue<Vector2>();
                List<RaycastResult> result = new();
                EventSystem.current.RaycastAll(pointerData, result);
                if (result.Any(r => r.gameObject.tag == "Overlay"))
                {
                    down = false;
                }
                else
                {
                    autoCamToggle.selected = false;
                }
            }
            isMouseDown = down;
        }
        private void OnFOVChanged(float value)
        {
            fovText.text = value.ToString();
            cam.fieldOfView = value;
            NRSettings.config.previewFOV = value;
        }
    }

}
