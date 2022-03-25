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
        private void Awake()
        {
            particlesLeft = transform.GetChild(0).GetComponent<ParticleSystem>();
            particlesRight = transform.GetChild(1).GetComponent<ParticleSystem>();
        }

        private void Start()
        {
            NRSettings.OnLoad(() =>
            {
                var mainLeft = particlesLeft.main;
                mainLeft.startColor = new ParticleSystem.MinMaxGradient(NRSettings.config.leftColor, NRSettings.config.leftColor);
                var mainRight = particlesRight.main;
                mainRight.startColor = new ParticleSystem.MinMaxGradient(NRSettings.config.rightColor, NRSettings.config.rightColor);
            });
        }

        public static void Emit(TargetData data)
        {
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
    }
}
