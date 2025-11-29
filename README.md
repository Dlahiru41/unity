# Combat Arena Project

This Unity project contains a tile-based arena generator, player controller, and new enemy AI systems.

## Features
- Procedural arena generation with cost map for pathfinding.
- Player controller supporting WASD movement and shooting.
- Two enemy types:
  - NavMesh-based chaser (`NavMeshEnemyController`).
  - Grid pathfinding hunter (`GridEnemyController`).
- Shared projectile logic with team assignments.
- Enemy spawner that instantiates both prefabs at runtime.

## Setup
1. Open the project in Unity 2021+.
2. Open `SampleScene`.
3. Ensure the `ArenaGenerator` and `EnemySpawner` components are in the scene.
4. Assign enemy prefabs to the spawner.
5. Bake a NavMesh for the arena floor for NavMesh enemies.
6. Press Play to generate the arena, spawn player, and spawn enemies.

