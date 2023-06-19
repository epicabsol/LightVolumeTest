Shader "Unlit/OpaqueIntersectionShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.0, 1.0, 0.0, 0.3)
        _IntersectionColor ("Intersection Color", Color) = (0.0, 1.0, 0.0, 0.6)
        _IntersectionDistance ("Intersection Distance", Float) = 1.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        Blend SrcAlpha One // Add to the existing screen color, but take our output alpha into account
        Cull Off // We want to be able to see the far side of the volume
        ZWrite Off // We want to be able to render farther volumes after this one has drawn
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Materials/IntersectionCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;

                float4 projPos : TEXCOORD4;
            };

            sampler2D_float _CameraDepthTexture;
            fixed4 _BaseColor;
            fixed4 _IntersectionColor;
            float _IntersectionDistance;

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
                // Determine how far away the last solid object behind the current volume surface is
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
				
                // Determine how far away the current pixel of the volume surface is
                float partZ = i.projPos.z;

                return DistanceToColor(sceneZ - partZ, _BaseColor, _IntersectionColor, _IntersectionDistance);
            }
            ENDCG
        }
    }
}
