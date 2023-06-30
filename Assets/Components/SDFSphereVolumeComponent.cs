using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFSphereVolumeComponent : SDFVolumeComponent
{
    public float Radius = 0.5f;

    protected override void LateUpdate()
    {
        base.LateUpdate();
        ((SDFSphereVolume)this.Volume).Radius = Radius;
    }

    protected override SDFVolume RegisterVolume()
    {
        return this.Manager.AddSphere(1.0f);
    }

    protected override void UnregisterVolume()
    {
        this.Manager.RemoveSphere((SDFSphereVolume)this.Volume);
    }
}
