# Enemy AI Controller Setup Guide

## Initial Setup

To properly set up an enemy in your scene:

1. Create a new GameObject for your enemy
2. Add the following components:
   - NavMeshAgent
   - Your existing Enemy component from the Characters/NPCs folder
   - EnemyAIControllerFix (not EnemyAIController)

The EnemyAIControllerFix enhances your existing Enemy class with advanced AI behaviors like patrolling, field of view detection, and integrated rhythm game combat.

## Fixing Common Issues

### "Can't add script component" Error

If you see this error, try these steps in order:

1. Make sure to use `EnemyAIControllerFix` instead of `EnemyAIController`
2. Close the Inspector and reopen it
3. Check the console for compiler errors and fix them
4. Restart Unity
5. If on Windows, temporarily switch to another application and back to Unity (forces script recompilation)
6. Make sure the namespace is correctly set to `EverdrivenDays` in your scripts

### Unity Won't Compile the Script

If Unity won't compile the script correctly:

1. Try deleting the .meta file for the script
2. Create a new script with a slightly different name
3. Copy the contents over
4. Restart Unity

### Enemy Doesn't Move

If your enemy isn't moving, verify:

1. The NavMeshAgent component is properly configured
2. You have baked a NavMesh for your scene (Window > AI > Navigation)
3. The enemy is placed on the NavMesh
4. You've set up patrol points if using patrol behavior

## Component Requirements

### NavMeshAgent Settings

- **Speed**: Set automatically by EnemyAIControllerFix based on state
- **Stopping Distance**: Recommended 0.5 or lower for precise movement
- **Auto Braking**: Enable for more realistic stopping

### Patrol Points

To set up patrol points:

1. Create empty GameObjects in your scene
2. Position them where you want the enemy to patrol
3. Drag these objects into the "Patrol Points" array in the EnemyAIControllerFix

### Detection Settings

Adjust these settings in the EnemyAIControllerFix:

- **Detection Radius**: How far the enemy can see
- **Attack Range**: How close the enemy must be to attack
- **Field of View Angle**: The width of the enemy's vision cone (120° is typical)
- **Obstacle Layer Mask**: Layers that block enemy vision

## Component Relationships

Here's how the components work together:

1. **NavMeshAgent**: Handles pathfinding and movement
2. **Enemy**: Your existing enemy class that manages health and damage
3. **EnemyAIControllerFix**: Provides more advanced AI behavior and rhythm game integration

The controller will automatically:
- Detect the Player by tag
- Follow patrol paths
- Chase the player when detected
- Trigger combat when in range
- Integrate with the rhythm game system

## Testing Your Enemy Setup

After setup:

1. Enter Play mode
2. Verify the enemy patrols along designated patrol points
3. Move your player character into detection range
4. Verify the enemy chases and attacks
5. Confirm the rhythm game starts when the enemy attacks

## Adding NavMesh To Your Scene

If you haven't set up NavMesh yet:

1. Open the Navigation window (Window > AI > Navigation)
2. Go to the Bake tab
3. Adjust settings as needed
4. Click "Bake"
5. Make sure your ground objects are marked as "Walkable" in their Navigation settings

## Troubleshooting

- **If the enemy doesn't start patrolling**: Check if NavMeshAgent is enabled and your patrol points are set
- **If the enemy doesn't chase the player**: Make sure the player has the "Player" tag
- **If combat doesn't start**: Verify that RhythmGameController and CombatManager can be found in the scene
- **If animation doesn't work**: Make sure your animator has parameters named "Walk", "Chase", and "Attack" 