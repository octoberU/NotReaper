using NotReaper;
using NotReaper.MapPreview;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Managers;
using UnityEngine;

namespace NotReaper.UI.Particles
{
    public class GridParticles : MonoBehaviour
    {
        private static int defaultTargetParticleAmount = 100;
        private static int defaultMaxParticles = 500;

        private static int targetParticleAmount;
        private static int maxParticleAmount;
        private static int chainNodeParticleAmount => (int)(targetParticleAmount / 10f);

        private static ParticleSystem particlesLeft;
        private static ParticleSystem particlesRight;
        private static ParticleSystem sustainLeft;
        private static ParticleSystem sustainRight;

        private static ParticleSystem meleeStationaryTopLeft;
        private static ParticleSystem meleeStationaryTopRight;
        private static ParticleSystem meleeStationaryBottomLeft;
        private static ParticleSystem meleeStationaryBottomRight;

        private static ParticleSystem meleeDebrisTopLeft;
        private static ParticleSystem meleeDebrisTopRight;
        private static ParticleSystem meleeDebrisBottomRight;
        private static ParticleSystem meleeDebrisBottomLeft;

        private static Target lastLeftTarget;
        private static Target lastRightTarget;

        private static Preview3DManager preview;

        private static Transform mainCam;
        private static Vector2 CameraOffset => new(mainCam.position.x, mainCam.position.y - .5f);

        private static bool allowEmission = true;

        private void Awake()
        {
            mainCam = CameraProvider.main.transform;

            targetParticleAmount = defaultTargetParticleAmount;
            maxParticleAmount = defaultMaxParticles;

            particlesLeft = transform.GetChild(0).GetComponent<ParticleSystem>();
            particlesRight = transform.GetChild(1).GetComponent<ParticleSystem>();
            sustainLeft = transform.GetChild(2).GetComponent<ParticleSystem>();
            sustainRight = transform.GetChild(3).GetComponent<ParticleSystem>();

            var stationary = transform.GetChild(4);
            meleeStationaryTopLeft = stationary.GetChild(0).GetComponent<ParticleSystem>();
            meleeStationaryTopRight = stationary.GetChild(1).GetComponent<ParticleSystem>();
            meleeStationaryBottomLeft = stationary.GetChild(2).GetComponent<ParticleSystem>();
            meleeStationaryBottomRight = stationary.GetChild(3).GetComponent<ParticleSystem>();

            var debris = transform.GetChild(5);
            meleeDebrisTopLeft = debris.GetChild(0).GetComponent<ParticleSystem>();
            meleeDebrisTopRight = debris.GetChild(1).GetComponent<ParticleSystem>();
            meleeDebrisBottomRight = debris.GetChild(2).GetComponent<ParticleSystem>();
            meleeDebrisBottomLeft = debris.GetChild(3).GetComponent<ParticleSystem>();
        }

        private void Start()
        {
            NRSettings.OnLoad(() =>
            {
                UpdateColor(NRSettings.config);
            });

            NRSettings.onSettingsSaved += UpdateColor;

            EditorFile.onAudicaFileLoaded += _ =>
            {
                lastLeftTarget = null;
                lastRightTarget = null;
            };

            DifficultyManager.onDifficultyLoaded += (_) =>
            {
                lastLeftTarget = null;
                lastRightTarget = null;
            };

            preview = NRDependencyInjector.Get<Preview3DManager>();
        }

        public static void UpdateColor(NRJsonSettings config)
        {
            var leftColor = new ParticleSystem.MinMaxGradient(config.leftColor, config.leftColor);
            var rightColor = new ParticleSystem.MinMaxGradient(config.rightColor, config.rightColor);
            var mainLeft = particlesLeft.main;
            mainLeft.startColor = leftColor;
            var mainRight = particlesRight.main;
            mainRight.startColor = rightColor;

            var susLeft = sustainLeft.main;
            susLeft.startColor = leftColor;
            var susRight = sustainRight.main;
            susRight.startColor = rightColor;
        }

