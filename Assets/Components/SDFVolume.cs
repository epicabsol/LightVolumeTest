using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NOTE: Must match the VOLUME_TYPE_ definitions in SDFIntersectionShader.shader
public enum SDFVolumeType : int
{
    Invalid = 0,
    Sphere = 1,
    Cube = 2,
    Cylinder = 3,
    Cone = 4,
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
    /// <remarks>
    /// This should only be set by <see cref="SDFVolumeManagerComponent"/>
    /// </remarks>
    public int VolumeIndex { get; internal set; }

    // TODO: Adding bounds and ContainsPoint checks would make lots of sense if using these for gameplay systems on the CPU
    //public abstract Bounds LocalBounds { get; }
    //public abstract bool ContainsPoint(Vector3 point);
    
    public SDFVolume(int volumeIndex)
    {
        this.VolumeIndex = volumeIndex;
    }

    internal abstract SDFVolumeManagerComponent.VolumeData MakeVolumeData();
}
