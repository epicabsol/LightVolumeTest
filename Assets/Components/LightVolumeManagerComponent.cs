using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightVolumeManagerComponent : MonoBehaviour
{
    private List<LightVolumeComponent> _activeVolumes = new List<LightVolumeComponent>();

    public IReadOnlyList<LightVolumeComponent> ActiveVolumes => this._activeVolumes;

    public Material VolumeDepthMaterial;
    public Material VolumeIntersectionMaterial;

    public void AddVolume(LightVolumeComponent volume)
    {
        this._activeVolumes.Add(volume);
    }

    public void RemoveVolume(LightVolumeComponent volume)
    {
        this._activeVolumes.Remove(volume);
    }

    private void OnEnable()
    {
        Camera.onPreRender += OnCameraPreRender;
        Camera.onPostRender += OnCameraPostRender;
    }

    private void OnDisable()
    {
        Camera.onPostRender -= OnCameraPostRender;
        Camera.onPreRender -= OnCameraPreRender;
    }

    private void OnCameraPreRender(Camera camera)
    {
        // Render a depth texture for every active volume
        foreach (LightVolumeComponent volume in this.ActiveVolumes)
        {
            volume.GenerateDepthTexture(camera);
        }
    }

    private void OnCameraPostRender(Camera camera)
    {
        // Each volume draws its intersections with every other volume's depth texture
        foreach (LightVolumeComponent volumeA in this.ActiveVolumes)
        {
            foreach (LightVolumeComponent volumeB in this.ActiveVolumes)
            {
                if (volumeA != volumeB)
                {
                    volumeA.DrawIntersections(volumeB, camera);
                }
            }
        }

        foreach (LightVolumeComponent volume in this.ActiveVolumes)
        {
            volume.ReleaseDepthTexture();
        }
    }
}
