using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFCubeVolumeComponent : SDFVolumeComponent
{
    public Vector3 HalfExtents = new Vector3(0.5f, 0.5f, 0.5f);

    protected override void LateUpdate()
    {
        base.LateUpdate();
        ((SDFCubeVolume)this.Volume).HalfExtents = this.HalfExtents;
    }

    protected override SDFVolume CreateVolume()
    {
        return this.Manager.AddCube(this.HalfExtents);
    }
}
