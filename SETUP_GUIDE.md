# Combat Arena - Complete Setup Guide

## Automatic Setup (Recommended)

The easiest way to configure everything:

### Option 1: Use Unity Menu (One-Click Setup)
1. In Unity Editor, go to menu: **GameObject → Combat Arena → Setup Complete Scene**
2. Press **OK** on the confirmation dialog
3. Press **Play** - enemies will spawn automatically!

### Option 2: Manual Quick Setup
1. In Unity Editor, go to menu: **GameObject → Combat Arena → Add Enemy Spawner**
2. This creates an `EnemySpawner` GameObject with all settings configured
3. Press **Play** - enemies spawn automatically (no prefabs needed!)

---

## What Happens Automatically

When you press Play:
1. **Arena generates** (60×60 tile-based combat arena with walls, rooms, corridors)
2. **Player spawns** at center (cyan cube with red front marker)
3. **Enemies spawn** automatically:
   - **2 Magenta Capsules** = NavMesh-based chasers (use Unity's pathfinding)
   - **2 Green Capsules** = Grid-based hunters (use custom A* with tile costs)
4. **Camera follows** player automatically
5. All enemies **chase and shoot** at the player within range

---

## Current Scene Status

You currently have:
- ✓ `Arena` GameObject (generated arena with walls/floors)
- ✓ `Player` GameObject (with movement and shooting)
- ⚠ **Missing**: `EnemySpawner` GameObject

---

## Manual Configuration (If Needed)

### Step 1: Add EnemySpawner to Scene
```
1. Create empty GameObject: Hierarchy → Right-click → Create Empty
2. Rename it to "EnemySpawner"
3. Add component: EnemySpawner script
4. Configure Inspector settings:
   - Spawn On Start: ✓ (checked)
   - Respawn On Arena Generated: ✓ (checked)
   - Nav Mesh Enemy Count: 2
   - Grid Enemy Count: 2
   - Leave prefab fields EMPTY (runtime enemies will auto-create)
```

### Step 2: Press Play
- Enemies will spawn and chase the player automatically!

---

## Enemy Behavior Explained

### Enemy Type A: NavMesh Enemy (Magenta)
- **Pathfinding**: Uses Unity's NavMesh system
- **Behavior**: Direct aggressive chaser
- **Movement**: Smooth navigation around obstacles
- **Requires**: NavMesh baked on arena floor (optional - works without it initially)

### Enemy Type B: Grid Enemy (Green)
- **Pathfinding**: Custom A* algorithm with tile costs
- **Behavior**: Strategic pathfinder
- **Movement**: Grid-based with cost awareness
- **Cost weights**: Avoids narrow corridors (cost 3x), prefers open areas (cost 1x)

### Both Types:
- ✓ Health: 80 HP
- ✓ Attack Range: 10 units
- ✓ Fire Rate: Every 1.25 seconds
- ✓ Projectiles: Spawn bullets that damage player
- ✓ Die when health reaches 0

---

## Testing Your Setup

### Expected Behavior:
1. **Press Play**
2. See arena generate with walls/rooms
3. Player spawns at center (cyan cube)
4. 4 enemies spawn at random floor locations
5. Enemies chase player and shoot when in range
6. Player can move (WASD) and shoot (Space/Mouse)

### Controls:
- **Movement**: WASD or Arrow Keys
- **Shoot**: Space or Left Mouse Button
- **Camera**: Follows player automatically

---

## Troubleshooting

### No Enemies Spawning?
- Check Console for warnings
- Ensure `EnemySpawner` GameObject exists in Hierarchy
- Verify `ArenaGenerator` exists and generates arena

### Enemies Not Moving?
- **Magenta enemies**: May need NavMesh baked (see below)
- **Green enemies**: Should work immediately (grid pathfinding)

### NavMesh Baking (Optional for NavMesh Enemies)
If magenta enemies aren't moving smoothly:
```
1. Wait for arena to generate (press Play, then Stop)
2. Select Arena → Floor → ArenaBaseFloor in Hierarchy
3. Add Component → Navigation → Nav Mesh Surface
4. Set "Collect Objects" to "Children"
5. Click "Bake" button
6. Press Play again - NavMesh enemies now navigate perfectly!
```

---

## Advanced Configuration

### Custom Enemy Prefabs (Optional)
If you want custom enemy models:
1. Create prefab with `NavMeshEnemyController` or `GridEnemyController`
2. Assign prefab to EnemySpawner inspector slots
3. Prefabs override runtime enemy generation

### Adjust Enemy Count
Select `EnemySpawner` → Inspector:
- Increase/decrease enemy counts as needed
- Mix of both types recommended for variety

### Modify Enemy Stats
Edit scripts directly or create Inspector-exposed fields:
- `EnemyBase.cs` - shared health, attack range, fire rate
- `NavMeshEnemyController.cs` - NavMesh speed, re-path interval
- `GridEnemyController.cs` - grid speed, path refresh rate

---

## File Structure

All scripts are in `Assets/Scripts/`:
- `ArenaGenerator.cs` - Procedural arena generation
- `PlayerController.cs` - Player movement & shooting
- `EnemySpawner.cs` - Spawns & manages enemies
- `EnemyBase.cs` - Base enemy behavior (health, shooting)
- `NavMeshEnemyController.cs` - NavMesh pathfinding AI
- `GridEnemyController.cs` - Grid pathfinding AI
- `GridPathfinder.cs` - A* algorithm with tile costs
- `Bullet.cs` - Projectile behavior & damage
- `CameraFollower.cs` - Smooth camera follow
- `SceneSetupHelper.cs` - Auto-configuration utilities

---

## Quick Start Summary

**Fastest way to get everything working:**

```
Unity Menu → GameObject → Combat Arena → Setup Complete Scene
Press Play
Done! ✓
```

Enemies spawn automatically with no additional configuration needed!

---

## Next Steps

1. Press Play and test the combat
2. Adjust enemy counts in `EnemySpawner` inspector
3. Optionally bake NavMesh for smoother NavMesh enemy movement
4. Tweak arena size in `ArenaGenerator` (default 60×60)
5. Add custom enemy models/materials as needed

Enjoy your combat arena!

