using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFSphereVolumeComponent : SDFVolumeComponent
{
    public float Radius = 0.5f;

    protected override void LateUpdate()
    {
        base.LateUpdate();
        ((SDFSphereVolume)this.Volume).Radius = this.Radius;
    }

    protected override SDFVolume CreateVolume()
    {
        return this.Manager.AddSphere(this.Radius);
    }
}
