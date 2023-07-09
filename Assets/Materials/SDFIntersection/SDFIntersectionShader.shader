Shader "Unlit/SDFIntersectionShader"
{
    Properties
    {
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
        ZTest Always
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // NOTE: Must match enum SDFVolumeType in SDFVolume.cs
            #define VOLUME_TYPE_SPHERE 1
            #define VOLUME_TYPE_CUBE 2
            #define VOLUME_TYPE_CYLINDER 3
            #define VOLUME_TYPE_CONE 4

            struct VolumeData
            {
                float4x4 InverseWorldTransform;
                float4 Color;
                float3 ShapeParameters;
                int VolumeType;
            };

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD0;
                float4 projPos : TEXCOORD4;
            };

            sampler2D_float _CameraDepthTexture;
            fixed4 _EditModeColor;
            float _IntersectionDistance;

            StructuredBuffer<VolumeData> _VolumeBuffer;
            int _VolumeCount;

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

            inline float SignedDistanceSphere(float3 location, float4x4 inverseWorldTransform, float radius)
            {
                float3 localLocation = mul(inverseWorldTransform, float4(location, 1.0f)).xyz;

                return (length(localLocation) - radius) / EstimateScale(inverseWorldTransform);
            }

            inline float SignedDistanceCube(float3 location, float4x4 inverseWorldTransform, float3 halfExtents)
            {
                float3 localLocation = mul(inverseWorldTransform, float4(location, 1.0f)).xyz;

                // Based on https://iquilezles.org/articles/distfunctions/
                float3 q = abs(localLocation) - halfExtents;
                return (length(max(q, float3(0.0f, 0.0f, 0.0f))) + min(max(q.x, max(q.y, q.z)), 0.0f)) / EstimateScale(inverseWorldTransform);
            }

            inline float SignedDistanceCylinder(float3 location, float4x4 inverseWorldTransform, float radius, float height)
            {
                float3 localLocation = mul(inverseWorldTransform, float4(location, 1.0f)).xyz;

                // Based on https://iquilezles.org/articles/distfunctions/
                float2 d = abs(float2(length(localLocation.xz), localLocation.y)) - float2(radius, height / 2.0f);
                return (min(max(d.x, d.y), 0.0f) + length(max(d, float2(0.0f, 0.0f)))) / EstimateScale(inverseWorldTransform);
            }

            inline float SignedDistanceCone(float3 location, float4x4 inverseWorldTransform, float radius, float height)
            {
                float3 localLocation = mul(inverseWorldTransform, float4(location, 1.0f)).xyz;

                // Based on https://iquilezles.org/articles/distfunctions/
                float2 q = float2(radius, -height);

                float2 w = float2(length(localLocation.xz), localLocation.y);
                float2 a = w - q * saturate(dot(w, q) / dot(q, q));
                float2 b = w - q * float2(saturate(w.x / q.x), 1.0f);
                float k = sign(q.y); // Cope with negative heights
                float d = min(dot(a, a), dot(b, b));
                float s = max(k * (w.x * q.y - w.y * q.x), k * (w.y - q.y));
                return (sqrt(d) * sign(s)) / EstimateScale(inverseWorldTransform);
            }

            inline float SignedDistance(float3 location, VolumeData volume)
            {
                if (volume.VolumeType == VOLUME_TYPE_SPHERE)
                {
                    return SignedDistanceSphere(location, volume.InverseWorldTransform, volume.ShapeParameters.x);
                }
                else if (volume.VolumeType == VOLUME_TYPE_CUBE)
                {
                    return SignedDistanceCube(location, volume.InverseWorldTransform, volume.ShapeParameters.xyz);
                }
                else if (volume.VolumeType == VOLUME_TYPE_CYLINDER)
                {
                    return SignedDistanceCylinder(location, volume.InverseWorldTransform, volume.ShapeParameters.x, volume.ShapeParameters.y);
                }
                else if (volume.VolumeType == VOLUME_TYPE_CONE)
                {
                    return SignedDistanceCone(location, volume.InverseWorldTransform, volume.ShapeParameters.x, volume.ShapeParameters.y);
                }
                else
                {
                    return 100000.0f;
                }
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
                return abs(minDistance) < _IntersectionDistance * 0.2f ? 1 : 0;
            }

            // Draws gridlines spaced apart according to the given intersection distance
            inline float OutlineConcentric(float minDistance)
            {
                #define RING_THICKNESS 0.025
                return (fmod(minDistance, _IntersectionDistance) < RING_THICKNESS / 2 || fmod(minDistance, _IntersectionDistance) > 1.0f - RING_THICKNESS / 2) ? 1 : 0;
            }



            // Transforms location from world to object space
            // One would think it would make sense to have this function ship with Unity, but....
            inline float3 UnityObjectToWorldLocation(in float3 location)
            {
                return mul(unity_ObjectToWorld, float4(location, 1.0f)).xyz;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = UnityObjectToWorldLocation(v.vertex);
                o.projPos = ComputeScreenPos(o.vertex);
				COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // We need to implement depth testing a bit specially - if we are 'behind' a solid surface, check whether the solid surface point is inside our volume.
                // This way, we can show both front and backfaces, even when the volume is clipping into a solid surface
                // Determine how far away the last solid object behind the current volume surface is
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));

                float3 toVolume = -UnityWorldSpaceViewDir(i.world);
                float3 toSurface = toVolume * sceneZ / i.projPos.z; // I *think* this is correct??
                float3 surfaceWorld = _WorldSpaceCameraPos.xyz + toSurface;

                // If the pixel we are shading is behind the surface, check whether the surface is within the volume
                bool isAdjustedSurface = sceneZ <= i.projPos.z;
                if (isAdjustedSurface)
                {
                    // If the pixel we are shading isn't within the volume, don't draw it
                    float ownVolumeDistance = SignedDistance(surfaceWorld, _VolumeBuffer[_CurrentVolumeIndex]);
                    if (ownVolumeDistance > 0.0f)
                    {
                        discard;
                    }

                    // Otherwise, pretend it is on the surface that is in front of it
                    i.world = surfaceWorld;
                }

                // Check whether the camera is inside the volume being drawn
                float cameraDistance = SignedDistance(_WorldSpaceCameraPos, _VolumeBuffer[_CurrentVolumeIndex]);
                bool isCameraInside = cameraDistance < 0.0f;

                // If the camera is inside, double the amount of base color to compensate for only having one of the two sides visible
                float4 baseColor = _CurrentVolumeIndex < _VolumeCount ? _VolumeBuffer[_CurrentVolumeIndex].Color : _EditModeColor;
                float4 myOutlineColor = baseColor;
                float4 otherOutlineColor = myOutlineColor; // This will change based on what other object the outline is against
                if (isCameraInside)
                {
                    baseColor.xyz = baseColor.xyz * 2.0f;
                }

                // Find closest distance to something
                float minDistance = 100000.0f;

                // Check the distance to each volume
                for (int j = 0; j < _VolumeCount; j++)
                {
                    float distance = SignedDistance(i.world, _VolumeBuffer[j]);
                    
                    // Take the absolute value of these signed distance outputs to enable interior intersections
                    //distance = abs(distance);

                    if (distance < minDistance && j != _CurrentVolumeIndex)
                    {
                        minDistance = distance;
                        otherOutlineColor = _VolumeBuffer[j].Color;
                    }
                }

                // Check the distance to the solid surface behind
                float surfaceDistance = abs(sceneZ - i.projPos.z);
                if (surfaceDistance < minDistance)
                {
                    minDistance = surfaceDistance;
                    otherOutlineColor = myOutlineColor;
                }

                // If this is a part of the volume that has been faked to appear on top of a solid surface, draw an edge to the volume
                if (isAdjustedSurface)
                {
                    float actualOwnVolumeDistance = 0.5f * abs(SignedDistance(i.world, _VolumeBuffer[_CurrentVolumeIndex]));
                    if (actualOwnVolumeDistance < minDistance)
                    {
                        minDistance = actualOwnVolumeDistance;
                        otherOutlineColor = myOutlineColor;
                    }
                }

                // Determine whether to show the base color or the intersection color, based on the nearest distance to something
                // I added a couple different designs to try out:
                float value = OutlineCenter(minDistance);
                //float value = OutlineSteppedCenter(minDistance);
                //float value = OutlineConcentric(minDistance);

                return lerp(baseColor, (myOutlineColor + otherOutlineColor) * 0.5f * 2.0f, value);
            }
            ENDCG
        }
    }
}
