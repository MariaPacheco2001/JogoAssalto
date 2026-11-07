// Material com textura projetada pelo mundo: não estica em caixas com escalas diferentes.
// "Tamanho da textura" é quantos metros ocupa uma repetição da imagem.
Shader "GolpeCalculado/Triplanar"
{
    Properties
    {
        _Color ("Cor", Color) = (1,1,1,1)
        _MainTex ("Textura", 2D) = "white" {}
        [Normal] _BumpMap ("Relevo (normal map)", 2D) = "bump" {}
        _BumpScale ("Força do relevo", Float) = 1
        _Glossiness ("Brilho", Range(0,1)) = 0.2
        _Metallic ("Metálico", Range(0,1)) = 0
        _TileSize ("Tamanho da textura (m)", Float) = 2
        _Blend ("Suavidade nas arestas", Range(1,16)) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex, _BumpMap;
        fixed4 _Color;
        half _BumpScale, _Glossiness, _Metallic, _TileSize, _Blend;

        struct Input { float3 worldPos; float3 worldNormal; INTERNAL_DATA };

        float3 WorldToTangent(Input IN, float3 n)
        {
            float3x3 t2w = float3x3(WorldNormalVector(IN, float3(1,0,0)), WorldNormalVector(IN, float3(0,1,0)), WorldNormalVector(IN, float3(0,0,1)));
            return normalize(mul(t2w, n));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 wn = WorldNormalVector(IN, float3(0,0,1));
            float3 w = pow(abs(wn), _Blend);
            w /= (w.x + w.y + w.z);
            float3 p = IN.worldPos / max(_TileSize, 0.01);
            float2 uvX = p.zy, uvY = p.xz, uvZ = p.xy;

            fixed4 c = tex2D(_MainTex, uvX) * w.x + tex2D(_MainTex, uvY) * w.y + tex2D(_MainTex, uvZ) * w.z;
            o.Albedo = c.rgb * _Color.rgb;

            half3 nX = UnpackScaleNormal(tex2D(_BumpMap, uvX), _BumpScale);
            half3 nY = UnpackScaleNormal(tex2D(_BumpMap, uvY), _BumpScale);
            half3 nZ = UnpackScaleNormal(tex2D(_BumpMap, uvZ), _BumpScale);
            nX = half3(nX.xy + wn.zy, abs(nX.z) * wn.x);
            nY = half3(nY.xy + wn.xz, abs(nY.z) * wn.y);
            nZ = half3(nZ.xy + wn.xy, abs(nZ.z) * wn.z);
            float3 n = normalize(nX.zyx * w.x + nY.xzy * w.y + nZ.xyz * w.z);
            o.Normal = WorldToTangent(IN, n);

            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
