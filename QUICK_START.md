# 🎮 QUICK START - Combat Arena

## ⚡ 30-Second Setup

### In Unity Editor:
1. Menu: `GameObject → Combat Arena → Setup Complete Scene`
2. Press `Play`
3. Done! ✅

---

## 🎯 What You'll See

- **Arena**: 60×60 tiles, rooms, corridors, walls
- **Player**: Cyan cube at center (you)
- **Enemies**: 
  - 2 Magenta capsules (NavMesh AI)
  - 2 Green capsules (Grid A* AI)

---

## 🕹️ Controls

| Action | Keys |
|--------|------|
| Move | WASD / Arrows |
| Shoot | Space / Left Click |
| Help | H (in Play mode) |

---

## 🤖 Enemy Types

### Magenta (NavMesh)
- Direct aggressive chaser
- Uses Unity pathfinding
- Smooth obstacle avoidance

### Green (Grid A*)
- Strategic pathfinder
- Avoids narrow corridors
- Prefers open spaces

**Both shoot within 10 units!**

---

## 📂 File Locations

- **Scripts**: `Assets/Scripts/`
- **Full Guide**: `SETUP_GUIDE.md`
- **Details**: `CONFIGURATION_COMPLETE.md`

---

## 🆘 Problems?

**No enemies?**
→ Add `EnemySpawner` GameObject (use menu above)

**Enemies not moving?**
→ Green enemies work immediately
→ Magenta need NavMesh (optional, see guide)

**Player stuck?**
→ Check `PlayerController` is attached

---

## 💡 Tips

- Press H in Play mode for live help
- Adjust enemy counts in `EnemySpawner` inspector
- Arena regenerates each Play session
- Enemies auto-spawn at random locations

---

**Ready? Press Play and start fighting!** 🎮

