using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Models;
using NotReaper.Targets;
using UnityEngine;

namespace NotReaper
{
    public class HitsoundData : ContentData
    {
        public TimelineHitsound type => targetData.velocity.ToTimelineHitsound(targetData.behavior is TargetBehavior.Melee);
        public Target target;
        public TargetData targetData => target.data;
        public bool isDual;
    }

    public enum TimelineHitsound
    {
        Standard = 0,
        Snare = 1,
        Percussion = 2,
        ChainStart = 3,
        ChainNode = 4,
        Melee = 5,
        Silent = 6,
        StandardMelee = 7,
        SnareMelee = 8,
        Mine = 9
    }

    public static class TimelineHitsoundExtensions
    {
        public static string ToDisplayName(this TimelineHitsound hitsound) =>
            hitsound switch
            {
                TimelineHitsound.Standard => "Kick",
                TimelineHitsound.Snare => "Snare",
                TimelineHitsound.Percussion => "Percussion",
                TimelineHitsound.Melee => "Shatter",
                TimelineHitsound.ChainStart => "Open Hat",
                TimelineHitsound.ChainNode => "Hi Hat",
                TimelineHitsound.Silent => "Silent",
                TimelineHitsound.StandardMelee => "Standard Melee",
                TimelineHitsound.SnareMelee => "Snare Melee",
                _ => ""
            };

        public static bool IsMelee(this TimelineHitsound hitsound) =>
            hitsound switch
            {
                TimelineHitsound.StandardMelee => true,
                TimelineHitsound.SnareMelee => true,
                _ => false
            };

        public static InternalTargetVelocity ToInternalVelocity(this TimelineHitsound hitsound) =>
            hitsound switch
            {
                TimelineHitsound.Standard => InternalTargetVelocity.Kick,
                TimelineHitsound.Snare => InternalTargetVelocity.Snare,
                TimelineHitsound.Percussion => InternalTargetVelocity.Percussion,
                TimelineHitsound.ChainStart => InternalTargetVelocity.ChainStart,
                TimelineHitsound.ChainNode => InternalTargetVelocity.Chain,
                TimelineHitsound.Melee => InternalTargetVelocity.Melee,
                TimelineHitsound.Silent => InternalTargetVelocity.Silent,
                TimelineHitsound.StandardMelee => InternalTargetVelocity.Melee,
                TimelineHitsound.SnareMelee => InternalTargetVelocity.Snare,
                _ => InternalTargetVelocity.Kick
            };
    }
}