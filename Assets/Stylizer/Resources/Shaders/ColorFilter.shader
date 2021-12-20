Shader "Beffio/Image Effects/ColorFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FilterColor ("Filter Color", Color) = (0,1,2,1)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _FilterColor;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                
                float4 sub = abs(col - _FilterColor);
                if(length(sub)<0.01){
                    return _FilterColor;
                }else{
                    return fixed4(0,0,0,0);
                }

                //half3 delta = abs(col.rgb - _FilterColor.rgb);
                //half4 rgba = length(delta) < 0.05 ? half4(replace_color.rgb : IN.color.rgb;
                //result.color = half4(rgb, IN.color.a);
                //return sub;
                /*if(sub.r==0&&sub.g==0&&sub.b==0){
                    return _FilterColor;
                }else{
                    return fixed4(0,0,0,0);
                }*/

                // just invert the colors
                //col.rgb = 1 - col.rgb;
                //return col;
            }
            ENDCG
        }
    }
}
