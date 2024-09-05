# Mod Tools

## Description
Project made for users who want to create mods for Carnage Wars. 😊

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Usage](#usage)
4. [License](#license)
5. [Acknowledgements](#acknowledgements)

## Prerequisites
Before installing to your new Unity project, make sure you have installed the following packages from the Package Manager to avoid compiler issues:
- `com.unity.ai.navigation`
- `com.unity.nuget.newtonsoft-json`
- `com.unity.postprocessing`

## Installation
To install the project, download the latest version from the [Releases](#) section.

## Usage

### Setting Up a Map

After installing the Unity package, follow these steps to set up and export your map:

1. **Open or Create a Scene:**
   - Open the scene with your map, or create a new scene and build the map you want to export.
   
2. **Ensure Proper Collider Setup:**
   - Make sure your map has all necessary colliders set up correctly.

3. **Add the ModMapManager Prefab:**
   - Go to the `Prefabs` folder under `source/Prefabs` and drag the `ModMapManager` prefab into your scene.
   
   ![Drag and Drop Prefab](Documentation/Images/Drag_And_Drop_Prefab_On_Scene.jpg)

4. **Access Map Tools:**
   - Select the `ModMapManager` in the Hierarchy. You should see the Map Tools in the Inspector panel.
   
   ![Preview Map Tools](Documentation/Images/Preview_MapTools.jpg)

### Configuring the Map for Export

1. **General Setup:**
   - Expand the "General" section by clicking the arrow next to it.
   
   ![Open General](Documentation/Images/Open_General.jpg)

2. **Edit Playable Area:**
   - Click the "Edit Area" button to enter Area Edit Mode. Use this tool to set the area on your map that players are allowed to access. Please set this area as small as possible to avoid precision issues.
   
   ![Setup Playable Area](Documentation/Images/SetupPlayableArea.gif)

   **Note:** Placing points requires the stage colliders to be set up.

3. **Set Spawn Points:**
   - Select the "Edit SpawnPoints" button to enter SpawnPoints Mode. Left-click anywhere in the scene to place a spawn point. Right-click a spawn point to remove it.
   
   ![Place Spawn Points](Documentation/Images/PlaceSpawnPoints.gif)

4. **Set Weapon Spawn Points:**
   - Select the "Edit WeaponsSpawnPoints" button to enter WeaponsSpawnPoints Mode. Left-click anywhere in the scene to place a weapon spawn point. Right-click a spawn point to remove it. Weapon spawns are crucial for Battle Royale mode.
   
   ![Place Weapon Spawn Points](Documentation/Images/PlaceWeaponSpawnPoints.gif)

5. **Base Setup:**
   - To set up a team base point, click the "Set Team Base" button and then click the area where you want the team's base to be. Players will automatically spawn close to this point in modes that require them to spawn at their base.
   - To set up a team flag point, click the "Set Team Flag" button and then click the area where you want the team's flag to be.

   **Note:** You'll need to set up the base and flag for both teams to avoid issues.
   
   ![Base Setup](Documentation/Images/BaseSetup.gif)

### Exportable Components

The exporter can handle the following components, if available in your scene:

1. **Mesh Renderers:**
   - Exports mesh, sprite, and text renderers.

2. **Colliders:**
   - Includes all colliders set up in the scene.

3. **Sounds:**
   - Exports any sound sources present in the scene.

4. **Lights:**
   - Exports all types of lights.

5. **Lightmaps:**
   - Includes baked lightmaps.

6. **Light Probes:**
   - Exports light probes used in the scene.

7. **NavMesh Surface:**
   - Exports the local NavMesh surface. (Found in the `ModMapManager` prefab)

8. **Post Processing Volume:**
   - Must be set up on the Main Camera to be exported.

9. **Render Settings:**
   - Exports the render settings of the scene.

10. **Shaders:**
    - Exports all shaders used in the scene.

11. **Terrains:**
    - Exports terrain components.

12. **Materials:**
    - Includes all materials used in the scene.

13. **LOD Groups:**
    - Exports LOD (Level of Detail) groups.

14. **Reflection Probes:**
    - Exports reflection probes.

15. **NavMesh Links:**
    - Exports any NavMesh links present in the scene.

16. **Other Components:**
    - Exports additional components, such as post-processing volumes, that might be specific to your scene.

### Notes
- Ensure all required components are set up and correctly configured before exporting the map.
- The exporter currently supports only the local NavMesh surface. Other NavMesh surfaces will be supported in future updates.

## License
(This section will include information about the project's license.)

## Acknowledgements
(Credits and acknowledgements will go here.)
