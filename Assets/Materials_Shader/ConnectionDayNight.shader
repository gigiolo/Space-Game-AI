Shader "Aetheris/ConnectionDayNight_Procedural"
{
    Properties
    {
        [Header(Colors)]
        [HDR]_NightColor ("Night Color (Glow)", Color) = (1, 0.8, 0, 1)
        _DayColor ("Day Color (Dark)", Color) = (0.1, 0.1, 0.1, 0.8)
        
        [Header(System)]
        _SunDirection ("Sun Direction", Vector) = (0,0,1,0)
        _PlanetPosition ("Planet Position", Vector) = (0,0,0,0)
        
        [Header(Fading Settings)]
        _TerminatorBias ("Day/Night Shift", Range(-1, 1)) = 0.0
        _TerminatorSoftness ("Day/Night Blend", Range(0.01, 1)) = 0.3
        _EdgeSoftness ("Line Edge Softness", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        
        Blend SrcAlpha OneMinusSrcAlpha 
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR; // <--- AGGIUNTO: Legge il colore (alpha) della particella
                float2 texcoord : TEXCOORD0; 
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR; // <--- AGGIUNTO: Passa il colore al fragment
                float2 texcoord : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            float4 _NightColor;
            float4 _DayColor;
            float3 _SunDirection;
            float3 _PlanetPosition;
            float _TerminatorBias;
            float _TerminatorSoftness;
            float _EdgeSoftness;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = v.color; // <--- AGGIUNTO: Assegnazione
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Sfumatura forma
                float fadeHorizontal = sin(i.texcoord.x * 3.14159);
                float distFromCenter = abs(i.texcoord.y - 0.5) * 2.0;
                float fadeVertical = smoothstep(1.0, 1.0 - _EdgeSoftness, distFromCenter);
                float shapeAlpha = fadeHorizontal * fadeVertical;

                // 2. Giorno/Notte
                float3 normalDir = normalize(i.worldPos - _PlanetPosition);
                float NdotL = dot(normalDir, _SunDirection);
                float dayMask = smoothstep(_TerminatorBias - _TerminatorSoftness, _TerminatorBias + _TerminatorSoftness, NdotL);

                // 3. Colore Finale
                float4 finalColor = lerp(_NightColor, _DayColor, dayMask);
                
                // --- MODIFICA CRITICA: Moltiplichiamo per l'alpha della particella (i.color.a) ---
                // Questo permette lo script di controllare la dissolvenza in entrata
                finalColor.a *= shapeAlpha * i.color.a; 

                return finalColor;
            }
            ENDCG
        }
    }
}