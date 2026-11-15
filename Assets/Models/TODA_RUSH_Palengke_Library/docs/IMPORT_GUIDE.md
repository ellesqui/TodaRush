# Import guide

## Blender

1. Extract the ZIP completely.
2. Open `TODA_RUSH_Palengke_Assets.blend` in Blender 4.3 or newer. Use the Outliner to select a `TR_...` collection, then select its `_ROOT` to move the complete asset. The grid is only for inspecting the library.
3. To inspect just one export, use File > Import > glTF 2.0 and choose any file in `GLB/`. These exports arrive centered at the ground origin.
4. Materials use standard Principled BSDF base colors and metallic/roughness values. All lettering has already been converted into real mesh geometry.
5. To export an edited source asset at its own origin, move its root to (0,0,0), select the collection's mesh children, then File > Export > glTF 2.0, Format GLB, Selected Objects, +Y Up. Do not export the whole catalog as one level.

## Unity

1. Open your project and install **Unity glTFast** through Window > Package Manager > install/add package by name: `com.unity.cloud.gltfast`. Choose a package release compatible with your Editor. GLB is not a Unity .unitypackage.
2. Copy the categorized `GLB` folder into `Assets/TODA_RUSH/Palengke/` (or import only the models needed). glTFast registers the GLB importer and creates native imported prefabs and materials.
3. Drag a GLB prefab into a test scene. Keep Transform scale at (1,1,1); the models are authored in meters. A 1 m Unity cube provides a useful scale check. Axis conversion is handled by the importer; don't manually rotate every asset to fix Y-up.
4. Create prefab variants for gameplay. Use simple Box Colliders for market buildings/stalls/counters, capsule or box combinations for vehicles, and a limited set of primitive colliders for furniture. Do not put colliders on every tomato, text label or canopy rib. Vehicles need a separate driving/physics setup.
5. For the open market hall, use separate wall/column colliders so the entrance remains passable. Avoid a single box collider enclosing the entire market or shelter.
6. Keep road paint a few millimeters above the road surface to avoid z-fighting. No road or terrain mesh is included. Disable collision on the markings.
7. If baked lighting is required, generate suitable lightmap UVs and configure the imported renderer's lightmap settings. Test the chosen glTFast materials with your render pipeline and include required shaders in builds following the package's setup guidance.
8. Save tested prefab variants, then provide the `GLB/` paths and manifest to Claude Code for placement against the finalized 600 × 600 blueprint. The manifest's size values are model bounds in meters, not grid coordinates; establish the blueprint-to-world unit conversion before placing assets.

No Unity Editor is installed in the generation environment, so these models have been structurally checked and reimported in Blender, not playtested in your Unity project.

Official Unity glTFast references:
- Package installation: https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/Packages/com.unity.cloud.gltfast/Documentation~/index.md
- Editor import: https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/Packages/com.unity.cloud.gltfast/Documentation~/ImportEditor.md
- Rendering/build setup: https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/Packages/com.unity.cloud.gltfast/Documentation~/ProjectSetup.md
