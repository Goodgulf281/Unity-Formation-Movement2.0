# Unity-Formation-Movement2.0
Formation movement for Unity 3D using built in NavMesh navigation or A*Pathfinding

# Summary
This Github repo contains the 2.0 version of my Formation Movement scripts rebuilt from scratch. The Formation Movement 2.0 allows you to move objects in a present formation shape towards a destination. It supports both AStarPathfinding and Unity built-in NavMesh navigation. Other features include:
* Preset formation shapes (easily expanded with your own).
* Changing formation on route. Includes a trigger class to change the formation.
* Evasion if formation followers get stuck on terrain features
The scripts are free to use (see the license), feel free to ask questions or make pull requests but don’t expect on the spot support.

# Version History

* Version 2.0 - Original upload
* Version 2.1 - Bug fix to solve a bug caused by FormationLeader.Start() called before Formation.Start(). Included FormationAddFollwerTrigger utility class and small update to Formation class which allows dynamically adding followers to the formation (as suggested by Adebanji I. in Youtube channel comments). Added a Formation Sample project (Unity 2020.3); open the "Formation Example 2" scene. 

# Pre-requisites
* If you want to make use of the Formation Animation Movement script (FormationAnimation.cs) then first download and import the Animation Controller into your project from: https://docs.unity3d.com/Manual/nav-CouplingAnimationAndNavigation.html
* If you prefer AStarPathfinding over Unity NavMesh then install the AStarPathfinding asset (https://arongranberg.com/astar/) and enable this option in Formation Movement 2.0 (it’s disabled by default, see below).

# Quick start guide (Unity NavMesh)
Below script lists the steps explained in this video.

[![Tutorial](https://img.youtube.com/vi/XO4WdADkrg0/maxresdefault.jpg)](https://youtu.be/XO4WdADkrg0)

Create an environment for the formation to walk on.

![Environment](/Images/f1.png) 

The Cylinder at the top left will serve as the destination for the formation.

Prepare the Layers for the scene:

![Environment](/Images/f14.png) 
 
Assign the Terrain Layer to the Plane and the Obstacles layer to the Cubes. Next set the Plane and cubes to static.

![Environment](/Images/f7.png) 
 
Setup the Navmesh by using the [Bake] button:

![Environment](/Images/f15.png) 

Create a prefab from a sphere.

![Environment](/Images/f2.png) 

Click on the menu Window | Goodgulf | Formation Setup:

![Environment](/Images/f3.png) 

Click on "Set Navigation to Unity Navmesh" and wait for a couple of seconds to ensure compiling of the scripts has finished (typically NavMesh is the default so a compile is not necessary in a fresh project). Next click on "Create a formation":

![Environment](/Images/f4.png) 
 
The Formation and the Leader object have now been added to the hierarchy:

![Environment](/Images/f5.png) 

Add the Formation Follower script to the Sphere Prefab and set its Follower Stuck Mode to Random Walk:

![Environment](/Images/f13.png) 

Move the Sphere as a child object to the Leader and reset it’s coordinates then move it up a little:

![Environment](/Images/f6.png) 
 
Disable the Formation Follower script on the Sphere attached to the Leader.

In the Formation make the following changes:
* Add the Sphere prefab to the Follower Prefab property under the Demo section.

![Environment](/Images/f9.png) 

* Set Formation type to Circle:

![Environment](/Images/f10.png) 
 
* In the Layers section make the following changes:

![Environment](/Images/f16.png) 
 
 * Terrain Layer name = “Terrain”
 * Layer Mask Terrain = Terrain
 * Layer Mask terrain and Obstacles = Terrain + Obstacles

In the Formation Agent script at the Cylinder from the hierarch into the Destination:

![Environment](/Images/f11.png) 
 
**Now run the scene!**

# Additional Instructions

## AStarPathfinding

After importing AStarPathfinding into your project, change the Formation to support AStartPathfinding by opening the Formation Setup (menu Window | Goodgulf | Formation Setup).
Select the option "Set Navigation to AStarPathfinding" and wait for the compilation of the code to finish. Use your regular AStarPathfinding workflow to add navigation to your terrain. Don't forget to add any obstacle to the Obstacles Layer. You can use the Formation Setup menu to create a new Formation and Leader using AStarPathfinding or manually add the components. This is what the Formation object can look like:

![Environment](/Images/f17.png)
![Environment](/Images/f18.png)

The Seeker and AIDestinationSetter scripts are part of the AStarPathfinding solution. AIPathWithEvents inherits from AStarPathfinding's AIPath script; it adds an event to indicate the formation reached its destination. 

## Movement Animation

To use movement animation make sure you first import the Unity package mentioned in the pre-requisites section. It contains the animations and locomotion controller used by the FormationAnimation script you can add to the prefabs used by the formation leader and followers:

![Environment](/Images/f19.png)

![Environment](/Images/f21.png)

A typical setup of the script for the prefabs looks like this:

![Environment](/Images/f20.png)

Basically the FormationAnimation script calculates the speed of the object and sets the Animation Controller's parameters (velx, vely, move) based on the speed of the object. An animation blend tree mixes the animations (contained in teh Unity download) for the object. This works best for a humanoid character. The Audio Source contains the sound clip which will be played when the object is moving (in this case marching boots).

## Documentation

All scripts contain documentation which I think are sufficient to explain how this version of the Formation Movement works.

## Pull Requests

As per below statement on support, I'll look at pull requests and see if changes/fixes you propose should be merged. Please note that I'm not a full time Unity developer so I'll need to do this outside working hours and in the weekend. Any request may be delayed.

## Videos
[![Work in Progress 1](https://img.youtube.com/vi/iO9MAHb0w2w/maxresdefault.jpg)](https://youtu.be/iO9MAHb0w2w)
[![Work in Progress 2](https://img.youtube.com/vi/z-Wwcj_KhCc/maxresdefault.jpg)](https://youtu.be/z-Wwcj_KhCc)
[![Work in Progress 3](https://img.youtube.com/vi/d4LbZcDJTsA/maxresdefault.jpg)](https://youtu.be/d4LbZcDJTsA)
[![Work in Progress 4](https://img.youtube.com/vi/Ob64b8ItL4o/maxresdefault.jpg)](https://youtu.be/Ob64b8ItL4o)

# Support

The scripts are free to use (see the license), feel free to ask questions or make pull requests but don’t expect on the spot support. The best place to ask questions is to leave a comment in my [YouTube Channel](https://www.youtube.com/channel/UCWvtBWJSKiZuv1dTvEPx7OA). I'd appreciate it if you subscribe to the channel and like the Formation Movement videos if you plan to use this code.



