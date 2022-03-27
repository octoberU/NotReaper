using NotReaper;
using NotReaper.Models;
using NotReaper.Targets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI.Particles
{
    public class GridParticles : MonoBehaviour
    {
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

        private void Awake()
        {
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
                var leftColor = new ParticleSystem.MinMaxGradient(NRSettings.config.leftColor, NRSettings.config.leftColor);
                var rightColor = new ParticleSystem.MinMaxGradient(NRSettings.config.rightColor, NRSettings.config.rightColor);
                var mainLeft = particlesLeft.main;
                mainLeft.startColor = leftColor;
                var mainRight = particlesRight.main;
                mainRight.startColor = rightColor;

                var susLeft = sustainLeft.main;
                susLeft.startColor = leftColor;
                var susRight = sustainRight.main;
                susRight.startColor = rightColor;
            });
        }

        public static void Emit(TargetData data)
        {
            if (!NRSettings.config.enableGridParticles) return;
            if (data.behavior == TargetBehavior.Melee || data.behavior == TargetBehavior.Mine) return;

            var particles = data.handType == TargetHandType.Left ? particlesLeft : particlesRight;
            var emission = particles.emission;
            var burst = emission.GetBurst(0);
            burst.count = data.behavior == TargetBehavior.ChainNode ? 10 : 100;
            emission.SetBurst(0, burst);
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            particles.transform.position = data.position;
            particles.Play();
        }

        public static void StartEmitSustain(TargetData data)
        {
            if (!NRSettings.config.enableSustainAnimation) return;
            if (data.behavior != TargetBehavior.Sustain) return;

            var particles = data.handType == TargetHandType.Left ? sustainLeft : sustainRight;
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            particles.transform.position = data.position;
            particles.Play();
        }
        public static void StopEmitSustain(TargetData data)
        {
            if (!NRSettings.config.enableSustainAnimation) return;
            if (data.behavior != TargetBehavior.Sustain) return;
            var particles = data.handType == TargetHandType.Left ? sustainLeft : sustainRight;
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        public static void ShatterMelee(TargetData data)
        {
            if (!NRSettings.config.enableGridParticles) return;
            if (data.behavior != TargetBehavior.Melee) return;
            ParticleSystem system1, system2;
            if(data.x > 0)
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
            }
            DoShatter(data, system1, system2);
        }

        private static void DoShatter(TargetData data, ParticleSystem system1, ParticleSystem system2)
        {
            system1.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            system2.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            system1.transform.position = data.position;
            system2.transform.position = data.position;
            system1.Play();
            system2.Play();
        }
    }
}
