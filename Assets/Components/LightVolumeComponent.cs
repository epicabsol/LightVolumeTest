using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightVolumeComponent : MonoBehaviour
{
    private MeshRenderer MeshRenderer;
    private MeshFilter MeshFilter;
    private LightVolumeManagerComponent Manager;

    /// <summary>
    /// The current depth texture for this light volume.
    /// </summary>
    /// <remarks>
    /// Only available between calls to <see cref="GenerateDepthTexture(Camera)"/> and <see cref="ReleaseDepthTexture"/>.
    /// </remarks>
    public RenderTexture DepthTexture { get; private set; }

    private void OnEnable()
    {
        this.MeshRenderer = GetComponent<MeshRenderer>();
        this.MeshFilter = GetComponent<MeshFilter>();
        this.Manager = GetComponentInParent<LightVolumeManagerComponent>();
        if (this.Manager != null)
        {
            this.Manager.AddVolume(this);
        }
    }

    private void OnDisable()
    {
        if (this.Manager != null)
        {
            this.Manager.RemoveVolume(this);
        }
    }

    public void GenerateDepthTexture(Camera camera)
    {
        if (this.MeshRenderer == null
            || this.Manager == null
            || this.Manager.VolumeDepthMaterial == null)
        {
            return;
        }

        // Don't bother generating depth textures if we aren't actually visible
        // We could probably track this per-camera rather than using this all-camera variable,
        // if we anticipated caring about other cameras
        if (!this.MeshRenderer.isVisible)
        {
            return;
        }

        // Before the camera renders the scene, update the depth texture for this volume
        RenderTextureDescriptor depthBufferDescriptor = new RenderTextureDescriptor()
        {
            autoGenerateMips = false,
            bindMS = false,
            colorFormat = RenderTextureFormat.RFloat,
            depthBufferBits = 16,
            dimension = TextureDimension.Tex2D,
            enableRandomWrite = false,
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat,
            width = camera.pixelWidth,
            height = camera.pixelHeight,
            volumeDepth = 1,
            memoryless = RenderTextureMemoryless.None,
            mipCount = 1,
            msaaSamples = 1,
            shadowSamplingMode = ShadowSamplingMode.None,
            sRGB = false,
            stencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None,
            useDynamicScale = true,
            useMipMap = false,
        };
        this.DepthTexture = RenderTexture.GetTemporary(depthBufferDescriptor);

        CommandBuffer cb = new CommandBuffer();
        cb.name = $"LightVolumeComponent.GenerateDepthTexture ({this.name})";
        cb.SetRenderTarget(this.DepthTexture);
        cb.ClearRenderTarget(false, true, new Color(System.Single.MaxValue, 0.0f, 0.0f, 0.0f));
        cb.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        cb.DrawRenderer(this.MeshRenderer, this.Manager.VolumeDepthMaterial);
        Graphics.ExecuteCommandBuffer(cb);
    }

    public void ReleaseDepthTexture()
    {
        RenderTexture.ReleaseTemporary(this.DepthTexture);
        this.DepthTexture = null;
    }

    /// <summary>
    /// Draws an outline on the surface of this light volume everywhere that it is near to the given other light volume.
    /// </summary>
    public void DrawIntersections(LightVolumeComponent otherVolume, Camera camera)
    {
        if (this.Manager == null || this.Manager.VolumeIntersectionMaterial == null)
        {
            return;
        }

        // Early-out if either this volume or the other volume are not visible
        if (this.MeshRenderer == null || !this.MeshRenderer.isVisible
            || otherVolume == null || !otherVolume.MeshRenderer.isVisible
            || otherVolume.DepthTexture == null)
        {
            return;
        }

        // Early-out when the screen-space bounds of the volumes do not overlap
        // (I'm not 100% sure this math is correct because I get weird behavior sometimes...)
        if (!CouldMeshesOverlap(this.MeshRenderer, otherVolume.MeshRenderer, camera))
        {
            Debug.Log("Skipped non-overlapping volumes");
            return;
        }
        

        MaterialPropertyBlock pb = new MaterialPropertyBlock();
        pb.SetTexture("_OtherDepthTexture", otherVolume.DepthTexture);

        // Copy the properties of the opaque shader for the volume one
        Color baseColor = this.MeshRenderer.material.GetColor("_BaseColor");
        baseColor.a = 0.0f;
        pb.SetColor("_BaseColor", baseColor);
        pb.SetColor("_IntersectionColor", this.MeshRenderer.material.GetColor("_IntersectionColor"));
        pb.SetFloat("_IntersectionDistance", this.MeshRenderer.material.GetFloat("_IntersectionDistance"));

        CommandBuffer cb = new CommandBuffer();
        cb.name = $"LightVolumeComponent.DrawIntersections ({this.name}, {otherVolume.name})";
        cb.SetRenderTarget(camera.activeTexture);
        cb.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        cb.DrawMesh(this.MeshFilter.mesh, this.transform.localToWorldMatrix, this.Manager.VolumeIntersectionMaterial, 0, 0, pb);
        
        Graphics.ExecuteCommandBuffer(cb);

    }

    /// <summary>
    /// Determines whether the two meshes potentially overlap when viewed from the given camera.
    /// </summary>
    /// <param name="meshA"></param>
    /// <param name="meshB"></param>
    /// <param name="camera"></param>
    /// <returns></returns>
    private static bool CouldMeshesOverlap(MeshRenderer meshA, MeshRenderer meshB, Camera camera)
    {
        Rect boundsA = ComputeScreenBounds(meshA.bounds, camera);
        Rect boundsB = ComputeScreenBounds(meshB.bounds, camera);
        return boundsA.Overlaps(boundsB, false);
    }

    private static Rect ComputeScreenBounds(Bounds worldBounds, Camera camera)
    {
        Matrix4x4 projection = camera.projectionMatrix;

        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;

        Rect bounds = new Rect(ProjectPoint(min, projection), Vector2.zero);

        ExpandBounds(new Vector3(min.x, min.y, max.z), ref bounds, projection);
        ExpandBounds(new Vector3(min.x, max.y, min.z), ref bounds, projection);
        ExpandBounds(new Vector3(min.x, max.y, max.z), ref bounds, projection);
        ExpandBounds(new Vector3(max.x, min.y, min.z), ref bounds, projection);
        ExpandBounds(new Vector3(max.x, min.y, max.z), ref bounds, projection);
        ExpandBounds(new Vector3(max.x, max.y, min.z), ref bounds, projection);
        ExpandBounds(new Vector3(max.x, max.y, max.z), ref bounds, projection);

        return bounds;
    }

    private static void ExpandBounds(Vector3 point, ref Rect rect, in Matrix4x4 projection)
    {
        Vector2 point2D = ProjectPoint(point, projection);
        rect.min = Vector2.Min(rect.min, point2D);
        rect.max = Vector2.Max(rect.max, point2D);
    }

    private static Vector2 ProjectPoint(in Vector3 point, in Matrix4x4 projection)
    {
        Vector3 projected = projection.MultiplyPoint(point);
        return new Vector2(projected.x / projected.z, projected.y / projected.z);
    }
}
