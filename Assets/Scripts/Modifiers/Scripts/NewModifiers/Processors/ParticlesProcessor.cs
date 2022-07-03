using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class ParticlesProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.Particles;

        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.amount.DisplayName = "Particle Amount";
        }

        internal override string Hint => "The amount of particles to show up.";

        internal override Vector2 AmountMinMax => new(0, 1000);
    }
}