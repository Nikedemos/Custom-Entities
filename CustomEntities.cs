using Facepunch;
using Facepunch.Extend;
using Network;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Entities", "Nikedemos", "1.0.15")]
    [Description("A robust framework for registering, spawning, loading and saving entity prefabs")]

    public class CustomEntities : RustPlugin
    {
        #region CONST
        public const string FORMAT_FILENAME = "{0}.{1}";

        public const string PREFAB_PREFIX = "assets/custom/";

        public const string PREFAB_SPHERE = "assets/prefabs/visualization/sphere.prefab";

        public const string PREFAB_WOOD_STORAGE_BOX = "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab";

        public const string PREFAB_FX_IMPACT_METAL_BLUNT = "assets/bundled/prefabs/fx/impacts/blunt/metal/metal1.prefab";
        public const string PREFAB_FX_IMPACT_METAL_SLASH = "assets/bundled/prefabs/fx/impacts/slash/metal/metal1.prefab";
        public const string PREFAB_FX_IMPACT_METAL_STAB = "assets/bundled/prefabs/fx/impacts/stab/metal/metal1.prefab";
        public const string PREFAB_FX_IMPACT_METAL_BULLET = "assets/bundled/prefabs/fx/impacts/bullet/metal/metal1.prefab";

        public const string PREFAB_PLAYER = "assets/prefabs/player/player.prefab";

        public const string PREFAB_FX_IMPACT_METAL_PHYSICAL = "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-hollow-hard.prefab";
        public const string PREFAB_FX_EXPLOSION = "assets/bundled/prefabs/fx/impacts/additive/explosion.prefab";
        public const string PREFAB_FX_FIRE = "assets/bundled/prefabs/fx/impacts/additive/fire.prefab";

        public const string CMD_SPAWN_AT = "spawn_at";
        public const string CMD_PURGE_PREFAB = "purge_prefab";
        public const string CMD_PURGE_PLUGIN = "purge_plugin";
        public const string CMD_COUNT_PLUGIN = "count_plugin";

        public const string PREFIX_CUSTOMENTITIES = "customentities";
        public const string PERM_ADMIN = PREFIX_CUSTOMENTITIES + ".admin"; //required for the commands above

        public const string CONVAR_VERBOSE_LOGGING = "verbose_logging";

        #endregion

        #region STATIC
        public static CustomEntities Instance;
        public static bool Unloading = false;
        public static Effect ReusableEffect;

        public static readonly ReadOnlyDictionary<DamageType, float> DEFAULT_PROTECTION_AMOUNTS = new ReadOnlyDictionary<DamageType, float>(new Dictionary<DamageType, float>
        {
            [DamageType.Generic] = 0F, //Generic
            [DamageType.Hunger] = 0F, //Hunger
            [DamageType.Thirst] = 0F, //Thirst
            [DamageType.Cold] = 0F, //Cold
            [DamageType.Drowned] = 0F, //Drowned
            [DamageType.Heat] = 0F, //Heat
            [DamageType.Bleeding] = 0F, //Bleeding
            [DamageType.Poison] = 0F, //Poison
            [DamageType.Suicide] = 0F, //Suicide
            [DamageType.Bullet] = 0F, //Bullet
            [DamageType.Slash] = 0F, //Slash
            [DamageType.Blunt] = 0F, //Blunt
            [DamageType.Fall] = 0F, //Fall
            [DamageType.Radiation] = 0F, //Radiation
            [DamageType.Bite] = 0F, //Bite
            [DamageType.Stab] = 0F, //Stab
            [DamageType.Explosion] = 0F, //Explosion
            [DamageType.RadiationExposure] = 0F, //RadiationExposure
            [DamageType.ColdExposure] = 0F, //ColdExposure
            [DamageType.Decay] = 0F, //Decay
            [DamageType.ElectricShock] = 0F, //ElectricShock
            [DamageType.Arrow] = 0F, //Arrow
            [DamageType.AntiVehicle] = 0F, //AntiVehicle
            [DamageType.Collision] = 0F, //Collision
            [DamageType.Fun_Water] = 0F //Fun Water
        });

        #endregion

        #region LANG

        public const string MSG_PREFAB_REGISTERING_CUSTOM = nameof(MSG_PREFAB_REGISTERING_CUSTOM);
        public const string MSG_PREFAB_REGISTERING_MODIFIED = nameof(MSG_PREFAB_REGISTERING_MODIFIED);
        public const string MSG_PREFAB_REGISTRATION_EXCEPTION = nameof(MSG_PREFAB_REGISTRATION_EXCEPTION);
        public const string MSG_BUNDLE_UNREGISTRATION_REMOVED_VANILLA = nameof(MSG_BUNDLE_UNREGISTRATION_REMOVED_VANILLA);
        public const string MSG_BUNDLE_UNREGISTRATION_REMOVED_CUSTOM = nameof(MSG_BUNDLE_UNREGISTRATION_REMOVED_CUSTOM);
        public const string MSG_SAVING_SAVEFILES = nameof(MSG_SAVING_SAVEFILES);
        public const string MSG_SAVED_ENTITIES_1 = nameof(MSG_SAVED_ENTITIES_1);
        public const string MSG_SAVING_DATAFILE_EXCEPTION = nameof(MSG_SAVING_DATAFILE_EXCEPTION);
        public const string MSG_LOADING_NO_SAVEFILE = nameof(MSG_LOADING_NO_SAVEFILE);
        public const string MSG_LOADING_YES_DATAFILE = nameof(MSG_LOADING_YES_DATAFILE);
        public const string MSG_LOADING_FRESH_WIPE_DETECTED = nameof(MSG_LOADING_FRESH_WIPE_DETECTED);
        public const string MSG_LOADING_EMPTY_SAVEFILE = nameof(MSG_LOADING_EMPTY_SAVEFILE);
        public const string MSG_LOADING_ENTITIES_FROM_SAVEFILE = nameof(MSG_LOADING_ENTITIES_FROM_SAVEFILE);
        public const string MSG_LOADING_ERROR_ENTITY_ALREADY_EXISTS = nameof(MSG_LOADING_ERROR_ENTITY_ALREADY_EXISTS);
        public const string MSG_LOADING_ERROR_ENTITY_COULDNT_SPAWN = nameof(MSG_LOADING_ERROR_ENTITY_COULDNT_SPAWN);
        public const string MSG_LOADING_SPAWNING_ENTITIES = nameof(MSG_LOADING_SPAWNING_ENTITIES);
        public const string MSG_LOADED_ENTITIES = nameof(MSG_LOADED_ENTITIES);
        public const string MSG_LOADING_DATAFILE_LOAD_EXCEPTION = nameof(MSG_LOADING_DATAFILE_LOAD_EXCEPTION);
        public const string MSG_LOADING_NEW_SAVEFILE = nameof(MSG_LOADING_NEW_SAVEFILE);
        public const string MSG_LOADING_DATAFILE_NEW_EXCEPTION = nameof(MSG_LOADING_DATAFILE_NEW_EXCEPTION);

        public const string MSG_SAVING_BACKUP_EXCEPTION = nameof(MSG_SAVING_BACKUP_EXCEPTION);

        public const string MSG_CMD_PROVIDE_PREFAB = nameof(MSG_CMD_PROVIDE_PREFAB);
        public const string MSG_CMD_PROVIDE_PLUGIN_NAME = nameof(MSG_CMD_PROVIDE_PLUGIN_NAME);

        public const string MSG_CMD_PURGE_PREFAB_KILLED_ENTITIES = nameof(MSG_CMD_PURGE_PREFAB_KILLED_ENTITIES);
        public const string MSG_CMD_PURGE_PREFAB_NO_ENTITIES_FOUND = nameof(MSG_CMD_PURGE_PREFAB_NO_ENTITIES_FOUND);

        public const string MSG_CMD_PURGE_PLUGIN_NO_PLUGINS_REGISTERED = nameof(MSG_CMD_PURGE_PLUGIN_NO_PLUGINS_REGISTERED);
        public const string MSG_CMD_PURGE_PLUGIN_MAKE_SURE_LOADED = nameof(MSG_CMD_PURGE_PLUGIN_MAKE_SURE_LOADED);
        public const string MSG_CMD_PURGE_PLUGIN_NOTHING_FOUND = nameof(MSG_CMD_PURGE_PLUGIN_NOTHING_FOUND);
        public const string MSG_CMD_PURGE_PLUGIN_NO_ENTITIES_FOUND = nameof(MSG_CMD_PURGE_PLUGIN_NO_ENTITIES_FOUND);
        public const string MSG_CMD_PURGE_PLUGIN_KILLED_ENTITIES = nameof(MSG_CMD_PURGE_PLUGIN_KILLED_ENTITIES);

        public const string MSG_TRY_COMPO_REPLACE_ERROR = nameof(MSG_TRY_COMPO_REPLACE_ERROR);
        public const string MSG_TRY_COMPO_REPLACE_ERROR_GAMEOBJECT_NULL = nameof(MSG_TRY_COMPO_REPLACE_ERROR_GAMEOBJECT_NULL);
        public const string MSG_TRY_COMPO_REPLACE_ERROR_NO_OLD_COMPO_ATTACHED = nameof(MSG_TRY_COMPO_REPLACE_ERROR_NO_OLD_COMPO_ATTACHED);
        public const string MSG_TRY_COMPO_REPLACE_ERROR_ALREADY_HAS_NEW_COMPO = nameof(MSG_TRY_COMPO_REPLACE_ERROR_ALREADY_HAS_NEW_COMPO);
        public const string MSG_TRY_COMPO_REPLACE_DEBUG_HEADER = nameof(MSG_TRY_COMPO_REPLACE_DEBUG_HEADER);
        public const string MSG_TRY_COMPO_REPLACE_DEBUG_FIELD_INFO = nameof(MSG_TRY_COMPO_REPLACE_DEBUG_FIELD_INFO);

        private static readonly Dictionary<string, string> LangMessages = new Dictionary<string, string>
        {
            [MSG_PREFAB_REGISTERING_CUSTOM] = "Registering custom entity {0} : {1} as prefab \"{2}\"...",
            [MSG_PREFAB_REGISTERING_MODIFIED] = "Registering modified entity based on prefab \"{0}\" as prefab \"{1}\"...",
            [MSG_PREFAB_REGISTRATION_EXCEPTION] = "Exception while trying to register prefab: {0}\n{1}",
            [MSG_BUNDLE_UNREGISTRATION_REMOVED_VANILLA] = "Removed {0} instances of various vanilla entities handled by the savefile from world.",
            [MSG_BUNDLE_UNREGISTRATION_REMOVED_CUSTOM] = "Removed {0} instances of a custom prefab \"{1}\" from world.",
            [MSG_SAVING_SAVEFILES] = "Saving {0} binary savefiles...",
            [MSG_SAVED_ENTITIES_1] = "INFO: Saved {0} out of {1} entities ({2} custom, {3} vanilla. {4} removed because invalid) to \"{5}\"",
            [MSG_SAVING_DATAFILE_EXCEPTION] = "ERROR: {0} while trying to save \"{1}\": {2}\n{3}",
            [MSG_LOADING_NO_SAVEFILE] = "INFO: Savefile \"{0}\" doesn't exist, nothing to load.",
            [MSG_LOADING_YES_DATAFILE] = "Loading savefile from \"{0}\"...",
            [MSG_LOADING_FRESH_WIPE_DETECTED] = "INFO: The ID of the current Community Entity ({0}) is different than the one from the savefile ({1}) - this usually means a fresh wipe or switching between maps mid-wipes. A new savefile will be generated.",
            [MSG_LOADING_EMPTY_SAVEFILE] = "INFO: The savefile \"{0}\" contains no entities, nothing to load.",
            [MSG_LOADING_ENTITIES_FROM_SAVEFILE] = "Loading {0} entities from the savefile \"{1}\"...",
            [MSG_LOADING_ERROR_ENTITY_ALREADY_EXISTS] = "ERROR: The savefile \"{0}\" contains an entity with prefab ID {1} ({2}), Net.ID {3}, but another entity with that Net.ID already exists. Skipping to the next entity...",
            [MSG_LOADING_ERROR_ENTITY_COULDNT_SPAWN] = "ERROR: The savefile \"{0}\" contains an entity with prefab ID {1} ({2}), Net.ID {3} that could not be spawned. Skipping to the next entity...",
            [MSG_LOADING_SPAWNING_ENTITIES] = "Spawning {0} entities from the save file at {1}...",
            [MSG_LOADED_ENTITIES] = "INFO: Loaded {0} out of {1} entities ({2} custom, {3} vanilla) from \"{4}\"",
            [MSG_LOADING_DATAFILE_LOAD_EXCEPTION] = "ERROR: {0} while trying to load \"{1}\": {2}\n{3}",
            [MSG_LOADING_NEW_SAVEFILE] = "INFO: Generating default (blank) savefile at \"{0}\"...",
            [MSG_LOADING_DATAFILE_NEW_EXCEPTION] = "ERROR: {0} while trying to create a new datafile \"{1}\": {2}\n{3}",

            [MSG_CMD_PROVIDE_PREFAB] = "Please provide the short prefab name, or the initial part of it.",
            [MSG_CMD_PROVIDE_PLUGIN_NAME] = "Please provide the plugin name, or the initial part of it.",
            [MSG_CMD_PURGE_PREFAB_KILLED_ENTITIES] = "Purged {0} entities matching short prefab name \"{1}\"",
            [MSG_CMD_PURGE_PREFAB_NO_ENTITIES_FOUND] = "Could not find any entities matching short prefab name \"{0}\"",
            [MSG_SAVING_BACKUP_EXCEPTION] = "ERROR: {0} while trying to save backups of the datafile \"{1}\": {2}\n{3}",
            [MSG_CMD_PURGE_PLUGIN_NO_PLUGINS_REGISTERED] = "There's currently no Custom Entities plugins registered.",
            [MSG_CMD_PURGE_PLUGIN_MAKE_SURE_LOADED] = "Make sure it's loaded in. If it isn't, just delete the appropriate savefile from /oxide/data/CustomEntities/ and it will have the same effect.",
            [MSG_CMD_PURGE_PLUGIN_NOTHING_FOUND] = "Could not find any Plugins matching the provided name \"{0}\"",
            [MSG_CMD_PURGE_PLUGIN_NO_ENTITIES_FOUND] = "Could not find any entities from the Plugin \"{0}\"",
            [MSG_CMD_PURGE_PLUGIN_KILLED_ENTITIES] = "Purged {0} entities from the Plugin \"{1}\"",

            [MSG_TRY_COMPO_REPLACE_ERROR] = "ERROR WHILE TRYING TO REPLACE `{0}` with `{1}`:",
            [MSG_TRY_COMPO_REPLACE_ERROR_GAMEOBJECT_NULL] = "The GameObject is null!",
            [MSG_TRY_COMPO_REPLACE_ERROR_NO_OLD_COMPO_ATTACHED] = "There's no `{0}` component attached to the GameObject!",
            [MSG_TRY_COMPO_REPLACE_ERROR_ALREADY_HAS_NEW_COMPO] = "The GameObject already has a `{0}` component attached (and there's only one allowed!)",
            [MSG_TRY_COMPO_REPLACE_DEBUG_HEADER] = "`{0}` is a subclass of `{1}`, replacing matching fields:\n",
            [MSG_TRY_COMPO_REPLACE_DEBUG_FIELD_INFO] = "    set `{0}` to `{1}`",
        };

        private static string MSG(string msg, string userID = null, params object[] args)
        {
            if (args == null)
            {
                return Instance.lang.GetMessage(msg, Instance, userID);
            }
            else
            {
                return string.Format(Instance.lang.GetMessage(msg, Instance, userID), args);
            }

        }
        #endregion

        #region COVALENCE COMMANDS

        private void CommandCountPlugin(IPlayer iplayer, string command, string[] args)
        {
            if (args.Length == 0)
            {
                iplayer.Reply(MSG(MSG_CMD_PROVIDE_PLUGIN_NAME, iplayer.Id));
                return;
            }

            string pluginName = args[0].ToLower();

            BinaryData.PlayerRequestedPluginCount(iplayer, pluginName);
        }

        private void CommandPurgePlugin(IPlayer iplayer, string command, string[] args)
        {
            if (args.Length == 0)
            {
                iplayer.Reply(MSG(MSG_CMD_PROVIDE_PLUGIN_NAME, iplayer.Id));
                return;
            }

            string pluginName = args[0].ToLower();

            BinaryData.PlayerRequestedPluginPurge(iplayer, pluginName);
        }

        private void CommandPurgePrefab(IPlayer iplayer, string command, string[] args)
        {
            if (args.Length == 0)
            {
                iplayer.Reply(MSG(MSG_CMD_PROVIDE_PREFAB, iplayer.Id));
                return;
            }

            string prefabName = args[0].ToLower();

            BaseEntity[] iterateOver = BaseNetworkable.serverEntities.OfType<BaseEntity>().ToArray();

            int countKilled = 0;

            for (int i = 0; i < iterateOver.Length; i++)
            {
                BaseEntity entity = iterateOver[i];

                if (entity == null)
                {
                    continue;
                }

                if (entity.IsDestroyed)
                {
                    continue;
                }

                if (entity.net == null)
                {
                    continue;
                }

                if (!entity.ShortPrefabName.StartsWith(prefabName))
                {
                    continue;
                }

                entity.Kill(BaseNetworkable.DestroyMode.None);

                countKilled++;
            }

            if (countKilled == 0)
            {
                iplayer.Reply(MSG(MSG_CMD_PURGE_PREFAB_NO_ENTITIES_FOUND, iplayer.Id, prefabName));
                return;
            }

            iplayer.Reply(MSG(MSG_CMD_PURGE_PREFAB_KILLED_ENTITIES, iplayer.Id, countKilled, prefabName));
        }

        private void CommandSpawnAtPlayerEyes(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            if (player == null)
            {
                return;
            }

            if (args.Length == 0)
            {
                iplayer.Reply(MSG(MSG_CMD_PROVIDE_PREFAB, iplayer.Id));
                return;
            }

            string prefabName = args[0].ToLower();

            string commandResponse = ConVar.Entity.svspawn(prefabName, player.eyes.position, Vector3.zero);
            iplayer.Reply(commandResponse);
        }
        #endregion

        #region HOOK SUBSCRIPTIONS

        void Init()
        {
            lang.RegisterMessages(LangMessages, this);
        }

        void OnServerInitialized()
        {
            Instance = this;

            ReusableEffect = new Effect();

            permission.RegisterPermission(PERM_ADMIN, this);

            AddCovalenceCommand(CMD_PURGE_PREFAB, nameof(CommandPurgePrefab), PERM_ADMIN);
            AddCovalenceCommand(CMD_PURGE_PLUGIN, nameof(CommandPurgePlugin), PERM_ADMIN);
            AddCovalenceCommand(CMD_SPAWN_AT, nameof(CommandSpawnAtPlayerEyes), PERM_ADMIN);
            AddCovalenceCommand(CMD_COUNT_PLUGIN, nameof(CommandCountPlugin), PERM_ADMIN);

            CustomConvars.Init();

            BinaryData.Init();

            CastingNonAlloc.Init();

            CustomPrefabs.Init();
        }

        void Unload()
        {
            if (Instance == null)
            {
                return;
            }

            Unloading = true;

            //after this point, no saving and unregistering possible
            CustomPrefabs.Unload();
            BinaryData.Unload(); //doesn't do much either.
            CastingNonAlloc.Unload();

            CustomConvars.Unload();

            //unload top-level static
            Unloading = false;
            Instance = null;
            ReusableEffect = null;
        }

        void OnServerSave()
        {
            if (Instance == null)
            {
                return;
            }

            BinaryData.SaveAll();
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (Instance == null)
            {
                return;
            }

            List<BaseEntity> maybeCustomList = GetOwnerThingCustomSaveList(container);

            if (maybeCustomList == null)
            {
                return;
            }

            CustomPrefabs.EnsureMovedToCustomSaveListRecursively(item, maybeCustomList);

        }

        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (Instance == null)
            {
                return;
            }

            List<BaseEntity> maybeCustomList = GetOwnerThingCustomSaveList(container);

            if (maybeCustomList == null)
            {
                return;
            }

            CustomPrefabs.EnsureMovedToVanillaSaveListRecursively(item);
        }

        void OnEntityKill(BaseEntity entity)
        {
            if (Instance == null)
            {
                return;
            }

            if (Unloading)
            {
                return;
            }

            //only vanilla
            List<BaseEntity> checkForVanillaEnityList = CustomPrefabs.TryGetEntityCustomSaveList(entity, CustomPrefabs.SaveListEntityType.Vanilla);

            if (checkForVanillaEnityList == null)
            {
                return;
            }

            CustomPrefabs.ForgetEntityFromSaveListAndCache(entity, checkForVanillaEnityList);
        }

        #endregion

        #region CUSTOM PREFAB MANAGER

        public static class CustomPrefabs
        {
            private static GameObjectRef _emptyImpactGameObjectRef = null;
            private static Dictionary<string, GameObject> _prefabsPreProcessedCustom = null;

            private static Dictionary<GameObject, BaseEntity> _preProcessedGoToEntityCache = null;

            private static ListHashSet<GenericPrefabRecipe> _cachedRecipes = null;

            private static Dictionary<Plugin, BinaryData> _pluginToBinaryData = null;

            private static List<string> _gameManifestEntityList = null;            //this is not vanilla, just for building the vanilla manifest array

            private static Dictionary<ulong, List<BaseEntity>> _vanillaEntityToCustomSaveList = null;

            public static Dictionary<string, uint> ModifiedPrefabFullNameToModifiedPrefabIDHandledByBundles = null;

            public static Dictionary<string, List<BaseEntity>> ModifiedPrefabFullNameToCustomSaveList = null;

            public static void Init()
            {
                _emptyImpactGameObjectRef = new GameObjectRef();
                _prefabsPreProcessedCustom = new Dictionary<string, GameObject>();
                _preProcessedGoToEntityCache = new Dictionary<GameObject, BaseEntity>();

                _gameManifestEntityList = new List<string>();
                _cachedRecipes = new ListHashSet<GenericPrefabRecipe>();
                _pluginToBinaryData = new Dictionary<Plugin, BinaryData>();

                _vanillaEntityToCustomSaveList = new Dictionary<ulong, List<BaseEntity>>();

                ModifiedPrefabFullNameToModifiedPrefabIDHandledByBundles = new Dictionary<string, uint>();
                ModifiedPrefabFullNameToCustomSaveList = new Dictionary<string, List<BaseEntity>>();
            }

            public static void Unload()
            {
                _emptyImpactGameObjectRef = null;
                _prefabsPreProcessedCustom = null;
                _preProcessedGoToEntityCache = null;

                _gameManifestEntityList = null;
                _cachedRecipes = null;
                _pluginToBinaryData = null;

                _vanillaEntityToCustomSaveList = null;

                ModifiedPrefabFullNameToModifiedPrefabIDHandledByBundles = null;
                ModifiedPrefabFullNameToCustomSaveList = null;
            }

            public static BaseEntity TryGetPreprocessedPrototypeFromVanilla(string prefabName) => TryGetPreprocessedPrototypeFrom(GameManager.server.preProcessed.prefabList, prefabName);
            public static BaseEntity TryGetPreprocessedPrototypeFromCustom(string prefabName) => TryGetPreprocessedPrototypeFrom(_prefabsPreProcessedCustom, prefabName);

            private static BaseEntity TryGetPreprocessedPrototypeFrom(Dictionary<string, GameObject> preProcessedDictionary, string prefabName)
            {
                GameObject goResult;

                if (!preProcessedDictionary.TryGetValue(prefabName, out goResult))
                {
                    return null;
                }

                //is it in the cache?

                BaseEntity baseEntityResult;

                if (!_preProcessedGoToEntityCache.TryGetValue(goResult, out baseEntityResult))
                {
                    //get the component... if non null, add to cache

                    baseEntityResult = goResult.GetComponent<BaseEntity>();

                    if (baseEntityResult != null)
                    {
                        _preProcessedGoToEntityCache.Add(goResult, baseEntityResult);
                    }
                }

                //now you get it or a null

                return baseEntityResult;
            }

            //public API

            public static void ForgetEntityFromSaveListAndCache(BaseEntity vanillaEntity, List<BaseEntity> maybeCustomList)
            {
                maybeCustomList.Remove(vanillaEntity);

                _vanillaEntityToCustomSaveList.Remove(vanillaEntity.net.ID.Value);
            }

            public enum SaveListEntityType
            {
                Vanilla,
                Custom,
                VanillaAndCustom
            }

            public static List<BaseEntity> TryGetEntityCustomSaveList(BaseEntity vanillaOrCustomEntity, SaveListEntityType entityType)
            {

                if (!BaseNetworkableEx.IsValid(vanillaOrCustomEntity))
                {
                    return null;
                }

                bool checkForVanilla = (entityType == SaveListEntityType.Vanilla) || (entityType == SaveListEntityType.VanillaAndCustom);
                bool checkForCustom = (entityType == SaveListEntityType.Custom) || (entityType == SaveListEntityType.VanillaAndCustom);

                if (checkForCustom)
                {
                    ICustomEntity asInterface = vanillaOrCustomEntity as ICustomEntity;

                    if (asInterface != null)
                    {
                        //no need to check for vanilla any more
                        return asInterface.SaveListInDataFile;
                    }
                }

                if (checkForVanilla)
                {
                    List<BaseEntity> maybeCustomList;

                    if (!_vanillaEntityToCustomSaveList.TryGetValue(vanillaOrCustomEntity.net.ID.Value, out maybeCustomList))
                    {
                        //neither custom nor vanilla.
                        return null;
                    }

                    return maybeCustomList;
                }

                return null;
            }

            public static void EnsureMovedToVanillaSaveList(BaseEntity vanillaEntity)
            {
                ICustomEntity asInterface = vanillaEntity as ICustomEntity;

                if (asInterface != null)
                {
                    return;
                }

                List<BaseEntity> maybeAlreadyExistingList = TryGetEntityCustomSaveList(vanillaEntity, SaveListEntityType.Vanilla);

                if (maybeAlreadyExistingList != null)
                {
                    //it exists in some other custom save list, so let's forget it first...
                    ForgetEntityFromSaveListAndCache(vanillaEntity, maybeAlreadyExistingList);
                }

                vanillaEntity.EnableSaving(true); //this will add it back

            }

            public static void EnsureMovedToVanillaSaveListRecursively(Item item)
            {
                ForEachItemEntityRecursively(item, (entity) => EnsureMovedToVanillaSaveList(entity));
            }

            public static void EnsureMovedToCustomSaveListRecursively(Item item, List<BaseEntity> targetList)
            {
                ForEachItemEntityRecursively(item, (entity) => EnsureMovedToCustomSaveList(entity, targetList));
            }

            private static void EnsureMovedToCustomSaveListRecursivelyEntireContainer(ItemContainer itemContainer, List<BaseEntity> saveList)
            {
                if (itemContainer == null)
                {
                    return;
                }

                var itemListCount = itemContainer.itemList?.Count ?? 0;

                if (itemListCount == 0)
                {
                    return;
                }

                for (var i = 0; i < itemContainer.itemList.Count; i++)
                {
                    var item = itemContainer.itemList[i];

                    if (item == null)
                    {
                        continue;
                    }

                    EnsureMovedToCustomSaveListRecursively(item, saveList);
                }

            }


            private static void ForEachEntityInTransformHierarchyRecursivelyAndAlsoConsideringItemsInInventories(BaseEntity entity, Action<BaseEntity> entityAction, List<BaseEntity> saveList)
            {
                entityAction(entity);

                //still!

                if (entity == null)
                {
                    return;
                }

                //so we have several "obvious" cases here
                //in fact, maybe we should re-visit MetaphysicsEnchantedItems to get everything that has a container, presumably?

                //first obvious case:
                var someSortOfContainer = entity as IItemContainerEntity;

                //this covers a broad range of things that have "ItemContainer inventory".

                if (someSortOfContainer != null)
                {
                    //this will do nothing if the inventory is null or empty
                    EnsureMovedToCustomSaveListRecursivelyEntireContainer(someSortOfContainer.inventory, saveList);
                }
                else
                {
                    var asDroppedItem = entity as WorldItem;

                    if (asDroppedItem != null)
                    {
                        //this will do nothing if the asDroppedItem.item is null.
                        EnsureMovedToCustomSaveListRecursively(asDroppedItem.item, saveList);
                    }
                    else
                    {
                        var asPlayer = entity as BasePlayer;

                        if (asPlayer != null)
                        {
                            var playerInventory = asPlayer.inventory;

                            if (playerInventory != null)
                            {
                                EnsureMovedToCustomSaveListRecursivelyEntireContainer(playerInventory.containerMain, saveList);
                                EnsureMovedToCustomSaveListRecursivelyEntireContainer(playerInventory.containerBelt, saveList);
                                EnsureMovedToCustomSaveListRecursivelyEntireContainer(playerInventory.containerWear, saveList);

                                //any backpacks? almost forgot!

                                var getBackpack = playerInventory.GetBackpackWithInventory();

                                if (getBackpack != null)
                                {
                                    EnsureMovedToCustomSaveListRecursivelyEntireContainer(getBackpack.contents, saveList);
                                }
                            }
                        }
                        else
                        {
                            var asDroppedItemContainer = entity as DroppedItemContainer;

                            if (asDroppedItemContainer != null)
                            {
                                EnsureMovedToCustomSaveListRecursivelyEntireContainer(asDroppedItemContainer.inventory, saveList);
                            }
                            else
                            {
                                //and last chance... so far, that I can see, as much as edge cases go.

                                var asLootableCorpse = entity as LootableCorpse;

                                if (asLootableCorpse != null)
                                {
                                    var inventories = asLootableCorpse.containers;

                                    if (inventories != null)
                                    {
                                        int invLength = inventories.Length;
                                        for (var i = 0; i < invLength; i++)
                                        {
                                            var inventory = inventories[i];

                                            EnsureMovedToCustomSaveListRecursivelyEntireContainer(inventory, saveList);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }


                if (entity.children == null)
                {
                    return;
                }

                var childrenCount = entity.children.Count;

                for (int i = 0; i < childrenCount; i++)
                {
                    ForEachEntityInTransformHierarchyRecursivelyAndAlsoConsideringItemsInInventories(entity.children[i], entityAction, saveList);
                }
            }

            private static void ForEachItemEntityRecursively(Item item, Action<BaseEntity> entityAction) //so the action will be either move to custom, or move to vanilla... and keep passing the action
            {
                if (item == null)
                {
                    return;
                }

                //sub entity...
                if (item.instanceData != null)
                {
                    if (item.instanceData.subEntity.IsValid)
                    {
                        BaseEntity subEnt = BaseNetworkable.serverEntities.Find(item.instanceData.subEntity) as BaseEntity;

                        if (subEnt != null)
                        {
                            entityAction(subEnt);
                        }
                    }
                }

                //world entity...
                BaseEntity worldEntity = item.GetWorldEntity();

                if (worldEntity != null)
                {
                    entityAction(worldEntity);
                }


                //held entity...
                BaseEntity heldEntity = item.GetHeldEntity();

                if (heldEntity != null)
                {
                    entityAction(heldEntity);
                }

                //and then, if your item container is not null, keep going.

                if (item.contents == null)
                {
                    return;
                }

                if (item.contents.itemList.IsNullOrEmpty())
                {
                    return;
                }

                int count = item.contents.itemList.Count;

                for (int i = 0; i < count; i++)
                {
                    Item containerItem = item.contents.itemList[i];

                    if (containerItem == null)
                    {
                        continue;
                    }

                    ForEachItemEntityRecursively(containerItem, entityAction);
                }

            }

            /*
             * 
            public static void EnsureMovedToCustomSaveListRecursively(Item item, List<BaseEntity> targetList)
            {
                ForEachItemEntityRecursively(item, (entity) => EnsureMovedToCustomSaveList(entity, targetList));
            }

             */

            public static void EnsureMovedToCustomSaveListRecursivelyTopDown(BaseEntity vanillaEntity, List<BaseEntity> targetList)
            {
                bool first = true;

                ForEachEntityInTransformHierarchyRecursivelyAndAlsoConsideringItemsInInventories(vanillaEntity, (entity) =>
                {
                    EnsureMovedToCustomSaveListMindingIfFirstOneIsForced(entity, targetList, ref first);
                }, targetList);
            }

            public static void EnsureMovedToCustomSaveListMindingIfFirstOneIsForced(BaseEntity vanillaEntity, List<BaseEntity> targetList, ref bool first)
            {
                //so. subentities, and stuff like that,
                //their "enableSaving" status WILL matter.

                //i.e. here, in this lambda thingy,
                //if entity.enableSaving is false, we should NOT do this.

                //HOWEVER. For the first one, i.e. the topmost one,
                //we already know that we WANT it there.
                //so it needs to be ignored for that one.

                //so keep passing a bool.

                //

                if (!first)
                {
                    if (vanillaEntity.enableSaving == false)
                    {
                        return;
                    }
                }

                first = false;
                EnsureMovedToCustomSaveList(vanillaEntity, targetList);
            }

            public static void EnsureMovedToCustomSaveList(BaseEntity vanillaEntity, List<BaseEntity> targetList)
            {
                if (vanillaEntity == null)
                {
                    return;
                }

                if (vanillaEntity.net == null)
                {
                    return;
                }

                if (vanillaEntity.IsDestroyed)
                {
                    return;
                }

                ICustomEntity asInterface = vanillaEntity as ICustomEntity;

                if (asInterface != null)
                {
                    //this is already a custom entity, rest assured it's handled by a custom save file already
                    return;
                }

                if (targetList.Contains(vanillaEntity))
                {
                    //already contains
                    return;
                }

                //already a part of a list?
                List<BaseEntity> maybeAlreadyExistingList = TryGetEntityCustomSaveList(vanillaEntity, SaveListEntityType.Vanilla);

                if (maybeAlreadyExistingList != null)
                {
                    if (maybeAlreadyExistingList == targetList)
                    {
                        return; //already where it needs to be
                    }

                    //it exists in some other custom save list, so let's forget it first...
                    ForgetEntityFromSaveListAndCache(vanillaEntity, maybeAlreadyExistingList);
                }

                vanillaEntity.EnableSaving(false); //this will remove from vanilla list if enabled - use after spawning

                targetList.Add(vanillaEntity);
                _vanillaEntityToCustomSaveList.Add(vanillaEntity.net.ID.Value, targetList);
            }

            public static List<BaseEntity> EnsureMovedToCustomSaveListSameAs(BaseEntity vanillaEntity, ICustomEntity sameAsCustomEntity)
            {
                if (sameAsCustomEntity.SaveListInDataFile == null)
                {
                    //null for some reason, so false just to be on the safe side
                    return null;
                }

                EnsureMovedToCustomSaveList(vanillaEntity, sameAsCustomEntity.SaveListInDataFile);

                return sameAsCustomEntity.SaveListInDataFile;

            }

            public static bool RegisterAndLoadBundle(CustomPrefabBundle bundle, string optionalSuffix = default(string))
            {
                //step 0: ensure the data...
                BinaryData binaryData = BinaryData.SummonBinaryData(bundle.Owner, optionalSuffix);

                binaryData.PrefabBundle = bundle;

                List<CustomPrefabRecipe> customRecipes = Pool.Get<List<CustomPrefabRecipe>>();
                List<ModifiedPrefabRecipe> modifiedRecipes = Pool.Get<List<ModifiedPrefabRecipe>>();

                //step 1: separate the grain from the hull

                for (int i = 0; i < binaryData.PrefabBundle.Recipes.Length; i++)
                {
                    GenericPrefabRecipe recipe = binaryData.PrefabBundle.Recipes[i];

                    if (recipe is CustomPrefabRecipe customRecipe)
                    {
                        customRecipes.Add(customRecipe);
                    }
                    else if (recipe is ModifiedPrefabRecipe modifiedRecipe)
                    {
                        modifiedRecipes.Add(modifiedRecipe);
                    }
                }

                bool allGood = true;
                //step 2: register custom prefabs from recipes...

                for (int i = 0; i < customRecipes.Count; i++)
                {
                    CustomPrefabRecipe recipe = customRecipes[i];

                    Instance.PrintWarning(MSG(MSG_PREFAB_REGISTERING_CUSTOM, null, recipe.EntityType.Name, (recipe.BaseCombat == null ? nameof(BaseEntity) : nameof(BaseCombatEntity)), recipe.FullPrefabName));

                    try
                    {
                        RegisterPrefabCustom(recipe, binaryData);
                    }
                    catch (Exception e)
                    {
                        Instance.PrintError(MSG(MSG_PREFAB_REGISTRATION_EXCEPTION, null, e.Message, e.StackTrace));
                        allGood = false;
                    }

                }

                //step 3: register modified recipes. We're doing that second since we might wanna modify a custom prefab too
                for (int i = 0; i < modifiedRecipes.Count; i++)
                {
                    ModifiedPrefabRecipe recipe = modifiedRecipes[i];

                    Instance.PrintWarning(MSG(MSG_PREFAB_REGISTERING_MODIFIED, null, recipe.OriginalFullPrefabName, recipe.FullPrefabName));

                    try
                    {
                        RegisterPrefabModified(recipe, binaryData);
                    }
                    catch (Exception e)
                    {
                        Instance.PrintError(MSG(MSG_PREFAB_REGISTRATION_EXCEPTION, null, e.Message, e.StackTrace));
                        allGood = false;
                    }
                }

                Pool.FreeUnmanaged(ref customRecipes);
                Pool.FreeUnmanaged(ref modifiedRecipes);


                //step 4: now that everything is registered, load.
                //now they should all know their appropriate save lists.

                binaryData.Load();

                return allGood;
            }

            public static bool SaveAndUnregisterBundle(CustomPrefabBundle cookbook, string optionalSuffix = default(string))
            {
                //and this should be the exact opposite.
                //first, save...

                //this will ensure the data wrapper, just in case it wasn't before
                BinaryData binaryData = BinaryData.SummonBinaryData(cookbook.Owner, optionalSuffix);

                binaryData.Save(); //this will save everything

                List<CustomPrefabRecipe> customRecipes = Pool.Get<List<CustomPrefabRecipe>>();
                List<ModifiedPrefabRecipe> modifiedRecipes = Pool.Get<List<ModifiedPrefabRecipe>>();


                for (int i = 0; i < binaryData.PrefabBundle.Recipes.Length; i++)
                {
                    GenericPrefabRecipe recipe = binaryData.PrefabBundle.Recipes[i];

                    if (recipe is CustomPrefabRecipe customRecipe)
                    {
                        customRecipes.Add(customRecipe);
                    }
                    else if (recipe is ModifiedPrefabRecipe modifiedRecipe)
                    {
                        modifiedRecipes.Add(modifiedRecipe);
                    }
                }

                bool allGood = true;

                for (int i = 0; i < modifiedRecipes.Count; i++)
                {
                    ModifiedPrefabRecipe recipe = modifiedRecipes[i];

                    if (!UnregisterPrefabInternal(recipe))
                    {
                        allGood = false;
                    }
                }

                for (int i = 0; i < customRecipes.Count; i++)
                {
                    CustomPrefabRecipe recipe = customRecipes[i];

                    //caveat: this removes from _modified... cache!

                    if (!UnregisterPrefabInternal(recipe))
                    {
                        allGood = false;
                    }

                }

                Pool.FreeUnmanaged(ref customRecipes);
                Pool.FreeUnmanaged(ref modifiedRecipes);

                //and now we need to kill off vanilla entities that might still linger about

                int countKilled = 0;

                for (int i = 0; i < binaryData.CustomEntitySaveList.Count; i++)
                {
                    BaseEntity entity = binaryData.CustomEntitySaveList[i];

                    if (entity == null)
                    {
                        continue;
                    }

                    if (entity.IsDestroyed)
                    {
                        continue;
                    }

                    entity.Kill(BaseNetworkable.DestroyMode.None);
                    countKilled++;
                }

                if (countKilled > 0)
                {
                    if (CustomConvars.VerboseLogging)
                    {
                        Instance.PrintWarning(MSG(MSG_BUNDLE_UNREGISTRATION_REMOVED_VANILLA, null, countKilled));
                    }
                }


                BinaryData.ForgetBinaryData(cookbook.Owner);

                return allGood;
            }

            //and the rest are private methods

            private static bool RegisterPrefabModified(ModifiedPrefabRecipe recipe, BinaryData data)
            {
                var tryFindPrototype = TryGetPreprocessedPrototypeFromVanilla(recipe.OriginalFullPrefabName);

                if (tryFindPrototype == null)
                {
                    tryFindPrototype = TryGetPreprocessedPrototypeFromCustom(recipe.OriginalFullPrefabName);
                }

                if (tryFindPrototype == null)
                {
                    return false;
                }

                var newGo = UnityEngine.Object.Instantiate(tryFindPrototype.gameObject, null, true);

                newGo.SetActive(false);

                var newEntity = newGo.GetComponent<BaseEntity>();

                //we know entity won't be null because TryGetPreprocessed looks for an entity in first place

                if (recipe.ModificationFunction != null)
                {
                    if (!recipe.ModificationFunction(newEntity))
                    {
                        return false;
                    }
                }

                AddToGameManifest(recipe.FullPrefabName, newGo, recipe.EnableInSpawnCommand);

                AddToPreprocessed(recipe.FullPrefabName, newGo);

                //var originalPrefabStringPoolID = newEntity.prefabID;
                var modifiedPrefabStringPoolID = AddToStringPool(recipe.FullPrefabName);


                //MUST DISABLE VANILLA SAVING BECAUSE NOW IT HAS A NON-EXISTING PREFAB ID!

                //in this case, it will help identify the modified prefab in the dictionary, given its full prefab name.

                if (recipe.SaveHandling == ModifiedSaveHandling.SaveInVanillaSaveList)
                {
                    newEntity.EnableSaving(true);
                }
                else
                {
                    newEntity.EnableSaving(false);

                    //naughty reflection
                    newEntity._prefabName = recipe.FullPrefabName;

                    ModifiedPrefabFullNameToModifiedPrefabIDHandledByBundles.Add(recipe.FullPrefabName, modifiedPrefabStringPoolID);


                    //add the compo

                    var newCompo = newGo.AddComponent<ModifiedBundleSaveBehaviour>();

                    newCompo.RecipeFullPrefabName = recipe.FullPrefabName;

                    if (recipe.SaveHandling == ModifiedSaveHandling.SaveInBundleSaveList)
                    {
                        ModifiedPrefabFullNameToCustomSaveList[recipe.FullPrefabName] = data.CustomEntitySaveList;
                        newCompo.AddToCustomSaveList = true;
                    }

                }


                UnityEngine.Object.DontDestroyOnLoad(newGo);

                if (!_cachedRecipes.Contains(recipe))
                {
                    _cachedRecipes.Add(recipe);
                }

                return true;
            }



            private static bool RegisterPrefabCustom(CustomPrefabRecipe recipe, BinaryData data)
            {
                GameObject newGo = new GameObject(recipe.ShortPrefabName)
                {
                    layer = (int)recipe.Layer
                };

                BaseEntity newEntity = newGo.AddComponent(recipe.EntityType) as BaseEntity;

                AddToGameManifest(recipe.FullPrefabName, newGo, recipe.EnableInSpawnCommand);

                AddToPreprocessed(recipe.FullPrefabName, newGo);

                if (recipe.BaseCombat != null)
                {
                    ApplyBaseCombatEntityProperties((BaseCombatEntity)newEntity, recipe.ShortPrefabName, recipe.BaseCombat);
                }

                newEntity.impactEffect = _emptyImpactGameObjectRef;

                newEntity.prefabID = AddToStringPool(recipe.FullPrefabName);

                //naughty reflection
                newEntity._prefabName = recipe.FullPrefabName;

                ICustomEntity asInterface = (newEntity as ICustomEntity);

                var tryFindPrototype = TryGetPreprocessedPrototypeFromVanilla(asInterface.DefaultClientsideFullPrefabName());

                if (tryFindPrototype != null)
                {
                    newEntity.bounds = tryFindPrototype.bounds;
                }

                asInterface.SaveListInDataFile = data.CustomEntitySaveList;

                //we know that at this point it's not null

                asInterface.OnCustomPrefabPrototypeEntityRegistered();

                newGo.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(newGo);

                if (!_cachedRecipes.Contains(recipe))
                {
                    _cachedRecipes.Add(recipe);
                }

                return true;
            }


            private static bool UnregisterPrefabInternal(GenericPrefabRecipe recipe)
            {
                GameObject go;

                string recipeFullPrefabName = recipe.FullPrefabName;

                if (!_prefabsPreProcessedCustom.TryGetValue(recipe.FullPrefabName, out go))
                {
                    return false;
                }

                CustomPrefabRecipe recipeAsCustom = recipe as CustomPrefabRecipe;

                RemoveFromPreProcessed(recipeFullPrefabName);
                RemoveFromGameManifest(recipeFullPrefabName, recipe.EnableInSpawnCommand);
                RemoveFromStringPool(recipeFullPrefabName);

                ModifiedPrefabRecipe recipeAsModified = recipe as ModifiedPrefabRecipe;

                if (recipeAsModified != null)
                {
                    if (recipeAsModified.SaveHandling == ModifiedSaveHandling.SaveInBundleSaveList)
                    {
                        if (ModifiedPrefabFullNameToModifiedPrefabIDHandledByBundles.ContainsKey(recipeFullPrefabName))
                        {
                            ModifiedPrefabFullNameToModifiedPrefabIDHandledByBundles.Remove(recipeFullPrefabName);
                        }

                        if (ModifiedPrefabFullNameToCustomSaveList.ContainsKey(recipeFullPrefabName))
                        {
                            ModifiedPrefabFullNameToCustomSaveList.Remove(recipeFullPrefabName);
                        }
                    }
                }

                //kill the prototype                

                BaseEntity prototypeEntity = go.GetComponent<BaseEntity>();

                if (recipeAsCustom != null)
                {
                    (prototypeEntity as ICustomEntity).OnCustomPrefabPrototypeEntityUnregistered();

                    if (recipeAsCustom.BaseCombat != null)
                    {
                        BaseCombatEntity asBaseCombat = prototypeEntity as BaseCombatEntity;

                        if (asBaseCombat != null)
                        {
                            if (asBaseCombat.baseProtection != null)
                            {
                                UnityEngine.Object.DestroyImmediate(asBaseCombat.baseProtection);
                                asBaseCombat.baseProtection = null;
                            }
                        }
                    }
                }

                prototypeEntity.Kill(BaseNetworkable.DestroyMode.None);

                var iterateOver = BaseNetworkable.serverEntities.OfType<BaseEntity>().ToArray();

                int countKilled = 0;

                for (int i = 0; i < iterateOver.Length; i++)
                {
                    BaseEntity entity = iterateOver[i];

                    if (entity == null)
                    {
                        continue;
                    }

                    if (entity.IsDestroyed)
                    {
                        continue;
                    }

                    if (entity.PrefabName != recipeFullPrefabName)
                    {
                        //not what we're looking for
                        continue;
                    }

                    //kill it, because we're about to unregister. it was already saved.

                    entity.Kill(BaseNetworkable.DestroyMode.None);
                    countKilled++;
                }

                if (_cachedRecipes.Contains(recipe))
                {
                    _cachedRecipes.Remove(recipe);
                }

                if (countKilled > 0)
                {
                    if (CustomConvars.VerboseLogging)
                    {
                        Instance.PrintWarning(MSG(MSG_BUNDLE_UNREGISTRATION_REMOVED_CUSTOM, null, countKilled, recipeFullPrefabName));
                    }
                }

                return true;
            }

            private static void ApplyBaseCombatEntityProperties<T>(T newBaseCombatEntity, string shortPrefabName, CustomPrefabBaseCombat baseCombatRecipeStuff) where T : BaseCombatEntity
            {
                newBaseCombatEntity._health = baseCombatRecipeStuff.HealthStart;
                newBaseCombatEntity.startHealth = baseCombatRecipeStuff.HealthStart;
                newBaseCombatEntity._maxHealth = baseCombatRecipeStuff.HealthMax;

                newBaseCombatEntity.pickup.enabled = baseCombatRecipeStuff.PickupEnabled;
                newBaseCombatEntity.repair.enabled = baseCombatRecipeStuff.RepairEnabled;
                newBaseCombatEntity.markAttackerHostile = baseCombatRecipeStuff.MarkAttackerHostile;

                newBaseCombatEntity.baseProtection = SummonProtectionProperties(shortPrefabName, baseCombatRecipeStuff.ProtectionProperties ?? DEFAULT_PROTECTION_AMOUNTS.Values.ToArray());
            }

            private static ProtectionProperties SummonProtectionProperties(string shortPrefabName, float[] amounts)
            {
                ProtectionProperties result = ScriptableObject.CreateInstance<ProtectionProperties>();
                result.comments = $"DamageProperties.{shortPrefabName}";
                result.amounts = amounts;

                return result;
            }

            private static uint AddToStringPool(string fullPrefabName)
            {
                uint value;

                if (!StringPool.toNumber.TryGetValue(fullPrefabName, out value))
                {
                    value = fullPrefabName.ManifestHash();
                    StringPool.toString.Add(value, fullPrefabName);
                    StringPool.toNumber.Add(fullPrefabName, value);
                }

                return value;
            }

            private static void RemoveFromStringPool(string fullPrefabName)
            {
                if (StringPool.toNumber.ContainsKey(fullPrefabName))
                {
                    StringPool.toNumber.Remove(fullPrefabName);
                }

                uint value = fullPrefabName.ManifestHash();

                if (StringPool.toString.ContainsKey(value))
                {
                    StringPool.toString.Remove(value);

                    return;
                }
            }

            private static void AddToGameManifest(string fullPrefabName, GameObject newGo, bool alsoAddToCurrentEntities)
            {
                GameManifest.pathToGuid[fullPrefabName] = fullPrefabName;
                GameManifest.guidToPath[fullPrefabName] = fullPrefabName;
                GameManifest.guidToObject[fullPrefabName] = newGo;

                if (!alsoAddToCurrentEntities)
                {
                    return;
                }

                _gameManifestEntityList.Clear();

                for (int i = 0; i < GameManifest.Current.entities.Length; i++)
                {
                    _gameManifestEntityList.Add(GameManifest.Current.entities[i]);
                }

                _gameManifestEntityList.Add(fullPrefabName);

                GameManifest.Current.entities = _gameManifestEntityList.ToArray();

                UpdateGameManifestArray();

            }

            private static void RemoveFromGameManifest(string fullPrefabName, bool alsoRemoveFromCurrentEntities)
            {
                GameManifest.pathToGuid.Remove(fullPrefabName);
                GameManifest.guidToPath.Remove(fullPrefabName);
                GameManifest.guidToObject.Remove(fullPrefabName);

                if (!alsoRemoveFromCurrentEntities)
                {
                    return;
                }

                _gameManifestEntityList.Clear();

                for (int i = 0; i < GameManifest.Current.entities.Length; i++)
                {
                    string entityPrefabName = GameManifest.Current.entities[i];

                    if (entityPrefabName == fullPrefabName)
                    {
                        continue; //skip that element
                    }

                    _gameManifestEntityList.Add(entityPrefabName);
                }

                UpdateGameManifestArray();

            }

            private static void UpdateGameManifestArray()
            {
                GameManifest.Current.entities = _gameManifestEntityList.ToArray();
            }

            private static void AddToPreprocessed(string fullPrefabName, GameObject newGo)
            {
                RemoveFromPreProcessed(fullPrefabName);

                GameManager.server.preProcessed.prefabList.Add(fullPrefabName, newGo);
                _prefabsPreProcessedCustom.Add(fullPrefabName, newGo);
            }

            private static void RemoveFromPreProcessed(string fullPrefabName)
            {
                if (GameManager.server.preProcessed.prefabList.ContainsKey(fullPrefabName))
                {
                    GameManager.server.preProcessed.prefabList.Remove(fullPrefabName);
                }

                if (_prefabsPreProcessedCustom.ContainsKey(fullPrefabName))
                {
                    _prefabsPreProcessedCustom.Remove(fullPrefabName);
                }


            }
        }

        #endregion

        #region BINARY DATA
        private class BinaryData
        {
            private static Dictionary<Plugin, BinaryData> _cacheByOwner = null;

            private static ListHashSet<string> _prefabNamesToKill = null;

            private static Dictionary<string, (int, bool)> _pluginPrefabCount = null;

            private readonly string _fullFileDirectory;

            private readonly Plugin _ownerPlugin;

            private readonly string _fullFilePath;

            public List<BaseEntity> CustomEntitySaveList;

            public CustomPrefabBundle PrefabBundle = null;

            public static void Init()
            {
                _cacheByOwner = new Dictionary<Plugin, BinaryData>();
                _prefabNamesToKill = new ListHashSet<string>();
                _pluginPrefabCount = new Dictionary<string, (int, bool)>();
            }

            public static void Unload()
            {
                _cacheByOwner = null;
                _prefabNamesToKill = null;
                _pluginPrefabCount = null;
            }

            public static void SaveAll()
            {
                if (_cacheByOwner.IsNullOrEmpty())
                {
                    return;
                }

                Instance.PrintWarning(MSG(MSG_SAVING_SAVEFILES, null, _cacheByOwner.Count));

                foreach (KeyValuePair<Plugin, BinaryData> binaryData in _cacheByOwner)
                {
                    binaryData.Value.Save(); //do not kill here.
                }
            }

            public static void PlayerRequestedPluginCount(IPlayer iplayer, string partialPluginNameLowercase)
            {
                BinaryData findMatchingEntry = null;

                for (int i = 0; i < _cacheByOwner.Count; i++)
                {
                    KeyValuePair<Plugin, BinaryData> cacheEntry = _cacheByOwner.ElementAt(i);

                    if (cacheEntry.Key.Name.ToLower().StartsWith(partialPluginNameLowercase))
                    {
                        findMatchingEntry = cacheEntry.Value;
                        break;
                    }
                }

                if (findMatchingEntry == null)
                {
                    iplayer.Reply($"NO PLUGINS FOUND LOLO");
                    return;
                }

                //why don't we do that in PlayerRequestedPluginPurge?
                if (findMatchingEntry.CustomEntitySaveList.IsNullOrEmpty())
                {
                    iplayer.Reply($"CUSTOM ENTITY SAVE LIST FOR THAT FILE IS NULL");
                    return;
                }


                _pluginPrefabCount.Clear();

                for (int i = 0; i < findMatchingEntry.CustomEntitySaveList.Count; i++)
                {
                    var entry = findMatchingEntry.CustomEntitySaveList[i];

                    if (entry == null)
                    {
                        continue;
                    }

                    bool contains;

                    string fullPrefabName = entry.PrefabName;

                    if (!_pluginPrefabCount.TryGetValue(fullPrefabName, out var currentTuple))
                    {
                        currentTuple.Item1 = 0;
                        currentTuple.Item2 = entry is ICustomEntity;

                        contains = false;
                    }
                    else
                    {
                        contains = true;
                    }

                    currentTuple.Item1++;

                    if (contains)
                    {
                        _pluginPrefabCount[fullPrefabName] = currentTuple;
                    }
                    else
                    {
                        _pluginPrefabCount.Add(fullPrefabName, currentTuple);
                    }
                }

                _pluginPrefabCount = _pluginPrefabCount.OrderByDescending(e => e.Value.Item2).ThenByDescending(e => e.Value.Item1).ThenBy(e => e.Key).ToDictionary(e => e.Key, e => e.Value);

                StringBuilder builder = new StringBuilder("ENTITY COUNTS (custom first, then vanilla): \n");

                foreach (var entry in _pluginPrefabCount)
                {
                    builder.Append(entry.Value.Item1.ToString("00000"));
                    builder.Append(" of ");
                    builder.Append(entry.Key);
                    builder.AppendLine();
                }

                iplayer.Reply(builder.ToString());
            }

            public static void PlayerRequestedPluginPurge(IPlayer iplayer, string partialPluginNameLowercase)
            {
                if (_cacheByOwner.Count == 0)
                {
                    iplayer.Reply($"{MSG(MSG_CMD_PURGE_PLUGIN_NO_PLUGINS_REGISTERED, iplayer.Id)} {MSG(MSG_CMD_PURGE_PLUGIN_MAKE_SURE_LOADED, iplayer.Id)}");
                    return;
                }

                BinaryData findMatchingEntry = null;

                for (int i = 0; i < _cacheByOwner.Count; i++)
                {
                    KeyValuePair<Plugin, BinaryData> cacheEntry = _cacheByOwner.ElementAt(i);

                    if (cacheEntry.Key.Name.ToLower().StartsWith(partialPluginNameLowercase))
                    {
                        findMatchingEntry = cacheEntry.Value;
                        break;
                    }
                }

                if (findMatchingEntry == null)
                {
                    iplayer.Reply($"{MSG(MSG_CMD_PURGE_PLUGIN_NOTHING_FOUND, iplayer.Id)} {MSG(MSG_CMD_PURGE_PLUGIN_MAKE_SURE_LOADED, iplayer.Id)}");
                    return;
                }

                int countKilled = 0;

                _prefabNamesToKill.Clear();

                for (int i = 0; i < findMatchingEntry.PrefabBundle.Recipes.Length; i++)
                {
                    GenericPrefabRecipe recipe = findMatchingEntry.PrefabBundle.Recipes[i];

                    _prefabNamesToKill.Add(recipe.FullPrefabName);
                }

                ICustomEntity[] iterateOver = BaseNetworkable.serverEntities.OfType<ICustomEntity>().ToArray();

                for (int i = 0; i < iterateOver.Length; i++)
                {
                    ICustomEntity customEntity = iterateOver[i];

                    if (customEntity == null)
                    {
                        continue;
                    }

                    if (!_prefabNamesToKill.Contains(customEntity.PrefabName))
                    {
                        continue;
                    }

                    if (customEntity.IsDestroyed)
                    {
                        continue;
                    }

                    customEntity.Kill(BaseNetworkable.DestroyMode.None);

                    countKilled++;
                }

                if (countKilled == 0)
                {
                    iplayer.Reply(MSG(MSG_CMD_PURGE_PLUGIN_NO_ENTITIES_FOUND, iplayer.Id, findMatchingEntry._ownerPlugin.Name));
                    return;
                }

                iplayer.Reply(MSG(MSG_CMD_PURGE_PLUGIN_KILLED_ENTITIES, iplayer.Id, countKilled, findMatchingEntry._ownerPlugin.Name));

            }

            public static BinaryData SummonBinaryData(Plugin maybeOwnerPlugin, string optionalSuffix)
            {
                BinaryData resultData;

                if (!_cacheByOwner.TryGetValue(maybeOwnerPlugin, out resultData))
                {
                    //we need to summon it and add to cache by path

                    resultData = new BinaryData(maybeOwnerPlugin, optionalSuffix);

                    _cacheByOwner.Add(maybeOwnerPlugin, resultData);
                }

                return resultData;
            }

            public static void ForgetBinaryData(Plugin maybeOwnerPlugin)
            {
                if (!_cacheByOwner.TryGetValue(maybeOwnerPlugin, out BinaryData data))
                {
                    return;
                }

                data.CustomEntitySaveList = null;

                _cacheByOwner.Remove(maybeOwnerPlugin);
            }

            public BinaryData(Plugin ownerPlugin, string optionalSuffix)
            {
                _ownerPlugin = ownerPlugin;

                if (optionalSuffix == default(string))
                {
                    optionalSuffix = string.Empty;
                }

                _fullFileDirectory = Path.Combine(Interface.Oxide.DataFileSystem.Directory, Instance.Name);

                var filenameWithOrWithoutSuffixBuilder = new StringBuilder();

                filenameWithOrWithoutSuffixBuilder.Append(ownerPlugin.Name);

                if (optionalSuffix != string.Empty)
                {
                    filenameWithOrWithoutSuffixBuilder.Append('.');
                    filenameWithOrWithoutSuffixBuilder.Append(optionalSuffix);
                }

                filenameWithOrWithoutSuffixBuilder.Append(".sav");

                _fullFilePath = Path.Combine(_fullFileDirectory, filenameWithOrWithoutSuffixBuilder.ToString());

                CustomEntitySaveList = new List<BaseEntity>();

                EnsureFileDirectory();
            }

            private void EnsureFileDirectory()
            {
                if (Directory.Exists(_fullFileDirectory))
                {
                    return;
                }

                Directory.CreateDirectory(_fullFileDirectory);
            }

            public void ShiftSaveBackups()
            {
                if (!File.Exists(_fullFilePath))
                {
                    return;
                }
                try
                {
                    int num = Mathf.Max(ConVar.Server.saveBackupCount, 2);
                    int num2 = 0;
                    for (int j = 1; j <= num && File.Exists(_fullFilePath + "." + j); j++)
                    {
                        num2++;
                    }
                    string text = GetBackupName(_fullFilePath, num2 + 1);
                    for (int num3 = num2; num3 > 0; num3--)
                    {
                        string text2 = GetBackupName(_fullFilePath, num3);
                        if (num3 == num)
                        {
                            File.Delete(text2);
                        }
                        else if (File.Exists(text2))
                        {
                            if (File.Exists(text))
                            {
                                File.Delete(text);
                            }
                            File.Move(text2, text);
                        }
                        text = text2;
                    }
                    File.Copy(_fullFilePath, text, true);
                }
                catch (Exception e)
                {
                    Instance.PrintError(MSG(MSG_SAVING_BACKUP_EXCEPTION, null, e.GetType(), _fullFilePath, e.Message, e.StackTrace));
                    throw;
                }
            }

            public void Save()
            {
                int countAll = CustomEntitySaveList.Count;

                int countVanilla = 0;
                int countCustom = 0;
                int countSaved = 0;

                try
                {
                    EnsureFileDirectory();

                    ShiftSaveBackups();

                    bool swaparoo = false;
                    uint swaperooPrefabID = 0;
                    uint originalPrefabID = 0;

                    //so also we need to deal with invalids somehow.
                    //for now, just test if things were not broken by refactoring.
                    //ok, they work fine, seemingly

                    List<int> indicesToRemove = Pool.Get<List<int>>();
                    List<int> indicesToDestroy = Pool.Get<List<int>>();

                    using (FileStream fileStream = new FileStream(_fullFilePath, FileMode.Create))
                    {
                        using (BinaryWriter finalWriter = new BinaryWriter(fileStream))
                        {
                            using (MemoryStream intermediateMemoryStream = new MemoryStream())
                            {
                                using (BinaryWriter intermediateWriter = new BinaryWriter(intermediateMemoryStream))
                                {
                                    //and optionally...
                                    for (int i = 0; i < countAll; i++)
                                    {
                                        BaseEntity entity = CustomEntitySaveList[i];

                                        //now here, you're not doing any null checks... or checks if it's destroyed.

                                        if (entity == null)
                                        {
                                            indicesToRemove.Add(i);
                                            continue;
                                        }

                                        if (entity.net == null)
                                        {
                                            indicesToRemove.Add(i);
                                            indicesToDestroy.Add(i);
                                            continue;
                                        }

                                        if (entity.IsDestroyed)
                                        {
                                            indicesToRemove.Add(i);
                                            indicesToDestroy.Add(i);
                                            continue;
                                        }

                                        if (entity.enableSaving)
                                        {
                                            //just a safeguard lol.
                                            indicesToRemove.Add(i);
                                            continue;
                                        }

                                        swaparoo = false;

                                        //if the full prefab name of that entity is found in this cache,
                                        //it means that a swaperoo is required.

                                        if (CustomPrefabs.ModifiedPrefabFullNameToModifiedPrefabIDHandledByBundles.TryGetValue(entity.PrefabName, out swaperooPrefabID))
                                        {
                                            swaparoo = true;
                                            originalPrefabID = entity.prefabID;
                                        }

                                        //Do a swap?
                                        if (swaparoo)
                                        {
                                            entity.prefabID = swaperooPrefabID;
                                            entity.InvalidateNetworkCache();
                                        }

                                        //this is done always, even without swaperoo

                                        MemoryStream vanillaMemoryStream = entity.GetSaveCache();

                                        //Do a roo?
                                        if (swaparoo)
                                        {
                                            entity.prefabID = originalPrefabID;
                                        }

                                        long vanillaMemoryStreamLength = vanillaMemoryStream.Length;
                                        //first, vanilla stuff, that always happens...
                                        intermediateWriter.Write((uint)vanillaMemoryStreamLength); //4 bytes
                                        intermediateWriter.Write(vanillaMemoryStream.GetBuffer(), 0, (int)vanillaMemoryStreamLength);
                                        //this may or may not be a vanilla thing, so handle accordingly
                                        ICustomEntity asInterface = entity as ICustomEntity;

                                        if (asInterface != null)
                                        {

                                            using (MemoryStream customMemoryStream = new MemoryStream(4))
                                            {
                                                using (BinaryWriter customWriter = new BinaryWriter(customMemoryStream))
                                                {
                                                    asInterface.Handler.SaveExtra(customMemoryStream, customWriter);
                                                    long customMemoryStreamLength = customMemoryStream.Length;
                                                    //and now we write custom stuff.
                                                    intermediateWriter.Write((uint)customMemoryStreamLength); //4 bytes
                                                    intermediateWriter.Write(customMemoryStream.GetBuffer(), 0, (int)customMemoryStreamLength);
                                                }
                                            }

                                            countCustom++;
                                        }
                                        else
                                        {
                                            //is it normal vanilla, or modified vanilla?


                                            countVanilla++;
                                        }

                                        countSaved++;
                                    }

                                    finalWriter.Write(CommunityEntity.ServerInstance.net.ID.Value);

                                    finalWriter.Write(countSaved);

                                    long intermediateMemoryStringLength = intermediateMemoryStream.Length;

                                    finalWriter.Write(intermediateMemoryStream.GetBuffer(), 0, (int)intermediateMemoryStringLength);
                                }
                            }
                        }
                    }

                    if (indicesToDestroy.Count > 0)
                    {
                        for (int i = 0; i < indicesToDestroy.Count; i++)
                        {
                            var idx = indicesToDestroy[i];
                            var toDestroy = CustomEntitySaveList[idx];
                            GameObject.Destroy(toDestroy.gameObject);
                        }
                    }

                    if (indicesToRemove.Count > 0)
                    {
                        indicesToRemove.Sort((a, b) => b.CompareTo(a));

                        foreach (int index in indicesToRemove)
                        {
                            if (index >= 0 && index < countAll)
                            {
                                CustomEntitySaveList.RemoveAt(index);
                            }
                        }
                    }

                    int countInvalid = indicesToRemove.Count;

                    Pool.FreeUnmanaged(ref indicesToRemove);
                    Pool.FreeUnmanaged(ref indicesToDestroy);

                    if (CustomConvars.VerboseLogging)
                    {
                        Instance.PrintWarning(MSG(MSG_SAVED_ENTITIES_1, null, countSaved, countAll, countCustom, countVanilla, countInvalid, _fullFilePath));
                    }
                }
                catch (Exception e)
                {
                    Instance.PrintError(MSG(MSG_SAVING_DATAFILE_EXCEPTION, null, e.GetType(), _fullFilePath, e.Message, e.StackTrace));
                }

            }

            public void Load()
            {
                EnsureFileDirectory();

                if (!File.Exists(_fullFilePath))
                {
                    Instance.PrintWarning(MSG(MSG_LOADING_NO_SAVEFILE, null, _fullFilePath));
                    return;
                }

                Instance.PrintWarning(MSG(MSG_LOADING_YES_DATAFILE, null, _fullFilePath));

                bool needsBlankFile = false;

                try
                {
                    using (FileStream readStream = File.OpenRead(_fullFilePath))
                    {
                        using (BinaryReader reader = new BinaryReader(readStream))
                        {
                            ulong ceInstanceNetIDfromFile = reader.ReadUInt64();
                            ulong ceInstanceNetIDfromCurrent = CommunityEntity.ServerInstance.net.ID.Value;

                            if (ceInstanceNetIDfromFile != ceInstanceNetIDfromCurrent)
                            {
                                Instance.PrintWarning(MSG(MSG_LOADING_FRESH_WIPE_DETECTED, null, ceInstanceNetIDfromCurrent, ceInstanceNetIDfromFile));
                                needsBlankFile = true;
                            }
                            else
                            {
                                //the next in32 will be the amount of entities saved here.
                                int entityAmount = reader.ReadInt32();

                                if (entityAmount == 0)
                                {
                                    Instance.PrintWarning(MSG(MSG_LOADING_EMPTY_SAVEFILE, null, _fullFilePath));
                                }
                                else
                                {
                                    //this is where entities are actually loaded.

                                    Instance.PrintWarning(MSG(MSG_LOADING_ENTITIES_FROM_SAVEFILE, null, entityAmount, _fullFilePath));

                                    //implement the loop that will call ProtoBuf.Entity.Deserialize limited on the
                                    //entire stream. Add them all to the dictionary below.

                                    HashSet<ulong> hashSet = new HashSet<ulong>();
                                    Dictionary<BaseEntity, ProtoBuf.Entity> dictionaryVanilla = new Dictionary<BaseEntity, ProtoBuf.Entity>();

                                    Dictionary<ICustomEntity, byte[]> dictionaryCustom = new Dictionary<ICustomEntity, byte[]>();

                                    try
                                    {

                                        while (true)
                                        {
                                            //first we read the z bytes...
                                            ProtoBuf.Entity entData = null;
                                            int vanillaLength = reader.ReadInt32();
                                            var bytes = reader.ReadBytes(vanillaLength);
                                            using (var bufferStream = Pool.Get<BufferStream>())
                                            {
                                                bufferStream.Initialize(bytes, vanillaLength);
                                                entData = ProtoBuf.Entity.DeserializeLength(bufferStream, vanillaLength);
                                            }

                                            if (entData.baseNetworkable.uid.Value != 0 && (hashSet.Contains(entData.baseNetworkable.uid.Value) || BaseNetworkable.serverEntities.Contains(entData.baseNetworkable.uid)))
                                            {
                                                Instance.PrintError(MSG(MSG_LOADING_ERROR_ENTITY_ALREADY_EXISTS, null, _fullFilePath, entData.baseNetworkable.prefabID, StringPool.Get(entData.baseNetworkable.prefabID), entData.baseNetworkable.uid));
                                                continue;
                                            }

                                            if (entData.baseNetworkable.uid.Value != 0)
                                            {
                                                hashSet.Add(entData.baseNetworkable.uid.Value);
                                            }

                                            bool skipAfterReadingCustomLength = false;

                                            BaseEntity baseEntity = GameManager.server.CreateEntity(StringPool.Get(entData.baseNetworkable.prefabID), entData.baseEntity.pos, Quaternion.Euler(entData.baseEntity.rot));
                                            if ((bool)baseEntity)
                                            {
                                                baseEntity.InitLoad(entData.baseNetworkable.uid); //this will restore net ID
                                                baseEntity.PreServerLoad();
                                                dictionaryVanilla.Add(baseEntity, entData);
                                            }
                                            else
                                            {
                                                Instance.PrintError(MSG(MSG_LOADING_ERROR_ENTITY_COULDNT_SPAWN, null, _fullFilePath, entData.baseNetworkable.prefabID, StringPool.Get(entData.baseNetworkable.prefabID), entData.baseNetworkable.uid));

                                                //ok but we actually have to advance, still.

                                                skipAfterReadingCustomLength = true;
                                            }

                                            ICustomEntity asInterface = baseEntity as ICustomEntity;

                                            if (asInterface == null)
                                            {
                                                //well in that case it's just a vanilla entity existing here in the save.
                                                continue;
                                            }


                                            //and now we read custom bytes. The entity isn't spawned yet, so let's just get some streams and put them in the cache.

                                            int customLength = reader.ReadInt32(); //typically 4 or more

                                            //those need to be read to advance the stream to the next entity, whether we need them or not

                                            if (skipAfterReadingCustomLength)
                                            {
                                                //just advance, don't read
                                                readStream.Position += customLength;
                                                continue;
                                            }
                                            //if we're not skipping, we need to add those bytes to a cache and apply when the Handler is ready

                                            dictionaryCustom.Add(asInterface, reader.ReadBytes(customLength));


                                        }
                                    }
                                    catch (EndOfStreamException)
                                    {
                                        //reached the end of the file
                                    }

                                    Instance.PrintWarning(MSG(MSG_LOADING_SPAWNING_ENTITIES, null, dictionaryVanilla.Count, _fullFilePath));

                                    BaseNetworkable.LoadInfo info = default(BaseNetworkable.LoadInfo);
                                    info.fromDisk = true;
                                    System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                                    int num2 = 0;

                                    int countVanillaAndCustom = 0;
                                    int countVanilla = 0;
                                    int countCustom = 0;

                                    foreach (KeyValuePair<BaseEntity, ProtoBuf.Entity> item in dictionaryVanilla)
                                    {
                                        BaseEntity key = item.Key;
                                        if (key == null)
                                        {
                                            continue;
                                        }
                                        RCon.Update();
                                        info.msg = item.Value;

                                        countVanillaAndCustom++;

                                        ICustomEntity asInterface = key as ICustomEntity;
                                        byte[] maybeSomeExtraData = null;
                                        if (asInterface != null)
                                        {
                                            if (dictionaryCustom.TryGetValue(asInterface, out maybeSomeExtraData))
                                            {
                                                using (MemoryStream customStream = new MemoryStream(maybeSomeExtraData))
                                                {
                                                    using (BinaryReader customReader = new BinaryReader(customStream))
                                                    {
                                                        asInterface.Handler.LoadExtra(customStream, customReader);
                                                    }
                                                }
                                                countCustom++;
                                            }
                                        }
                                        else
                                        {
                                            //it's vanilla, so it's not gonna auto-add itself on Load unless we do this
                                            CustomPrefabs.EnsureMovedToCustomSaveList(key, CustomEntitySaveList);
                                            countVanilla++;
                                        }

                                        key.Spawn();
                                        key.Load(info);

                                        if (maybeSomeExtraData != null)
                                        {
                                            using (MemoryStream customStream = new MemoryStream(maybeSomeExtraData))
                                            {
                                                using (BinaryReader customReader = new BinaryReader(customStream))
                                                {
                                                    asInterface.Handler.PostLoadExtra(customStream, customReader);
                                                }
                                            }
                                        }

                                        if (key.IsValid())
                                        {
                                            num2++;
                                            if (stopwatch.Elapsed.TotalMilliseconds > 2000.0)
                                            {
                                                stopwatch.Reset();
                                                stopwatch.Start();
                                                DebugEx.Log("\t" + num2 + " / " + dictionaryVanilla.Count);
                                            }
                                        }
                                    }

                                    foreach (KeyValuePair<BaseEntity, ProtoBuf.Entity> item2 in dictionaryVanilla)
                                    {
                                        BaseEntity key2 = item2.Key;
                                        if (key2 == null)
                                        {
                                            //IT SHOULDN'T
                                            continue;
                                        }

                                        RCon.Update();
                                        if (key2.IsValid())
                                        {
                                            key2.UpdateNetworkGroup();
                                            key2.PostServerLoad();
                                        }
                                    }

                                    Instance.PrintWarning(MSG(MSG_LOADED_ENTITIES, null, countVanillaAndCustom, entityAmount, countCustom, countVanilla, _fullFilePath));
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Instance.PrintError(MSG(MSG_LOADING_DATAFILE_LOAD_EXCEPTION, null, e.GetType(), _fullFilePath, e.Message, e.StackTrace));
                    needsBlankFile = true;
                }

                if (needsBlankFile)
                {
                    Instance.PrintWarning(MSG(MSG_LOADING_NEW_SAVEFILE, null, _fullFilePath));

                    try
                    {
                        using (FileStream writeStream = new FileStream(_fullFilePath, FileMode.Create))
                        {
                            using (BinaryWriter writer = new BinaryWriter(writeStream))
                            {
                                writer.Write(CommunityEntity.ServerInstance.net.ID.Value);
                                writer.Write(0); //0 entities by default
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Instance.PrintError(MSG(MSG_LOADING_DATAFILE_NEW_EXCEPTION, null, e.GetType(), _fullFilePath, e.Message, e.StackTrace));
                    }
                }
            }

        }
        #endregion

        #region CUSTOM PREFAB RECIPES AND THEIR BUNDLES
        public class CustomPrefabBundle
        {
            public Plugin Owner;
            public GenericPrefabRecipe[] Recipes;

            public CustomPrefabBundle(Plugin owner, params GenericPrefabRecipe[] recipes)
            {
                Owner = owner;
                Recipes = recipes;
            }
        }

        public class GenericPrefabRecipe
        {
            public readonly string ShortPrefabName;
            public readonly string FullPrefabName;
            public readonly bool EnableInSpawnCommand;

            public GenericPrefabRecipe(string shortName, bool enableInSpawnCommand = true)
            {
                ShortPrefabName = shortName;
                FullPrefabName = SanitizedFullPrefabName(shortName);
                EnableInSpawnCommand = enableInSpawnCommand;
            }
        }

        public class ModifiedPrefabRecipe : GenericPrefabRecipe
        {
            public readonly string OriginalFullPrefabName;

            public readonly Func<BaseEntity, bool> ModificationFunction;

            public readonly ModifiedSaveHandling SaveHandling;

            public ModifiedPrefabRecipe(string shortName, string originalFullPrefabName, Func<BaseEntity, bool> modificationFunction = null, bool enableInSpawnCommand = true, ModifiedSaveHandling saveHandling = ModifiedSaveHandling.DontSave) : base(shortName, enableInSpawnCommand)
            {
                OriginalFullPrefabName = originalFullPrefabName;
                ModificationFunction = modificationFunction;
                SaveHandling = saveHandling;
            }
        }

        public class CustomPrefabRecipe : GenericPrefabRecipe
        {
            public readonly Layer Layer;

            public readonly Type EntityType;

            public readonly CustomPrefabBaseCombat BaseCombat;

            public CustomPrefabRecipe(string shortName, Type entityType, Layer layer = Layer.Default, CustomPrefabBaseCombat baseCombat = null, bool enableInSpawnCommand = true) : base(shortName, enableInSpawnCommand)
            {
                EntityType = entityType;
                Layer = layer;
                BaseCombat = baseCombat;
            }

        }
        #endregion

        #region CUSTOM PREFAB RECIPE BASE COMBAT EXTENSION

        public class CustomPrefabBaseCombat
        {
            public float HealthStart = 100F;
            public float HealthMax = 100F;

            public bool RepairEnabled = false;
            public bool PickupEnabled = false;
            public bool MarkAttackerHostile = false;

            public float[] ProtectionProperties = null;
        }
        #endregion

        #region INTERFACE FOR CUSTOM ENTITIES

        public interface ICustomEntity
        {
            CustomHandler Handler { get; set; }
            List<BaseEntity> SaveListInDataFile { get; set; }
            bool IsDestroyed { get; }

            bool HasDefaultInventory { get; }

            bool DefaultInventoryHandledByBaseType { get; }

            int DefaultInventoryCapacity { get; }


            void OnDefaultInventoryPreAnnihilation();

            void OnDefaultInventoryFirstCreated();

            bool DefaultInventoryItemFilter(Item item, int targetSlot);

            void OnItemAddedOrRemoved(Item item, bool added);

            void OnDefaultInventoryDirty();

            ItemContainer DefaultInventory { get; set; }
            string PrefabName { get; }

            bool EnableSavingToDiskByDefault { get; }

            string DefaultClientsideFullPrefabName();
            void OnCustomPrefabPrototypeEntityRegistered();
            void OnCustomPrefabPrototypeEntityUnregistered();

            void OnParentChanging(BaseEntity oldParent, BaseEntity newParent);

            void Awake();

            void ServerInit();
            void Save(BaseNetworkable.SaveInfo info);
            void Load(BaseNetworkable.LoadInfo info);

            void Kill(BaseNetworkable.DestroyMode destroyMode = BaseNetworkable.DestroyMode.None);

            MemoryStream GetSaveCache();

            bool IsBaseCombat();

            void DestroyShared();

            void SaveExtra(Stream stream, BinaryWriter writer);

            void LoadExtra(Stream stream, BinaryReader reader);
            void PostLoadExtra(Stream stream, BinaryReader reader);

            void OnEntitySaveForNetwork(BaseNetworkable.SaveInfo info);
        }


        public enum ModifiedSaveHandling
        {
            DontSave,
            SaveInVanillaSaveList,
            SaveInBundleSaveList
        }

        //wait a sec. You're adding. That's nice...
        //...but what about removing?
        //...shall we keep the mono alive, instead of destroying it...
        //...and do OnDestroy()? And there ignore if Unloading?
        //...but how do we detect if the bundle is unloading?
        //...I think that during each save, if a null element is detected in the save list,
        //...

        public class ModifiedBundleSaveBehaviour : MonoBehaviour
        {
            public string RecipeFullPrefabName = string.Empty;
            public bool AddToCustomSaveList = false;

            BaseEntity modifiedEntity;

            void Awake()
            {
                if (RecipeFullPrefabName == string.Empty)
                {
                    //means most likely this is a prototype.
                    return;
                }

                modifiedEntity = GetComponent<BaseEntity>();

                if (modifiedEntity.PrefabName != RecipeFullPrefabName)
                {
                    modifiedEntity._prefabName = RecipeFullPrefabName;
                }
            }

            //this won't happen unless instantiated. Start() so the entity has time to populate inventory, spawn subentities etc.
            void Start()
            {
                if (modifiedEntity == null)
                {
                    goto DestroyStuff;
                    //congrats, I think this is the first time you actually found a use for "goto" and labels, like it's 2002 again and you're
                    //messing about in QBASIC on Windows 98
                }

                //so as it turns out, somewhere along the way of instantiation with GameManager.server.Create and/or spawn command,
                //if a "prototype" MonoBehaviour living in the DontDestroyOnLoad scene has a List<T>, like List<BaseEntity>,
                //when we use the `spawn` command and/or GameManager.server.CreateEntity (presumably, haven't even bothered checking that case)
                //the new instance will have that List cloned - instead of having its value set to the same reference as the prototype.
                //this goes against all Instantiation intuitions, but at this point, we just want a solution that works.
                //originally this class just had a field with a reference to the save list, but clearly, that is not the way to go!

                if (!AddToCustomSaveList)
                {
                    goto DestroyStuff;
                }

                List<BaseEntity> entitySaveList;

                if (!CustomPrefabs.ModifiedPrefabFullNameToCustomSaveList.TryGetValue(RecipeFullPrefabName, out entitySaveList))
                {

                    goto DestroyStuff;
                }

                CustomPrefabs.EnsureMovedToCustomSaveListRecursivelyTopDown(modifiedEntity, entitySaveList);

            DestroyStuff:

                Destroy(this);
            }
        }


        //SO ACTUALLY.

        //BinaryData can handle this, like it handles saving. Let's keep it confined.

        //if a vanilla prefabID is found in the Dictionary of modified prefab IDs,

        //then we alter the original prefab id of that modifed base entity during saving to the modified prefab id.

        public class CustomHandler
        {
            private readonly BaseEntity _ownerEntity;
            private readonly ICustomEntity _ownerEntityAsInterface;
            private readonly string _ownerDefaultClientsideFullPrefabName;

            private uint _clientsidePrefabID = 0;
            private string _clientsideFullPrefabName = string.Empty;

            public string ServersideFullPrefabName;

            private bool _enableSavingToDisk = false; //this is false by default, so setting it the first time will auto add it

            public static void AttachNewHandlerToCustomEntityIfNotPrototype(BaseEntity entity)
            {
                BaseEntity tryPrototypeEntity = CustomPrefabs.TryGetPreprocessedPrototypeFromCustom(entity.PrefabName);

                if (tryPrototypeEntity == null)
                {
                    //that means you, yourself, are the prototype
                    return;
                }

                ICustomEntity asInterface = (ICustomEntity)entity;

                asInterface.Handler = new CustomHandler(entity, asInterface.DefaultClientsideFullPrefabName(), tryPrototypeEntity);
            }

            //using this will update the ID
            public string ClientsideFullPrefabName
            {
                get
                {
                    if (_clientsideFullPrefabName == string.Empty)
                    {
                        _clientsideFullPrefabName = _ownerDefaultClientsideFullPrefabName;
                    }

                    return _clientsideFullPrefabName;
                }
                set
                {
                    if (_clientsideFullPrefabName == value)
                    {
                        return;
                    }

                    _clientsideFullPrefabName = value;
                    ClientsidePrefabID = StringPool.Get(_clientsideFullPrefabName);
                }
            }


            //and using this will update the string. now it shouldn't cause an infinite loop because both setter/getter pairs do a check before setting
            public uint ClientsidePrefabID
            {
                get
                {
                    if (_clientsidePrefabID == 0)
                    {
                        _clientsidePrefabID = StringPool.Get(ClientsideFullPrefabName);
                    }

                    return _clientsidePrefabID;
                }
                set
                {
                    if (_clientsidePrefabID == value)
                    {
                        return;
                    }

                    _clientsidePrefabID = value;

                    ClientsideFullPrefabName = StringPool.Get(_clientsidePrefabID);

                    if (_ownerEntity.net == null)
                    {
                        return;
                    }

                    if (_ownerEntity.net.group == null)
                    {
                        return;
                    }

                    if (_ownerEntity.net.group.subscribers == null)
                    {
                        return;
                    }

                    DestroyClientsideForSendInfo(_ownerEntity, new SendInfo(_ownerEntity.net.group.subscribers), true);
                }
            }

            public bool EnableSavingToDisk
            {
                get
                {
                    return _enableSavingToDisk;
                }
                set
                {
                    if (_enableSavingToDisk == value)
                    {
                        return; //nothing changes
                    }

                    if (value)
                    {
                        AddToSaveListIfNotContains();
                    }
                    else
                    {
                        RemoveFromSaveListIfContains();
                    }

                    _enableSavingToDisk = value;
                }
            }

            public void OnParentChanging(BaseEntity oldParent, BaseEntity newParent)
            {
                //TODO:
                //the purpose of this method is that when a custom entity switches parents,
                //we need to move that entity to the proper save file if needed.

                //implement this when there's a hook for detecting any entity changing any parents.
                //propose that hook first lol.

                //TODO LOGIC HERE ACTUALLY ONE DAY LOL. oldParent can be null or non null, newParent can be null or non null. Either of them can be custom or vanilla.
                //So we need to be prepared for all 8 combinations here.

                /*
                bool oldParentIsCustom = false;
                bool newParentIsCustom = false;

                bool oldParentIsNull = oldParent == null;
                bool newParentIsNull = newParent == null;

                if (!oldParentIsNull)
                {
                    oldParentIsCustom = oldParent is ICustomEntity;
                }

                if (!newParentIsNull)
                {
                    newParentIsCustom = newParent is ICustomEntity;
                }

                if (oldParentIsNull && newParentIsNull)
                {
                    //If both are null, do nothing. only 7 possibilities to go, lol.
                    return;
                }

                if (!oldParentIsNull && !newParentIsNull)
                {
                    //none are null.
                }*/

            }

            private void RemoveFromSaveListIfContains()
            {
                if (_ownerEntityAsInterface.SaveListInDataFile.Contains(_ownerEntity))
                {
                    _ownerEntityAsInterface.SaveListInDataFile.Remove(_ownerEntity);
                }
            }

            private void AddToSaveListIfNotContains()
            {
                if (!_ownerEntityAsInterface.SaveListInDataFile.Contains(_ownerEntity))
                {
                    _ownerEntityAsInterface.SaveListInDataFile.Add(_ownerEntity);
                }
            }

            public CustomHandler(BaseEntity ownerEntity, string defaultClientsidePrefabName, BaseEntity prototypeEntity)
            {
                _ownerEntity = ownerEntity;

                _ownerEntity.EnableSaving(false); //disable vanilla saving, always

                _ownerEntityAsInterface = (ICustomEntity)ownerEntity;

                _ownerDefaultClientsideFullPrefabName = defaultClientsidePrefabName;

                if (_clientsidePrefabID == 0)
                {
                    _clientsideFullPrefabName = _ownerDefaultClientsideFullPrefabName;
                    _clientsidePrefabID = StringPool.Get(_clientsideFullPrefabName);
                }

                ServersideFullPrefabName = _ownerEntity.PrefabName;

                List<BaseEntity> useThisList = ((ICustomEntity)prototypeEntity).SaveListInDataFile;

                _ownerEntityAsInterface.SaveListInDataFile = useThisList;
            }

            public void PreServerLoad()
            {
                if (!_ownerEntityAsInterface.HasDefaultInventory || _ownerEntityAsInterface.DefaultInventoryHandledByBaseType)
                {
                    return;
                }

                _ownerEntityAsInterface.DefaultInventory = CreateInventory(_ownerEntity, false, _ownerEntityAsInterface.DefaultInventoryCapacity);

            }

            public void ServerInit()
            {
                EnableSavingToDisk = _ownerEntityAsInterface.EnableSavingToDiskByDefault; //

                if (!_ownerEntityAsInterface.HasDefaultInventory || _ownerEntityAsInterface.DefaultInventoryHandledByBaseType)
                {
                    return;
                }

                if (_ownerEntityAsInterface.DefaultInventory != null)
                {
                    return;
                }

                _ownerEntityAsInterface.DefaultInventory = CreateInventory(_ownerEntity, true, _ownerEntityAsInterface.DefaultInventoryCapacity);
                _ownerEntityAsInterface.OnDefaultInventoryFirstCreated();

            }

            public void Save(BaseNetworkable.SaveInfo info)
            {
                if (info.forDisk)
                {
                    if (!_ownerEntityAsInterface.HasDefaultInventory || _ownerEntityAsInterface.DefaultInventoryHandledByBaseType)
                    {
                        return;
                    }

                    if (_ownerEntityAsInterface.DefaultInventory != null)
                    {
                        info.msg.storageBox = Pool.Get<ProtoBuf.StorageBox>();
                        info.msg.storageBox.contents = _ownerEntityAsInterface.DefaultInventory.Save();
                    }
                    else
                    {
                        Debug.LogWarning("Assigned storage container is null!");
                    }

                    return;
                }

                //for networking? Then pretend you're something else, PREFERABLY SOMETHING THAT EXISTS
                info.msg.baseNetworkable.prefabID = ClientsidePrefabID;

                _ownerEntityAsInterface.OnEntitySaveForNetwork(info);
            }

            public void Load(BaseNetworkable.LoadInfo info)
            {
                if (!_ownerEntityAsInterface.HasDefaultInventory || _ownerEntityAsInterface.DefaultInventoryHandledByBaseType)
                {
                    return;
                }

                if (info.msg.storageBox != null)
                {
                    if (_ownerEntityAsInterface.DefaultInventory != null)
                    {
                        _ownerEntityAsInterface.DefaultInventory.Load(info.msg.storageBox.contents);
                        _ownerEntityAsInterface.DefaultInventory.capacity = _ownerEntityAsInterface.DefaultInventoryCapacity;
                    }
                    else
                    {
                        Debug.LogWarning("Assigned storage container is null!");
                    }
                }
            }

            public void SaveExtra(Stream stream, BinaryWriter writer)
            {
                writer.Write(_clientsidePrefabID);

                _ownerEntityAsInterface.SaveExtra(stream, writer);
            }

            public void LoadExtra(Stream stream, BinaryReader reader)
            {
                ClientsidePrefabID = reader.ReadUInt32();

                _ownerEntityAsInterface.LoadExtra(stream, reader);
            }

            internal void PostLoadExtra(Stream stream, BinaryReader reader)
            {
                ClientsidePrefabID = reader.ReadUInt32();

                _ownerEntityAsInterface.PostLoadExtra(stream, reader);
            }


            public void DestroyShared()
            {
                //I think this is th eonly part that doesn't care whether vanilla handles it or not!
                //Annihilation is important. We need to get rid of all those nasty entities.

                if (_ownerEntityAsInterface.HasDefaultInventory)
                {
                    //this gives you a chance to move, duplicate, analyse or w/e
                    _ownerEntityAsInterface.OnDefaultInventoryPreAnnihilation();

                    AnnihilateInventory(_ownerEntityAsInterface.DefaultInventory);
                }

                if (Unloading || Instance == null)
                {
                    return;
                }

                if (EnableSavingToDisk)
                {
                    RemoveFromSaveListIfContains();
                }
            }

            public ItemContainer CreateInventory(BaseEntity owner, bool giveUID, int capacity)
            {
                ItemContainer newInventory = new ItemContainer
                {
                    entityOwner = owner,
                    allowedContents = ItemContainer.ContentsType.Generic
                };
                newInventory.SetOnlyAllowedItem(null);
                newInventory.maxStackSize = 0;
                newInventory.ServerInitialize(null, capacity);

                if (giveUID)
                {
                    newInventory.GiveUID();
                }
                newInventory.onDirty += OnInventoryDirty;
                newInventory.onItemAddedRemoved = _ownerEntityAsInterface.OnItemAddedOrRemoved;
                newInventory.canAcceptItem = _ownerEntityAsInterface.DefaultInventoryItemFilter;

                return newInventory;
            }

            public void OnInventoryDirty()
            {
                _ownerEntity.InvalidateNetworkCache();
                _ownerEntityAsInterface.OnDefaultInventoryDirty();
            }

            public static void AnnihilateInventory(ItemContainer assignedInventoryContainer)
            {
                if (assignedInventoryContainer == null)
                {
                    return;
                }

                if (assignedInventoryContainer.itemList.IsNullOrEmpty())
                {
                    return;
                }

                assignedInventoryContainer.Clear();
                ItemManager.DoRemoves();
            }
        }

        #endregion

        #region CUSTOM BASE ENTITY
        public class CustomBaseEntity : BaseEntity, ICustomEntity
        {
            public CustomHandler Handler { get; set; }
            public List<BaseEntity> SaveListInDataFile { get; set; }

            public virtual bool EnableSavingToDiskByDefault => true;
            public virtual bool HasDefaultInventory => false;
            public virtual bool DefaultInventoryHandledByBaseType => false;
            public virtual bool DefaultInventoryItemFilter(Item item, int targetSlot) => true;
            public virtual void OnDefaultInventoryDirty()
            {

            }
            public virtual void OnItemAddedOrRemoved(Item item, bool added)
            {

            }
            public ItemContainer DefaultInventory { get; set; } = null;

            public virtual int DefaultInventoryCapacity => 48;

            public virtual string DefaultClientsideFullPrefabName() => PREFAB_SPHERE;

            public bool IsBaseCombat() => false;

            public virtual void OnDefaultInventoryPreAnnihilation()
            {

            }
            public virtual void OnDefaultInventoryFirstCreated()
            {

            }

            public virtual void OnCustomPrefabPrototypeEntityRegistered()
            {

            }

            public virtual void OnCustomPrefabPrototypeEntityUnregistered()
            {

            }

            public virtual void Awake()
            {
                CustomHandler.AttachNewHandlerToCustomEntityIfNotPrototype(this);
            }

            public override void ServerInit()
            {
                base.ServerInit();

                Handler?.ServerInit();
            }

            public override void Save(SaveInfo info)
            {
                base.Save(info);

                Handler?.Save(info);
            }

            public override void Load(LoadInfo info)
            {
                base.Load(info);

                Handler?.Load(info);

            }


            public override void DestroyShared()
            {
                base.DestroyShared();
                Handler?.DestroyShared();
            }

            //prior to 1.0.5, this was never called for some reason. Lol.

            public override void PreServerLoad()
            {
                base.PreServerLoad();
                Handler?.PreServerLoad();
            }


            public virtual void OnEntitySaveForNetwork(SaveInfo info)
            {

            }

            public override void OnParentChanging(BaseEntity oldParent, BaseEntity newParent)
            {
                //do we even need that rigidbody shit from base? Most base entities wouldn't have one, would they
                base.OnParentChanging(oldParent, newParent);

                Handler?.OnParentChanging(oldParent, newParent);
            }

            public virtual void SaveExtra(Stream stream, BinaryWriter writer)
            {

            }

            public virtual void LoadExtra(Stream stream, BinaryReader reader)
            {

            }

            public virtual void PostLoadExtra(Stream stream, BinaryReader reader)
            {

            }

        }
        #endregion

        #region CUSTOM BASE COMBAT ENTITY
        public class CustomBaseCombatEntity : BaseCombatEntity, ICustomEntity
        {
            public CustomHandler Handler { get; set; }
            public List<BaseEntity> SaveListInDataFile { get; set; } = null;

            public virtual bool ShouldDoNormalVanillaBaseCombatEntityHurt => true;

            public virtual bool EnableSavingToDiskByDefault => true;

            public virtual bool HasDefaultInventory => false;
            public virtual bool DefaultInventoryHandledByBaseType => false;
            public virtual bool DefaultInventoryItemFilter(Item item, int targetSlot) => true;

            public virtual void OnDefaultInventoryDirty()
            {

            }
            public virtual void OnItemAddedOrRemoved(Item item, bool added)
            {

            }

            public ItemContainer DefaultInventory { get; set; } = null;

            public virtual int DefaultInventoryCapacity => 48;

            public virtual string DefaultClientsideFullPrefabName() => PREFAB_SPHERE; //change this to sth base combat related for testing yo.
            public bool IsBaseCombat() => true;

            public virtual void OnDefaultInventoryPreAnnihilation()
            {

            }

            public virtual void OnDefaultInventoryFirstCreated()
            {

            }

            public override float MaxVelocity()
            {
                return 100000F;
            }

            public override void Hurt(HitInfo info)
            {
                if (!ShouldDoNormalVanillaBaseCombatEntityHurt)
                {
                    return;
                }

                base.Hurt(info);
            }

            public virtual void OnCustomPrefabPrototypeEntityRegistered()
            {

            }
            public virtual void OnCustomPrefabPrototypeEntityUnregistered()
            {

            }

            public virtual void Awake()
            {
                CustomHandler.AttachNewHandlerToCustomEntityIfNotPrototype(this);
            }


            public override void ServerInit()
            {
                base.ServerInit();

                Handler?.ServerInit();
            }

            public override void Save(SaveInfo info)
            {
                base.Save(info);

                Handler?.Save(info);
            }

            public override void Load(LoadInfo info)
            {
                base.Load(info);

                Handler?.Load(info);

            }

            public virtual void OnEntitySaveForNetwork(SaveInfo info)
            {

            }

            public override void OnParentChanging(BaseEntity oldParent, BaseEntity newParent)
            {
                //do we even need that rigidbody shit from base? Most base entities wouldn't have one, would they
                base.OnParentChanging(oldParent, newParent);

                Handler?.OnParentChanging(oldParent, newParent);
            }

            //that's purely base combat entity shit right here
            public float lastNotifyFrameReplacement = float.MinValue;

            public void DoHitNotifyWithArgForcedToFalseOtherwiseItDoesntWorkLol(HitInfo info)
            {
                using (TimeWarning.New("DoHitNotify"))
                {
                    if (sendsHitNotification && !(info.Initiator == null) && info.Initiator is BasePlayer && !(this == info.Initiator) && (!info.isHeadshot || !(info.HitEntity is BasePlayer)) && Time.frameCount != lastNotifyFrameReplacement)
                    {
                        lastNotifyFrameReplacement = Time.frameCount;
                        bool flag = info.Weapon is BaseMelee;
                        if (isServer && (!flag || sendsMeleeHitNotification))
                        {
                            //the solution was obvious and staring us in the face all along 
                            //if the client receives an RPC for the HitNotify, but we mention an entity net ID that actualy got hit
                            //and that entity has a client-sided prefab that suggests it's NOT a BaseCombatEntity, it will not play-client-side.
                            //so the solution is to make the player think they... hit themselves. Thanks 0xF!

                            ClientRPC(RpcTarget.PlayerAndSpectators("HitNotify", info.Initiator as BasePlayer), false); //this must be false
                        }
                    }
                }
            }

            //that's also purely base combat shit
            public override void OnAttacked(HitInfo info)
            {
                using (TimeWarning.New("BaseCombatEntity.OnAttacked"))
                {
                    if (!IsDead())
                    {
                        DoHitNotifyWithArgForcedToFalseOtherwiseItDoesntWorkLol(info);
                    }

                    string effectToRun;

                    switch (info.damageTypes.GetMajorityDamageType())
                    {
                        case DamageType.Blunt:
                            {
                                effectToRun = PREFAB_FX_IMPACT_METAL_BLUNT;
                            }
                            break;
                        case DamageType.Stab:
                        case DamageType.Arrow:
                            {
                                effectToRun = PREFAB_FX_IMPACT_METAL_STAB;
                            }
                            break;
                        case DamageType.Slash:
                            {
                                effectToRun = PREFAB_FX_IMPACT_METAL_SLASH;
                            }
                            break;
                        case DamageType.Bullet:
                            {
                                effectToRun = PREFAB_FX_IMPACT_METAL_BULLET;
                            }
                            break;
                        default:
                            {
                                effectToRun = PREFAB_FX_IMPACT_METAL_PHYSICAL;
                            }
                            break;
                    }

                    Effect.server.Run(effectToRun, info.HitPositionWorld, info.HitNormalWorld);

                    if (info.damageTypes.Has(DamageType.Explosion))
                    {
                        Effect.server.DoAdditiveImpactEffect(info, PREFAB_FX_EXPLOSION);
                    }

                    if (info.damageTypes.Has(DamageType.Heat))
                    {
                        Effect.server.DoAdditiveImpactEffect(info, PREFAB_FX_FIRE);
                    }

                    Hurt(info);
                }
            }

            public override void DestroyShared()
            {
                base.DestroyShared();
                Handler?.DestroyShared();
            }

            public override void PreServerLoad()
            {
                base.PreServerLoad();
                Handler?.PreServerLoad();
            }

            public virtual void SaveExtra(Stream stream, BinaryWriter writer)
            {

            }

            public virtual void LoadExtra(Stream stream, BinaryReader reader)
            {

            }

            public virtual void PostLoadExtra(Stream stream, BinaryReader reader)
            {

            }
        }
        #endregion

        #region CUSTOM STORAGE CONTAINER
        public class CustomStorageContainer : StorageContainer, ICustomEntity
        {
            public CustomHandler Handler { get; set; }
            public List<BaseEntity> SaveListInDataFile { get; set; } = null;

            public virtual bool ShouldDoNormalVanillaBaseCombatEntityHurt => true;

            public virtual bool EnableSavingToDiskByDefault => true;

            public virtual bool HasDefaultInventory => true;

            public virtual bool DefaultInventoryHandledByBaseType => true; //this is important!

            public virtual bool DefaultInventoryItemFilter(Item item, int targetSlot) => true;

            public virtual void OnDefaultInventoryDirty()
            {
                //not needed as the base invalidates network cache which was already done before calling this
                //base.OnInventoryDirty();
            }
            public override void OnItemAddedOrRemoved(Item item, bool added)
            {
                base.OnItemAddedOrRemoved(item, added);
            }

            public ItemContainer DefaultInventory
            {
                get
                {
                    return _inventory;
                }
                set
                {
                    _inventory = value;
                }
            }

            public virtual int DefaultInventoryCapacity => 48;

            public virtual string DefaultClientsideFullPrefabName() => PREFAB_WOOD_STORAGE_BOX;

            public bool IsBaseCombat() => true;

            public virtual void OnDefaultInventoryPreAnnihilation()
            {

            }

            public virtual void OnDefaultInventoryFirstCreated()
            {

            }

            public override float MaxVelocity()
            {
                return 100000F;
            }

            public override void Hurt(HitInfo info)
            {
                if (!ShouldDoNormalVanillaBaseCombatEntityHurt)
                {
                    return;
                }

                base.Hurt(info);
            }

            public virtual void OnCustomPrefabPrototypeEntityRegistered()
            {
                panelName = "generic";
                panelTitle = new Translate.Phrase("loot", "Loot");
                dropLootDestroyPercent = 0F;
                dropFloats = true;
                onlyOneUser = false;
                pickup.enabled = false; //for now, pickup disabled, unless we handle associated item shortname/skin combos
                inventorySlots = DefaultInventoryCapacity;
            }
            public virtual void OnCustomPrefabPrototypeEntityUnregistered()
            {

            }

            public virtual void Awake()
            {
                CustomHandler.AttachNewHandlerToCustomEntityIfNotPrototype(this);
            }


            public override void ServerInit()
            {
                base.ServerInit();

                Handler?.ServerInit();
            }

            public override void Save(SaveInfo info)
            {
                base.Save(info);

                Handler?.Save(info);
            }

            public override void Load(LoadInfo info)
            {
                base.Load(info);

                Handler?.Load(info);

            }

            public virtual void OnEntitySaveForNetwork(SaveInfo info)
            {

            }

            public override void OnParentChanging(BaseEntity oldParent, BaseEntity newParent)
            {
                //do we even need that rigidbody shit from base? Most base entities wouldn't have one, would they
                base.OnParentChanging(oldParent, newParent);

                Handler?.OnParentChanging(oldParent, newParent);
            }

            //that's purely base combat entity shit right here
            public float lastNotifyFrameReplacement = float.MinValue;

            public void DoHitNotifyWithArgForcedToFalseOtherwiseItDoesntWorkLol(HitInfo info)
            {
                using (TimeWarning.New("DoHitNotify"))
                {
                    if (sendsHitNotification && !(info.Initiator == null) && info.Initiator is BasePlayer && !(this == info.Initiator) && (!info.isHeadshot || !(info.HitEntity is BasePlayer)) && Time.frameCount != lastNotifyFrameReplacement)
                    {
                        lastNotifyFrameReplacement = Time.frameCount;
                        bool flag = info.Weapon is BaseMelee;
                        if (isServer && (!flag || sendsMeleeHitNotification))
                        {
                            //bool arg = info.Initiator.net.connection == info.Predicted;
                            ClientRPC(RpcTarget.PlayerAndSpectators("HitNotify", info.Initiator as BasePlayer), false); //this must be false
                        }
                    }
                }
            }

            //that's also purely base combat shit
            public override void OnAttacked(HitInfo info)
            {
                using (TimeWarning.New("BaseCombatEntity.OnAttacked"))
                {
                    if (!IsDead())
                    {
                        DoHitNotifyWithArgForcedToFalseOtherwiseItDoesntWorkLol(info);
                    }

                    string effectToRun;

                    switch (info.damageTypes.GetMajorityDamageType())
                    {
                        case DamageType.Blunt:
                            {
                                effectToRun = PREFAB_FX_IMPACT_METAL_BLUNT;
                            }
                            break;
                        case DamageType.Stab:
                        case DamageType.Arrow:
                            {
                                effectToRun = PREFAB_FX_IMPACT_METAL_STAB;
                            }
                            break;
                        case DamageType.Slash:
                            {
                                effectToRun = PREFAB_FX_IMPACT_METAL_SLASH;
                            }
                            break;
                        case DamageType.Bullet:
                            {
                                effectToRun = PREFAB_FX_IMPACT_METAL_BULLET;
                            }
                            break;
                        default:
                            {
                                effectToRun = PREFAB_FX_IMPACT_METAL_PHYSICAL;
                            }
                            break;
                    }

                    Effect.server.Run(effectToRun, info.HitPositionWorld, info.HitNormalWorld);

                    if (info.damageTypes.Has(DamageType.Explosion))
                    {
                        Effect.server.DoAdditiveImpactEffect(info, PREFAB_FX_EXPLOSION);
                    }

                    if (info.damageTypes.Has(DamageType.Heat))
                    {
                        Effect.server.DoAdditiveImpactEffect(info, PREFAB_FX_FIRE);
                    }

                    Hurt(info);
                }
            }

            public override void DestroyShared()
            {
                base.DestroyShared();
                Handler?.DestroyShared();
            }

            public override void PreServerLoad()
            {
                base.PreServerLoad();
                Handler?.PreServerLoad();
            }

            public virtual void SaveExtra(Stream stream, BinaryWriter writer)
            {

            }

            public virtual void LoadExtra(Stream stream, BinaryReader reader)
            {

            }

            public virtual void PostLoadExtra(Stream stream, BinaryReader reader)
            {

            }
        }
        #endregion

        #region DRAWING

        public const int CUBE_CORNER_LEFT_DOWN_BACK = 0;
        public const int CUBE_CORNER_LEFT_DOWN_FORWARD = 1;
        public const int CUBE_CORNER_LEFT_UP_BACK = 2;
        public const int CUBE_CORNER_LEFT_UP_FORWARD = 3;

        public const int CUBE_CORNER_RIGHT_DOWN_BACK = 4;
        public const int CUBE_CORNER_RIGHT_DOWN_FORWARD = 5;
        public const int CUBE_CORNER_RIGHT_UP_BACK = 6;
        public const int CUBE_CORNER_RIGHT_UP_FORWARD = 7;


        public const string TEXT_BULLET_SMALL = "<size=20>X</size>";
        public const string TEXT_BULLET_BIG = "<size=40>X</size>";

        public static bool PrePlayerDraw(BasePlayer player)
        {
            if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendNetworkUpdateImmediate();

                return true;
            }

            return false;
        }

        public static void PostPlayerDraw(BasePlayer player, bool setAdmin)
        {
            if (setAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                player.SendNetworkUpdateImmediate();
            }
        }
        public static void DrawText(BasePlayer player, float duration, Color color, Vector3 position, string text)
        {
            bool setAdmin = PrePlayerDraw(player);

            DrawTextCommon(player, duration, color, position, text);

            PostPlayerDraw(player, setAdmin);
        }

        public static void DrawTextCommon(BasePlayer player, float duration, Color color, Vector3 position, string text)
        {
            player.SendConsoleCommand("ddraw.text", duration, color, position, text);
        }

        public static Vector3[] GetObbCoordinates(OBB obb)
        {
            Vector3[] result = new Vector3[8];

            result[0] = obb.GetPoint(-1F, -1F, -1F);
            result[1] = obb.GetPoint(-1F, -1F, 1F);
            result[2] = obb.GetPoint(-1F, 1F, -1F);
            result[3] = obb.GetPoint(-1F, 1F, 1F);

            result[4] = obb.GetPoint(1F, -1F, -1F);
            result[5] = obb.GetPoint(1F, -1F, 1F);
            result[6] = obb.GetPoint(1F, 1F, -1F);
            result[7] = obb.GetPoint(1F, 1F, 1F);

            return result;
        }

        public static void DrawCube(BasePlayer player, float duration, Color color, Vector3 center, Vector3[] eightPoints, bool alsoDrawCenterDot = true, bool centerDotIsBig = false)
        {
            bool setAdmin = PrePlayerDraw(player);

            DrawCubeCommon(player, duration, color, center, eightPoints, alsoDrawCenterDot, centerDotIsBig);

            PostPlayerDraw(player, setAdmin);
        }


        public static void DrawCubeCommon(BasePlayer player, float duration, Color color, Vector3 center, Vector3[] eightPoints, bool alsoDrawCenterDot = true, bool centerDotIsBig = false)
        {
            if (color.Equals(Color.clear))
            {
                return;
            }

            if (eightPoints == null)
            {
                return;
            }

            if (eightPoints.Length != 8)
            {
                return;
            }

            //draw 4 floor lines
            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_LEFT_DOWN_BACK], eightPoints[CUBE_CORNER_RIGHT_DOWN_BACK]);
            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_LEFT_DOWN_BACK], eightPoints[CUBE_CORNER_LEFT_DOWN_FORWARD]);
            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_RIGHT_DOWN_FORWARD], eightPoints[CUBE_CORNER_RIGHT_DOWN_BACK]);
            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_RIGHT_DOWN_FORWARD], eightPoints[CUBE_CORNER_LEFT_DOWN_FORWARD]);

            //draw 4 ceiling lines

            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_LEFT_UP_BACK], eightPoints[CUBE_CORNER_RIGHT_UP_BACK]);
            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_LEFT_UP_BACK], eightPoints[CUBE_CORNER_LEFT_UP_FORWARD]);
            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_RIGHT_UP_FORWARD], eightPoints[CUBE_CORNER_RIGHT_UP_BACK]);
            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_RIGHT_UP_FORWARD], eightPoints[CUBE_CORNER_LEFT_UP_FORWARD]);

            //draw 4 columns

            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_LEFT_DOWN_BACK], eightPoints[CUBE_CORNER_LEFT_UP_BACK]);
            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_RIGHT_DOWN_BACK], eightPoints[CUBE_CORNER_RIGHT_UP_BACK]);
            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_LEFT_DOWN_FORWARD], eightPoints[CUBE_CORNER_LEFT_UP_FORWARD]);
            DrawLineCommon(player, duration, color, eightPoints[CUBE_CORNER_RIGHT_DOWN_FORWARD], eightPoints[CUBE_CORNER_RIGHT_UP_FORWARD]);

            if (alsoDrawCenterDot)
            {
                DrawTextCommon(player, duration, color, center, centerDotIsBig ? TEXT_BULLET_BIG : TEXT_BULLET_SMALL);
            }
        }


        public static void DrawLineCommon(BasePlayer player, float duration, Color color, Vector3 point1, Vector3 point2)
        {
            if (color.Equals(Color.clear))
            {
                return;
            }
            player.SendConsoleCommand("ddraw.line", duration, color, point1, point2);
        }

        #endregion

        #region CASTING NON ALLOC
        public static class CastingNonAlloc
        {
            public static Collider[] ReusableColBuffer;
            public static RaycastHit[] ReusableRaycastBuffer;

            public static int ReusableColCount = 0;
            public static int ReusableHitCount = 0;

            public static void Init()
            {
                ReusableColBuffer = new Collider[2048];
                ReusableRaycastBuffer = new RaycastHit[2048];
                ReusableColCount = 0;
                ReusableHitCount = 0;
            }

            public static void Unload()
            {
                ReusableColBuffer = null;
                ReusableRaycastBuffer = null;
                ReusableColCount = 0;
                ReusableHitCount = 0;
            }

            private static void BufferForVisEntitiesUniqueClear<T>(ListHashSet<T> list)
            {
                list.Clear();
            }

            public static void ClearBufferExcess(int num)
            {
                for (int i = ReusableColCount; i < num; i++)
                {
                    ReusableColBuffer[i] = null;
                }

                if (ReusableColCount >= ReusableColBuffer.Length)
                {
                    Debug.LogWarning("Vis query is exceeding collider buffer length.");
                    ReusableColCount = ReusableColBuffer.Length;
                }
            }

            private static void BufferForVisEntitiesUniqueWithinOBB<T>(OBB bounds, ListHashSet<T> listToClear, int layerMask = -1, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
            {
                GetCollidersWithinOBB(bounds, layerMask, triggerInteraction);
                BufferForVisEntitiesUniqueClear(listToClear);
            }

            public static void RaycastNonAlloc(Vector3 origin, Vector3 direction, float maxDistance, int layerMask = -1, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            {
                ReusableHitCount = Physics.RaycastNonAlloc(origin, direction, ReusableRaycastBuffer, maxDistance, layerMask, queryTriggerInteraction);
            }

            public static void SpherecastNonAlloc(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask = -1, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            {
                ReusableHitCount = Physics.SphereCastNonAlloc(origin, radius, direction, ReusableRaycastBuffer, maxDistance, layerMask, queryTriggerInteraction);
            }

            public static void GetCollidersWithinOBB(OBB bounds, int layerMask = -1, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
            {
                int num = ReusableColCount;
                ReusableColCount = Physics.OverlapBoxNonAlloc(bounds.position, bounds.extents, ReusableColBuffer, bounds.rotation, layerMask, triggerInteraction);
                ClearBufferExcess(num);
            }

            private static void BufferForVisEntitiesUniqueWithinRadius<T>(Vector3 position, float radius, ListHashSet<T> listPrimaryToClear, int layerMask = -1, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore, ListHashSet<T> listSecondaryToClear = null)
            {
                GetCollidersWithinRadius(position, radius, layerMask, triggerInteraction);
                listSecondaryToClear?.Clear();
                BufferForVisEntitiesUniqueClear(listPrimaryToClear);
            }

            public static void GetCollidersWithinRadius(Vector3 position, float radius, int layerMask, QueryTriggerInteraction triggerInteraction)
            {
                int num = ReusableColCount;
                ReusableColCount = Physics.OverlapSphereNonAlloc(position, radius, ReusableColBuffer, layerMask, triggerInteraction);
                ClearBufferExcess(num);
            }


            public static void ProcessColliderBufferInto<T>(ListHashSet<T> listPrimary, Func<T, bool> conditionForAddingPrimary = null, ListHashSet<T> listSecondary = null, Func<T, bool> conditionForAddingSecondary = null, bool stopAfterFirstFound = false) where T : BaseEntity
            {
                bool conditionForAddingPrimaryIsNotNullNull = conditionForAddingPrimary != null;
                bool conditionForAddingSecondaryIsNotNull = conditionForAddingSecondary != null;
                bool secondaryListIsNull = listSecondary == null;

                for (int i = 0; i < ReusableColCount; i++)
                {
                    Collider collider = ReusableColBuffer[i];

                    if (!(collider == null) && collider.enabled)
                    {
                        T val = GameObjectEx.ToBaseEntity(collider) as T;

                        if (val == null)
                        {
                            continue;
                        }

                        if (listPrimary.Contains(val))
                        {
                            //if it's in the primary list already, means we checked it for both
                            //primary and secondary conditions
                            continue;
                        }

                        if (conditionForAddingPrimaryIsNotNullNull)
                        {
                            if (!conditionForAddingPrimary(val))
                            {
                                continue;
                            }
                        }

                        listPrimary.Add(val);

                        if (secondaryListIsNull)
                        {
                            continue;
                        }

                        if (conditionForAddingSecondaryIsNotNull)
                        {
                            if (!conditionForAddingSecondary(val))
                            {
                                continue;
                            }
                        }

                        listSecondary.Add(val);

                        if (stopAfterFirstFound)
                        {
                            return;
                        }

                    }
                }
            }

            public static void VisEntitiesUniqueWithinRadius<T>(Vector3 position, float radius, ListHashSet<T> list, int layerMask, QueryTriggerInteraction interaction = QueryTriggerInteraction.Ignore, Func<T, bool> conditionForAdding = null) where T : BaseEntity
            {
                BufferForVisEntitiesUniqueWithinRadius(position, radius, list, layerMask, interaction);
                ProcessColliderBufferInto(list, conditionForAdding);
            }

            public static void VisEntitiesUniqueWithinRadiusPrimarySecondary<T>(Vector3 position, float radius, ListHashSet<T> listPrimary, int layerMask, QueryTriggerInteraction interaction, Func<T, bool> conditionForAddingPrimary, ListHashSet<T> listSecondary, Func<T, bool> conditionForAddingSecondary) where T : BaseEntity
            {
                BufferForVisEntitiesUniqueWithinRadius(position, radius, listPrimary, layerMask, interaction, listSecondary);
                ProcessColliderBufferInto(listPrimary, conditionForAddingPrimary, listSecondary, conditionForAddingSecondary);
            }

            public static void VisEntitiesUniqueWithinOBB<T>(OBB obb, ListHashSet<T> list, int layerMask, QueryTriggerInteraction interaction = QueryTriggerInteraction.Ignore, Func<T, bool> conditionForAdding = null) where T : BaseEntity
            {
                BufferForVisEntitiesUniqueWithinOBB(obb, list, layerMask, interaction);
                ProcessColliderBufferInto(list, conditionForAdding);
            }
        }
        #endregion

        #region HELPERS
        public static string DumpObjectFields<T>(T obj)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var fields = typeof(T).GetFields(flags);

            var sb = new StringBuilder();
            foreach (var field in fields)
            {
                // Handling for static fields to avoid passing instance
                var value = field.IsStatic ? field.GetValue(null) : field.GetValue(obj);
                sb.Append(field.Name);
                sb.Append(" = ");
                sb.Append(value);
                sb.AppendLine();
            }

            // Removes the last comma and space if sb is not empty
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 2, 2);
            }

            return sb.ToString();
        }

        public static NEW TryComponentReplacement<OLD, NEW>(GameObject go, bool onlyOneNewAllowed = true, bool printDebug = false) where OLD : Component where NEW : Component
        {
            var typeofOld = typeof(OLD);
            var typeofNew = typeof(NEW);

            if (go == null)
            {
                Instance.PrintError($"{MSG(MSG_TRY_COMPO_REPLACE_ERROR, null, typeofOld, typeofNew)} {MSG(MSG_TRY_COMPO_REPLACE_ERROR_GAMEOBJECT_NULL)}");
                return null;
            }

            var oldComponent = go.GetComponent<OLD>();
            if (oldComponent == null)
            {
                Instance.PrintError($"{MSG(MSG_TRY_COMPO_REPLACE_ERROR, null, typeofOld, typeofNew)} {MSG(MSG_TRY_COMPO_REPLACE_ERROR_NO_OLD_COMPO_ATTACHED, null, typeofOld)}");
                return null;
            }

            if (onlyOneNewAllowed)
            {
                var maybeOldComponentOfNewType = go.GetComponent<NEW>();
                if (maybeOldComponentOfNewType != null)
                {
                    Instance.PrintError($"{MSG(MSG_TRY_COMPO_REPLACE_ERROR, null, typeofOld, typeofNew)} {MSG(MSG_TRY_COMPO_REPLACE_ERROR_ALREADY_HAS_NEW_COMPO, null, typeofNew)}");
                    return null;
                }
            }

            var newComponent = go.AddComponent<NEW>();

            bool newIsSubclassOfOld = typeofNew.IsSubclassOf(typeofOld);

            if (!newIsSubclassOfOld)
            {
                UnityEngine.Object.DestroyImmediate(oldComponent);

                return newComponent;
            }

            StringBuilder buildDebug = null;

            if (printDebug)
            {
                buildDebug = new StringBuilder(MSG(MSG_TRY_COMPO_REPLACE_DEBUG_HEADER, null, typeofNew, typeofOld));
            }

            //naughty reflection

            var fields = typeofOld.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var field in fields)
            {
                var value = field.GetValue(oldComponent);
                field.SetValue(newComponent, value);

                if (printDebug)
                {
                    buildDebug.AppendLine(MSG(MSG_TRY_COMPO_REPLACE_DEBUG_FIELD_INFO, null, field.Name, value));
                }
            }

            if (printDebug)
            {
                Instance.PrintWarning(buildDebug.ToString());
            }

            UnityEngine.Object.DestroyImmediate(oldComponent);

            return newComponent;
        }


        public static string GetBackupName(string fileName, int i) => string.Format(FORMAT_FILENAME, fileName, i);

        public static void PlayEffect(string effect, Vector3 position, Vector3 forward, BasePlayer player = null)
        {
            ReusableEffect.Init(Effect.Type.Generic, position, Vector3.up);
            ReusableEffect.pooledString = effect;
            if (player != null)
            {
                EffectNetwork.Send(ReusableEffect, player.net.connection);
            }
            else
            {
                EffectNetwork.Send(ReusableEffect);
            }
        }

        public static List<BaseEntity> GetOwnerThingCustomSaveList(ItemContainer container)
        {
            BaseEntity ownerThing = GetOwnerThing(container);

            if (ownerThing == null)
            {
                return null;
            }

            return CustomPrefabs.TryGetEntityCustomSaveList(ownerThing, CustomPrefabs.SaveListEntityType.VanillaAndCustom);

        }

        public static BaseEntity GetOwnerThing(ItemContainer container)
        {
            if (container == null)
            {
                return null;
            }

            if (container.entityOwner != null)
            {
                return container.entityOwner;
            }
            else
                return container.playerOwner ?? null;
        }

        public static string SanitizedFullPrefabName(string prefabName)
        {
            if (!prefabName.StartsWith(PREFAB_PREFIX))
            {
                if (prefabName.StartsWith("/"))
                {
                    prefabName = prefabName.Substring(1);
                }

                prefabName = $"{PREFAB_PREFIX}{prefabName}.prefab";
            }

            prefabName = Regex.Replace(prefabName, "[^0-9|a-zA-Z|.|-|_|\\/]", "");

            return prefabName.ToLower();
        }

        public static void DestroyClientsideForAll(BaseNetworkable entity, bool networkUpdateAfterwards = true) => DestroyClientsideForSendInfo(entity, new SendInfo(Net.sv.connections), networkUpdateAfterwards);

        public static void DestroyClientsideForSendInfo(BaseNetworkable entity, SendInfo sendInfo, bool networkUpdateAfterwards = true)
        {
            if (entity.net == null)
            {
                return;
            }

            if (Net.sv.IsConnected())
            {
                NetWrite netWrite = Net.sv.StartWrite();
                netWrite.PacketID(Message.Type.EntityDestroy);
                netWrite.EntityID(entity.net.ID);
                netWrite.UInt8(0);
                netWrite.Send(sendInfo);
            }

            if (networkUpdateAfterwards)
            {
                entity.SendNetworkUpdateImmediate();
            }

        }
        #endregion


        #region CUSTOM CONVARS

        public static class CustomConvars
        {
            public static ListHashSet<ConsoleSystem.Command> ConvarsToRegister;
            public static bool ShouldSave = true;

            public static bool VerboseLogging = true;
            public static string GetConfigFilePath => ConVar.Server.GetServerFolder("cfg") + "/customentities.cfg";

            public static void SaveToConfigStringFileIfShouldSave()
            {
                if (!ShouldSave)
                {
                    return;
                }

                StringBuilder textBuilder = new StringBuilder();

                foreach (var item in ConvarsToRegister)
                {
                    textBuilder.Append(item.FullName);
                    textBuilder.Append(' ');
                    textBuilder.Append(item.String.QuoteSafe());
                    textBuilder.Append(Environment.NewLine);
                }

                File.WriteAllText(GetConfigFilePath, textBuilder.ToString());
            }

            public static ListHashSet<ConsoleSystem.Command> GetConvarsToRegister()
            {
                return new ListHashSet<ConsoleSystem.Command>
                {
                    new ConsoleSystem.Command
                    {
                        Name = CONVAR_VERBOSE_LOGGING,
                        Parent = PREFIX_CUSTOMENTITIES,
                        FullName = PREFIX_CUSTOMENTITIES + "." + CONVAR_VERBOSE_LOGGING,
                        ServerAdmin = true,
                        Saved = false,
                        Variable = true,
                        GetOveride = () => VerboseLogging.ToString(),
                        SetOveride = delegate(string str)
                        {
                            VerboseLogging = str.ToBool();
                            SaveToConfigStringFileIfShouldSave();
                        }
                    },
                };
            }

            public static void Init()
            {

                ConvarsToRegister = GetConvarsToRegister();

                var consoleCommandsServer = ConsoleSystem.Index.Server.Dict;

                var consoleCommandsAll = ConsoleSystem.Index.All;

                foreach (var convar in ConvarsToRegister)
                {
                    //add to server dict stuff
                    consoleCommandsServer[convar.FullName] = convar;
                }

                //this registers all
                ConsoleSystem.Index.All = consoleCommandsAll.Concat(ConvarsToRegister).ToArray();

                //and now restore from config file, if an entry exists

                var configFilePath = GetConfigFilePath;

                ShouldSave = true;

                if (!File.Exists(configFilePath))
                {
                    SaveToConfigStringFileIfShouldSave();
                    return;
                }

                var configString = File.ReadAllText(configFilePath);

                string[] allOptions = configString.Split(new char[1]
                {
                    '\n'
                }, StringSplitOptions.RemoveEmptyEntries);


                int foundUseful = 0;
                int foundUseless = 0;

                //when we're running, we dont wanna save, since we're only reading here
                ShouldSave = false;

                for (int i = 0; i < allOptions.Length; i++)
                {
                    string optionText = allOptions[i].Trim();

                    //ignore empties and comments
                    if (string.IsNullOrWhiteSpace(optionText) || optionText[0] == '#')
                    {
                        goto FoundUseless;
                    }

                    var split = optionText.Split(' ');

                    if (split.Length < 2)
                    {
                        goto FoundUseless;
                    }

                    var fullName = split[0];

                    //ignore stuff outside of the scope

                    if (!ExistsInConvarsToRegister(fullName))
                    {
                        goto FoundUseless;
                    }

                    ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), optionText);
                    foundUseful++;
                    continue;

                FoundUseless:
                    foundUseless++;
                }

                ShouldSave = true;

                if (foundUseful == 0 || foundUseless > 0)
                {
                    //re-save
                    SaveToConfigStringFileIfShouldSave();
                }

                ConsoleSystem.HasChanges = false;

            }

            public static bool ExistsInConvarsToRegister(string fullName)
            {
                bool found = false;

                foreach (var c in ConvarsToRegister)
                {
                    if (c.FullName == fullName)
                    {
                        found = true;
                        break;
                    }
                }

                return found;
            }

            public static void Unload()
            {
                var consoleCommandsAllList = ConsoleSystem.Index.All.ToList();
                var consoleCommandsServer = ConsoleSystem.Index.Server.Dict;

                foreach (var convar in ConvarsToRegister)
                {
                    //remove from server dict stuff
                    var fullName = convar.FullName;
                    consoleCommandsServer.Remove(fullName);
                }

                //this unregisters all

                for (var i = consoleCommandsAllList.Count - 1; i >= 0; i--)
                {
                    var convar = consoleCommandsAllList[i];
                    var fullName = convar.FullName;

                    if (!ExistsInConvarsToRegister(fullName))
                    {
                        continue;
                    }

                    //found, so remove. it's safe to do so cause we're iterating backwards
                    consoleCommandsAllList.RemoveAt(i);
                }

                ConsoleSystem.Index.All = consoleCommandsAllList.ToArray();

                ConvarsToRegister = null;
            }
        }



        #endregion
    }
}
