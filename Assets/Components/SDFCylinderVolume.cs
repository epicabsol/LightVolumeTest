using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFCylinderVolume : SDFVolume
{
    public float Radius { get; set; }
    public float Height { get; set; }

    public override SDFVolumeType VolumeType => SDFVolumeType.Cylinder;

    public SDFCylinderVolume(int volumeIndex, float radius, float height) : base(volumeIndex)
    {
        this.Radius = radius;
        this.Height = height;
    }
}
