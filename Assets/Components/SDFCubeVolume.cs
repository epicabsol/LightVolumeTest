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
}
