using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ModifierIO : MonoBehaviour
    {

        [NRInject] private static ModifierManager manager;
        [NRInject] private static ModifierTrackManager trackManager;
        
        public static IEnumerator LoadModifiers(List<ModifierDTO> modifierData, List<TrackManager.TrackOrder> trackArrangement)
        {
            manager.SetIsLoading(true);

            bool hasTrackArrangement = false;

            if (trackArrangement != null && trackArrangement.Count > 0)
            {
                manager.LoadTrackArrangement(trackArrangement);
                hasTrackArrangement = true;
            }

            if (!hasTrackArrangement && modifierData != null && modifierData.Count > 0)
                manager.LoadTrackArrangement(ModifierTrackManager.DefaultModifierTrackOrder);

            foreach (var data in modifierData)
            {
                manager.LoadModifier(new Data
                {
                    startTick = (int)data.startTick,
                    endTick = (int)data.endTick,
                    amount = data.amount,
                    independantBool = data.independantBool,
                    leftHandColor = data.leftHandColor,
                    option1 = data.option1,
                    option2 = data.option2,
                    rightHandColor = data.rightHandColor,
                    type = Enum.Parse<ModifierType>(data.type),
                    typeIndex = data.typeTrackIndex,
                    value1 =  data.value1,
                    value2 = data.value2,
                    xoffset = data.xoffset,
                    yoffset = data.yoffset,
                    zoffset =  data.zoffset,
                });
            }
            manager.SetIsLoading(false);
            trackManager.SortAllTrackContent();
            manager.UpdateVisibleTracks();
            yield return null;
        }

        public static List<ModifierDTO> GetModifierData()
        {
            var modifiers = manager.Content;
            modifiers.Sort((m1, m2) => m1.startTime.CompareTo(m2.startTime));
            List<ModifierDTO> modifierData = new();
            foreach (var modifier in modifiers)
            {
                var data = modifier.GetData() as Data;
                modifierData.Add(new ModifierDTO
                {
                    amount = data.amount,
                    independantBool = data.independantBool,
                    endPosX = 0,
                    endTick = data.endTick,
                    leftHandColor = data.leftHandColor,
                    rightHandColor = data.rightHandColor,
                    type = data.type.ToString(),
                    typeTrackIndex = data.typeIndex,
                    miniEndX = 0,
                    miniStartX = 0,
                    option1 = data.option1,
                    option2 = data.option2,
                    value1 = data.value1,
                    value2 = data.value2,
                    xoffset = data.xoffset,
                    yoffset = data.yoffset,
                    zoffset = data.zoffset,
                    startTick = data.startTick,
                    startPosX = 0
                });
            }
            
            return modifierData;
        }
        
        public static List<TrackManager.TrackOrder> GetTrackArrangement() => trackManager.GetTrackArrangementData();
    }
}
