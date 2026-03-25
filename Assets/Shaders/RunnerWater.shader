Shader "Featurehole/Runner/Water"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.55, 0.9, 0.96, 0.82)
        _DeepColor ("Deep Color", Color) = (0.08, 0.29, 0.54, 0.9)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _WaveTex ("Wave Texture", 2D) = "gray" {}
        _DetailTex ("Detail Texture", 2D) = "gray" {}
        _FoamTex ("Foam Texture", 2D) = "white" {}
        _WaveScale ("Wave Scale", Float) = 2.2
        _DetailScale ("Detail Scale", Float) = 4.5
        _FlowSpeedA ("Flow Speed A", Float) = 0.08
        _FlowSpeedB ("Flow Speed B", Float) = 0.14
        _WaveHeight ("Wave Height", Float) = 0.07
        _NormalStrength ("Normal Strength", Float) = 1.3
        _FoamStrength ("Foam Strength", Float) = 0.5
        _FresnelStrength ("Fresnel Strength", Float) = 1.8
        _Smoothness ("Smoothness", Range(0,1)) = 0.92
        _Metallic ("Metallic", Range(0,1)) = 0.02
        _Alpha ("Alpha", Range(0,1)) = 0.78
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        LOD 300
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade vertex:vert
        #pragma target 3.0

        sampler2D _WaveTex;
        sampler2D _DetailTex;
        sampler2D _FoamTex;

        half _WaveScale;
        half _DetailScale;
        half _FlowSpeedA;
        half _FlowSpeedB;
        half _WaveHeight;
        half _NormalStrength;
        half _FoamStrength;
        half _FresnelStrength;
        half _Smoothness;
        half _Metallic;
        half _Alpha;
        fixed4 _ShallowColor;
        fixed4 _DeepColor;
        fixed4 _FoamColor;

        struct Input
        {
            float2 uv_WaveTex;
            float3 worldPos;
            float3 viewDir;
        };

        void vert(inout appdata_full v)
        {
            float2 uv = v.texcoord.xy;
            float t = _Time.y;

            float waveA = tex2Dlod(_WaveTex, float4(uv * _WaveScale + float2(0, t * _FlowSpeedA), 0, 0)).r;
            float waveB = tex2Dlod(_DetailTex, float4(uv * _DetailScale + float2(0.13, t * _FlowSpeedB), 0, 0)).r;
            float wave = (waveA * 0.65 + waveB * 0.35) - 0.5;

            v.vertex.y += wave * _WaveHeight;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float t = _Time.y;
            float2 uvA = IN.uv_WaveTex * _WaveScale + float2(0, t * _FlowSpeedA);
            float2 uvB = IN.uv_WaveTex * _DetailScale + float2(0.17, t * _FlowSpeedB);

            float waveA = tex2D(_WaveTex, uvA).r;
            float waveB = tex2D(_DetailTex, uvB).r;
            float waveMix = saturate(waveA * 0.58 + waveB * 0.42);

            float2 eps = float2(0.01, 0.0);
            float h = waveMix;
            float hx = tex2D(_WaveTex, uvA + eps).r * 0.58 + tex2D(_DetailTex, uvB + eps).r * 0.42;
            float hy = tex2D(_WaveTex, uvA + eps.yx).r * 0.58 + tex2D(_DetailTex, uvB + eps.yx).r * 0.42;
            float3 pseudoNormal = normalize(float3((h - hx) * _NormalStrength, (h - hy) * _NormalStrength, 1.0));

            float depthLerp = saturate(pow(1.0 - waveMix, 1.35));
            fixed3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, depthLerp);

            float foamSample = tex2D(_FoamTex, IN.uv_WaveTex * (_DetailScale * 0.8) + float2(0.0, t * (_FlowSpeedB * 1.2))).a;
            float crest = saturate((waveMix - 0.62) * 2.4);
            float foam = saturate(foamSample * crest * _FoamStrength);

            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), float3(0, 0, 1))), 3.2) * _FresnelStrength;
            float sparkle = saturate(pow(waveB, 6.0) * 1.8 + fresnel * 0.65);

            o.Albedo = waterColor;
            o.Normal = pseudoNormal;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Emission = _FoamColor.rgb * (foam + sparkle);
            o.Alpha = saturate(_Alpha + foam * 0.12);
        }
        ENDCG
    }

    FallBack "Transparent/Diffuse"
}
