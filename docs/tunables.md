# Tunables

Generated from `@tune` headers by the menu `CGDD/Export Trace Docs`. Hand edits are overwritten on the next export.

| Script | Field | Type | Range | Default | Note |
| --- | --- | --- | --- | --- | --- |
| CardAlignmentUI | startRotationSteps | int | 1-3 | 1 | 90-degree steps away from aligned at start |
| CardReader | slideDuration | float | 0.3-2 | 0.8 | cabinet door slide time (s) |
| CeilingCardPanel | minFootHeight | float | 0.2-1 | 0.4 | required standing height (m) |
| CeilingCardPanel | maxHorizontalDistance | float | 0.5-2 | 1 | required proximity to L5 (m) |
| DrawerClue | slideDuration | float | 0.2-1.5 | 0.6 | drawer slide time (s) |
| FirstPersonController | moveSpeed | float | 1-8 | 3.5 | walking speed (m/s) |
| FirstPersonController | mouseSensitivity | float | 0.05-0.5 | 0.12 | look sensitivity |
| GrazingLightReveal | maxDistance | float | 0.5-3 | 1.5 | required lamp distance (m) |
| GrazingLightReveal | maxGrazeAngle | float | 10-45 | 30 | allowed angle from parallel-to-surface (deg) |
| KeypadUI | doorSlideDuration | float | 0.5-2 | 1 | door open animation time |
| KeypadUI | wrongGuessPenalty | float | 0-30 | 0 | seconds lost per wrong code |
| LightSwitchClue | fadeDuration | float | 0.2-1 | 0.5 | light/text fade time (s) |
| MainBreaker | wrongPenalty | float | 0-30 | 15 | seconds lost per wrong submit |
| MovableCabinetClue | slideDuration | float | 0.3-2 | 1 | cabinet slide time (s) |
| ObjectiveToast | holdDuration | float | 2-10 | 5 | how long the toast stays fully visible (s) |
| PlayerInteractor | range | float | 1-4 | 2 | interaction raycast distance (m) |
| RoomTimer | totalDuration | float | 60-400 | 180 | room time limit (s) |
| RoomTimer | flickerWindow | float | 20-90 | 60 | seconds remaining when flicker starts |
| RoomTimer | shutoffWindow | float | 5-40 | 20 | seconds remaining when sequential shutoff starts |
| RoomTimer | blackoutHold | float | 1-5 | 2 | blackout hold before loss (s) |
| RoomTimer | penaltyFlashDuration | float | 0.1-1 | 0.3 | penalty dim-flash length (s) |
| StoolCarrier | maxPlaceDistance | float | 1-4 | 2 | how far from the player the stool can be placed (m) |

