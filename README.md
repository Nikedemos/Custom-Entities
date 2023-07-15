## Introduction

Custom Entities is a framework meant as a hard dependency for other plugins to register their own Types of spawnable entities under arbitrary server-sided prefab names (e.g. `"/assets/custom/changeme.prefab"`). Developers can extend BaseEntity and BaseCombatEntity classes (using provided wrapper Types) with their own logic that’s supposed to provide more or less functionality than desired. Once spawned, those custom entities will network an existing vanilla prefab name to the clients - and that networked prefab name can even be switched to a different one dynamically during run-time.

Custom prefab names are registered with the GameManager, GameManifest and StringPool. This means that any admin, plugin (like CopyPaste) or even the server command "spawn" will treat it just like vanilla entity prefabs would be treated, and that prefab will show up on the list of spawnable entity prefabs. As long as the plugin is still loaded in!

This framework does all the heavy lifting associated with

- registering prefab names
- unregistering prefab names
- saving custom entities to plugin-specific, separate savefiles
- loading and spawning custom entities from plugin-specific datafiles
- addressing some nuances and edge cases for common case scenarios
- hiding the unorthodox hackery involved to make it work behind a comfortable curtain of abstraction so that the developers get to focus on the fun bits

NOTE: This plugin utilises Reflection in 2 or 3 places (CTRL+F for the comments "//naughty reflection") in order to force the BaseEntity._prefabName field to its custom, registered prefab name, instead of the prefab name of the original entity it was based on. This field is private and changing its accessor to public leads to less than stellar results, according to WhiteThunder's experiments with patching Oxide. In my opinion there is no point in turning the entire plugin into a compiled extension for the sole purpose of addressing the BaseEntity._prefabName field without breaking the "unbreakable" rule of not allowing Reflection.

## Localization

## Permissions

- `customentities.admin` - Required to execute the `spawn_at`, `purge_prefab` and `purge_plugin` commands.

## Admin commands

- `spawn_at <short prefab name>` - spawn a new instance of a given entity by its short prefab name at the position of the executing admin’s head. Useful for spawning a vanilla or a custom entity when not looking at any solid colliders. You don’t need to provide the complete short prefab name - just the initial portion will do, as long as it’s unique. It’s only a wrapper for the vanilla `spawn` command, after all.
- `purge_prefab <short prefab name>` - this command can also be executed from the server console/RCON or by a plugin. It will find and kill all entities currently spawned in with a matching short prefab name. Same name matching rules apply as above. **CAUTION: this will kill given vanilla and custom entities alike and cannot be reverted. Only really meant for emergency debugging situations. There’s no safeguarding for this command apart from the required `customentities.admin` permission. You have been warned.**

- `purge_plugin <plugin name>` -  similar to the command above and also requires the `customentities.admin` permission. The difference is that instead of filtering by prefab name, it kills all the entities registered by a certain dependent plugin. In case of the example plugin (see below), it will clear all the waypoints (and brushes).

## Data

Custom Entities have a "take only memories, leave only footprints" approach to handling the game state. When a dependent plugin registers a custom prefab bundle, all the entity data from that bundle will be stored in its own, separate savefile under `/oxide/data/CustomEntities/<PluginName>.save`. This includes the custom entity instances as well as the purely vanilla entities somehow connected with the custom ones. We’re mainly talking about all the vanilla entities related to Items that could be stored/handled by the bundle savefile: held entities, sub entities, instance data subentities, all checked recursively.

When a dependent plugin needs to spawn a vanilla entity - for example, to have it parented or referenced later by Net.Id to a custom one, a built-in API method allows the developers to permanently place those entities in the same custom savefile as the one belonging to the plugin.

This custom savefile is populated (if there’s anything to save to it, that is) every time a normal server save happens - so usually during set intervals and on server shutdown. It’s also saved when the relevant dependent plugin is unloaded. During plugin unload, all the instances of custom entities (and any vanilla entities associated with them) are removed from the world, as if they never existed. Then the savefile can be safely removed/copied/replaced without affecting anything else or wiping the vanilla save.

The custom savefiles are serialised in a format similar to vanilla ones, utilising memory streams and native ProtoBufs. This allows a CustomBaseEntity or a CustomBaseCombatEntity implementation to override Save and Load methods to take advantage of the vast collection of native serialisable ProtoBufs. In addition to this, custom entities inside of a savefile can have any extra data about them serialised and deserialised using BinaryWriters and BinaryReaders.

The Net.ID of the last known Community Instance is also stored in the datafile. On plugin load, it will try and detect if the one currently present on the server matches it. If it doesn’t, which is usually caused by wiping a savefile manually or restarting the server with a different map, it won’t load any stale entities and instead generate a new savefile under the same name.

Just like in vanilla, whenever an existing savefile is about to be overwritten, its contents are copied and shifted to iterative backups. The number of those backups stored is the same as the current vanilla one - by default it’s 2, but it can be controlled with the `server.saveBackupCount` convar.

## HOW TO: Example dependent plugin

The best way to convey how to utilise Custom Entities as a dependency for your plugins is by [having a look at the provided example](https://github.com/Nikedemos/Custom-Entities/blob/main/CustomEntitiesExample.cs), which can be viewed/downloaded from [Custom Entities on GitHub](https://github.com/Nikedemos/Custom-Entities).

This example features Waypoints, a collection of nodes in 3D space, grouped into graphs that can dynamically join and split.When a player puts a Map Item in their belt and switches to it, it activates a brush tool that allows the player to create/erase Waypoints in front of their eyes.

Left-click to place a new waypoint where you’re looking, right-click to erase the one you’re currently looking at (if there is one). New Waypoints can only be placed if they don’t collide with other Waypoints or the terrain.

When a waypoint is placed, it will automatically connect to other waypoints in the radius of 30 metres - provided there’s a direct, unobstructed line of sight on a pretty arbitrarily chosen layer mask, using a spherecast which is 0.5 m thick. 

The graphs currently serve no purpose, but in the future I could see it as a way to visually edit traversable 3D navigation meshes. Both the Waypoint and the Waypoint Brush are custom entities.

The licence of the example is the same as the licence of Custom Entities - that is, MIT.

