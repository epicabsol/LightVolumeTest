using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFConeVolumeComponent : SDFVolumeComponent
{
    public float Radius = 1.0f;
    public float Height = 2.0f;

    protected override void UpdateVolume()
    {
        ((SDFConeVolume)this.Volume).Radius = Radius;
        ((SDFConeVolume)this.Volume).Height = Height;
    }

    protected override SDFVolume CreateVolume()
    {
        return this.Manager.AddCone(this.Radius, this.Height);
    }
}
