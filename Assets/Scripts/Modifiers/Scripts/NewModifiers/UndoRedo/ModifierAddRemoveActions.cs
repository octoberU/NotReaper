using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Timing;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class AddModifierAction : ModifierAction
    {
        private Modifier addedModifier;
        private QNT_Timestamp startTime;
        private Track track;

        private bool initialAdd = true;

        public AddModifierAction(QNT_Timestamp startTime, Track track)
        {
            this.startTime = startTime;
            this.track = track;
        }
        
        public override void DoAction(ModifierManager manager)
        {
            if (initialAdd)
            {
                addedModifier = manager.PlaceModifierFromAction(startTime, track);
                initialAdd = false;
            }
            else
            {
                addedModifier = manager.PlaceModifierFromAction(addedModifier.timeframe, addedModifier.Track);
            }
        }

        public override void UndoAction(ModifierManager manager)
        {
            if (addedModifier != null)
            {
                manager.RemoveModifierFromAction(addedModifier);
            }
        }
    }

    public class MultiAddModifierAction : ModifierAction
    {
        private List<Modifier> addedModifiers = new();
        private List<Data> datas;

        public MultiAddModifierAction(List<Data> datas) =>  this.datas = datas;

        public override void DoAction(ModifierManager manager)
        {
            foreach (var data in datas)
            {
                addedModifiers.Add(manager.LoadModifier(data));
            }
        }

        public override void UndoAction(ModifierManager manager)
        {
            if (addedModifiers.Count <= 0) return;
            
            foreach(var modifier in addedModifiers)
                manager.RemoveModifierFromAction(modifier);
                
            addedModifiers.Clear();
        }
    }

    public class RemoveModifierAction : ModifierAction
    {
        private Modifier modifier;
        private Data data;

        public RemoveModifierAction(Modifier modifier)
        {
            this.modifier = modifier;
            data = modifier.Data;
        }
        
        public override void DoAction(ModifierManager manager)
        {
            manager.RemoveModifierFromAction(modifier);
        }

        public override void UndoAction(ModifierManager manager)
        {
            modifier = manager.LoadModifier(data);
        }
    }

    public class MultiRemoveModifierAction : ModifierAction
    {
        private List<Data> datas = new();
        private List<Modifier> modifiers;
        
        public MultiRemoveModifierAction(List<Modifier> modifiers)
        {
            this.modifiers = modifiers;
            foreach (var modifier in modifiers)
            {
                datas.Add(modifier.Data);
            }
        }
        public override void DoAction(ModifierManager manager)
        {
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                manager.RemoveModifierFromAction(modifiers[i]);
            }
        }

        public override void UndoAction(ModifierManager manager)
        {
            modifiers.Clear();
            foreach (var data in datas)
            {
                modifiers.Add(manager.LoadModifier(data));
            }
        }
    }
    
}
