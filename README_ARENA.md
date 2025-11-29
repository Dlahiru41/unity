Combat Arena Generator

This Unity script generates a tile-based combat arena (minimum 50x50) with outer boundary walls, internal rectangular rooms, corridors, hiding spots, and open areas suitable for AI navigation testing.

Files added:
- Assets/Scripts/ArenaGenerator.cs — main generator component.

Usage:
1. Open the Unity project.
2. Create an empty GameObject in the scene and attach the `ArenaGenerator` component (drag the script onto the object).
3. Configure parameters in the inspector (width/height, seed, etc.).
4. Click the context menu "Generate Arena" (three-dot context menu on the component) or enable `generateOnStart` for runtime generation.

Notes:
- The generator uses a simple cellular automata smoothing and deterministic room placement (no Perlin noise).
- Walls are Unity Cubes, floor is a Plane. For more polished visuals, replace primitives with prefabs or meshes.
- The generator keeps only the largest connected floor region to avoid isolated pockets.

If you want I can add a NavMesh baking step, prefabs for tiles, or editor tooling to save the layout as an asset.
