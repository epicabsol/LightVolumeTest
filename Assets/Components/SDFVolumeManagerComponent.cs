using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFVolumeManagerComponent : MonoBehaviour
{
    private const int MaxShapeCount = 30; // Max number of each shape

    internal struct VolumeData
    {
        public Matrix4x4 InverseWorldTransform; // 64 bytes

        public Vector4 Color; // 16 bytes

        // This holds up to three shape-specific values
        public Vector3 ShapeParameters; // 12 bytes

        public SDFVolumeType VolumeType; // 4 bytes

        public VolumeData(Matrix4x4 inverseWorldTransform, Vector4 color, SDFVolumeType volumeType, float shapeParameter0 = 0.0f, float shapeParameter1 = 0.0f, float shapeParameter2 = 0.0f)
        {
            this.InverseWorldTransform = inverseWorldTransform;
            this.Color = color;
            this.VolumeType = volumeType;
            this.ShapeParameters = new Vector3(shapeParameter0, shapeParameter1, shapeParameter2);
        }
    }

    private List<SDFVolume> _volumes = new List<SDFVolume>();
    public IReadOnlyList<SDFVolume> Volumes => this._volumes;
    public ComputeBuffer VolumeBuffer { get; private set; }

    public SDFSphereVolume AddSphere(float radius)
    {
        if (this.Volumes.Count >= SDFVolumeManagerComponent.MaxShapeCount)
            throw new System.Exception($"Cannot exceed maximum volume count of {SDFVolumeManagerComponent.MaxShapeCount} ({nameof(SDFVolumeManagerComponent)}.{nameof(MaxShapeCount)})");

        var newSphere = new SDFSphereVolume(this.Volumes.Count, radius);
        this._volumes.Add(newSphere);
        return newSphere;
    }

    public SDFCubeVolume AddCube(Vector3 halfExtents)
    {
        if (this.Volumes.Count >= SDFVolumeManagerComponent.MaxShapeCount)
            throw new System.Exception($"Cannot exceed maximum volume count of {SDFVolumeManagerComponent.MaxShapeCount} ({nameof(SDFVolumeManagerComponent)}.{nameof(MaxShapeCount)})");

        var newCube = new SDFCubeVolume(this.Volumes.Count, halfExtents);
        this._volumes.Add(newCube);
        return newCube;
    }

    public SDFCylinderVolume AddCylinder(float radius, float height)
    {
        if (this.Volumes.Count >= SDFVolumeManagerComponent.MaxShapeCount)
            throw new System.Exception($"Cannot exceed maximum volume count of {SDFVolumeManagerComponent.MaxShapeCount} ({nameof(SDFVolumeManagerComponent)}.{nameof(MaxShapeCount)})");

        var newCylinder = new SDFCylinderVolume(this.Volumes.Count, radius, height);
        this._volumes.Add(newCylinder);
        return newCylinder;
    }

    public SDFConeVolume AddCone(float radius, float height)
    {
        if (this.Volumes.Count >= SDFVolumeManagerComponent.MaxShapeCount)
            throw new System.Exception($"Cannot exceed maximum volume count of {SDFVolumeManagerComponent.MaxShapeCount} ({nameof(SDFVolumeManagerComponent)}.{nameof(MaxShapeCount)})");

        var newCone = new SDFConeVolume(this.Volumes.Count, radius, height);
        this._volumes.Add(newCone);
        return newCone;
    }

    public void RemoveVolume(SDFVolume volume)
    {
        int index = this._volumes.IndexOf(volume);

        if (index == -1)
        {
            return;
        }

        // If `volume` is not the last volume in the list, swap the last volume in the list into the slot being removed
        if (this.Volumes.Count > 1 && index < this.Volumes.Count - 1)
        {
            this._volumes[index] = this._volumes[this._volumes.Count - 1];
            this._volumes[index].VolumeIndex = index;
        }

        // Now we know that `volume` has to be at the end of `Volumes`
        this._volumes.RemoveAt(this._volumes.Count - 1);

        // Give the removed volume some nonsense value so that use-after-remove errors become more obvious
        volume.VolumeIndex = -1;
    }

    private void OnEnable()
    {
        this.VolumeBuffer = new ComputeBuffer(MaxShapeCount, /*sizeof(VolumeData)*/ 96, ComputeBufferType.Structured);

        Camera.onPreRender += OnCameraPreRender;
    }

    private void OnDisable()
    {
        Camera.onPreRender -= OnCameraPreRender;

        this.VolumeBuffer.Dispose();
    }

    // Rather than reallocating this each time, just keep it around from one OnCameraPreRender to the next
    // (which makes this non-threadsafe, but you weren't going to call OnCameraPreRender from multiple threads, were you?!)
    private VolumeData[] VolumeDataBuffer = new VolumeData[MaxShapeCount];
    private void OnCameraPreRender(Camera camera)
    {
        for (int i = 0; i < this.Volumes.Count; i++)
        {
            this.VolumeDataBuffer[i] = this.Volumes[i].MakeVolumeData();
        }
        this.VolumeBuffer.SetData(this.VolumeDataBuffer);
    }
}
