Light Volume Test
=================

This is a Unity project demonstrating how to draw additive light volumes with outlines where they intersect other geometry and each other.

![Screenshot](Screenshot.png)


Summary
-------

Because lights are naturally additive, we don't have any sorting concerns. However, all of the light volumes need to go through each of the following steps together:
 - First, each light volume renders itself using normal Unity `MeshFilter` + `MeshRenderer` components, with border lines based on the depth buffer from the opaque objects (in `Materials/OpaqueIntersection/OpaqueIntersectionShader.shader`)
 - Then, each light volume renders a depth map of itself for the current camera (in `Materials/VolumeIntersection/VolumeDepthShader.shader`)
 - Finally, each light volume loops through every other light volume and draws a copy of its mesh using `VolumeIntersectionShader.shader`, which is very similar to the opaque shader except that it samples the depth texture of the other volume to produce additional border lines.


Components
----------

**Components/LightVolumeManagerComponent.cs**: This is the container for the light volumes. It handles driving the extra rendering of the volumes.  
**Components/LightVolumeComponent.cs**: Add one of these to a `GameObject` that has a model with a single `OpaqueIntersectionShader` material to register it with the manager, render its depth texture, and draw the additional volume-on-volume border lines.


Future Improvement Opportunities
--------------------------------

 - Have each `LightVolumeComponent` render an additional depth texture for its backfaces as well as the current one for its front-facing faces, to allow the far side of volumes to cause border lines to appear on other volumes (should be very straightforward to implement, not sure about perf impact)
 - Draw border lines using many depth samples (like SSAO) rather than one, to give a nicer border appearance (perf concerns again though)
