# Enemy Ground Check Setup Guide

This guide explains how to set up the ground check for enemies in a way similar to the player's ground check system.

## Required Components

For proper ground checking, each enemy GameObject needs:

1. **EnemyResizableCapsuleCollider** - Manages the enemy's collider dimensions
2. **EnemyGroundCheck** - Handles ground detection logic
3. **BoxCollider** (as a child GameObject) - For ground detection

## Step-by-Step Setup

### 1. Create Enemy GameObject Structure

```
Enemy (Parent GameObject)
└── GroundCheck (Child GameObject)
```

### 2. Set Up the GroundCheck GameObject

1. Create a child GameObject named "GroundCheck" under your Enemy GameObject
2. Add a **BoxCollider** component to it
3. Set it as a **Trigger** (check "Is Trigger")
4. Position it at the enemy's feet
5. Size it appropriately (example: X=0.3, Y=0.1, Z=0.3)
6. Set its Layer to an appropriate layer (e.g., "GroundCheck" or "Trigger")

### 3. Set Up the EnemyResizableCapsuleCollider

1. Add the **EnemyResizableCapsuleCollider** component to the Enemy GameObject
2. In the inspector, under "Collider Data":
   - Set appropriate Height, Center Y, and Radius values
   - Drag the GroundCheck GameObject's BoxCollider to the "Ground Check Collider" field

### 4. Set Up the EnemyGroundCheck

1. Add the **EnemyGroundCheck** component to the Enemy GameObject
2. In the inspector, set the "Ground Layer" to include all layers that should count as ground

### 5. Layer Settings

Make sure your Layers are set up correctly:
1. Create a "GroundCheck" layer if you don't have one
2. In Edit > Project Settings > Physics:
   - Make sure "GroundCheck" layer can collide with your ground layers
   - Make sure it doesn't collide with other enemy layers
   - Make sure it doesn't collide with "Enemy" layer

## Testing

To verify your ground check is working correctly:
1. Enter Play mode
2. Select your enemy
3. Look at the EnemyGroundCheck component
4. You should see a green wireframe box if the enemy is grounded
5. It will turn red if the enemy is not touching the ground

## Troubleshooting

If the ground check isn't working properly:

1. **Enemy falls through the ground**
   - Make sure the GroundLayer mask is set correctly
   - Ensure the GroundCheck collider is properly positioned

2. **Ground not detected**
   - Make sure layers are set up correctly
   - Check that the GroundCheck box collider is properly sized and positioned
   - Verify the collision matrix in Project Settings

3. **Visual debugging**
   - In the Scene view, you should see a wire cube at the enemy's feet
   - It will be green when grounded, red when not grounded 