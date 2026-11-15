# TODA RUSH — Level 2: Palengke District asset library

Actual low-poly, flat-shaded 3D meshes. This is a reusable asset library, not an assembled Level 2 map. No reference blueprint has been interpreted or altered.

## Contents

- `GLB/01_Public_Market` to `GLB/12_Furniture`: individual GLB 2.0 models, with embedded PBR color materials. Every model is self-contained; no external texture files required.
- `TODA_RUSH_Palengke_Assets.blend`: editable component meshes in named asset collections, with one root per asset. The spaced grid is an inspection catalog only.
- `asset_manifest.csv` and `.json`: exact exported bounds in meters, triangle counts, filenames, and original editable mesh part counts.
- `source/build_assets.py`: fully reproducible Blender Python generator.
- `source/validate_assets.py` and `docs/validation_report.json`: validation code and results for every GLB.
- `docs/IMPORT_GUIDE.md`: Blender and Unity import and scaling instructions.

## Conventions and scope

One Blender unit = one meter. Each exported asset is centered on X/Y and sits on Z=0 in Blender. glTF uses Y-up; importers handle the conversion. Dimensions in the manifest are Blender X width, Y depth, Z height (Unity width, height, depth correspond to X, Z, Y in that listing). All source scales and rotations are applied. Vehicle noses face +X; signs face Blender -Y. Road arrows point Blender +Y. Road markings are thin actual mesh volumes.

The main market is approximately 12.5 × 8.5 meters, stalls approximately 2.4 meters wide, and table tops approximately 0.9 meters high. Check the manifest for exact bounds, including roofs, ropes, mirrors and labels. Dimensions describe each asset, not placement coordinates on the 600 × 600 blueprint.

All export geometry is combined per asset, with material groups, to reduce mesh objects and material draw calls. Component meshes remain separate in the editable blend. Text labels are mesh geometry. Vehicles are static props, with editable wheels in the blend; driving controls, rigs, animations, lights, colliders, LOD chains and gameplay scripts are not included. Glass is opaque stylized blue for consistent inexpensive rendering. Assets use solid colors, not detailed texture maps; create UV/lightmap layouts in Unity or Blender if needed. Default cube UVs may exist, but this library does not provide an authored texture unwrap.

The main market is an open hall with columns and rear wall/doors, not a fully furnished interior. Stocked stalls include produce, and their matching produce items and display trays are also reusable individual files. Shelter seating is built in; separate benches are provided too.

For large populations, reuse prefabs, share material instances, and profile draw calls. Tiny labels and produce can be omitted at distance. Repeated colors are intentionally shared material names; material deduplication depends on importer settings.

## Regenerate

With Blender 4.3 or later: `blender -b --python source/build_assets.py` from the extracted folder. Output is resolved relative to the script, so this overwrites the generated GLBs, manifest and blend in this library. Run `blender -b --python source/validate_assets.py` afterward. The build script uses only Blender and Python standard libraries.

## Usage rights

These original procedural models and generator were created for this request. You may use, modify, and distribute them in your TODA RUSH project and other projects. No third-party model downloads or textures are included. Lettering is converted from Blender's built-in Bfont. No attribution is requested for this asset collection.
