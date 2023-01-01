using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Models;
using NotReaper.Targets;
using UnityEngine;

namespace NotReaper
{
    public class AssetContainer : ScriptableObject
    {
        [Header("Grid Target Sprites")]
        public SpritePack standardGrid;
        public SpritePack sustainGrid;
        public SpritePack horizontalGrid;
        public SpritePack verticalGrid;
        public SpritePack chainStartGrid;
        public SpritePack chainNodeGrid;
        public SpritePack meleeGrid;
        public SpritePack mineGrid;
        
        [Space, Header("Timeline Target Sprites")]
        public SpritePack standardTimeline;
        public SpritePack sustainTimeline;
        public SpritePack horizontalTimeline;
        public SpritePack verticalTimeline;
        public SpritePack chainStartTimeline;
        public SpritePack chainNodeTimeline;
        public SpritePack meleeTimeline;
        public SpritePack mineTimeline;

        [Space, Header("Noise Textures")]
        public Texture standard;
        public Texture fracture;

        [Space, Header("Hitsound Sprites")]
        public Sprite kick;
        public Sprite snare;
        public Sprite percussion;
        public Sprite chainStart;
        public Sprite chainNode;
        public Sprite melee;
        public Sprite mine;
        public Sprite silent;
        
        public static AssetContainer Instance { get; private set; } = null;

        private static Dictionary<TargetDefinition, TargetProperties> targetProperties { get;  } = new();
        private static Dictionary<HitsoundDefinition, Property> hitsoundProperties { get; } = new();
        private static Dictionary<InternalTargetVelocity, Sprite> hitsoundSprites { get; } = new();

        public struct TargetProperties
        {
            public Property target;
            public Property ring;
            public Property selectRing;
            public Property preFade;
            public LineRendererProperty lineRenderer;
        }

        public struct Property
        {
            public Sprite sprite;
            public MaterialPropertyBlock block;
        }

        public struct LineRendererProperty
        {
            public MaterialPropertyBlock block;
            public Color color;
        }

        public record HitsoundDefinition
        {
            public InternalTargetVelocity velocity;
            public TargetHandType handType;
            
            public HitsoundDefinition(InternalTargetVelocity velocity, TargetHandType handType)
            {
                this.velocity = velocity;
                this.handType = handType;
            }
        }
        
        public record TargetDefinition
        {
            public TargetBehavior behavior;
            public TargetHandType handType;
            public TargetIconLocation location;

            public TargetDefinition(TargetIconLocation location, TargetBehavior behavior, TargetHandType handType)
            {
                this.behavior = behavior;
                this.handType = handType;
                this.location = location;
            }
        }

        public void CreatePresetProperties()
        {
            if (Instance != null)
            {
                Debug.LogError("Tried to create a second instance of asset container!");
                return;
            }
            
            Instance = this;
            targetProperties.Clear();
            hitsoundSprites.Clear();
            
            hitsoundSprites.Add(InternalTargetVelocity.Kick, kick);
            hitsoundSprites.Add(InternalTargetVelocity.Snare, snare);
            hitsoundSprites.Add(InternalTargetVelocity.Percussion, percussion);
            hitsoundSprites.Add(InternalTargetVelocity.ChainStart, chainStart);
            hitsoundSprites.Add(InternalTargetVelocity.Chain, chainNode);
            hitsoundSprites.Add(InternalTargetVelocity.Melee, melee);
            hitsoundSprites.Add(InternalTargetVelocity.Mine, mine);
            hitsoundSprites.Add(InternalTargetVelocity.Silent, silent);
            
            UpdateTargetColors();
        }

        public static void UpdateTargetColors()
        {
            targetProperties.Clear();
            for (int i = 0; i < Enum.GetValues(typeof(TargetBehavior)).Length - 2; i++) //loop through all behaviors minus none and legacy pb
            {
                AddProperties((TargetBehavior)i);
            }
            
            
            hitsoundProperties.Clear();
            var values = Enum.GetValues(typeof(InternalTargetVelocity));
            foreach (var velocity in values)
            {
                AddHitsoundProperties((InternalTargetVelocity)velocity);
            }
        }

        private static void AddProperties(TargetBehavior behavior)
        {
            //Timeline - Left
            TargetDefinition timelineLeft = new(TargetIconLocation.Timeline, behavior, TargetHandType.Left);
            targetProperties.Add(timelineLeft, GenerateTargetProperties(timelineLeft));
            
            //Timeline - Right
            TargetDefinition timelineRight = new(TargetIconLocation.Timeline, behavior, TargetHandType.Right);
            targetProperties.Add(timelineRight, GenerateTargetProperties(timelineRight));
            
            //Timeline - Either
            TargetDefinition timelineEither = new(TargetIconLocation.Timeline, behavior, TargetHandType.Either);
            targetProperties.Add(timelineEither, GenerateTargetProperties(timelineEither));
            
            //Grid - Left
            TargetDefinition gridLeft = new(TargetIconLocation.Grid, behavior, TargetHandType.Left);
            targetProperties.Add(gridLeft, GenerateTargetProperties(gridLeft));
            
            //Grid - Right
            TargetDefinition gridRight = new(TargetIconLocation.Grid, behavior, TargetHandType.Right);
            targetProperties.Add(gridRight, GenerateTargetProperties(gridRight));
            
            //Grid - Either
            TargetDefinition gridEither = new(TargetIconLocation.Grid, behavior, TargetHandType.Either);
            targetProperties.Add(gridEither, GenerateTargetProperties(gridEither));
        }

