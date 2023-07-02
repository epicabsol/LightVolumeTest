using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFCylinderVolumeComponent : SDFVolumeComponent
{
    public float Radius = 0.5f;
    public float Height = 2.0f;

    protected override void LateUpdate()
    {
        base.LateUpdate();
        ((SDFCylinderVolume)this.Volume).Radius = this.Radius;
        ((SDFCylinderVolume)this.Volume).Height = this.Height;
    }

    protected override SDFVolume RegisterVolume()
    {
        return this.Manager.AddCylinder(this.Radius, this.Height);
    }

    protected override void UnregisterVolume()
    {
        this.Manager.RemoveCylinder((SDFCylinderVolume)this.Volume);
    }
}
