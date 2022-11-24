using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public static class ProcessorManager
    {
        private static Dictionary<ModifierType, Processor> processors = new Dictionary<ModifierType, Processor>();

        public static void RegisterProcessor(ModifierType type, Processor processor)
        {
            if (!processors.ContainsKey(type))
            {
                processors.Add(type, processor);
            }
        }

        public static bool TryGetProcessor(Modifier modifier, out Processor processor)
        {
            processor = null;
            if (!processors.ContainsKey(modifier.ModifierType)) return false;

            processor = processors[modifier.ModifierType];
            return true;
        }
    }
}
