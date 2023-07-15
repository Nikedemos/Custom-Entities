// Requires: CustomEntities

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Entities Example", "Nikedemos", "1.0.0")]
    [Description("Sample dependency for Custom Entities that registers a bundle with some test entities")]
    public class CustomEntitiesExample : RustPlugin
    {
        private static CustomEntitiesExample Instance;
        public static bool Unloading = false;

        #region CONST
        public const string PREFAB_LIGHT_POINT_GREEN = "assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_green.prefab";
        public const string PREFAB_LIGHT_POINT_WARM = "assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_warm.prefab";
        public const string PREFAB_LIGHT_POINT_RED = "assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_red.prefab";
        public const string PREFAB_LIGHT_POINT_COOL = "assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_cool.prefab";

        public const string PREFAB_FX_SELECT = "assets/bundled/prefabs/fx/notice/item.select.fx.prefab";
        public const string PREFAB_FX_DROP_SUCCESS = "assets/bundled/prefabs/fx/notice/loot.drag.dropsuccess.fx.prefab";

        public const string PREFAB_FX_LOCK_CODE_DENIED = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";

        public const byte LAYER_DEFAULT = 0;

        public const byte LAYER_IGNORE_RAYCAST = 2;

        public const byte LAYER_WAYPOINTS = 3;

        public const byte LAYER_PLAYER = 17;

        public const byte LAYER_VEHICLE_DETAILED = 13;
        public const byte LAYER_VEHICLE_LARGE = 27;
        public const byte LAYER_VEHICLE_WORLD = 15;
        public const byte LAYER_CONSTRUCTION = 21;

        public const byte LAYER_DEPLOYED = 8;

        public const byte LAYER_AI = 11;

        public const byte LAYER_TREE = 30;

        public const byte LAYER_TERRAIN = 23;
        public const byte LAYER_WORLD = 16;

        public const int LAYERMASK_PLAYER = 1 << LAYER_PLAYER;

        public const int LAYERMASK_OBSTACLES = 1 << LAYER_VEHICLE_DETAILED | 1 << LAYER_VEHICLE_LARGE | 1 << LAYER_VEHICLE_WORLD | 1 << LAYER_CONSTRUCTION | 1 << LAYER_DEFAULT | 1 << LAYER_DEPLOYED | 1 << LAYER_AI | 1 << LAYER_TREE | 1 << LAYER_TERRAIN | 1 << LAYER_WORLD;

        public const int LAYERMASK_WAYPOINTS = 1 << LAYER_WAYPOINTS;

        public const int LAYERMASK_OBSTACLES_AND_WAYPOINTS = LAYERMASK_OBSTACLES | LAYERMASK_WAYPOINTS;

        public const float WAYPOINT_PERSONAL_SPACE_SPHERE_RADIUS = 0.5F;

        public const float WAYPOINT_NEIGHBOUR_AUTO_CONNECT_RADIUS = 30F;
        #endregion

        #region HOOK SUBSCRIPTIONS
        void OnServerInitialized()
        {
            Instance = this;

            RegisterAndLoadTestBundle();

        }

        void Unload()
        {
            Unloading = true;

            SaveAndUnregisterTestBundle();

            Unloading = false;
            Instance = null;
        }

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (Instance == null)
            {
                return;
            }

            WaypointBrushEntity.OnActiveItemChanged(player, oldItem, newItem);
        }

        #endregion

        #region TEST ENTITY BUNDLE
        public static CustomEntities.CustomPrefabRecipe WaypointEntityRecipe;
        public static CustomEntities.CustomPrefabRecipe WaypointBrushRecipe;

        public static CustomEntities.CustomPrefabBundle TestBundle;

        public static void RegisterAndLoadTestBundle()
        {
            WaypointEntityRecipe = new CustomEntities.CustomPrefabRecipe("waypoint", typeof(VisualWaypointEntity), (Rust.Layer)LAYER_DEFAULT);
            WaypointBrushRecipe = new CustomEntities.CustomPrefabRecipe("waypoint_brush", typeof(WaypointBrushEntity), (Rust.Layer)LAYER_IGNORE_RAYCAST);

            VisualWaypointEntity.Graphs = new Dictionary<ulong, List<VisualWaypointEntity>>();
            VisualWaypointEntity.AllWaypoints = new List<VisualWaypointEntity>();

            TestBundle = new CustomEntities.CustomPrefabBundle(Instance, WaypointEntityRecipe, WaypointBrushRecipe);

            if (!CustomEntities.CustomPrefabs.RegisterAndLoadBundle(TestBundle))
            {
                Instance.PrintError($"Something went wrong while trying to register the custom prefab bundle from the plugin {Instance.Name}");
            }
        }

        public static void SaveAndUnregisterTestBundle()
        {
            if (!CustomEntities.CustomPrefabs.SaveAndUnregisterBundle(TestBundle))
            {
                Instance.PrintError("Something went wrong while trying to unregister the custom prefab bundle.");
            }

            VisualWaypointEntity.Graphs = null;
            VisualWaypointEntity.AllWaypoints = null;

            TestBundle.Recipes = null;
            TestBundle = null;
        }


        #endregion

        #region WAYPOINT BRUSH ENTITY
        public class WaypointBrushEntity : CustomEntities.CustomBaseEntity
        {
            public const string BRUSH_ITEM_SHORTNAME = "map";
            public const ulong BRUSH_ITEM_SKIN = 0;
            public override string DefaultClientsideFullPrefabName() => PREFAB_LIGHT_POINT_COOL;
            public override bool EnableSavingToDiskByDefault => false;

            public static Dictionary<ulong, WaypointBrushEntity> PlayerToBrush;

            private BasePlayer _ownerPlayer = null;

            private InputState _ownerPlayerInput;

            private bool _ready = false;

            private Vector3 _posPrevious;
            private Vector3 _posCurrent;

            private bool _posChanged = true;
            private float _beenStayingStillFor = 0F;

            private bool _isStablePrevious = true;
            private bool _isStableCurrent = true;

            public const float MUST_BE_STILL_FOR_N_SECS_TO_STABILISE = 0.125F;

            private ListHashSet<VisualWaypointEntity> _lastWaypointsNearby = new ListHashSet<VisualWaypointEntity>();

            private VisualWaypointEntity _firstScannedWaypoint = null;

            private VisualWaypointEntity.CinematicLightColour _colour = VisualWaypointEntity.CinematicLightColour.Cool;

            public VisualWaypointEntity.CinematicLightColour Colour
            {
                get
                {
                    return _colour;
                }
                set
                {
                    if (_colour == value)
                    {
                        return;
                    }

                    _colour = value;

                    //update to new colour...

                    Handler.ClientsideFullPrefabName = VisualWaypointEntity.ColourToPrefabName[value];
                }
            }

            public override void ServerInit()
            {
                base.ServerInit();

                if (_ownerPlayer == null)
                {
                    Instance.PrintError("Error: you can't spawn the Waypoint Brush directly without an associated player or item, killing...");
                    Invoke(() => { Kill(); }, 0.1F);
                    return;
                }

                _ownerPlayerInput = _ownerPlayer.serverInput;

                _ready = true;
            }

            public override void PostInitShared()
            {
                base.PostInitShared();

                if (!_ready)
                {
                    return;
                }
            }

            public void LateUpdate()
            {
                if (!_ready)
                {
                    return;
                }

                if (_ownerPlayer == null)
                {
                    Kill(DestroyMode.None);
                    return;
                }

                if (IsDestroyed)
                {
                    return;
                }

                _posPrevious = _posCurrent;
                _posCurrent = GetPositionInFrontOfPlayerEyes(_ownerPlayer);
                _posChanged = false;

                if (_posPrevious != _posCurrent)
                {
                    transform.position = _posCurrent;
                    transform.hasChanged = true;
                    _posChanged = true;
                }

                if (!_isStableCurrent)
                {
                    if (!_posChanged)
                    {
                        //you're not stable but you're still, start stabilising

                        _beenStayingStillFor += Time.deltaTime;

                        if (_beenStayingStillFor > MUST_BE_STILL_FOR_N_SECS_TO_STABILISE)
                        {
                            _isStablePrevious = false;
                            _isStableCurrent = true;

                            _beenStayingStillFor = 0F;
                        }
                    }
                    else
                    {
                        _beenStayingStillFor = 0F;
                    }
                }
                else
                {
                    if (_posChanged)
                    {
                        //you're stable but your position changed, no more stable
                        _isStablePrevious = true;
                        _isStableCurrent = false;
                    }
                    else
                    {
                        //you're stable and your position hasn't changed, do nothing
                    }
                }

                if (_isStablePrevious != _isStableCurrent)
                {
                    if (_isStableCurrent)
                    {
                        //you were not stable before but you are now
                        OnStoppedMoving();
                        _isStablePrevious = true; //so we don't trigger this again until next time
                    }
                    else
                    {
                        //you were stable before but you aren't now
                        OnStartedMoving();
                        _isStablePrevious = false; //so we don't trigger this again until next time
                    }
                }

                if (Colour == VisualWaypointEntity.CinematicLightColour.Cool)
                {
                    //we're not stable.
                    return;
                }

                bool leftJustPressed = _ownerPlayerInput.WasJustPressed(BUTTON.FIRE_PRIMARY);
                bool rightJustPressed = _ownerPlayerInput.WasJustPressed(BUTTON.FIRE_SECONDARY);

                switch (Colour)
                {
                    case VisualWaypointEntity.CinematicLightColour.Black:
                        {
                            if (rightJustPressed)
                            {
                                CustomEntities.PlayEffect(PREFAB_FX_DROP_SUCCESS, _firstScannedWaypoint.transform.position, Vector3.up, _ownerPlayer);
                                _firstScannedWaypoint.Kill(DestroyMode.None);

                                //opposite day it, we need to trigger OnStoppedMoving
                                _isStableCurrent = false;
                                _ownerPlayerInput.SwallowButton(BUTTON.FIRE_PRIMARY);
                            }
                        }
                        break;
                    case VisualWaypointEntity.CinematicLightColour.Red:
                        {
                            if (leftJustPressed)
                            {
                                CustomEntities.PlayEffect(PREFAB_FX_LOCK_CODE_DENIED, _posCurrent, Vector3.up, _ownerPlayer);

                                _isStableCurrent = false;
                            }
                        }
                        break;
                    case VisualWaypointEntity.CinematicLightColour.Yellow:
                    case VisualWaypointEntity.CinematicLightColour.Green:
                        {
                            if (leftJustPressed)
                            {
                                CustomEntities.PlayEffect(PREFAB_FX_SELECT, _posCurrent, Vector3.up, _ownerPlayer);
                                var newWaypoint = GameManager.server.CreateEntity("assets/custom/waypoint.prefab", _posCurrent);
                                newWaypoint.Spawn();

                                //to trigger OnStoppedMoving the very next frame again
                                _isStableCurrent = false;
                            }

                        }
                        break;
                }
            }

            private void OnStartedMoving()
            {
                _firstScannedWaypoint = null;
                Colour = VisualWaypointEntity.CinematicLightColour.Cool;
            }

            private void OnStoppedMoving()
            {
                _lastWaypointsNearby.Clear();
                _firstScannedWaypoint = null;

                CustomEntities.CastingNonAlloc.GetCollidersWithinRadius(_posCurrent, WAYPOINT_PERSONAL_SPACE_SPHERE_RADIUS, LAYERMASK_OBSTACLES_AND_WAYPOINTS, QueryTriggerInteraction.Ignore);

                if (CustomEntities.CastingNonAlloc.ReusableColCount > 0)
                {
                    //unless we can get some waypoints

                    //stop after first found
                    CustomEntities.CastingNonAlloc.ProcessColliderBufferInto(_lastWaypointsNearby, null, true);

                    int found = _lastWaypointsNearby.Count;

                    if (found == 1)
                    {
                        //found the first one closeby, go Black
                        Colour = VisualWaypointEntity.CinematicLightColour.Black;
                        _firstScannedWaypoint = _lastWaypointsNearby.First();
                    }
                    else
                    {
                        //just an obstacle.
                        Colour = VisualWaypointEntity.CinematicLightColour.Red;
                    }
                }
                else
                {
                    //free of obstacles or closeby stuff. 

                    CustomEntities.CastingNonAlloc.VisEntitiesUniqueWithinRadius(_posCurrent, WAYPOINT_NEIGHBOUR_AUTO_CONNECT_RADIUS, _lastWaypointsNearby, LAYERMASK_WAYPOINTS, QueryTriggerInteraction.Ignore);

                    int found = _lastWaypointsNearby.Count;

                    if (found > 0)
                    {
                        Colour = VisualWaypointEntity.CinematicLightColour.Green;
                    }
                    else
                    {
                        Colour = VisualWaypointEntity.CinematicLightColour.Yellow;
                    }
                }
            }

            public override void OnCustomPrefabPrototypeEntityRegistered()
            {
                base.OnCustomPrefabPrototypeEntityRegistered();
                syncPosition = true;

                PlayerToBrush = new Dictionary<ulong, WaypointBrushEntity>();
            }

            public override void OnCustomPrefabPrototypeEntityUnregistered()
            {
                base.OnCustomPrefabPrototypeEntityUnregistered();

                PlayerToBrush = null;
            }

            public static void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
            {
                bool oldItemIsNull = oldItem == null;
                bool newItemIsNull = newItem == null;

                bool oldItemIsBrush = oldItemIsNull ? false : IsBrushItem(oldItem);
                bool newItemIsBrush = newItemIsNull ? false : IsBrushItem(newItem);

                if (!oldItemIsBrush && !newItemIsBrush)
                {
                    //no brushes involved, just return
                    return;
                }

                if (oldItem == newItem)
                {
                    //don't think that's possible... but just in case
                    return;
                }

                if (oldItemIsBrush && newItemIsBrush)
                {
                    return;
                }

                if (oldItemIsBrush)
                {
                    PlayerStoppedUsingBrush(player);
                }

                if (newItemIsBrush)
                {
                    PlayerStartedUsingBrush(player);
                }
            }

            public static void PlayerStartedUsingBrush(BasePlayer player)
            {
                player.ChatMessage("STARTED USING BRUSH");

                var newBrush = GameManager.server.CreateEntity("assets/custom/waypoint_brush.prefab", GetPositionInFrontOfPlayerEyes(player)) as WaypointBrushEntity;

                newBrush._ownerPlayer = player;

                newBrush.OwnerID = player.OwnerID;

                newBrush.Spawn();

                PlayerToBrush.Add(player.userID, newBrush);
            }

            public static void PlayerStoppedUsingBrush(BasePlayer player)
            {
                player.ChatMessage("STOPPED USING BRUSH");

                WaypointBrushEntity maybePlayerBrush;

                if (!PlayerToBrush.TryGetValue(player.userID, out maybePlayerBrush))
                {
                    return;
                }

                PlayerToBrush.Remove(player.userID);

                maybePlayerBrush.Kill();

            }

            public static Vector3 GetPositionInFrontOfPlayerEyes(BasePlayer player)
            {
                return player.eyes.position + 2 * player.eyes.HeadForward();
            }

            public static bool IsBrushItem(Item item)
            {
                if (item == null)
                {
                    return false;
                }

                if (item.info.shortname != BRUSH_ITEM_SHORTNAME)
                {
                    return false;
                }

                if (item.skin != BRUSH_ITEM_SKIN)
                {
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region WAYPOINT ENTITY
        public class VisualWaypointEntity : CustomEntities.CustomBaseEntity
        {
            public override string DefaultClientsideFullPrefabName() => PREFAB_LIGHT_POINT_RED;

            public static Dictionary<ulong, List<VisualWaypointEntity>> Graphs;
            public static List<VisualWaypointEntity> AllWaypoints;

            [SerializeField]
            private SphereCollider _myTinyCollider;


            public ListHashSet<VisualWaypointEntity> WaypointLinks;

            private ListHashSet<VisualWaypointEntity> _waypointsWithinRadius;

            public static void EnsureAllWaypointsHaveCorrectColours()
            {
                for (var i = 0; i < AllWaypoints.Count; i++)
                {
                    var waypoint = AllWaypoints[i];

                    waypoint.Invoke(() =>
                    {
                        if (waypoint.IsDestroyed)
                        {
                            return;
                        }

                        waypoint.EnsureCorrectColour();
                    }, UnityEngine.Random.Range(0.01F, 0.1F));
                }
            }

            //public static ListHashSet<BasePlayer> EditingPlayers;

            /*
             * RED: is a part of a graph with only one element in it
             * YELLOW: is part of the largest graph
             * GREEN: is part of the only graph (so either all are green, or none - this indicates anything can reach anything)
             * COOL: is part of any other graph
             */
            public static readonly Dictionary<string, CinematicLightColour> PrefabNameToColour = new Dictionary<string, CinematicLightColour>
            {
                [CustomEntities.PREFAB_SPHERE] = CinematicLightColour.Black,
                [PREFAB_LIGHT_POINT_RED] = CinematicLightColour.Red,
                [PREFAB_LIGHT_POINT_WARM] = CinematicLightColour.Yellow,
                [PREFAB_LIGHT_POINT_GREEN] = CinematicLightColour.Green,
                [PREFAB_LIGHT_POINT_COOL] = CinematicLightColour.Cool

            };

            public static readonly Dictionary<CinematicLightColour, string> ColourToPrefabName = new Dictionary<CinematicLightColour, string>
            {
                [CinematicLightColour.Black] = CustomEntities.PREFAB_SPHERE,
                [CinematicLightColour.Red] = PREFAB_LIGHT_POINT_RED,
                [CinematicLightColour.Yellow] = PREFAB_LIGHT_POINT_WARM,
                [CinematicLightColour.Green] = PREFAB_LIGHT_POINT_GREEN,
                [CinematicLightColour.Cool] = PREFAB_LIGHT_POINT_COOL
            };


            public static readonly Dictionary<CinematicLightColour, Color> ColourToUnityColour = new Dictionary<CinematicLightColour, Color>
            {
                [CinematicLightColour.Black] = Color.black,
                [CinematicLightColour.Red] = Color.red,
                [CinematicLightColour.Yellow] = Color.yellow,
                [CinematicLightColour.Green] = Color.green,
                [CinematicLightColour.Cool] = Color.white
            };

            public enum CinematicLightColour
            {
                Black = -1,
                Red = 0,
                Yellow = 1,
                Green = 2,
                Cool = 3,
            }

            private CinematicLightColour _colour = CinematicLightColour.Red;

            public CinematicLightColour Colour
            {
                get
                {
                    return _colour;
                }
                set
                {
                    if (_colour == value)
                    {
                        return;
                    }

                    _colour = value;

                    //update to new colour...

                    Handler.ClientsideFullPrefabName = ColourToPrefabName[value];
                }
            }

            public ulong GraphID = 0;

            public static List<VisualWaypointEntity> TryGetGraphByGraphID(ulong graphID)
            {
                List<VisualWaypointEntity> graph;
                if (!Graphs.TryGetValue(graphID, out graph))
                {
                    return null;
                }

                return graph;

            }

            public override void ServerInit()
            {
                base.ServerInit();

                OnAdded();

                InvokeRandomized(DrawSelf, 0.1F, 0.75F, 0.5F);
            }

            public override void DestroyShared()
            {
                base.DestroyShared();

                CancelInvoke(DrawSelf);

                if (Unloading)
                {
                    return;
                }

                if (Instance == null)
                {
                    return;
                }

                OnRemoved();

            }

            public override void LoadExtra(Stream stream, BinaryReader reader)
            {
                base.LoadExtra(stream, reader);
                GraphID = reader.ReadUInt64();
            }

            public override void SaveExtra(Stream stream, BinaryWriter writer)
            {
                base.SaveExtra(stream, writer);
                writer.Write(GraphID);
            }

            public override void OnCustomPrefabPrototypeEntityRegistered()
            {
                base.OnCustomPrefabPrototypeEntityRegistered();

                //run this only once, otherwise you're gonna have trouble

                _myTinyCollider = gameObject.AddComponent<SphereCollider>();
                _myTinyCollider.isTrigger = false;
                _myTinyCollider.radius = 0.2F;

            }

            public void EstablishLinkBetweenWaypointAndYou(VisualWaypointEntity otherLinkedWaypoint, bool invokeThisInTheOtherWaypoint)
            {
                if (!WaypointLinks.Contains(otherLinkedWaypoint))
                {
                    WaypointLinks.Add(otherLinkedWaypoint);
                }

                if (!invokeThisInTheOtherWaypoint)
                {
                    return;
                }

                otherLinkedWaypoint.EstablishLinkBetweenWaypointAndYou(this, false); //false to prevent infinite loops
            }

            public void ClearLinkBetweenWaypointAndYou(VisualWaypointEntity otherLinkedWaypoint, bool invokeThisInTheOtherWaypoint)
            {
                if (WaypointLinks.Contains(otherLinkedWaypoint))
                {
                    WaypointLinks.Remove(otherLinkedWaypoint);
                }

                if (!invokeThisInTheOtherWaypoint)
                {
                    return;
                }

                otherLinkedWaypoint.ClearLinkBetweenWaypointAndYou(this, false); //false to prevent infinite loops
            }

            public void EnsureRemovedFromGraph(ulong graphID)
            {
                var maybeCurrentGraph = TryGetGraphByGraphID(GraphID);

                if (maybeCurrentGraph != null)
                {
                    maybeCurrentGraph.Remove(this);
                    if (maybeCurrentGraph.Count == 0)
                    {
                        //if it was the last element, remove the entire graph
                        Graphs.Remove(graphID);
                    }
                }

                GraphID = 0;
            }

            public void EnsureMovedToGraph(ulong newGraphID)
            {
                //remove from old graph first...
                EnsureRemovedFromGraph(GraphID);

                //and now GraphID is 0....

                var maybeNewGraph = TryGetGraphByGraphID(newGraphID);

                if (maybeNewGraph == null)
                {
                    //no such graph, create a new graph with yourself in it
                    maybeNewGraph = new List<VisualWaypointEntity> { this };

                    Graphs.Add(newGraphID, maybeNewGraph);
                }
                else
                {
                    if (!maybeNewGraph.Contains(this))
                    {
                        //graph found, add yourself
                        maybeNewGraph.Add(this);
                    }
                }

                GraphID = newGraphID;
            }

            public void OnAdded()
            {
                _waypointsWithinRadius = new ListHashSet<VisualWaypointEntity>();
                WaypointLinks = new ListHashSet<VisualWaypointEntity>();
                AllWaypoints.Add(this);

                //first, attach to a self graph based on your NET id
                EnsureMovedToGraph(net.ID.Value);

                CustomEntities.CastingNonAlloc.VisEntitiesUniqueWithinRadius(transform.position, WAYPOINT_NEIGHBOUR_AUTO_CONNECT_RADIUS, _waypointsWithinRadius, LAYERMASK_WAYPOINTS, QueryTriggerInteraction.Ignore, OtherWaypointIsNotYou);

                for (var v = 0; v < _waypointsWithinRadius.Count; v++)
                {
                    var otherWaypoint = _waypointsWithinRadius[v];

                    //can you fully "see" each other? and nothing else in between or overlapping in the "capsule" encompassing personal space from your position to the other position?
                    Vector3 dir = otherWaypoint.transform.position - transform.position;
                    float length = dir.magnitude;
                    dir /= length;

                    var colliderResults = Physics.SphereCastNonAlloc(transform.position, WAYPOINT_PERSONAL_SPACE_SPHERE_RADIUS, dir, CustomEntities.CastingNonAlloc.ReusableRaycastBuffer, length, LAYERMASK_OBSTACLES, QueryTriggerInteraction.Ignore);

                    if (colliderResults > 0)
                    {
                        continue;
                    }
                    //good, nothing in between you and the other waypoint - pair up

                    EstablishLinkBetweenWaypointAndYou(otherWaypoint, true); //true, becuase we want it to be mutual
                }

                if (_waypointsWithinRadius.Count == 0)
                {
                    //nothing to do as you won't have any neighbours
                    EnsureAllWaypointsHaveCorrectColours();
                    return;
                }

                //now it's time to iterate over links.

                ulong lowestNetIDfoundSoFar = ulong.MaxValue;

                //we should make it into a reusable
                var buildListOfAll = new ListHashSet<VisualWaypointEntity>();

                FloodFillRecursively(this, buildListOfAll, ref lowestNetIDfoundSoFar);

                //now iterate over buildListOfAll and ensure they are all in the graph with the id of lowestNetIDfoundSoFar.
                //this will also include yourself.

                for (var i = 0; i < buildListOfAll.Count; i++)
                {
                    var fromBuildList = buildListOfAll[i];
                    fromBuildList.EnsureMovedToGraph(lowestNetIDfoundSoFar);
                }

                //no need to execute this if you had no waypoints to begin with!
                EnsureAllWaypointsHaveCorrectColours();

            }
            public void OnRemoved()
            {
                //okay something is still wrong. We're getting a NRE.
                //it looks like as if the connections are NOT properly severed.


                AllWaypoints.Remove(this);

                var currentGraphID = GraphID;
                EnsureRemovedFromGraph(currentGraphID);

                if (WaypointLinks.Count == 0)
                {
                    //nothing to do, you had no links
                    EnsureAllWaypointsHaveCorrectColours();
                    return;
                }

                var linksCopy = WaypointLinks.ToArray();

                //sever all your connections

                var buildListOfAll = new ListHashSet<VisualWaypointEntity>();

                //do them branches

                //clear links first, maybe?

                for (var l = 0; l < linksCopy.Length; l++)
                {
                    var branchWaypoint = linksCopy[l];

                    //sever your link mutually so it doesn't take you in the flood fill
                    ClearLinkBetweenWaypointAndYou(branchWaypoint, true);
                }

                for (var l = 0; l < linksCopy.Length; l++)
                {
                    var branchWaypoint = linksCopy[l];

                    ulong lowestNetIDfoundSoFarForThisBranch = ulong.MaxValue;

                    ListHashSet<VisualWaypointEntity> branchSpecificList = new ListHashSet<VisualWaypointEntity>();

                    FloodFillRecursively(branchWaypoint, branchSpecificList, ref lowestNetIDfoundSoFarForThisBranch, buildListOfAll);

                    //we have the branch, now make sure they all assume the lowest net ID as their Graph ID...
                    //and we can also ensure their colours

                    for (var i = 0; i < branchSpecificList.Count; i++)
                    {
                        var fromBranchSpecificList = branchSpecificList[i];

                        fromBranchSpecificList.EnsureMovedToGraph(lowestNetIDfoundSoFarForThisBranch);
                    }
                }

                EnsureAllWaypointsHaveCorrectColours();
            }

            public static void FloodFillRecursively(VisualWaypointEntity doingNow, ListHashSet<VisualWaypointEntity> branchList, ref ulong lowestNetIDFoundSoFar, ListHashSet<VisualWaypointEntity> globalList = null)
            {
                if (globalList != null)
                {
                    if (globalList.Contains(doingNow))
                    {
                        //terminate
                        return;
                    }
                }

                if (branchList.Contains(doingNow))
                {
                    //terminate the recursion branch
                    return;
                }

                //now add so you never do yourself again

                var doingNowNetID = doingNow.net.ID.Value;

                if (doingNowNetID < lowestNetIDFoundSoFar)
                {
                    lowestNetIDFoundSoFar = doingNowNetID;
                }

                branchList.Add(doingNow);

                if (globalList != null)
                {
                    globalList.Add(doingNow);
                }

                //and now your neighbours

                for (var i = 0; i < doingNow.WaypointLinks.Count; i++)
                {
                    var linked = doingNow.WaypointLinks[i];

                    FloodFillRecursively(linked, branchList, ref lowestNetIDFoundSoFar, globalList);
                }
            }

            public bool OtherWaypointIsNotYou(VisualWaypointEntity otherWaypoint)
            {
                return !EqualNetID(otherWaypoint);
            }

            public void EnsureCorrectColour()
            {
                //by default you're "other"
                var useCinematicColour = CinematicLightColour.Cool;

                var ourGraph = TryGetGraphByGraphID(GraphID);

                if (ourGraph != null)
                {
                    if (Graphs.Count == 1)
                    {
                        //the only graph - become green like everyone else
                        useCinematicColour = CinematicLightColour.Green;
                    }
                    else
                    {
                        int ourGraphCount = ourGraph.Count;

                        //are you the only memeber of your graph?
                        if (ourGraphCount == 1)
                        {
                            useCinematicColour = CinematicLightColour.Red;
                        }
                        else
                        {
                            //are you a member of the largest graph?

                            bool someoneBeatOurGraph = false;

                            foreach (var graph in Graphs)
                            {
                                if (graph.Value == ourGraph)
                                {
                                    continue;
                                }

                                if (graph.Value.Count > ourGraphCount)
                                {
                                    someoneBeatOurGraph = true;
                                    break;
                                }
                            }

                            if (!someoneBeatOurGraph)
                            {
                                useCinematicColour = CinematicLightColour.Yellow;
                            }
                        }
                    }
                }

                //set it...
                Colour = useCinematicColour;
            }

            public void DrawSelf()
            {
                if (IsDestroyed)
                {
                    return;
                }

                var unityColour = ColourToUnityColour[Colour];


                for (var p = 0; p < BasePlayer.activePlayerList.Count; p++)
                {
                    var player = BasePlayer.activePlayerList[p];

                    if (!player.IsAdmin)
                    {
                        continue;
                    }

                    //uncomment to only draw when close enough

                    /*
                    if (Vector3.Distance(player.transform.position, transform.position) > WAYPOINT_NEIGHBOUR_AUTO_CONNECT_RADIUS)
                    {
                        continue;
                    }*/


                    //draw yourself...
                    CustomEntities.DrawTextCommon(player, 1F, unityColour, transform.position, $"{net.ID}\n{GraphID}");

                    if (WaypointLinks.Count == 0)
                    {
                        continue;
                    }

                    var myDistanceToPlayer = Vector3.Distance(player.transform.position, transform.position);


                    for (var i = 0; i < WaypointLinks.Count; i++)
                    {
                        var linkedWaypoint = WaypointLinks[i];

                        if (linkedWaypoint.IsDestroyed)
                        {
                            continue;
                        }

                        var otherDistanceToPlayer = Vector3.Distance(player.transform.position, linkedWaypoint.transform.position);

                        if (myDistanceToPlayer > otherDistanceToPlayer)
                        {
                            //the other one is closer, only draw the other one
                            continue;
                        }

                        CustomEntities.DrawLineCommon(player, 1F, unityColour, transform.position, linkedWaypoint.transform.position);
                    }
                }
            }

        }
        #endregion
    }
}