        public static void SetParticleAmount(int amount)
        {
            targetParticleAmount = amount;
            if (amount > maxParticleAmount)
                maxParticleAmount = (int)(amount * 2.5f);
        }

        public static void ResetParticleAmount()
        {
            targetParticleAmount = defaultTargetParticleAmount;
            maxParticleAmount = defaultMaxParticles;
        }

        public static void AllowEmission(bool allow)
        {
            StopEmitting();
            allowEmission = allow;
        }

        public static void Emit(Target target)
        {
            if (!CanEmit(target)) return;

            var data = target.data;
            if (data.behavior == TargetBehavior.Melee || data.behavior == TargetBehavior.Mine) return;
            var particles = data.handType == TargetHandType.Left ? particlesLeft : particlesRight;
            var main = particles.main;
            main.maxParticles = maxParticleAmount;
            var emission = particles.emission;
            var burst = emission.GetBurst(0);
            burst.count = data.behavior == TargetBehavior.ChainNode ? chainNodeParticleAmount : targetParticleAmount;
            emission.SetBurst(0, burst);
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            if (preview.IsActive)
            {
                var previewTarget = preview.GetPreviewTarget(target);
                if(previewTarget != null)
                {
                    particles.transform.position = previewTarget.TargetData.transformData.position;
                    particles.transform.rotation = previewTarget.TargetData.transformData.rotation;
                    particles.transform.localScale = Vector3.one * .2f;
                    particles.Play();
                }
            }
            else
            {
                particles.transform.rotation = Quaternion.identity;
                particles.transform.position = data.position - CameraOffset;
                particles.transform.localScale = Vector3.one * .05f;
                particles.Play();
            }

            if (data.handType == TargetHandType.Left) lastLeftTarget = target;
            else if (data.handType == TargetHandType.Right) lastRightTarget = target;
        }

