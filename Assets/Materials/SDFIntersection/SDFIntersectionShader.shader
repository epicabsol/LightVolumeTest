Shader "Unlit/SDFIntersectionShader"
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

            #define VOLUME_TYPE_SPHERE 1
            #define VOLUME_TYPE_CUBE 2
            #define VOLUME_TYPE_CYLINDER 3

            struct SphereData
            {
                float4x4 InverseWorldTransform;
                float Radius;
                float3 _Padding;
            };

            struct CubeData
            {
                float4x4 InverseWorldTransform;
                float3 HalfExtents;
                float _Padding;
            };

            struct CylinderData
            {
                float4x4 InverseWorldTransform;
                float Radius;
                float Height;
                float2 _Padding;
            };

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD0;
            };

            fixed4 _BaseColor;
            fixed4 _IntersectionColor;
            float _IntersectionDistance;

            StructuredBuffer<SphereData> _SphereBuffer;
            int _SphereCount;

            StructuredBuffer<CubeData> _CubeBuffer;
            int _CubeCount;

            StructuredBuffer<CylinderData> _CylinderBuffer;
            int _CylinderCount;

            int _CurrentVolumeType;
            int _CurrentVolumeIndex;

            inline float EstimateScale(float4x4 _matrix)
            {
                // HACK: Approximate the scale by averaging the scale in each axis
                // There's definitely a correct way to compensate for scale but I'm not sure what that is at the moment
                float unitX = mul(_matrix, float4(1, 0, 0, 0)).xyz;
                float unitY = mul(_matrix, float4(0, 1, 0, 0)).xyz;
                float unitZ = mul(_matrix, float4(0, 0, 1, 0)).xyz;
                return (length(unitX) + length(unitY) + length(unitZ)) / 3;
            }

            //
            // Signed Distance Functions for each primitive type
            //

            inline float SignedDistanceSphere(float3 location, SphereData sphere)
            {
                float3 localLocation = mul(sphere.InverseWorldTransform, float4(location, 1.0f)).xyz;

                return (length(localLocation) - sphere.Radius) / EstimateScale(sphere.InverseWorldTransform);
            }

            inline float SignedDistanceCube(float3 location, CubeData cube)
            {
                float3 localLocation = mul(cube.InverseWorldTransform, float4(location, 1.0f)).xyz;

                // Based on https://iquilezles.org/articles/distfunctions/
                float3 q = abs(localLocation) - cube.HalfExtents;
                return (length(max(q, float3(0.0f, 0.0f, 0.0f))) + min(max(q.x, max(q.y, q.z)), 0.0f)) / EstimateScale(cube.InverseWorldTransform);
            }

            inline float SignedDistanceCylinder(float3 location, CylinderData cylinder)
            {
                float3 localLocation = mul(cylinder.InverseWorldTransform, float4(location, 1.0f)).xyz;

                float2 d = abs(float2(length(localLocation.xz), localLocation.y)) - float2(cylinder.Radius, cylinder.Height / 2.0f);
                return (min(max(d.x, d.y), 0.0f) + length(max(d, float2(0.0f, 0.0f)))) / EstimateScale(cylinder.InverseWorldTransform);
            }

            //
            // Outline functions for different intersection appearances
            //

            // Draws a dim outline at the given intersection distance, and a bright line even closer to the intersection
            inline float OutlineSteppedCenter(float minDistance)
            {
                bool close = abs(minDistance) < _IntersectionDistance;
                bool veryClose = abs(minDistance) < _IntersectionDistance * 0.2f;

                return veryClose ? 1 : (close ? 0.25 : 0);
            }

            // Draws a single line at the intersection with the given intersection distance
            inline float OutlineCenter(float minDistance)
            {
                return abs(minDistance) < _IntersectionDistance ? 1 : 0;
            }

            // Draws gridlines spaced apart according to the given intersection distance
            inline float OutlineConcentric(float minDistance)
            {
                #define RING_THICKNESS 0.025
                return (fmod(minDistance, _IntersectionDistance) < RING_THICKNESS / 2 || fmod(minDistance, _IntersectionDistance) > 1.0f - RING_THICKNESS / 2) ? 1 : 0;
            }



            // Transforms location from world to object space
            // One would think this would make sense to have this function ship with Unity, but....
            inline float3 UnityObjectToWorldLocation(in float3 location)
            {
                return mul(unity_ObjectToWorld, float4(location, 1.0f)).xyz;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = UnityObjectToWorldLocation(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Find closest distance to something
                float minDistance = 100000.0f;


                for (int j = 0; j < _SphereCount; j++)
                {
                    // Take the absolute value of these signed distance outputs to enable interior intersections
                    float distance = SignedDistanceSphere(i.world, _SphereBuffer[j]);
                    if (distance < minDistance && !(j == _CurrentVolumeIndex && _CurrentVolumeType == VOLUME_TYPE_SPHERE))
                    {
                        minDistance = distance;
                    }
                }

                for (int k = 0; k < _CubeCount; k++)
                {
                    float distance = SignedDistanceCube(i.world, _CubeBuffer[k]);
                    if (distance < minDistance && !(k == _CurrentVolumeIndex && _CurrentVolumeType == VOLUME_TYPE_CUBE))
                    {
                        minDistance = distance;
                    }
                }

                for (int m = 0; m < _CylinderCount; m++)
                {
                    float distance = SignedDistanceCylinder(i.world, _CylinderBuffer[m]);
                    if (distance < minDistance && !(m == _CurrentVolumeIndex && _CurrentVolumeType == VOLUME_TYPE_CYLINDER))
                    {
                        minDistance = distance;
                    }
                }

                // Determine whether to show the base color or the intersection color, based on the nearest distance to something
                // I added a couple different designs to try out:
                //float value = OutlineCenter(minDistance);
                float value = OutlineSteppedCenter(minDistance);
                //float value = OutlineConcentric(minDistance);

                return lerp(_BaseColor, _IntersectionColor, value);
            }
            ENDCG
        }
    }
}
