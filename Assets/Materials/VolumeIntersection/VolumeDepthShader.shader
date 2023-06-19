Shader "Unlit/VolumeDepthShader"
{
    Properties
    {
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 projPos : TEXCOORD4;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.projPos = ComputeScreenPos(o.vertex);
				COMPUTE_EYEDEPTH(o.projPos.z);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Write the depth
                // We're using an R32_Float buffer for this because I am lazy, so we don't have to worry about fitting this into a 0 to 1 range.
                return fixed4(i.projPos.z, 0.0, 0.0, 1.0);
            }
            ENDCG
        }
    }
}
