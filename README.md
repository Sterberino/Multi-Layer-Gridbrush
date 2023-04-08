# Multi-Layer-Gridbrush
A Unity game engine editor tool for drawing map area prefabs to multiple tilemaps simultaneously.

<h1>Overview</h1>

The multilayer brush is a custom tilemap brush that inherits from Gridbrush. It allows the user to select from a number of prefabs and draw those prefabs' tilemaps to your scene tilemaps. To test it, first create a new multilayer brush under Assets->Create->Multi Layer Brush. 

<br/>

<img src="https://github.com/Sterberino/Multi-Layer-Gridbrush/blob/main/Examples/Create%20New%20Brush.gif" width="512" height="256" />

<h1>Creating Your Prefabs</h1>

Then create a prefab you would like to draw to the scene. The prefab should have the same number of tilemaps as your scene. You can also add any other game objects you would like to paint into your scene, such as other prefabs or a gameobject with a sprite renderer. Those objects will be placed into the scene in the correct position relative to your brush location. You will also be able to see them as part of the paint preview in the scene before painting.

Once you have your prefabs to serve as placeable map areas, add them to your brush asset's Paintable Objects list. You can select the current object manually on the asset, or you can toggle through the list while painting using Alt+Right Arrow, Alt+ Left Arrow.

<br/>

<img src="https://github.com/Sterberino/Multi-Layer-Gridbrush/blob/main/Examples/paintable%20objects.gif" width="512" height="256" />

<h1>Randomizing Map Prefabs</h1>

There are also two scripts that can help you to increase the variety of your map areas. Map area tile randomizer will replace all instances of a group of tiles with all another set of tiles. The tiles in your default tiles list will be replaced with the tiles in your randomly selected tilereplacements list. It maps the tiles by index, so if you have "Lower Middle Wall A" and want to replace it with "Lower Middle Wall B," make sure they have the same index in their respective lists. If you want the default to be a selectable option, copy the default tiles list and add it to the possible replacements list.

The second randomizer script, MapAreaObjectsRandomizer.cs, replaces a default game object with some other gameobject. You can parent a game object to your prefab, and the game object you wish to replace it with to the same prefab. The default object, as well as all of the objects unselected by the randomizer will be deleted, and the selected one will not. Then, the remaining objects will be placed into the scene. 

If you wish to use the area randomizer scripts, add them to the Prefab root object. You can also create your own by having your script implement the IMapAreaRandomizer script. Make sure to enable "Randomize If Able" on your brush asset.

<br/>

<img src="https://github.com/Sterberino/Multi-Layer-Gridbrush/blob/main/Examples/Example%20Object.gif" width="512" height="256" />
