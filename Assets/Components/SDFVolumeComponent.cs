using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SDFVolumeComponent : MonoBehaviour
{
    public SDFVolume Volume { get; private set; } = null;
    public SDFVolumeManagerComponent Manager { get; private set; }
    public Renderer Renderer { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        this.Manager = this.GetComponentInParent<SDFVolumeManagerComponent>();
        this.Renderer = this.GetComponent<Renderer>();
    }

    protected virtual void LateUpdate()
    {
        this.Volume.InverseWorldTransform = this.transform.worldToLocalMatrix;
    }

    private void OnWillRenderObject()
    {
        if (this.Renderer != null && this.Manager != null && this.Volume != null)
        {
            MaterialPropertyBlock pb = new MaterialPropertyBlock();

            // Update the current volume info
            pb.SetInt("_CurrentVolumeType", (int)this.Volume.VolumeType);
            pb.SetInt("_CurrentVolumeIndex", this.Volume.VolumeIndex);

            // Update the other volume info
            pb.SetInt("_SphereCount", this.Manager.Spheres.Count);
            pb.SetBuffer("_SphereBuffer", this.Manager.SphereBuffer);
            pb.SetInt("_CubeCount", this.Manager.Cubes.Count);
            pb.SetBuffer("_CubeBuffer", this.Manager.CubeBuffer);

            this.Renderer.SetPropertyBlock(pb);
        }
    }

    private void OnEnable()
    {
        this.Manager = this.GetComponentInParent<SDFVolumeManagerComponent>();
        if (this.Manager != null)
        {
            this.Volume = this.RegisterVolume();
        }
        else
        {
            Debug.LogError("No manager for SDF volume component!");
        }
    }

    private void OnDisable()
    {
        if (this.Volume != null)
        {
            this.UnregisterVolume();
            this.Volume = null;
        }
    }

    protected abstract SDFVolume RegisterVolume();
    protected abstract void UnregisterVolume();
}
