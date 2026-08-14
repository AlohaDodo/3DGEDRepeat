Dorota Kaminska GD3 - 3DGED repeat

## Project overview
This project is an interactive 3D feature showcase created using the GD Engine. The environment is designed as a central hub connected to five demonstration rooms with each room focusing on a different engine system : Physics, Audio, Camera, Orchestration and UI & Input. I modelled the showcase map myself in Blender and imported it into the project as the main environment.

The player can freely explore the environment in first person and interact with demonstrations such as rigid body physics, spatial audio, different camera behaviours, a scripted orchestration sequence and real time UI controls. Each area also contains an annotation explaining the engine systems and components being used.

## Architecture
The showcase is built within a single GD Engine Scene containing the hub and five demonstration rooms. Main.cs is responsible for initialising the scene, engine systems, cameras, UI and the objects used by each demonstration. This project follows the engine’s GameObject component structure, where objects are given components such as transforms, colliders, rigid bodies, cameras and UI elements depending on their purpose. Engine systems including PhysicsSystem, AudioSystem, InputSystem, UIRenderSystem and OrchestrationSystem manage the relevant components. I also use the EventBus to send events between different systems in the showcase.


## Elective Zones - R4 & R5
For my two elective rooms I chose Orchestration and UI & Input.

I chose Orchestration because I wanted to create a sequence that combined several engine features instead of demonstrating only one system. My sequence uses camera switching, audio, UI updates, transforms and conditional behaviour to create a crate reveal with a surprise monkey. (There is also a secret monkey mode)

I chose UI & Input because I wanted to demonstrate how the engine UI can interact with the game. The room contains a live HUD showing camera position and elapsed time. I didn’t want the live HUD showing in each room so I added a menu control that allows the player to toggle the debug information using a button. I also added menu controls that allow the player to change the music and SFX volume using sliders.


##Design pattern - Observer
The design pattern I chose to demonstrate is the Observer pattern. I use the engine’s EventBus to publish events which other systems can listen for and respond to. The EventBus acts as the subject while the systems listening for events act as the observers. I use this across multiple parts of the showcase. For example ; the audio room publishes PlaySfxEvent and PlayMusicEvent, while the orchestration room publishes CameraEvent and PlaySfxEvent during its sequence. This allows different systems to react to events without the rooms needing to directly control those systems.


## Significant design decisions
One of my main design decisions was to create the whole environment as one connected map so the player can walk naturally between each demonstration room. I also gave each room its own “theme” and interactive demonstration so that  the engine features could be seen clearly rather than only shown through code. 

I used on screen annotations that only appear when the player enters the relevant area, keeping the rest of the screen clear. I also reused the existing engine systems and components where possible such as the EventBus, UI components and orchestration system instead of creating separate systems for features the engine already had.


##Controls
- ##WASD## - First person
- ##UHJK## - Third person
- ##Mouse## - Look around/Zoom in and out
- ##ESC## - Open/pause the menu
- ##B## - Launch the bowling ball
- ##1 / 2 / 3## - Switch between camera modes
- ##7 / 8## - Interact with the spatial audio emitters
- ##G## - Trigger the EventBus SFX in the audio room
- ##O## - Start orchestration sequence
- ##R## - Toggle the secret monkey mode before starting the sequence
- ##Menu sliders## - Adjust music and SFX volume
- ##Debug info button## - Toggle the engine debug information


## Screencast
Coming soon
