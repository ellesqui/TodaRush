# TODA RUSH — Level 3: Bukid Hillside Route

73 original procedural low-poly GLB models, covering only the four requested asset categories. These are actual mesh assets with embedded color materials, not images. No environment, terrain layout, hillside route or road map is assembled.

## Use this package with Claude Code

Extract this ZIP into a project folder accessible to Claude Code. The reusable models are inside `GLB/`, in four categorized subfolders. Give Claude Code the `asset_manifest.json` and your finalized blueprint separately. The manifest contains dimensions in meters, not map coordinates. Establish your blueprint-to-world scale before placement.

Suggested instruction: "Use the individual GLB models in TODA_RUSH_Level3_Bukid_Assets/GLB to build the Level 3 environment according to my finalized blueprint. Read asset_manifest.json for sizes and filenames. Reuse the models; do not treat the Blender inspection grid as the map."

## Files included

- `GLB/01_Farm_Agricultural_Structures/`: 20 models.
- `GLB/02_Rice_Fields_Farmland/`: 14 models.
- `GLB/03_Road_Construction_Obstacles/`: 22 models.
- `GLB/04_Animals_Countryside_Props/`: 17 models.
- `TODA_RUSH_Level3_Bukid_Assets.blend`: editable component meshes, one named collection and root per asset. The spaced grid is for inspection only.
- `asset_manifest.csv` and `asset_manifest.json`: exported bounds, triangle counts and relative GLB paths.
- `ASSET_INVENTORY.md`: categorized model links and dimensions.
- `docs/IMPORT_GUIDE.md`: import, placement and material notes.
- `docs/REQUEST_COVERAGE.md`: requested models mapped to delivered GLBs.
- `docs/validation_report.json`: individual validation results and SHA-256 hashes.
- `source/build_assets.py` and `source/validate_assets.py`: Blender Python generation and validation scripts.

## Modeling conventions

One unit = one meter. Every exported GLB has its ground center at the origin, using the bounding box including accessories. Blender source is Z-up; GLB uses Y-up and the importer handles conversion. The manifest's dimensions are Blender X width, Y depth, Z height. Source object transforms are applied before export. Animals and the excavator face +X; signs face Blender -Y. Paths and irrigation ditches run along Blender Y. Fence straight sections run along X.

GLBs combine mesh components per asset with material groups to reduce object overhead; the component meshes remain separate in the editable blend. Flat shading and a shared palette give the collection a consistent art style. No downloaded third-party models or external textures are required. Lettering is converted to mesh from Blender's built-in font. Water and window materials are stylized opaque colors, avoiding transparency sorting cost. Amber lenses use an emissive material.

The full collection contains 26,752 triangles; the largest asset is 2,580 triangles. Asset files contain between one and several material groups; small triangle counts alone do not guarantee scene performance. Reuse prefabs and shared materials and profile your assembled scene.

## Scope and limitations

Animals and machinery are static, unrigged models. They have no skeletal animation, working hydraulics, vehicle controls or physics. Warning lights have no blinking animation or Light components. Buildings are exterior props/open shelters, not furnished interiors. No colliders, Unity prefab assets, LOD chains, baked lightmaps, custom texture unwraps or gameplay scripts are included. Simple UVs may exist on text but the asset library does not depend on UV textures. Add lightmap UVs if baking lighting.

All 73 GLBs were checked for valid GLB 2 headers, embedded buffer ranges, finite mesh positions, triangle indices/counts, assigned materials, complete Blender reimport, dimensions and ground-centered pivots. Representative meshes were rendered and visually inspected as an internal quality check. This was not a Unity Editor playtest or Khronos-validator certification.

## Rebuild

Use Blender 4.3 or newer. Run `blender -b --python source/build_assets.py` from the extracted package directory. The script resolves its output relative to its own location and overwrites the generated GLBs, manifests and blend. Then run `blender -b --python source/validate_assets.py`. The generator uses Blender and Python standard libraries only. The authored catalog deliberately spaces model roots apart for inspection; only export each asset at its own origin for use in a map.

You may use and modify the original procedural models and generator for TODA RUSH and other projects. No attribution is requested for this collection.
