# AR Product Viewer

Place and arrange 3D products in your real space using mobile AR.

[![YouTube - Watch the demo](https://img.shields.io/badge/YouTube-Watch%20the%20demo-red?logo=youtube&logoColor=white)](https://youtu.be/8dCTaCd1Hc4)

[![AR Product Viewer demo](https://img.youtube.com/vi/8dCTaCd1Hc4/hqdefault.jpg)](https://youtu.be/8dCTaCd1Hc4)

An Android app built with Unity and AR Foundation. Point your phone at the floor, pick a piece of furniture, and place it in your room through the camera. Placed products stay locked to the real floor, and you can move, turn, resize or delete them to try out different layouts. The lighting follows the real room and a soft contact shadow grounds each piece.

## Screenshots

<!-- Screenshots go here, e.g.
<p>
  <img src="Docs/images/placement.jpg" width="260" alt="Placing a chair">
  <img src="Docs/images/arranged.jpg" width="260" alt="Arranged room">
</p>
-->

_Coming soon._

## Features

- **Floor detection:** detects horizontal surfaces with ARCore and works out which one is the floor, so tables, beds and other raised surfaces are ignored. A subtle grid shows the detected floor.
- **Product picker:** a scrollable bar with five products: chair, table, sofa, potted plant and tall plant.
- **Ghost-preview placement:** a translucent ghost of the selected product and a reticle follow the centre of the screen on the floor. Tap to place the product exactly where the ghost is.
- **Floor-locked, anchored placement:** every placed product is attached to an AR anchor, so it stays put as you walk around. Small tracking corrections are absorbed and the product never leaves the floor.
- **Move, rotate, scale, delete:** tap a placed product to select it, drag with one finger to move it across the floor, twist with two fingers to turn it, and pinch to resize it (0.5x to 2x). A toolbar lets you deselect or delete the selected product.
- **Light estimation:** the scene light follows ARCore's estimate of the real room's brightness, colour and main light direction, smoothed over time. If the phone doesn't provide an estimate, a neutral indoor light is used instead.
- **Grounding shadows:** a soft contact shadow under each product makes it look like it is standing on the floor, without the cost of real-time shadows.
- **Scan hint:** a short on-screen hint asks you to move the phone slowly until the floor is found, then fades out.
- **View mode:** tap the highlighted product in the picker again to turn placement off. The ghost and reticle disappear, so you can look around and edit what you've placed. Tap any product to start placing again.

## Tech stack

- Unity 6000.3.5f2, Universal Render Pipeline (URP)
- AR Foundation 6.7.1, ARCore XR Plugin 6.7.1
- C#, Input System (touch input through Enhanced Touch)
- NUnit Edit Mode tests (Unity Test Framework)
- Android: IL2CPP, ARM64, minimum API level 29, ARCore required

## Architecture

The code is split into layers, each in its own assembly, with dependencies pointing toward Core:

- **Core** (plain C#, no engine references): placement and floor selection, anchor following, move/rotate/scale maths, product catalog and selection, light estimate mapping and smoothing, scan hint state. All of it is covered by Edit Mode tests.
- **AR:** thin adapters around AR Foundation: plane raycasts, anchors, the placement ghost and reticle, touch gestures on placed products, light estimation and contact shadows. They turn AR data into Core calls and apply the results.
- **UI:** the product picker, selection toolbar and scan hint. They talk to Core, never to AR directly.
- **App:** the composition root that builds the catalog and wires Core, AR and UI together.
- **Tests:** 165 NUnit Edit Mode tests, all passing.

Performance target is a steady 30 FPS on a mid-range phone: reduced render scale, no MSAA or HDR, no real-time shadows, and the SRP Batcher on.

## Build and run

1. Install Unity **6000.3.5f2** with the **Android Build Support** module (including the Android SDK, NDK and OpenJDK).
2. Clone this repo and open the folder as a project in Unity Hub. Let Unity resolve packages on first open.
3. Open `Assets/Scenes/ARMain.unity`.
4. Go to **File > Build Profiles**, select **Android** and click **Switch Platform**.
5. Connect an [ARCore-supported Android device](https://developers.google.com/ar/devices) with USB debugging enabled.
6. Click **Build And Run**.

To run the tests: **Window > General > Test Runner**, open the **EditMode** tab and click **Run All**.

## Credits

3D models: [Kenney](https://kenney.nl) (CC0)
