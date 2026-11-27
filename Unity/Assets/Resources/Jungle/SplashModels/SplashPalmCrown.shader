
Shader "Jungle/Splash Palm Crown"
{
    Properties
    {
        _MainTex ("Color", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 1
        _Glossiness ("Smoothness", Range(0,1)) = .18
        _PalmBounds ("Palm base and height", Vector) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard vertex:ApplyWind addshadow
        #include "UnityStandardUtils.cginc"
        sampler2D _MainTex, _BumpMap;
        fixed4 _Color;
        float _BumpScale, _Glossiness;
        float4 _PalmBounds;
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
        };

        void ApplyWind(inout appdata_full vertex)
        {
            float3 world = mul(unity_ObjectToWorld, vertex.vertex).xyz;
            float height = (world.y - _PalmBounds.y) / max(_PalmBounds.w, .001);
            float3 albedo = tex2Dlod(_MainTex, float4(vertex.texcoord.xy, 0, 1)).rgb;
            float foliage = smoothstep(.025, .11, albedo.g - albedo.r);
            float weight = smoothstep(.58, .94, height) * foliage;
            float phase = _Time.y * 1.6 + dot(world.xz, float2(.53, .29));
            float3 offset = float3(sin(phase), .32 * cos(phase * 1.1), cos(phase * .86));
            vertex.vertex.xyz += mul((float3x3)unity_WorldToObject, offset * weight * _PalmBounds.w * .023);
        }

        void surf(Input input, inout SurfaceOutputStandard surface)
        {
            surface.Albedo = tex2D(_MainTex, input.uv_MainTex).rgb * _Color.rgb;
            surface.Normal = UnpackScaleNormal(tex2D(_BumpMap, input.uv_BumpMap), _BumpScale);
            surface.Smoothness = _Glossiness;
            surface.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}