        private static TargetProperties GenerateTargetProperties(TargetDefinition definition)
        {
            TargetProperties properties = new();
            bool isGrid = definition.location is TargetIconLocation.Grid;
            var colorProperty = isGrid ? "_Tint" : "_Color";
            var color = GetColorForTarget(definition.behavior, definition.handType);
            var pack = GetSpritePackForBehavior(definition.behavior, definition.location);

            properties.target.sprite = pack.target;
            properties.target.block = new();
            properties.target.block.SetTexture("_MainTex", pack.target.texture);
            properties.target.block.SetColor(colorProperty, color);

            properties.selectRing.sprite = pack.ring;
            properties.selectRing.block = new();
            properties.selectRing.block.SetTexture("_MainTex", pack.ring.texture);
            
            properties.lineRenderer.block = new();
            properties.lineRenderer.block.SetColor("_Tint", color);
            properties.lineRenderer.block.SetColor("_Color", color);
            properties.lineRenderer.color = color;

            if (isGrid)
            {

                properties.ring.sprite = pack.ring;
                properties.ring.block = new();
                properties.ring.block.SetTexture("_MainTex", pack.ring.texture);
                properties.ring.block.SetColor("_Tint", color);

                properties.preFade.sprite = pack.telegraph;
                properties.preFade.block = new();
                properties.preFade.block.SetTexture("_MainTex", pack.telegraph.texture);
                properties.preFade.block.SetColor("_Tint", color);
                properties.preFade.block.SetTexture("Texture2D_EFB53AD2", pack.noise);
                properties.preFade.block.SetFloat("Vector1_6D268C6B", pack.bloom);
            }

            return properties;
        }
        
        private static void AddHitsoundProperties(InternalTargetVelocity velocity)
        {
            HitsoundDefinition left = new(velocity, TargetHandType.Left);
            hitsoundProperties.Add(left, GenerateHitsoundProperties(left));

            HitsoundDefinition right = new(velocity, TargetHandType.Right);
            hitsoundProperties.Add(right, GenerateHitsoundProperties(right));
            
            HitsoundDefinition either = new(velocity, TargetHandType.Either);
            hitsoundProperties.Add(either, GenerateHitsoundProperties(either));
        }

        private static Property GenerateHitsoundProperties(HitsoundDefinition definition)
        {
            Property property = new();
            property.sprite = hitsoundSprites[definition.velocity];
            property.block = new();
            property.block.SetTexture("_MainTex", property.sprite.texture);
            property.block.SetColor("_Tint", GetColorForTarget(TargetBehavior.Standard, definition.handType));
            
            
            property.block.SetFloat("_FadeThreshold", 1.7f);
            property.block.SetFloat("_OpaqueDuration", 1f);
            property.block.SetFloat("_FadeOutThreshold", 0.5f);
            property.block.SetFloat("_WorldPosOffset", 0f);
            return property;
        }
        

        public static SpritePack GetGridSpritesForBehavior(TargetBehavior behavior) =>
            behavior switch
            {
                TargetBehavior.Standard => Instance.standardGrid,
                TargetBehavior.Vertical => Instance.verticalGrid,
                TargetBehavior.Horizontal => Instance.horizontalGrid,
                TargetBehavior.Sustain => Instance.sustainGrid,
                TargetBehavior.ChainStart => Instance.chainStartGrid,
                TargetBehavior.ChainNode => Instance.chainNodeGrid,
                TargetBehavior.Melee => Instance.meleeGrid,
                TargetBehavior.Mine => Instance.mineGrid,
                _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "No sprite pack available for this behavior!")
            };
        
        public static SpritePack GetTimelineSpritesForBehavior(TargetBehavior behavior) =>
            behavior switch
            {
                TargetBehavior.Standard => Instance.standardTimeline,
                TargetBehavior.Vertical => Instance.verticalTimeline,
                TargetBehavior.Horizontal => Instance.horizontalTimeline,
                TargetBehavior.Sustain => Instance.sustainTimeline,
                TargetBehavior.ChainStart => Instance.chainStartTimeline,
                TargetBehavior.ChainNode => Instance.chainNodeTimeline,
                TargetBehavior.Melee => Instance.meleeTimeline,
                TargetBehavior.Mine => Instance.mineTimeline,
                _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "No sprite pack available for this behavior!")
            };

        public static SpritePack GetSpritePackForBehavior(TargetBehavior behavior, TargetIconLocation location)
            => location is TargetIconLocation.Grid ? GetGridSpritesForBehavior(behavior) : GetTimelineSpritesForBehavior(behavior);

        public static TargetProperties GetVisualProperties(TargetBehavior behavior, TargetHandType handType, TargetIconLocation location)
            => targetProperties[new TargetDefinition(location, behavior, handType)];

        public static (TargetProperties, Property) GetTargetProperties(TargetData data, TargetIconLocation location)
            => (targetProperties[new TargetDefinition(location, data.behavior, data.handType)], hitsoundProperties[new HitsoundDefinition(data.velocity, data.handType)]);

        public static Property GetHitsoundProperty(InternalTargetVelocity velocity, TargetHandType handType)
            => hitsoundProperties[new HitsoundDefinition(velocity, handType)];

        public static Color GetColorForTarget(TargetBehavior behavior, TargetHandType handType) =>
            handType switch
            {
                TargetHandType.Either when behavior is TargetBehavior.Mine => Color.red,
                TargetHandType.Either  => UserPrefsManager.bothColor,
                TargetHandType.Right => NRSettings.config.rightColor,
                TargetHandType.Left => NRSettings.config.leftColor,
                _ => throw new ArgumentOutOfRangeException(nameof(handType), handType, "Invalid hand type!")
            };
    }
}
