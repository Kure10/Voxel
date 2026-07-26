Shader "Custom/CubeFrame"
{
    Properties
    {
        [HDR] _Color ("Frame Color", Color) = (0, 1, 1, 1)
        _Thickness ("Thickness", Range(0.01, 0.2)) = 0.05
    }
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 localPos     : TEXCOORD0;
            };

            // Definice vlastností kompatibilní s URP CBufferem
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Thickness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Převod pozice do Clip Space pomocí moderní URP funkce
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // Převod z rozsahu kostky (-0.5 až 0.5) na (0.0 až 1.0)
                output.localPos = input.positionOS.xyz + 0.5;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Detekce hran kostky
                bool xEdge = input.localPos.x < _Thickness || input.localPos.x > (1.0 - _Thickness);
                bool yEdge = input.localPos.y < _Thickness || input.localPos.y > (1.0 - _Thickness);
                bool zEdge = input.localPos.z < _Thickness || input.localPos.z > (1.0 - _Thickness);

                // Pokud jsme na průsečíku hran, vykreslíme barvu rámu
                if ((xEdge && yEdge) || (xEdge && zEdge) || (yEdge && zEdge))
                {
                    return _Color;
                }
                
                discard; // Skryje vnitřek kostky
                return float4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
