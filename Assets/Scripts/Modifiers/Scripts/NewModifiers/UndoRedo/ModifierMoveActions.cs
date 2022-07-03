using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ModifierMoveAction : ModifierAction
    {
        private List<MoveData> moveData;

        public ModifierMoveAction(List<MoveData> moveData) => this.moveData = moveData;
        
        public override void DoAction(ModifierManager manager)
        {
            foreach (var data in moveData)
            {
                manager.MoveModifierFromAction(data.modifier, data.newTimeframe);
            }
        }

        public override void UndoAction(ModifierManager manager)
        {
            foreach (var data in moveData)
            {
                manager.MoveModifierFromAction(data.modifier, data.oldTimeframe);
            }
        }

        public class MoveData
        {
            public Modifier modifier;
            public Timeframe oldTimeframe;
            public Timeframe newTimeframe;
        }
    }
}
