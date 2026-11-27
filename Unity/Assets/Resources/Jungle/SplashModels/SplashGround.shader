
Shader "Jungle/Splash Ground"
{
    Properties
    {
        _MainTex ("Ground color", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;

        struct Input { float3 worldPos; };

        float hashCell(float2 cell)
        {
            return frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
        }

        float groundNoise(float2 p)
        {
            float2 cell = floor(p);
            float2 f = smoothstep(0, 1, frac(p));
            return lerp(lerp(hashCell(cell), hashCell(cell + float2(1,0)), f.x),
                lerp(hashCell(cell + float2(0,1)), hashCell(cell + 1), f.x), f.y);
        }

        void surf(Input input, inout SurfaceOutputStandard surface)
        {
            float2 p = input.worldPos.xz;
            float variation = groundNoise(p * .27) * .7 + groundNoise(p * 1.1) * .3;
            float3 sampleColor = tex2D(_MainTex, p * .095).rgb;
            float3 earth = lerp(float3(.36,.49,.17), float3(.67,.64,.33), smoothstep(.36,.76,variation));
            float3 albedo = earth * lerp(.68, 1.22, dot(sampleColor, float3(.299,.587,.114)));
            float fine = groundNoise(p * 13.5);
            albedo *= lerp(.87,1.12,fine);

            float2 leafGrid = p * 3.1;
            float seed = hashCell(floor(leafGrid));
            float2 leaf = frac(leafGrid) - float2(.28 + seed * .4, .48);
            float angle = seed * 6.283;
            leaf = mul(float2x2(cos(angle),-sin(angle),sin(angle),cos(angle)), leaf);
            float shape = abs(leaf.x) * 5.5 + abs(leaf.y) * 18;
            float litter = step(.79, seed) * (1 - smoothstep(.7,1.0,shape));
            albedo = lerp(albedo, float3(.26,.19,.074), litter * .63);
            surface.Albedo = albedo * _Color.rgb;
            surface.Smoothness = .09;
            surface.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}

