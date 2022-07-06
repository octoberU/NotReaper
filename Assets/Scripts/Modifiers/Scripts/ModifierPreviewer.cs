using NotReaper.MapPreview;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.UI;
using NotReaper.UI.Particles;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Modifiers.Preview
{
    public class ModifierPreviewer : MonoBehaviour
    {
        public static ModifierPreviewer Instance = null;
        public SpriteRenderer lightRend;
        public SpriteRenderer psyRend;
        public SpriteRenderer skyboxRend;
        public RawImage backgroundImage;
        private Color lightColor;
        private List<Modifier> modifiers = new List<Modifier>();
        [HideInInspector]
        public bool isPlaying = false;
        private float currentBrightness = 0f;
        private float currentPsySpeed = 0f;
        public TextMeshProUGUI textPopup;
        private Dictionary<int, TextMeshProUGUI> textDict = new Dictionary<int, TextMeshProUGUI>();
        private int textIndex = 0;
        private bool zOffsetCalculated = false;
        [NRInject] private ModifierPreview3D preview;
        [NRInject] private CurrentSongDisplay songDisplay;
        [NRInject] private ModifierManager manager;

        private Color originalLeftColor;
        private Color originalRightColor;
        private float originalSpeed = 1f;
        private void Start()
        {
            if (Instance is null) Instance = this;
            else
            {
                Debug.LogWarning("Trying to create second ModifierPreviewer instance.");
                return;
            }
            lightColor = lightRend.color;
            SetBrightness(1f);
            lightRend.enabled = false;
            EditorAudio.onPlaybackToggled += (bool play) =>
            {
                if (!play && isPlaying)
                    StopPreview();
            };

            NRSettings.OnLoad(() =>
            {
                originalLeftColor = NRSettings.config.leftColor;
                originalRightColor = NRSettings.config.rightColor;
            });

            NRSettings.onSettingsSaved += OnSettingsSaved;
        }

        private void OnSettingsSaved(NRJsonSettings config)
        {
            if (isPlaying)
                return;

            originalLeftColor = config.leftColor;
            originalRightColor = config.rightColor;
        }

        private void PreparePreview(QNT_Timestamp currentTime)
        {
            var list = manager.Modifiers;
            if (list == null || list.Count == 0) return;
            modifiers = list.ToList();
            modifiers.Sort((s1, s2) => s1.startTime.tick.CompareTo(s2.startTime.tick));

            for(int i = modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = modifiers[i];
                if (modifier.Type == ModifierType.zOffset)
                    modifiers.RemoveAt(i);
                else if (modifier.endTime != modifier.startTime && modifier.endTime.tick != 0 && modifier.endTime < currentTime)
                    modifiers.RemoveAt(i);                
            }
            lightRend.enabled = true;
            originalLeftColor = NRSettings.config.leftColor;
            originalRightColor = NRSettings.config.rightColor;
            originalSpeed = EditorAudio.PlaybackSpeed;
            HandleZOffset();
            isPlaying = true;
        }

        public void StartPreview()
        {
            PreparePreview(EditorTime.Time);
        }

        public void StopPreview()
        {
            StopAllCoroutines();
            isPlaying = false;
            SetBrightness(1f);
            StopPsy();
            ResetRotation();
            ResetPopup();
            ResetTargets();
            GridParticles.ResetParticleAmount();
            EditorAudio.SetPlaybackSpeed(originalSpeed);
            skyboxRend.color = new Color(0f, 0f, 0f, 0f);
            skyboxRend.gameObject.SetActive(false);
            songDisplay.ResetTitle();
            lightRend.enabled = false;
            preview.Reset();
            if (zOffsetCalculated)
            {
                ResetZOffset();
                zOffsetCalculated = false;
            }
        }

        private void ResetTargets()
        {
            NRSettings.config.leftColor = originalLeftColor;
            NRSettings.config.rightColor = originalRightColor;
            EditorTargets.UpdateTargetColors();

            foreach (var target in EditorNotes.OrderedNotes)
                target.gridTargetIcon.HideTelegraph(false);
        }

        private void StopPsy()
        {
            psyRend.gameObject.SetActive(false);
            preview.StopPsychedelia();
        }

        private void HandlePsy(Modifier modifier)
        {
            if(modifier.Type == ModifierType.Psychedelia)
            {
                StartCoroutine(DoPsychedelia(modifier));
            }
            else
            {
                currentPsySpeed = modifier.Data.amount;
            }
        }

        internal void DoPsychedeliaUpdate(Modifier modifier) => currentPsySpeed = modifier.Data.amount;

        internal IEnumerator DoPsychedelia(Modifier modifier)
        {
            psyRend.gameObject.SetActive(true);
            currentPsySpeed = modifier.Data.amount;
            while (IsModifierActive(modifier))
            {
                float h, s, v;
                Color.RGBToHSV(psyRend.color, out h, out s, out v);
                float increment = 0.01f * currentPsySpeed / 1000f;
                Color c = Color.HSVToRGB(h + increment, s, v);
                c.a = .1f;
                psyRend.color = c;
                preview.CyclePsychedelia(currentPsySpeed / 100f);
                yield return new WaitForSeconds(Time.unscaledDeltaTime);
            }
            StopPsy();
        }

        private IEnumerator HandleSkyboxColor(Modifier modifier)
        {
            skyboxRend.gameObject.SetActive(true);
            Color startColor = skyboxRend.color;
            Color endColor = modifier.Data.option2 ? new Color(0f, 0f, 0f, 0f) : new Color(modifier.Data.leftHandColor[0], modifier.Data.leftHandColor[1], modifier.Data.leftHandColor[2], .35f);
            while (IsModifierActive(modifier))
            {
                float percentage = GetPercentageAtCurrentTime(modifier);
                Color c = Color.Lerp(startColor, endColor, percentage);
                skyboxRend.color = c;
                preview.SetSkyboxTint(c);
                yield return new WaitForSeconds(Time.unscaledDeltaTime);
            }            
        }

        private void Update()
        {
            /*if (manager.IsActive && ModifierHandler.Instance.isEditingManipulation) 
                ModifierHandler.Instance.UpdateManipulationValues();*/

            if (modifiers == null || modifiers.Count == 0 || !isPlaying) return;
            
            if (modifiers[0].startTime <= EditorTime.Time)
            {
                Modifier m = modifiers[0];
                switch (m.Type)
                {
                    case ModifierType.ArenaBrightness:
                    case ModifierType.Fader:
                        HandleLightingEvent(m);
                        break;
                    case ModifierType.Psychedelia:
                    case ModifierType.PsychedeliaUpdate:
                        HandlePsy(m);
                        break;
                    case ModifierType.ArenaRotation:
                        HandleRotation(m);
                        break;
                    case ModifierType.TextPopup:
                        HandlePopup(m);
                        break;
                    case ModifierType.SkyboxColor:
                        StartCoroutine(HandleSkyboxColor(m));
                        break;
                    case ModifierType.OverlaySetter:
                        HandleOverlaySetter(m);
                        break;
                    case ModifierType.Particles:
                        HandleParticles(m);
                        break;
                    case ModifierType.Speed:
                        HandleSpeed(m);
                        break;
                    case ModifierType.ColorChange:
                    case ModifierType.ColorUpdate:
                        HandleColorChange(m);
                        break;
                    case ModifierType.ColorSwap:
                        HandleColorSwap(m);
                        break;
                    case ModifierType.HiddenTelegraphs:
                        HandleHiddenTeles(m);
                        break;
                    default:
                        break;
                }                
                modifiers.RemoveAt(0);
            }
            
        }

        private void HandleHiddenTeles(Modifier modifier)
        {
            foreach (var target in EditorNotes.OrderedNotes)
                target.gridTargetIcon.HideTelegraph(true);

            preview.HideTelegraphs(true);

            StartCoroutine(WaitForHiddenTeleFinish(modifier));
        }

        private IEnumerator WaitForHiddenTeleFinish(Modifier modifier)
        {
            while (IsModifierActive(modifier))
                yield return new WaitForSeconds(Time.unscaledDeltaTime);

            foreach (var target in EditorNotes.OrderedNotes)
                target.gridTargetIcon.HideTelegraph(false);

            preview.HideTelegraphs(false);
        }

        private void HandleColorSwap(Modifier modifier)
            => DoColorChange(modifier, NRSettings.config.rightColor, NRSettings.config.leftColor);

        private void HandleColorChange(Modifier modifier)
            => DoColorChange(modifier, ConvertToColor(modifier.Data.leftHandColor), ConvertToColor(modifier.Data.rightHandColor));

        private void DoColorChange(Modifier modifier, Color leftColor, Color rightColor)
        {
            var previousLeftColor = NRSettings.config.leftColor;
            var previousRightColor = NRSettings.config.rightColor;

            NRSettings.config.leftColor = leftColor;
            NRSettings.config.rightColor = rightColor;
            EditorTargets.UpdateTargetColors();
            preview.SetTargetColors(leftColor, rightColor);
            if (modifier.endTime > modifier.startTime)
                StartCoroutine(WaitForColorChangeFinish(modifier, previousLeftColor, previousRightColor));
        }

        private IEnumerator WaitForColorChangeFinish(Modifier modifier, Color previousLeftColor, Color previousRightColor)
        {
            while (IsModifierActive(modifier))
                yield return new WaitForSeconds(Time.unscaledDeltaTime);

            NRSettings.config.leftColor = previousLeftColor;
            NRSettings.config.rightColor = previousRightColor;
            EditorTargets.UpdateTargetColors();
            preview.SetTargetColors(previousLeftColor, previousRightColor);
        }

        private void HandleSpeed(Modifier modifier)
            => StartCoroutine(DoSpeedTransition(modifier));

        private IEnumerator DoSpeedTransition(Modifier modifier)
        {
            float target = modifier.Data.amount / 100f;
            float originalSpeed = EditorAudio.PlaybackSpeed;
            while(IsModifierActive(modifier))
            {
                float percentage = GetPercentageAtCurrentTime(modifier);
                float newAmount = Mathf.Lerp(originalSpeed, target, percentage);
                EditorAudio.SetPlaybackSpeedUnclamped(newAmount);
                yield return new WaitForSeconds(Time.unscaledDeltaTime);
            }
        }

        private void HandleOverlaySetter(Modifier modifier)
        {
            string newText;
            string title = modifier.Data.value1;
            string mapper = modifier.Data.value2;

            newText = string.IsNullOrEmpty(title) ? songDisplay.GetSongTitle() : title;
            newText += string.IsNullOrEmpty(mapper) ? "" : mapper;

            songDisplay.SetModifierSongTitle(newText);

            if (modifier.endTime > modifier.startTime)
                StartCoroutine(WaitForOverlayFinish(modifier));
        }

        private IEnumerator WaitForOverlayFinish(Modifier modifier)
        {
            while (IsModifierActive(modifier))
                yield return new WaitForSeconds(Time.unscaledDeltaTime);

            songDisplay.ResetTitle();
        }

        private void HandleParticles(Modifier modifier)
        {
            GridParticles.SetParticleAmount((int)modifier.Data.amount);

            if (modifier.endTime > modifier.startTime)
                StartCoroutine(WaitForParticlesFinish(modifier));
        }

        private IEnumerator WaitForParticlesFinish(Modifier modifier)
        {
            while (IsModifierActive(modifier))
                yield return new WaitForSeconds(Time.unscaledDeltaTime);

            GridParticles.ResetParticleAmount();
        }

        private void ResetZOffset()
        {
            foreach(Target target in EditorNotes.OrderedNotes)
            {
                target.gridTargetIcon.transform.localScale = new Vector3(.4f, .4f, .4f);
            }
            preview.ClearZOffsets();
        }

        private void HandleZOffset()
        {
            List<Modifier> zOffsetList = manager.GetZOffsetModifiers();
            zOffsetList.Sort((mod1, mod2) => mod1.startTime.CompareTo(mod2.startTime));
            Dictionary<Target, float> oldOffsetDict = new Dictionary<Target, float>();
            foreach (Target t in EditorNotes.OrderedNotes) oldOffsetDict.Add(t, t.gridTargetIcon.transform.localScale.x);
            preview.ClearZOffsets();
            foreach (Modifier m in zOffsetList)
            {
                float currentCount = 1f;
                bool endTickSet = m.endTime.tick != 0 && m.startTime.tick != m.endTime.tick;
                foreach (Target target in EditorNotes.OrderedNotes)
                {
                    var targetData = target.data;
                    if (targetData.time.tick < m.startTime.tick) continue;
                    if (targetData.time.tick > m.endTime.tick && endTickSet) break;
                    if (targetData.behavior != TargetBehavior.Melee && targetData.behavior != TargetBehavior.Mine)
                    {
                        float transitionNumberOfTargets = 0f;
                        float.TryParse(m.Data.value1, out transitionNumberOfTargets);
                        if (transitionNumberOfTargets > 0)
                        {
                            float percent = m.Data.amount / 500f * -1f;
                            float sign = Mathf.Sign(percent);
                            float scaledTargetAmount = percent * 0.5f;
                            if (m.Data.amount < 0) scaledTargetAmount *= 10f;
                            float targetScale = Mathf.Lerp(target.gridTargetIcon.transform.localScale.x, .4f + scaledTargetAmount, currentCount / (float)transitionNumberOfTargets);
                            target.gridTargetIcon.transform.localScale = new Vector3(targetScale, targetScale, targetScale);

                            preview.SetZOffset(target, Mathf.Lerp(target.ToCue().zOffset, m.Data.amount, currentCount / (float)transitionNumberOfTargets));
                        }
                        else
                        {
                            if(m.Data.amount != 0f)
                            {
                                float scale = .4f - (m.Data.amount / 1000f);
                                target.gridTargetIcon.transform.localScale = new Vector3(scale, scale, scale);
                                preview.SetZOffset(target, m.Data.amount);
                            }
                            else
                            {
                                target.gridTargetIcon.transform.localScale = new Vector3(.4f, .4f, .4f);
                            }
                            
                            //cue.zOffset = m.amount;
                        }
                        //cue.zOffset /= 100f;
                        if (currentCount < transitionNumberOfTargets) currentCount++;
                    }
                }
            }
            preview.ApplyZOffset();
        }

        private void HandlePopup(Modifier modifier)
        {
            StartCoroutine(DoPopup(modifier, CreatePopup(modifier)));
        }

        private IEnumerator DoPopup(Modifier modifier, int textIndex)
        {
            while (IsModifierActive(modifier))
                yield return new WaitForSeconds(Time.unscaledDeltaTime);
            
            RemovePopup(textIndex);
        }

        private int CreatePopup(Modifier modifier)
        {
            float x, y;
            float.TryParse(modifier.Data.xoffset, out x);
            float.TryParse(modifier.Data.yoffset, out y);
            x /= 10f;
            y /= 10f;
            TextMeshProUGUI txt = Instantiate(textPopup, textPopup.transform.parent);
            Vector3 position = new Vector2(x, y);
            txt.transform.position = position;
            txt.text = modifier.Data.value1;
            float fontSize = 0f;
            if (!float.TryParse(modifier.Data.value2, out fontSize)) fontSize = 24f;
            txt.fontSize = fontSize;
            textIndex++;
            preview.CreatePopup(textIndex, modifier.Data.value1, position, fontSize);
            textDict.Add(textIndex, txt);
            return textIndex;
        }

        private void RemovePopup(int index)
        {
            if (textDict.ContainsKey(index))
            {
                GameObject.Destroy(textDict[index].gameObject);
                textDict.Remove(index);
                preview.RemovePopup(index);
            }          
        }

        private void ResetPopup()
        {
            preview.RemoveAllPopups();
            foreach(KeyValuePair<int, TextMeshProUGUI> entry in textDict)
            {
                GameObject.Destroy(entry.Value.gameObject);
            }
            textDict.Clear();
        }

        private void HandleRotation(Modifier modifier)
        {
            if (modifier.Data.option1) //continuous
            {
                StartCoroutine(DoRotationContinuous(modifier));
            }
            else if (modifier.Data.option2) //incremental
            {
                StartCoroutine(DoRotationIncremental(modifier));
            }
            else //default
            {
                Rotate(modifier.Data.amount);
                preview.SetRotation(modifier.Data.amount);
            }
        }

        private IEnumerator DoRotationContinuous(Modifier modifier)
        {
            while (IsModifierActive(modifier))
            {
                Rotate(modifier.Data.amount / 10f);
                preview.SetContinuousRotationAmount(modifier.Data.amount / 100f);
                yield return new WaitForSeconds(Time.unscaledDeltaTime);
            }
        }

        private IEnumerator DoRotationIncremental(Modifier modifier)
        {
            while (IsModifierActive(modifier))
            {
                float percentage = GetPercentageAtCurrentTime(modifier);
                float currentRot = Mathf.Lerp(0f, modifier.Data.amount, percentage);
                Rotate(currentRot / 10f);
                preview.SetContinuousRotationAmount(currentRot / 100f);
                yield return new WaitForSeconds(Time.unscaledDeltaTime);
            }
        }

        private void Rotate(float amount)
        {
            amount /= 1000f;
            Rect original = backgroundImage.uvRect;
            Vector2 offset = original.position;
            offset.x += amount;
            backgroundImage.uvRect = new Rect(offset, original.size);
        }

        private void ResetRotation()
        {
            Rect original = backgroundImage.uvRect;
            Vector2 offset = Vector2.zero;
            backgroundImage.uvRect = new Rect(offset, original.size);
            preview.ResetRotation();
        }

        private void HandleLightingEvent(Modifier modifier)
        {
            if(modifier.Type == ModifierType.ArenaBrightness)
            {
                if (modifier.Data.option1)
                {
                    StartCoroutine(DoContinuousBrightness(modifier));
                }
                else if (modifier.Data.option2)
                {
                    StartCoroutine(DoStrobe(modifier));
                }
                else
                {
                    SetBrightness(modifier.Data.amount / 100f);
                }
                
            }
            else
            {
                StartCoroutine(HandleFader(modifier));
            }
        }

        private IEnumerator DoContinuousBrightness(Modifier modifier)
        {
            float dir = 1;
            float newAmount = 0.01f;
            newAmount *= modifier.Data.amount;
            while (IsModifierActive(modifier))
            {
                if (currentBrightness >= 1f) dir = -1;
                else if (currentBrightness <= 0f) dir = 1;
                SetBrightnessIncremental(newAmount * dir);
                yield return new WaitForSeconds(Time.unscaledDeltaTime);
            }
        }

        private IEnumerator DoStrobe(Modifier modifier)
        {
            float dir = 1;
            if (1f / currentBrightness >= .5f) dir = 0;
            float interval = 480f / modifier.Data.amount;
            float nextStrobe = modifier.startTime.tick;
            while (IsModifierActive(modifier))
            {
                if(nextStrobe <= EditorTime.Time.tick)
                {
                    float amnt = 1f * dir;
                    SetBrightness(amnt);
                    if (dir == 1) dir = 0;
                    else if (dir == 0) dir = 1;
                    nextStrobe += interval;
                }
                yield return new WaitForSeconds(Time.unscaledDeltaTime);
            }
        }

        private void SetBrightnessIncremental(float brightness)
        {
            currentBrightness += brightness;
            preview.SetBrightness(brightness);
            brightness = .15f + (brightness * .7f);
            lightRend.color = new Color(lightColor.r, lightColor.g, lightColor.b, 1f - brightness);
        }

        private void SetBrightness(float brightness)
        {
            currentBrightness = brightness;
            preview.SetBrightness(brightness);
            brightness = .15f + (brightness * .7f);
            lightRend.color = new Color(lightColor.r, lightColor.g, lightColor.b, 1f - brightness);
        }

        private IEnumerator HandleFader(Modifier modifier)
        {
            float startBrightness = currentBrightness;
            float amount = modifier.Data.amount / 100f;
            while (IsModifierActive(modifier))
            {
                float percentage = GetPercentageAtCurrentTime(modifier);
                float currentExp = Mathf.Lerp(startBrightness, amount, percentage);
                SetBrightness(currentExp);
                yield return new WaitForSeconds(Time.unscaledDeltaTime);
            }
        }

        private float GetPercentageAtCurrentTime(Modifier modifier)
            => (((EditorTime.Time.tick - modifier.startTime.tick) * 100f) / (modifier.endTime.tick - modifier.startTime.tick)) * .01f;

        private bool IsModifierActive(Modifier modifier)
            => EditorTime.Time >= modifier.startTime && EditorTime.Time <= modifier.endTime;

        private Color ConvertToColor(float[] color)
            => new(color[0], color[1], color[2]);
    }
}

