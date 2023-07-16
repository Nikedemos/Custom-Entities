using Facepunch;
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
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Entities", "Nikedemos", "1.0.1")]
    [Description("A robust framework for registering, spawning, loading and saving entity prefabs")]

    public class CustomEntities : RustPlugin
    {
        #region CONST
        public const string FORMAT_FILENAME = "{0}.{1}";

        public const string PREFAB_PREFIX = "assets/custom/";

        public const string PREFAB_SPHERE = "assets/prefabs/visualization/sphere.prefab";

        public const string PREFAB_FX_IMPACT_METAL_BLUNT = "assets/bundled/prefabs/fx/impacts/blunt/metal/metal1.prefab";
        public const string PREFAB_FX_IMPACT_METAL_SLASH = "assets/bundled/prefabs/fx/impacts/slash/metal/metal1.prefab";
        public const string PREFAB_FX_IMPACT_METAL_STAB = "assets/bundled/prefabs/fx/impacts/stab/metal/metal1.prefab";
        public const string PREFAB_FX_IMPACT_METAL_BULLET = "assets/bundled/prefabs/fx/impacts/bullet/metal/metal1.prefab";

        public const string PREFAB_FX_IMPACT_METAL_PHYSICAL = "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-hollow-hard.prefab";
        public const string PREFAB_FX_EXPLOSION = "assets/bundled/prefabs/fx/impacts/additive/explosion.prefab";
        public const string PREFAB_FX_FIRE = "assets/bundled/prefabs/fx/impacts/additive/fire.prefab";

        public const string CMD_SPAWN_AT = "spawn_at";
        public const string CMD_PURGE_PREFAB = "purge_prefab";
        public const string CMD_PURGE_PLUGIN = "purge_plugin";

        public const string PERM_ADMIN = "customentities.admin"; //currently only required for the spawn_at command

        #endregion

        #region STATIC
        public static CustomEntities Instance;
        public static bool Unloading = false;
        public static Effect ReusableEffect;

        public static readonly ReadOnlyDictionary<Rust.DamageType, float> DEFAULT_PROTECTION_AMOUNTS = new ReadOnlyDictionary<Rust.DamageType, float>(new Dictionary<Rust.DamageType, float>
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

        public const string MSG_PREFAB_REGISTERING = nameof(MSG_PREFAB_REGISTERING);
        public const string MSG_PREFAB_REGISTRATION_EXCEPTION = nameof(MSG_PREFAB_REGISTRATION_EXCEPTION);
        public const string MSG_BUNDLE_UNREGISTRATION_REMOVED_VANILLA = nameof(MSG_BUNDLE_UNREGISTRATION_REMOVED_VANILLA);
        public const string MSG_BUNDLE_UNREGISTRATION_REMOVED_CUSTOM = nameof(MSG_BUNDLE_UNREGISTRATION_REMOVED_CUSTOM);
        public const string MSG_SAVING_SAVEFILES = nameof(MSG_SAVING_SAVEFILES);
        public const string MSG_SAVED_ENTITIES = nameof(MSG_SAVED_ENTITIES);
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

        private static readonly Dictionary<string, string> LangMessages = new Dictionary<string, string>
        {
            [MSG_PREFAB_REGISTERING] = "Registering entity {0} : {1} as prefab \"{2}\"...",
            [MSG_PREFAB_REGISTRATION_EXCEPTION] = "Exception while trying to register prefab: {0}\n{1}",
            [MSG_BUNDLE_UNREGISTRATION_REMOVED_VANILLA] = "Removed {0} instances of various vanilla entities handled by the savefile from world.",
            [MSG_BUNDLE_UNREGISTRATION_REMOVED_CUSTOM] = "Removed {0} instances of a custom prefab \"{1}\" from world.",
            [MSG_SAVING_SAVEFILES] = "Saving {0} binary savefiles...",
            [MSG_SAVED_ENTITIES] = "INFO: Saved {0} out of {1} entities ({2} custom, {3} vanilla) to \"{4}\"",
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

        private void CommandPurgePlugin(IPlayer iplayer, string command, string[] args)
        {
            if (args.Length == 0)
            {
                iplayer.Reply(MSG(MSG_CMD_PROVIDE_PLUGIN_NAME, iplayer.Id));
                return;
            }

            string pluginName = args[0].ToLower();

            BinaryData.PlayerRequestedPurge(iplayer, pluginName);
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
            //naughty reflection
            private static FieldInfo _entityPrefabNameFieldInfo = null;

            private static GameObjectRef _emptyImpactGameObjectRef = null;
            private static Dictionary<string, GameObject> _prefabsPreProcessedCustom = null;

            private static Dictionary<GameObject, BaseEntity> _preProcessedGoToEntityCache = null;

            private static ListHashSet<CustomPrefabRecipe> _cachedRecipes = null;

            private static Dictionary<Plugin, BinaryData> _pluginToBinaryData = null;

            private static List<string> _gameManifestEntityList = null;            //this is not vanilla, just for building the vanilla manifest array

            private static Dictionary<ulong, List<BaseEntity>> _vanillaEntityToCustomSaveList = null;

            internal static void Init()
            {
                _emptyImpactGameObjectRef = new GameObjectRef();
                _prefabsPreProcessedCustom = new Dictionary<string, GameObject>();
                _preProcessedGoToEntityCache = new Dictionary<GameObject, BaseEntity>();

                //naughty reflection
                _entityPrefabNameFieldInfo = typeof(BaseNetworkable).GetField("_prefabName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                _gameManifestEntityList = new List<string>();
                _cachedRecipes = new ListHashSet<CustomPrefabRecipe>();
                _pluginToBinaryData = new Dictionary<Plugin, BinaryData>();

                _vanillaEntityToCustomSaveList = new Dictionary<ulong, List<BaseEntity>>();
            }

            internal static void Unload()
            {
                _emptyImpactGameObjectRef = null;
                _prefabsPreProcessedCustom = null;
                _preProcessedGoToEntityCache = null;

                //naughty reflection
                _entityPrefabNameFieldInfo = null;

                _gameManifestEntityList = null;
                _cachedRecipes = null;
                _pluginToBinaryData = null;

                _vanillaEntityToCustomSaveList = null;
            }

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

            private static void ForEachItemEntityRecursively(Item item, Action<BaseEntity> entityAction) //so the action will be either move to custom, or move to vanilla... and keep passing the action
            {
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

            public static void EnsureMovedToCustomSaveList(BaseEntity vanillaEntity, List<BaseEntity> targetList)
            {
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

            public static BaseEntity TryGetPreprocessedPrototypeFromVanilla(string prefabName) => TryGetPreprocessedPrototypeFrom(GameManager.server.preProcessed.prefabList, prefabName);
            public static BaseEntity TryGetPreprocessedPrototypeFromCustom(string prefabName) => TryGetPreprocessedPrototypeFrom(_prefabsPreProcessedCustom, prefabName);

            public static bool RegisterAndLoadBundle(CustomPrefabBundle bundle)
            {
                //step 1: ensure the data...
                BinaryData binaryData = BinaryData.SummonBinaryData(bundle.Owner);

                binaryData.PrefabBundle = bundle;

                bool allGood = true;
                //step 2: register prefabs from recipes...
                for (int i = 0; i < binaryData.PrefabBundle.Recipes.Length; i++)
                {
                    CustomPrefabRecipe recipe = binaryData.PrefabBundle.Recipes[i];

                    Instance.PrintWarning(MSG(MSG_PREFAB_REGISTERING, null, recipe.EntityType.Name, (recipe.BaseCombat == null ? nameof(BaseEntity) : nameof(BaseCombatEntity)), recipe.FullPrefabName));

                    try
                    {
                        RegisterPrefabInternal(recipe, binaryData);
                    }
                    catch (Exception e)
                    {
                        Instance.PrintError(MSG(MSG_PREFAB_REGISTRATION_EXCEPTION, null, e.Message, e.StackTrace));
                        allGood = false;
                    }

                }

                //step 3: now that everything is registered, load.
                //now they should all know their appropriate save lists.

                binaryData.Load();

                return allGood;
            }

            public static bool SaveAndUnregisterBundle(CustomPrefabBundle cookbook)
            {
                //and this should be the exact opposite.
                //first, save...

                //this will ensure the data wrapper, just in case it wasn't before
                BinaryData binaryData = BinaryData.SummonBinaryData(cookbook.Owner);

                binaryData.Save(); //this will save everything and then kill everything

                bool allGood = true;

                for (int i = 0; i < cookbook.Recipes.Length; i++)
                {
                    if (!UnregisterPrefabInternal(cookbook.Recipes[i]))
                    {
                        allGood = false;
                    }
                }

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
                    Instance.PrintWarning(MSG(MSG_BUNDLE_UNREGISTRATION_REMOVED_VANILLA, null, countKilled));
                }

                return allGood;
            }


            //and the rest are private methods

            private static bool RegisterPrefabInternal(CustomPrefabRecipe recipe, BinaryData data)
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
                _entityPrefabNameFieldInfo.SetValue(newEntity, recipe.FullPrefabName);

                ICustomEntity asInterface = (newEntity as ICustomEntity);

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

            private static bool UnregisterPrefabInternal(CustomPrefabRecipe recipe)
            {
                GameObject go;

                if (!_prefabsPreProcessedCustom.TryGetValue(recipe.FullPrefabName, out go))
                {
                    return false;
                }

                RemoveFromPreProcessed(recipe.FullPrefabName);
                RemoveFromGameManifest(recipe.FullPrefabName, recipe.EnableInSpawnCommand);
                RemoveFromStringPool(recipe.FullPrefabName);

                //kill the prototype                

                BaseEntity prototypeEntity = go.GetComponent<BaseEntity>();                

                (prototypeEntity as ICustomEntity).OnCustomPrefabPrototypeEntityUnregistered();

                if (recipe.BaseCombat != null)
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

                prototypeEntity.Kill(BaseNetworkable.DestroyMode.None);

                ICustomEntity[] iterateOver = BaseNetworkable.serverEntities.OfType<ICustomEntity>().ToArray();

                int countKilled = 0;

                for (int i = 0; i < iterateOver.Length; i++)
                {
                    ICustomEntity entity = iterateOver[i];

                    if (entity == null)
                    {
                        continue;
                    }

                    if (entity.IsDestroyed)
                    {
                        continue;
                    }

                    if (entity.PrefabName != recipe.FullPrefabName)
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
                    Instance.PrintWarning(MSG(MSG_BUNDLE_UNREGISTRATION_REMOVED_CUSTOM, null, countKilled, recipe.FullPrefabName));
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
                GameManifest.pathToGuid.Add(fullPrefabName, fullPrefabName);
                GameManifest.guidToPath.Add(fullPrefabName, fullPrefabName);
                GameManifest.guidToObject.Add(fullPrefabName, newGo);

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

            private readonly string _fullFileDirectory;

            private readonly Plugin _ownerPlugin;

            private readonly string _fullFilePath;


            internal List<BaseEntity> CustomEntitySaveList = new List<BaseEntity>();

            internal CustomPrefabBundle PrefabBundle = null;

            internal static void Init()
            {
                _cacheByOwner = new Dictionary<Plugin, BinaryData>();
                _prefabNamesToKill = new ListHashSet<string>();
            }

            internal static void Unload()
            {
                _cacheByOwner = null;
                _prefabNamesToKill = null;
            }

            internal static void SaveAll()
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

            internal static void PlayerRequestedPurge(IPlayer iplayer, string partialPluginNameLowercase)
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

                //k what now.
                //just get the full prefab names from the bundle?

                int countKilled = 0;

                _prefabNamesToKill.Clear();

                for (int i = 0; i < findMatchingEntry.PrefabBundle.Recipes.Length; i++)
                {
                    CustomPrefabRecipe recipe = findMatchingEntry.PrefabBundle.Recipes[i];

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

            internal static BinaryData SummonBinaryData(Plugin maybeOwnerPlugin)
            {
                BinaryData resultData;

                if (!_cacheByOwner.TryGetValue(maybeOwnerPlugin, out resultData))
                {
                    //we need to summon it and add to cache by path

                    resultData = new BinaryData(maybeOwnerPlugin);

                    _cacheByOwner.Add(maybeOwnerPlugin, resultData);
                }

                return resultData;
            }

            internal BinaryData(Plugin ownerPlugin)
            {
                _ownerPlugin = ownerPlugin;

                _fullFileDirectory = Path.Combine(Interface.Oxide.DataFileSystem.Directory, Instance.Name);

                _fullFilePath = Path.Combine(_fullFileDirectory, $"{ownerPlugin.Name}.sav");

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

            internal void ShiftSaveBackups()
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

            internal void Save()
            {
                int entityAmount = CustomEntitySaveList.Count;
                if (entityAmount == 0)
                {
                    return;
                }

                int countVanillaAndCustom = 0;

                int countVanilla = 0;
                int countCustom = 0;

                try
                {
                    EnsureFileDirectory();

                    ShiftSaveBackups();

                    using (FileStream fileStream = new FileStream(_fullFilePath, FileMode.Create))
                    {
                        using (BinaryWriter finalWriter = new BinaryWriter(fileStream))
                        {
                            //write the community instance net id...
                            finalWriter.Write(CommunityEntity.ServerInstance.net.ID.Value);
                            //the amount of entities...
                            finalWriter.Write(entityAmount);
                            //and optionally...
                            for (int i = 0; i < entityAmount; i++)
                            {
                                BaseEntity entity = CustomEntitySaveList[i];
                                MemoryStream vanillaMemoryStream = entity.GetSaveCache();
                                long vanillaMemoryStreamLength = vanillaMemoryStream.Length;
                                //first, vanilla stuff, that always happens...
                                finalWriter.Write((uint)vanillaMemoryStreamLength); //4 bytes
                                finalWriter.Write(vanillaMemoryStream.GetBuffer(), 0, (int)vanillaMemoryStreamLength);
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
                                            finalWriter.Write((uint)customMemoryStreamLength); //4 bytes
                                            finalWriter.Write(customMemoryStream.GetBuffer(), 0, (int)customMemoryStreamLength);
                                        }
                                    }

                                    countCustom++;
                                }
                                else
                                {
                                    countVanilla++;
                                }


                                countVanillaAndCustom++;
                            }
                        }
                    }


                    Instance.PrintWarning(MSG(MSG_SAVED_ENTITIES, null, countVanillaAndCustom, entityAmount, countCustom, countVanilla, _fullFilePath));


                }
                catch (Exception e)
                {
                    Instance.PrintError(MSG(MSG_SAVING_DATAFILE_EXCEPTION, null, e.GetType(), _fullFilePath, e.Message, e.StackTrace));
                }

            }

            internal void Load()
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
                                            //first we read the vanilla bytes...

                                            int vanillaLength = reader.ReadInt32();

                                            ProtoBuf.Entity entData = ProtoBuf.Entity.DeserializeLength(readStream, vanillaLength);

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

                                        if (asInterface != null)
                                        {
                                            byte[] maybeSomeExtraData;

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
            public CustomPrefabRecipe[] Recipes;

            public CustomPrefabBundle(Plugin owner, params CustomPrefabRecipe[] recipes)
            {
                Owner = owner;
                Recipes = recipes;
            }
        }

        public struct CustomPrefabRecipe : IEquatable<CustomPrefabRecipe>
        {
            public readonly string ShortPrefabName;
            public readonly string FullPrefabName;
            public readonly Rust.Layer Layer;

            public readonly bool EnableInSpawnCommand;

            public readonly Type EntityType;

            public readonly CustomPrefabBaseCombat BaseCombat;

            public CustomPrefabRecipe(string shortName, Type entityType, Rust.Layer layer = Layer.Default, CustomPrefabBaseCombat baseCombat = null, bool enableInSpawnCommand = true)
            {
                ShortPrefabName = shortName;
                FullPrefabName = SanitizedFullPrefabName(shortName);
                EntityType = entityType;
                Layer = layer;
                BaseCombat = baseCombat;
                EnableInSpawnCommand = enableInSpawnCommand;
            }

            public static bool operator ==(CustomPrefabRecipe lhs, CustomPrefabRecipe rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(CustomPrefabRecipe lhs, CustomPrefabRecipe rhs)
            {
                return !lhs.Equals(rhs);
            }

            public bool Equals(CustomPrefabRecipe other)
            {
                return other.ShortPrefabName == ShortPrefabName;
            }

            public override int GetHashCode()
            {
                return ShortPrefabName.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                CustomPrefabRecipe recipe = (CustomPrefabRecipe)obj;

                if (recipe != default(CustomPrefabRecipe))
                {
                    return Equals(recipe);
                }

                return base.Equals(obj);
            }

            public override string ToString()
            {
                return ShortPrefabName;
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
            bool IsDestroyed { get;}

            bool HasDefaultInventory { get; }

            int DefaultInventoryCapacity { get; }

            void OnDefaultInventoryPreAnnihilation();

            void OnDefaultInventoryFirstCreated();

            bool DefaultInventoryItemFilter(Item item, int targetSlot);

            void OnItemAddedOrRemoved(Item item, bool added);

            void OnInventoryDirty();

            ItemContainer DefaultInventory { get; set; }
            string PrefabName { get; }

            bool EnableSavingToDiskByDefault { get; }

            string DefaultClientsideFullPrefabName();
            void OnCustomPrefabPrototypeEntityRegistered();
            void OnCustomPrefabPrototypeEntityUnregistered();

            void OnParentChanging(BaseEntity oldParent,  BaseEntity newParent);

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

            void OnEntitySaveForNetwork(BaseNetworkable.SaveInfo info);
        }

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

                    DestroyClientsideForAll(_ownerEntity, true);
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

            internal void OnParentChanging(BaseEntity oldParent, BaseEntity newParent)
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

            internal CustomHandler(BaseEntity ownerEntity, string defaultClientsidePrefabName, BaseEntity prototypeEntity)
            {
                _ownerEntity = ownerEntity;

                _ownerEntity.enableSaving = false; //disable vanilla saving, always

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

            internal void PreServerLoad()
            {
                if (_ownerEntityAsInterface.HasDefaultInventory)
                {
                    _ownerEntityAsInterface.DefaultInventory = CreateInventory(_ownerEntity, false, _ownerEntityAsInterface.DefaultInventoryCapacity);              
                }
            }

            internal void ServerInit()
            {
                EnableSavingToDisk = _ownerEntityAsInterface.EnableSavingToDiskByDefault; //

                if (_ownerEntityAsInterface.HasDefaultInventory)
                {
                    if (_ownerEntityAsInterface.DefaultInventory == null)
                    {
                        _ownerEntityAsInterface.DefaultInventory = CreateInventory(_ownerEntity, true, _ownerEntityAsInterface.DefaultInventoryCapacity);
                        _ownerEntityAsInterface.OnDefaultInventoryFirstCreated();
                    }
                }
            }

            internal void Save(BaseNetworkable.SaveInfo info)
            {
                if (info.forDisk)
                {
                    if (_ownerEntityAsInterface.HasDefaultInventory)
                    {
                        if (_ownerEntityAsInterface.DefaultInventory != null)
                        {
                            info.msg.storageBox = Facepunch.Pool.Get<ProtoBuf.StorageBox>();
                            info.msg.storageBox.contents = _ownerEntityAsInterface.DefaultInventory.Save();
                        }
                        else
                        {
                            Debug.LogWarning("Assigned storage container is null!");
                        }
                    }

                    return;
                }

                //for networking? Then pretend you're something else, PREFERABLY SOMETHING THAT EXISTS
                info.msg.baseNetworkable.prefabID = ClientsidePrefabID;

                _ownerEntityAsInterface.OnEntitySaveForNetwork(info);
            }

            internal void Load(BaseNetworkable.LoadInfo info)
            {
                if (_ownerEntityAsInterface.HasDefaultInventory)
                {
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
            }

            internal void SaveExtra(Stream stream, BinaryWriter writer)
            {
                writer.Write(_clientsidePrefabID);

                _ownerEntityAsInterface.SaveExtra(stream, writer);
            }

            internal void LoadExtra(Stream stream, BinaryReader reader)
            {
                ClientsidePrefabID = reader.ReadUInt32();

                _ownerEntityAsInterface.LoadExtra(stream, reader);
            }


            internal void DestroyShared()
            {
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
                newInventory.SetOnlyAllowedItems(null, null);
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
                _ownerEntityAsInterface.OnInventoryDirty();
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
            public virtual bool DefaultInventoryItemFilter(Item item, int targetSlot) => true;
            public virtual void OnInventoryDirty()
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

            public override void PreServerLoad()
            {
                base.PreServerLoad();
                Handler?.PreServerLoad();
            }


            public virtual void OnEntitySaveForNetwork(BaseNetworkable.SaveInfo info)
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
            public virtual bool DefaultInventoryItemFilter(Item item, int targetSlot) => true;

            public virtual void OnInventoryDirty()
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

            public virtual void OnEntitySaveForNetwork(BaseNetworkable.SaveInfo info)
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
                            ClientRPCPlayerAndSpectators(null, info.Initiator as BasePlayer, "HitNotify", false); //this must be false
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

            public static void Init()
            {
                ReusableColBuffer = new Collider[2048];
                ReusableRaycastBuffer = new RaycastHit[2048];
                ReusableColCount = 0;
            }

            public static void Unload()
            {
                ReusableColBuffer = null;
                ReusableRaycastBuffer = null;
                ReusableColCount = 0;
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
                    UnityEngine.Debug.LogWarning("Vis query is exceeding collider buffer length.");
                    ReusableColCount = ReusableColBuffer.Length;
                }
            }

            private static void BufferForVisEntitiesUniqueWithinOBB<T>(OBB bounds, ListHashSet<T> listToClear, int layerMask = -1, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
            {
                GetCollidersWithinOBB(bounds, layerMask, triggerInteraction);
                BufferForVisEntitiesUniqueClear(listToClear);
            }

            public static void GetCollidersWithinOBB(OBB bounds, int layerMask = -1, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
            {
                int num = ReusableColCount;
                ReusableColCount = Physics.OverlapBoxNonAlloc(bounds.position, bounds.extents, ReusableColBuffer, bounds.rotation, layerMask, triggerInteraction);
                ClearBufferExcess(num);
            }

            private static void BufferForVisEntitiesUniqueWithinRadius<T>(Vector3 position, float radius, ListHashSet<T> listToClear, int layerMask = -1, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
            {
                GetCollidersWithinRadius(position, radius, layerMask, triggerInteraction);
                BufferForVisEntitiesUniqueClear(listToClear);
            }

            public static void GetCollidersWithinRadius(Vector3 position, float radius, int layerMask, QueryTriggerInteraction triggerInteraction)
            {
                int num = ReusableColCount;
                ReusableColCount = Physics.OverlapSphereNonAlloc(position, radius, ReusableColBuffer, layerMask, triggerInteraction);
                ClearBufferExcess(num);
            }

            public static void ProcessColliderBufferInto<T>(ListHashSet<T> list, Func<T, bool> conditionForAdding = null, bool stopAfterFirstFound = false) where T : BaseEntity
            {
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

                        if (conditionForAdding != null)
                        {
                            if (!conditionForAdding(val))
                            {
                                continue;
                            }
                        }

                        if (!list.Contains(val))
                        {
                            list.Add(val);

                            if (stopAfterFirstFound)
                            {
                                return;
                            }
                        }

                    }
                }
            }

            public static void VisEntitiesUniqueWithinRadius<T>(Vector3 position, float radius, ListHashSet<T> list, int layerMask, QueryTriggerInteraction interaction = QueryTriggerInteraction.Ignore, Func<T, bool> conditionForAdding = null) where T : BaseEntity
            {
                BufferForVisEntitiesUniqueWithinRadius(position, radius, list, layerMask, interaction);
                ProcessColliderBufferInto(list, conditionForAdding);
            }
            public static void VisEntitiesUniqueWithinOBB<T>(OBB obb, ListHashSet<T> list, int layerMask, QueryTriggerInteraction interaction = QueryTriggerInteraction.Ignore, Func<T, bool> conditionForAdding = null) where T : BaseEntity
            {
                BufferForVisEntitiesUniqueWithinOBB(obb, list, layerMask, interaction);
                ProcessColliderBufferInto(list, conditionForAdding);
            }
        }
        #endregion

        #region HELPERS

        public static string GetBackupName(string fileName, int i) => string.Format(FORMAT_FILENAME, fileName, i);

        public static void PlayEffect(string effect, Vector3 position, Vector3 forward, BasePlayer player = null)
        {
            ReusableEffect.Clear();
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
    }
}
