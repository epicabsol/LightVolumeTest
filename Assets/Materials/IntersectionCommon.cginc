
#ifndef LIGHTVOLUME_INTERSECTION_COMMON
#define LIGHTVOLUME_INTERSECTION_COMMON

float4 DistanceToColor(float distance, float4 baseColor, float4 intersectionColor, float intersectionDistance)
{
    // Map the distance from the range [0, intersectionDistance] to [1, 0]
    float inter = 1 - saturate(abs(distance) / intersectionDistance);

    // Sharpen the intersecting/non-intersecting border
    inter = saturate(inter * 10); 

    return lerp(baseColor, intersectionColor, inter);
}

#endif
