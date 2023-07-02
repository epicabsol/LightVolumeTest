using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SDFVolumeType : int
{
    Invalid = 0,
    Sphere = 1,
    Cube = 2,
    Cylinder = 3,
}

public abstract class SDFVolume
{
    /// <summary>
    /// The world-to-object transform of this volume.
    /// </summary>
    public Matrix4x4 InverseWorldTransform { get; set; }
    /// <summary>
    /// Which type of volume this is.
    /// </summary>
    public abstract SDFVolumeType VolumeType { get; }
    /// <summary>
    /// The index of this volume relative to the other volume of the same type.
    /// </summary>
    public int VolumeIndex { get; set; }

    // TODO: Adding bounds and ContainsPoint checks would make lots of sense if using these for gameplay systems on the CPU
    //public abstract Bounds LocalBounds { get; }
    //public abstract bool ContainsPoint(Vector3 point);
    
    public SDFVolume(int volumeIndex)
    {
        this.VolumeIndex = volumeIndex;
    }
}
