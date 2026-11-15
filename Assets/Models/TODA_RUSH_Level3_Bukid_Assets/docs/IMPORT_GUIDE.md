# Import and placement guide

## Blender

1. Extract the entire ZIP.
2. Open `TODA_RUSH_Level3_Bukid_Assets.blend` with Blender 4.3 or newer. Each `TR3_...` collection contains separate editable mesh components and a `_ROOT` empty. Move the root to move a whole asset.
3. For one ready-to-use model, choose File > Import > glTF 2.0 and select a file from `GLB/`. This imports the model alone at its ground-centered origin.
4. To re-export edited source components, move the asset root to (0,0,0), select its mesh children, and export glTF 2.0 with GLB, Selected Objects, and +Y Up. The master blend's inspection spacing is not a level map.

## Unity

1. Install a compatible release of Unity glTFast using Package Manager's install/add package by name option: `com.unity.cloud.gltfast`. Check its official documentation for your Editor and rendering pipeline.
2. Copy the categorized GLB folder into `Assets/TODA_RUSH/Level3/`. glTFast imports GLB assets as native imported prefabs with meshes and materials.
3. Drag a model into a test scene. Keep scale at (1,1,1). A 1 m Unity cube is a useful scale reference. Let the importer convert axes; do not manually rotate everything to compensate for Blender's Z-up.
4. Create prefab variants for scene-specific colliders and gameplay. Use simple boxes for structures, barriers, crates and field bases. Use a few primitive colliders for animals and the excavator. Avoid colliders on every leaf, label, fence rail or tread.
5. The excavator and animals are static props; add rigs, animations, physics or behavior separately if they must move. The windmill is static too; its components are editable in the blend if a separate rotor is needed.
6. Rice plot base footprints are 4 x 4 m and 8 x 4 m. Align their soil bases consistently. Place plants above your ground/water surfaces; avoid overlapping or coplanar tiles. Separate plant clusters, borders and pathways let you build custom field shapes.
7. The narrow farm path and irrigation ditch are 4 m long. Ditches contain an actual recessed trough bounded by banks; water is a thin solid, opaque blue mesh. Water does not simulate flow. The corner field border has its own centered bounds, not a corner-aligned pivot.
8. Add actual Light components and a blink script to the warning light prefabs only if needed; the supplied amber lens is an emissive mesh material.
9. Generate lightmap UVs before baking lighting. If materials look wrong or turn pink, check your glTFast render pipeline and shader setup against its official documentation. These models use solid PBR base colors and require no texture files.
10. Provide Claude Code this extracted folder, `asset_manifest.json`, and your blueprint. Use the manifest's relative paths for placement. Blueprint dimensions do not automatically correspond to Unity meters.

These assets have not been tested in the Unity Editor in this environment.

Official Unity glTFast documentation:
- https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/Packages/com.unity.cloud.gltfast/Documentation~/index.md
- https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/Packages/com.unity.cloud.gltfast/Documentation~/ImportEditor.md
- https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/Packages/com.unity.cloud.gltfast/Documentation~/ProjectSetup.md
