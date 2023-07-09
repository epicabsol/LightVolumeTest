using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SDFCubeVolume : SDFVolume
{
    public override SDFVolumeType VolumeType => SDFVolumeType.Cube;

    public Vector3 HalfExtents { get; set; }

    public SDFCubeVolume(int volumeIndex, Vector3 halfExtents) : base(volumeIndex)
    {
        this.HalfExtents = halfExtents;
    }

    internal override SDFVolumeManagerComponent.VolumeData MakeVolumeData()
    {
        return new SDFVolumeManagerComponent.VolumeData(this.InverseWorldTransform, this.Color, this.VolumeType, this.HalfExtents.x, this.HalfExtents.y, this.HalfExtents.z);
    }
}
