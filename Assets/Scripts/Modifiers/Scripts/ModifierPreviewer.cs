using NotReaper.MapPreview;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Modifier
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
            EditorAudio.onPlaybackToggled += (bool play) =>
            {
                if (!play && isPlaying)
                    StopPreview();
            };
        }

        private void UpdateModifierList(QNT_Timestamp currentTime)
        {
            var list = ModifierHandler.Instance.modifiers;
            if (list == null || list.Count == 0) return;
            modifiers = list.ToList();
            modifiers.Sort((s1, s2) => s1.startTime.tick.CompareTo(s2.startTime.tick));
            for(int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (modifiers[i].startTime < currentTime) modifiers.RemoveAt(i);
                //else if (modifiers[i].modifierType != ModifierHandler.ModifierType.ArenaBrightness && modifiers[i].modifierType != ModifierHandler.ModifierType.Fader && modifiers[i].modifierType != ModifierHandler.ModifierType.Psychedelia && modifiers[i].modifierType != ModifierHandler.ModifierType.PsychedeliaUpdate) modifiers.RemoveAt(i);
            }
            isPlaying = true;
            Debug.Log("Updating modifiers");
        }

        public void StartPreview()
        {
            UpdateModifierList(EditorTime.Time);
        }

        public void StopPreview()
        {
            StopAllCoroutines();
            isPlaying = false;
            SetBrightness(1f);
            StopPsy();
            ResetRotation();
            ResetPopup();
            skyboxRend.color = new Color(0f, 0f, 0f, 0f);
            skyboxRend.gameObject.SetActive(false);
            preview.Reset();
            if (zOffsetCalculated)
            {
                ResetZOffset();
                zOffsetCalculated = false;
            }
        }

        private void StopPsy()
        {
            psyRend.gameObject.SetActive(false);
            preview.StopPsychedelia();
        }

        private void HandlePsy(Modifier modifier)
        {
            if(modifier.modifierType == ModifierHandler.ModifierType.Psychedelia)
            {
                StartCoroutine(DoPsychedelia(modifier));
            }
            else
            {
                currentPsySpeed = modifier.amount;
            }
        }

        private IEnumerator DoPsychedelia(Modifier modifier)
        {
            psyRend.gameObject.SetActive(true);
            currentPsySpeed = modifier.amount;
            while (modifier.endTime > EditorTime.Time && modifier.startTime <= EditorTime.Time)
            {
                float h, s, v;
                Color.RGBToHSV(psyRend.color, out h, out s, out v);
                float increment = 0.01f * currentPsySpeed / 1000f;
                Color c = Color.HSVToRGB(h + increment, s, v);
                c.a = .1f;
                psyRend.color = c;
                preview.CyclePsychedelia(currentPsySpeed / 100f);
                yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            }
            StopPsy();
        }

        private IEnumerator HandleSkyboxColor(Modifier modifier)
        {
            skyboxRend.gameObject.SetActive(true);
            Color startColor = skyboxRend.color;
            Color endColor = modifier.option2 ? new Color(0f, 0f, 0f, 0f) : new Color(modifier.leftHandColor[0], modifier.leftHandColor[1], modifier.leftHandColor[2], .35f);
            float percentage;
            while (modifier.endTime > EditorTime.Time && modifier.startTime <= EditorTime.Time)
            {
                percentage = ((EditorTime.Time.tick - modifier.startTime.tick) * 100f) / (modifier.endTime.tick - modifier.startTime.tick);
                Color c = Color.Lerp(startColor, endColor, percentage / 100f);
                skyboxRend.color = c;
                preview.SetSkyboxTint(c);
                yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            }            
        }

        private void Update()
        {
            if (ModifierHandler.activated && ModifierHandler.Instance.isEditingManipulation) 
                ModifierHandler.Instance.UpdateManipulationValues();

            if (modifiers == null || modifiers.Count == 0) return;
            if (!isPlaying)
            {
                return;
            }

            if (!zOffsetCalculated)
            {
                HandleZOffset();
                zOffsetCalculated = true;
            }

            if (modifiers[0].startTime <= EditorTime.Time)
            {
                Modifier m = modifiers[0];
                switch (m.modifierType)
                {
                    case ModifierHandler.ModifierType.ArenaBrightness:
                    case ModifierHandler.ModifierType.Fader:
                        HandleLightingEvent(m);
                        break;
                    case ModifierHandler.ModifierType.Psychedelia:
                    case ModifierHandler.ModifierType.PsychedeliaUpdate:
                        HandlePsy(m);
                        break;
                    case ModifierHandler.ModifierType.ArenaRotation:
                        HandleRotation(m);
                        break;
                    case ModifierHandler.ModifierType.TextPopup:
                        StartCoroutine(HandlePopup(m));
                        break;
                    case ModifierHandler.ModifierType.SkyboxColor:
                        StartCoroutine(HandleSkyboxColor(m));
                        break;
                    default:
                        break;
                }                
                modifiers.RemoveAt(0);
            }
        }

        private void ResetZOffset()
        {
            foreach(Target target in EditorNotes.OrderedNotes)
            {
                target.gridTargetIcon.transform.localScale = new Vector3(.4f, .4f, .4f);
            }
        }

        private void HandleZOffset()
        {
            List<Modifier> zOffsetList = ModifierHandler.Instance.GetZOffsetModifiers();
            zOffsetList.Sort((mod1, mod2) => mod1.startTime.CompareTo(mod2.startTime));
            Debug.Log("Zoffset modifiers: " + zOffsetList.Count);
            Dictionary<Target, float> oldOffsetDict = new Dictionary<Target, float>();
            foreach (Target t in EditorNotes.OrderedNotes) oldOffsetDict.Add(t, t.gridTargetIcon.transform.localScale.x);

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
                        float.TryParse(m.value1, out transitionNumberOfTargets);
                        if (transitionNumberOfTargets > 0)
                        {
                            float percent = m.amount / 500f * -1f;
                            float sign = Mathf.Sign(percent);
                            float scaledTargetAmount = percent * 0.5f;
                            if (m.amount < 0) scaledTargetAmount *= 10f;
                            float targetScale = Mathf.Lerp(target.gridTargetIcon.transform.localScale.x, .4f + scaledTargetAmount, currentCount / (float)transitionNumberOfTargets);
                            target.gridTargetIcon.transform.localScale = new Vector3(targetScale, targetScale, targetScale);
                        }
                        else
                        {
                            if(m.amount != 0f)
                            {
                                float scale = .4f - (m.amount / 1000f);
                                target.gridTargetIcon.transform.localScale = new Vector3(scale, scale, scale);
                                Debug.Log(.4f - (m.amount / 1000f));
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
        }

        private IEnumerator HandlePopup(Modifier modifier)
        {
            int index = CreatePopup(modifier);
            while (modifier.endTime > EditorTime.Time && modifier.startTime <= EditorTime.Time)
            {
                yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            }
            RemovePopup(index);
        }

        private int CreatePopup(Modifier modifier)
        {
            float x, y;
            float.TryParse(modifier.xoffset, out x);
            float.TryParse(modifier.yoffset, out y);
            x /= 10f;
            y /= 10f;
            TextMeshProUGUI txt = Instantiate(textPopup, textPopup.transform.parent);
            Vector3 position = new Vector2(x, y);
            txt.transform.position = position;
            txt.text = modifier.value1;
            float fontSize = 0f;
            if (!float.TryParse(modifier.value2, out fontSize)) fontSize = 24f;
            txt.fontSize = fontSize;
            textIndex++;
            preview.CreatePopup(textIndex, modifier.value1, position, fontSize);
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
            if (modifier.option1) //continuous
            {
                StartCoroutine(DoRotationContinuous(modifier));
            }
            else if (modifier.option2) //incremental
            {
                StartCoroutine(DoRotationIncremental(modifier));
            }
            else //default
            {
                Rotate(modifier.amount);
                preview.SetRotation(modifier.amount);
            }
        }

        private IEnumerator DoRotationContinuous(Modifier modifier)
        {
            while (modifier.endTime > EditorTime.Time && modifier.startTime <= EditorTime.Time)
            {
                Rotate(modifier.amount / 10f);
                preview.SetContinuousRotationAmount(modifier.amount / 100f);
                yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            }
        }

        private IEnumerator DoRotationIncremental(Modifier modifier)
        {
            float startTick = modifier.startTime.tick;
            while (modifier.endTime > EditorTime.Time && modifier.startTime <= EditorTime.Time)
            {
                float percentage = ((EditorTime.Time.tick - modifier.startTime.tick) * 100f) / (modifier.endTime.tick - modifier.startTime.tick);
                float currentRot = Mathf.Lerp(0f, modifier.amount, percentage / 100f);
                Rotate(currentRot / 10f);
                preview.SetContinuousRotationAmount(currentRot / 100f);
                yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
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
            if(modifier.modifierType == ModifierHandler.ModifierType.ArenaBrightness)
            {
                if (modifier.option1)
                {
                    StartCoroutine(DoContinuousBrightness(modifier));
                }
                else if (modifier.option2)
                {
                    StartCoroutine(DoStrobe(modifier));
                }
                else
                {
                    SetBrightness(modifier.amount / 100f);
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
            newAmount *= modifier.amount;
            while (modifier.endTime > EditorTime.Time && modifier.startTime <= EditorTime.Time)
            {
                if (currentBrightness >= 1f) dir = -1;
                else if (currentBrightness <= 0f) dir = 1;
                SetBrightnessIncremental(newAmount * dir);
                yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            }
        }

        private IEnumerator DoStrobe(Modifier modifier)
        {
            float dir = 1;
            if (1f / currentBrightness >= .5f) dir = 0;
            float interval = 480f / modifier.amount;
            float nextStrobe = modifier.startTime.tick;
            while (modifier.endTime > EditorTime.Time && modifier.startTime <= EditorTime.Time)
            {
                if(nextStrobe <= EditorTime.Time.tick)
                {
                    float amnt = 1f * dir;
                    SetBrightness(amnt);
                    if (dir == 1) dir = 0;
                    else if (dir == 0) dir = 1;
                    nextStrobe += interval;
                }
                yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
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
            while (modifier.endTime.tick > EditorTime.Time.tick && modifier.startTime.tick <= EditorTime.Time.tick)
            {
                
                float percentage = ((EditorTime.Time.tick - modifier.startTime.tick) * 100f) / (modifier.endTime.tick - modifier.startTime.tick);
                float currentExp = Mathf.Lerp(startBrightness, modifier.amount / 100f, percentage / 100f);
                SetBrightness(currentExp);
                yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            }
        }
    }
}

