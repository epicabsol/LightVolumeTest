using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFVolumeManagerComponent : MonoBehaviour
{
    private const int MaxShapeCount = 30; // Max number of each shape

    #region Sphere

    private struct SphereData // 80 bytes
    {
        public Matrix4x4 InverseWorldTransform; // 64 bytes
        public float Radius;
        private Vector3 _Padding; // TODO: Needed?
    }
    private List<SDFSphereVolume> _spheres = new List<SDFSphereVolume>();
    public IReadOnlyList<SDFSphereVolume> Spheres => this._spheres;
    public ComputeBuffer SphereBuffer { get; private set; }

    public SDFSphereVolume AddSphere(float radius)
    {
        var newSphere = new SDFSphereVolume(this.Spheres.Count, radius);
        this._spheres.Add(newSphere);
        return newSphere;
    }

    public void RemoveSphere(SDFSphereVolume sphere)
    {
        this.RemoveVolume(this._spheres, sphere);
    }

    #endregion

    #region Cube

    private struct CubeData // 80 bytes
    {
        public Matrix4x4 InverseWorldTransform; // 64 bytes
        public Vector3 HalfExtents; // 12 bytes
        private float _Padding; // 4 bytes
    }
    private List<SDFCubeVolume> _cubes = new List<SDFCubeVolume>();
    public IReadOnlyList<SDFCubeVolume> Cubes => this._cubes;
    public ComputeBuffer CubeBuffer { get; private set; }

    public SDFCubeVolume AddCube(Vector3 halfExtents)
    {
        var newCube = new SDFCubeVolume(this.Cubes.Count, halfExtents);
        this._cubes.Add(newCube);
        return newCube;
    }

    public void RemoveCube(SDFCubeVolume cube)
    {
        this.RemoveVolume(this._cubes, cube);
    }

    #endregion

    private void RemoveVolume<TVolume>(List<TVolume> list, TVolume volume) where TVolume : SDFVolume
    {
        int index = list.IndexOf(volume);
        if (index != -1)
        {
            // TODO: We could swap the last volume into the removed slot to avoid having to shift everything.
            // Shifting a Relatively Small (tm) number of object references isn't slow but if we wanted to push
            // the limits it could make incremental changes to the corresponding GPU buffer easier, rather than having
            // to repopulate the GPU buffer each frame
            list.RemoveAt(index);

            for (int i = index; i < list.Count; i++)
            {
                list[i].VolumeIndex = i;
            }
        }
    }

    private void OnEnable()
    {
        this.SphereBuffer = new ComputeBuffer(MaxShapeCount, /*sizeof(SphereData)*/ 80, ComputeBufferType.Structured);
        this.CubeBuffer = new ComputeBuffer(MaxShapeCount, /* sizeof(CubeData) */ 80, ComputeBufferType.Structured);

        Camera.onPreRender += OnCameraPreRender;
    }

    private void OnDisable()
    {
        Camera.onPreRender -= OnCameraPreRender;

        this.SphereBuffer.Dispose();
        this.CubeBuffer.Dispose();
    }

    // Rather than reallocating these each time, just keep these around from one OnCameraPreRender to the next
    private SphereData[] SphereDataBuffer = new SphereData[MaxShapeCount];
    private CubeData[] CubeDataBuffer = new CubeData[MaxShapeCount];
    private void OnCameraPreRender(Camera camera)
    {
        for (int i = 0; i < this.Spheres.Count; i++)
        {
            this.SphereDataBuffer[i] = new SphereData() { InverseWorldTransform = this.Spheres[i].InverseWorldTransform, Radius = this.Spheres[i].Radius };
        }
        this.SphereBuffer.SetData(this.SphereDataBuffer);

        for (int i = 0; i < this.Cubes.Count; i++)
        {
            this.CubeDataBuffer[i] = new CubeData() { InverseWorldTransform = this.Cubes[i].InverseWorldTransform, HalfExtents = this.Cubes[i].HalfExtents };
        }
        this.CubeBuffer.SetData(this.CubeDataBuffer);
    }
}
