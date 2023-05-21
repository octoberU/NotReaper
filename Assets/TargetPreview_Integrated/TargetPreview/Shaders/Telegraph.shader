// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "TargetPreview/Telegraph"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TelegraphBoost("_TelegraphBoost", Float) = 1.9
        [PerRendererData] [HDR]_Color ("Main Color", Color) = (1,1,1,1)
        [PerRendererData] _Strength("Twirl", Float) = 1.0
        [PerRendererData] _Spherize("Spherize", Float) = 1.0
        [PerRendererData] _Scale("Scale", Float) = 1.0
        [PerRendererData] _Rotate("Rotate", Float) = 1.0
        [PerRendererData] _SpinSpeed("Spin Speed", Float) = 1.0
        [PerRendererData] _MaskScale("Mask Scale", Float) = 1.0
        
        //Fading in and out
        [PerRendererData] _FadeInDuration("Fade out duration", Float) = 100.0
        [PerRendererData] _FadeOutDuration("Fade in duration", Float) = 100.0
        [PerRendererData] _TargetTime("Current target time (Set in c#)", Float) = 0
        
        [PerRendererData] _TargetTextures ("Texture", 2DArray) = "" {}
        [PerRendererData] _TextureIndex ("Texture Index", Range(0, 7)) = 0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma require 2darray
            #pragma target 3.5
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float2 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _GlobalTime;
            float _TelegraphBoost;

            UNITY_DECLARE_TEX2DARRAY(_TargetTextures);

            UNITY_INSTANCING_BUFFER_START(Props)
                fixed4 _Color;
                fixed _Strength;
                fixed _Scale;
                fixed _Spherize;
                fixed _Rotate;
                fixed _SpinSpeed;
                fixed _MaskScale;
                
                fixed _FadeOutDuration;
                fixed _FadeInDuration;
                fixed _TargetTime;
                fixed _TextureIndex;
            UNITY_INSTANCING_BUFFER_END(Props)
            
            fixed2 Twirl(fixed2 UV, fixed2 Center, fixed Strength, fixed2 Offset)
            {
                fixed2 delta = UV - Center;
                fixed angle = Strength * length(delta);
                fixed x = cos(angle) * delta.x - sin(angle) * delta.y;
                fixed y = sin(angle) * delta.x + cos(angle) * delta.y;
                return fixed2(x + Center.x + Offset.x, y + Center.y + Offset.y);
            }

            fixed2 Scale(fixed amount, fixed2 uv)
            {
                fixed2 firstOffset = uv - fixed2(0.5, 0.5);
                fixed2 scale = firstOffset * fixed2(amount, amount);
                return scale + fixed2(0.5, 0.5);
            }

            fixed2 Spherize(fixed2 UV, fixed2 Center, fixed Strength, fixed2 Offset)
            {
                fixed2 delta = UV - Center;
                fixed delta2 = dot(delta.xy, delta.xy);
                fixed delta4 = delta2 * delta2;
                fixed2 delta_offset = delta4 * Strength;
                return UV + delta * delta_offset + Offset;
            }

            fixed2 Rotate(fixed2 UV, fixed2 Center, fixed Rotation)
            {
                Rotation = Rotation * (3.1415926f/180.0f);
                UV -= Center;
                fixed s = sin(Rotation);
                fixed c = cos(Rotation);
                fixed2x2 rMatrix = fixed2x2(c, -s, s, c);
                rMatrix *= 0.5;
                rMatrix += 0.5;
                rMatrix = rMatrix * 2 - 1;
                UV.xy = mul(UV.xy, rMatrix);
                UV += Center;
                return UV;
            }
            


            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                //fixed fadeOut = clamp(((_GlobalTime - _TargetTime ) / _FadeOutDuration), 0, 1);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = Scale(UNITY_ACCESS_INSTANCED_PROP(Props, _MaskScale), v.uv2);
                o.uv = Rotate(o.uv, fixed2(0.5,0.5), UNITY_ACCESS_INSTANCED_PROP(Props, _Rotate) + (_Time.y * UNITY_ACCESS_INSTANCED_PROP(Props, _SpinSpeed)));
                o.uv = Scale(UNITY_ACCESS_INSTANCED_PROP(Props, _Scale), o.uv);
                o.uv = Twirl(o.uv, fixed2(0.5,0.5), UNITY_ACCESS_INSTANCED_PROP(Props, _Strength), fixed2(0,0));

                //fixed fadeOut = clamp(1 - (((_GlobalTime + _FadeOutDuration) - (_TargetTime) ) / _FadeOutDuration), 0, 1);
                //o.vertex = UnityObjectToClipPos(v.vertex * fadeOut);


                o.uv = Spherize(o.uv, fixed2(0.5, 0.5), UNITY_ACCESS_INSTANCED_PROP(Props, _Spherize), fixed2(0,0));
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed3 projectedCoordinates = fixed3(i.uv2, UNITY_ACCESS_INSTANCED_PROP(Props, _TextureIndex));
                fixed4 maskTexture = UNITY_SAMPLE_TEX2DARRAY(_TargetTextures, projectedCoordinates);

                // sample the texture
                fixed4 col = pow(tex2D(_MainTex, i.uv), _TelegraphBoost) * _TelegraphBoost;
                //apply telegraph mask
                fixed4 mask = maskTexture * col;
                // tint the color
                fixed4 tint = mask * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                half globalTime = _GlobalTime;
                half fadeInDuration = UNITY_ACCESS_INSTANCED_PROP(Props, _FadeInDuration);
                half fadeOutDuration = UNITY_ACCESS_INSTANCED_PROP(Props, _FadeOutDuration);
                half targetTime = UNITY_ACCESS_INSTANCED_PROP(Props, _TargetTime);
                
                fixed fadeIn = clamp((1 * (((globalTime + fadeInDuration) - targetTime ) / fadeInDuration)) + 1, 0, 1); //Fade in the target by duration.

                fixed fadeOut = clamp(1 - (((globalTime + fadeOutDuration) - (targetTime) ) / fadeOutDuration), 0, 1);
                tint *= fixed4(1, 1, 1, min(fadeIn,fadeOut));
                return tint;
            }


            ENDCG
        }
    }
}
