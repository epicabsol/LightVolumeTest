using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFConeVolume : SDFVolume
{
    public override SDFVolumeType VolumeType => SDFVolumeType.Cone;

    public float Radius { get; set; }
    public float Height { get; set; }

    public SDFConeVolume(int volumeIndex, float radius, float height) : base(volumeIndex)
    {
        this.Radius = radius;
        this.Height = height;
    }

    internal override SDFVolumeManagerComponent.VolumeData MakeVolumeData()
    {
        return new SDFVolumeManagerComponent.VolumeData(this.InverseWorldTransform, this.Color, this.VolumeType, this.Radius, this.Height);
    }
}
