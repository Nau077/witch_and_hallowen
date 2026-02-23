Shader "UI/IrisTransitionHole"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0,0,0,1)
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Range(0, 2)) = 1.2
        _Feather ("Feather", Range(0.0001, 0.25)) = 0.02
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _Center;
            float _Radius;
            float _Feather;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = distance(i.uv, _Center.xy);
                float feather = max(0.0001, _Feather);
                float alphaMask = smoothstep(_Radius - feather, _Radius + feather, dist);

                fixed4 c = _Color;
                c *= tex2D(_MainTex, i.uv);
                c.a *= alphaMask;
                return c;
            }
            ENDCG
        }
    }
}
