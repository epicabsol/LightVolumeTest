using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public abstract class SDFVolumeComponent : MonoBehaviour
{
    public SDFVolume Volume { get; private set; } = null;
    public SDFVolumeManagerComponent Manager { get; private set; }
    public Renderer Renderer { get; private set; }

    public Color Color = new Color(1.0f, 0.0f, 0.0f, 0.1f);

    // Start is called before the first frame update
    void Start()
    {
        if (Application.IsPlaying(gameObject))
        {
            this.Manager = this.GetComponentInParent<SDFVolumeManagerComponent>();
            this.Renderer = this.GetComponent<Renderer>();
        }
    }

    protected virtual void LateUpdate()
    {
        if (Application.IsPlaying(gameObject))
        {
            this.Volume.InverseWorldTransform = this.transform.worldToLocalMatrix;
            this.Volume.Color = this.Color;

            this.UpdateVolume();
        }
    }

    private void OnWillRenderObject()
    {
        if (!Application.IsPlaying(gameObject))
        {
            // During edit mode the shader will not have the volume infos, including our color, so pass it here
            MaterialPropertyBlock pb = new MaterialPropertyBlock();
            pb.SetColor("_EditModeColor", this.Color);
            this.GetComponent<Renderer>()?.SetPropertyBlock(pb);
        }
        else if (this.Renderer != null && this.Manager != null && this.Volume != null)
        {
            MaterialPropertyBlock pb = new MaterialPropertyBlock();

            // Catch use-after-remove errors
            if (this.Volume.VolumeIndex < 0)
                throw new System.Exception("Cannot render a volume that has been removed from the volume manager!");

            // Update the current volume info
            pb.SetInt("_CurrentVolumeIndex", this.Volume.VolumeIndex);

            // Update the other volume info
            pb.SetBuffer("_VolumeBuffer", this.Manager.VolumeBuffer);
            pb.SetInt("_VolumeCount", this.Manager.Volumes.Count);

            this.Renderer.SetPropertyBlock(pb);
        }
    }

    private void OnEnable()
    {
        if (Application.IsPlaying(gameObject))
        {
            this.Manager = this.GetComponentInParent<SDFVolumeManagerComponent>();
            if (this.Manager != null)
            {
                this.Volume = this.CreateVolume();
            }
            else
            {
                Debug.LogError("No manager for SDF volume component!");
            }
        }
    }

    private void OnDisable()
    {
        if (this.Volume != null)
        {
            this.Manager.RemoveVolume(this.Volume);
            this.Volume = null;
        }
    }

    protected abstract SDFVolume CreateVolume();
    protected abstract void UpdateVolume();
}
