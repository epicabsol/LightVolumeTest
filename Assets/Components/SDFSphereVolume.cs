using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFSphereVolume : SDFVolume
{
    public float Radius { get; set; }

    public override SDFVolumeType VolumeType => SDFVolumeType.Sphere;

    public SDFSphereVolume(int volumeIndex, float radius = 1.0f) : base(volumeIndex)
    {
        this.Radius = radius;
    }

    internal override SDFVolumeManagerComponent.VolumeData MakeVolumeData()
    {
        return new SDFVolumeManagerComponent.VolumeData(this.InverseWorldTransform, this.Color, this.VolumeType, this.Radius);
    }
}
