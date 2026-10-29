
Shader "UI/ChromaKey"
{
    Properties
    {
        [PerRendererData] _MainTex ("Video Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _KeyColor ("Color a eliminar", Color) = (0,1,0,1)
        _Threshold ("Tolerancia", Range(0,1)) = 0.25
        _Smoothness ("Suavizado", Range(0.001,0.5)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _KeyColor;
            float _Threshold;
            float _Smoothness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float distanceToKey =
                    distance(col.rgb, _KeyColor.rgb);

                float alpha = smoothstep(
                    _Threshold,
                    _Threshold + _Smoothness,
                    distanceToKey
                );

                col.a *= alpha;
                return col * i.color;
            }
            ENDCG
        }
    }
}
