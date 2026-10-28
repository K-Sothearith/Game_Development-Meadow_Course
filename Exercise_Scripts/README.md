# Part 2 — Exercise scripts (you complete these)

Four scripts for the Orb Collector. Copy them into `Assets/_Project/Scripts/` and fill in the eight `TODO` blocks. Each TODO is one function from the lecture.

Everything not marked TODO already works: movement, camera positioning, orb bobbing and spinning, the score counter, and the win check. You are adding the events that make the game playable.

## The eight tasks

| TODO | File | Function you write | What it must do |
|---|---|---|---|
| 1 | `PlayerController.cs` | `OnCollisionEnter(Collision c)` | Log the name of any wall you bump |
| 2 | `PlayerController.cs` | `OnTriggerEnter(Collider other)` | If `other.CompareTag("Orb")`, `Destroy` the orb and log the pickup |
| 3 | `PlayerController.cs` | `LateUpdate()` | Move `cameraTransform` to the player position plus an offset |
| 4 | `Orb.cs` | `OnMouseDown()` | Cycle the orb colour through the palette when clicked |
| 5 | `PlayerController.cs` | `OnDestroy()` | `Instantiate` one bonus orb at the player's last position |
| 6 | `GameManager.cs` | `Awake()` | Count the orbs live with `GameObject.FindGameObjectsWithTag` |
| 7 | `GameManager.cs` | `OnGUI()` | Draw a centred win banner when every orb is collected |
| 8 (stretch) | `CameraFollow.cs` | `Update()` | Zoom with `Input.GetAxis("Mouse ScrollWheel")` |

## Scripts and where they attach

| Script | Tasks | Attach to | Required setup |
|---|---|---|---|
| `PlayerController.cs` | 1, 2, 3, 5 | Player cube | Rigidbody, freeze rotation X and Z. Drag the orb prefab and Main Camera into the Inspector fields. |
| `Orb.cs` | 4 | Orb sphere | Scale 0.6, Collider with **Is Trigger** ticked, tag `Orb` |
| `CameraFollow.cs` | 8 | Main Camera | Drag Player into *Target* |
| `GameManager.cs` | 6, 7 | Empty GameObject `GameManager` | Nothing else |

## Working order

1. **TODO 1 and 2 first.** They make the game respond to contact, and TODO 2 is what makes orbs collectable at all.
2. **TODO 3.** The camera follows you, so you can see what you are doing.
3. **TODO 4.** Click an orb to confirm the mouse events from the lecture.
4. **TODO 5.** Destroy the player and watch a bonus orb appear.
5. **TODO 6 and 7.** Turn the score into a real count and a win state.
6. **TODO 8** only when the rest works.

After each TODO, press Play and check the Console. If nothing prints, the function is not being called: check the Collider, the Rigidbody, and the tag before changing the code.

## Test that it works

- Drive into a wall: one `[OnCollisionEnter]` line per bump, not one per frame.
- Drive into an orb: the orb disappears and the Console logs the pickup.
- Click an orb: its colour changes.
- Collect all five: the win banner appears.
- Press Play again: the score resets to zero.
