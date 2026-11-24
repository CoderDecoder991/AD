// Searchlight beam seen through night haze (VS-4, docs/VFX_AND_ANIMATION_SPEC.md §8).
// Drawn on a double-sided open cone (UV.y = 0 at the lamp, 1 at the far end). Cheap, no raymarching:
// - soft edges: brightness follows how squarely the cone surface faces the viewer, so the silhouette fades
//   to nothing (no hard-edged flat strips) and the core, where front and back faces overlap, is brightest;
// - falloff along the beam: light spreading and haze extinction, with a short fade-in at the lens;
// - dust: slowly drifting 3D value noise in world space, so the beam shimmers like light in airborne dust.
// Additive and fog-free like the other light-emitting effects (AdditiveGlowNoFog).
Shader "AirDefense/LightBeam"
{
    Properties
    {
        [HDR] _Color ("Colour (HDR)", Color) = (0.9, 0.95, 1, 1)
        _Intensity ("Intensity", Float) = 0.35
        _EdgePower ("Edge softness (higher = narrower core)", Float) = 2.2
        _Falloff ("Falloff along the beam", Float) = 5
        _NoiseScale ("Dust scale (1/m)", Float) = 0.08
        _NoiseSpeed ("Dust drift (m/s)", Float) = 1.5
        _NoiseAmount ("Dust amount", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Intensity, _EdgePower, _Falloff, _NoiseScale, _NoiseSpeed, _NoiseAmount;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float along : TEXCOORD2;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.along = v.uv.y;
                return o;
            }

            float hash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float noise(float3 x)
            {
                float3 i = floor(x), f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(hash(i), hash(i + float3(1,0,0)), f.x), lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x), lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 V = normalize(GetCameraPositionWS() - i.positionWS);
                float3 N = normalize(i.normalWS);
                float facing = pow(saturate(abs(dot(N, V))), _EdgePower);           // 0 at the silhouette
                float a = saturate(i.along);
                float along = smoothstep(0.0, 0.025, a) * pow(1.0 - a, 1.6) / (1.0 + a * _Falloff);
                float3 p = i.positionWS * _NoiseScale + float3(0.3, 0.1, 0.2) * _Time.y * _NoiseSpeed * _NoiseScale;
                float dust = noise(p) * 0.65 + noise(p * 2.7) * 0.35;
                float d = lerp(1.0, 0.55 + dust, _NoiseAmount);
                return half4(_Color.rgb * (_Intensity * facing * along * d), 1.0);
            }
            ENDHLSL
        }
    }
}