        public static void StartEmitSustain(Target target)
        {
            if (!CanEmit(target, true)) return;

            var data = target.data;
            if (data.behavior != TargetBehavior.Sustain) return;
            var particles = data.handType == TargetHandType.Left ? sustainLeft : sustainRight;
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            if (preview.IsActive)
            {
                var previewTarget = preview.GetPreviewTarget(target);
                if (previewTarget != null)
                {
                    particles.transform.position = previewTarget.TargetData.transformData.position;
                    particles.transform.rotation = previewTarget.TargetData.transformData.rotation;
                    particles.transform.localScale = Vector3.one * 1.2f;
                    particles.Play();
                }
            }
            else
            {
                particles.transform.position = data.position - CameraOffset;
                particles.transform.rotation = Quaternion.identity;
                particles.transform.localScale = Vector3.one * .3f;
                particles.Play();
            }
        }
        public static void StopEmitSustain(Target target)
        {
            var data = target.data;
            var particles = data.handType == TargetHandType.Left ? sustainLeft : sustainRight;
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        public static void StopEmitSustain(TargetHandType hand)
        {
            var particles = hand == TargetHandType.Left ? sustainLeft : sustainRight;
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        public static void StopEmitting(bool killParticles = true)
        {
            var behavior = killParticles ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting;

            particlesLeft.Stop(true, behavior);
            particlesRight.Stop(true, behavior);

            sustainLeft.Stop(true, behavior);
            sustainRight.Stop(true, behavior);

            meleeStationaryBottomLeft.Stop(true, behavior);
            meleeStationaryBottomRight.Stop(true, behavior);
            meleeStationaryTopLeft.Stop(true, behavior);
            meleeStationaryTopRight.Stop(true, behavior);

            meleeDebrisBottomLeft.Stop(true, behavior);
            meleeDebrisBottomRight.Stop(true, behavior);
            meleeDebrisTopLeft.Stop(true, behavior);
            meleeDebrisTopRight.Stop(true, behavior);
        }

        public static void ShatterMelee(Target target)
        {
            //if (preview.isActive) return;
            if (!CanEmit(target)) return;

            var data = target.data;
            if (data.behavior != TargetBehavior.Melee) return;
            ParticleSystem system1, system2;
            Target prevTarget;
            TargetHandType hand;
            if (data.x > 0)
            {
                if(data.y > 0)
                {
                    system1 = meleeDebrisTopRight;
                    system2 = meleeStationaryTopRight;                   
                }
                else
                {
                    system1 = meleeDebrisBottomRight;
                    system2 = meleeStationaryBottomRight;
                }
                hand = TargetHandType.Right;
                prevTarget = GetLastTargetWithHand(data, hand);
            }
            else
            {
                if(data.y > 0)
                {
                    system1 = meleeDebrisTopLeft;
                    system2 = meleeStationaryTopLeft;
                }
                else
                {
                    system1 = meleeDebrisBottomLeft;
                    system2 = meleeStationaryBottomLeft;
                }
                hand = TargetHandType.Left;
                prevTarget = GetLastTargetWithHand(data, hand);
            }
            DoShatter(target, system1, system2, prevTarget, hand);
        }

        private static void DoShatter(Target target, ParticleSystem system1, ParticleSystem system2, Target prev, TargetHandType hand)
        {
            system1.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            system2.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            Vector3 position;
            if (preview.IsActive)
            {
                var previewTarget = preview.GetPreviewTarget(target);
                if (previewTarget == null)
                    return;

                position = previewTarget.TargetData.transformData.position;
                position.x += Mathf.Sign(position.x);
                position.y += 1f;
                position.z += 1f;
            }
            else
            {
                position = target.data.position;
            }
            system1.transform.position = position;
            system2.transform.position = position;
            if(prev != null && !preview.IsActive)
            {
                Vector3 targetPos = prev.gridTargetIcon.transform.position;
                targetPos.z = 0f;
                system1.transform.LookAt(targetPos);
                system2.transform.LookAt(targetPos);
                Vector3 euler = system1.transform.eulerAngles;
                euler.x -= 180f;
                system1.transform.eulerAngles = euler;
                system2.transform.eulerAngles = euler;
            }
            else
            {
                Vector3 rot = new Vector3(hand == TargetHandType.Left ? 0 : 180f, -90f, 0f);
                system1.transform.eulerAngles = rot;
                system2.transform.eulerAngles = rot;
            }
            system1.Play();
            system2.Play();
        }

        private static Target GetLastTargetWithHand(TargetData data, TargetHandType hand)
        {           
            if(hand == TargetHandType.Left && lastLeftTarget != null)
            {
                if(IsTargetInValidTime(data, lastLeftTarget))
                {
                    return lastLeftTarget;
                }
            }
            else if(hand == TargetHandType.Right && lastRightTarget != null)
            {
                if(IsTargetInValidTime(data, lastRightTarget))
                {
                    return lastRightTarget;
                }
            }
            NoteEnumerator notes = new NoteEnumerator(new QNT_Timestamp(0), data.time);
            notes.reverse = true;
            foreach(var note in notes)
            {
                if (note.data.time == data.time) continue;
                if(!IsTargetInValidTime(data, note))
                {
                    return null;
                }

                if (note.data.handType != hand) continue;
                return note;
            }
            return null;
        }

        private static Relative_QNT maxTime = new Relative_QNT((long)Constants.QuarterNoteDuration.tick * 4);
        private static bool IsTargetInValidTime(TargetData meleeData, Target target)
        {
            var time = meleeData.time - maxTime;
            return target.data.time >= time;
        }

        private static bool CanEmit(Target target, bool sustain = false) 
            => allowEmission && target != null && target.data != null && 
            (sustain ? NRSettings.config.enableSustainAnimation : NRSettings.config.enableGridParticles);
    }
}
