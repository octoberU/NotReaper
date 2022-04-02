using NotReaper.Targets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.TargetEditor
{
    public static class EditorTargetSpawner
    {
        private static TargetSpawner spawner;
        static EditorTargetSpawner()
        {
            spawner = NRDependencyInjector.Get<TargetSpawner>();
        }
        /// <summary>
        /// Spawns a new target.
        /// </summary>
        /// <param name="data">The data for the target.</param>
        /// <param name="transient">True if a transient note (e.g. pathbuilder note)</param>
        /// <returns>The spawned target.</returns>
        public static Target SpawnTarget(TargetData data, bool transient) => spawner.SpawnTarget(data, transient);
        public static void ReturnTarget(Target target) => spawner.ReturnTarget(target);
    }
}
