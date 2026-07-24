using Microsoft.Xna.Framework;

namespace GDGame
{
    /// <summary>
    /// Centralised, game-specific configuration and asset keys.
    /// </summary>
    public static class AppData
    {
        #region Physics
        public static readonly Vector3 GRAVITY = new Vector3(0, -9.81f, 0);

        #endregion

        #region Asset Paths

        public static readonly string CONTENT_ROOT = "Content";

        public static readonly string ASSET_MANIFEST_PATH =
            "assets/data/asset_manifest.json";

        public static readonly string SINGLE_MODEL_SPAWN_PATH =
            "assets/data/single_model_spawn.json";

        public static readonly string MULTI_MODEL_SPAWN_PATH =
            "assets/data/multi_model_spawn.json";

        public static readonly string GROUND_TEXTURE_KEY = "ground_grass";
        public static readonly string GROUND_TEXTURE_PATH =
            "assets/textures/foliage/ground/grass1";

        public static readonly string TREE_TEXTURE_KEY = "tree4";

        public static readonly string PERF_STATS_FONT_KEY = "perf_stats_font";
        public static readonly string MOUSE_RETICLE_FONT_KEY = "mouse_reticle_font";

        public static readonly string RETICLE_ATLAS_KEY = "Crosshair_21";

        public static readonly string SKYBOX_BACK_TEXTURE_KEY = "skybox_back";
        public static readonly string SKYBOX_LEFT_TEXTURE_KEY = "skybox_left";
        public static readonly string SKYBOX_RIGHT_TEXTURE_KEY = "skybox_right";
        public static readonly string SKYBOX_FRONT_TEXTURE_KEY = "skybox_front";
        public static readonly string SKYBOX_SKY_TEXTURE_KEY = "skybox_sky";

        public static readonly string PLAYER_TEXTURE_KEY = "crate1";
        public static readonly string PLAYER_MODEL_KEY = "monkey1";

        #endregion

        #region GameObjects

        public static readonly string SCENE_NAME_OUTDOORS_LEVEL1 =
            "outdoors - level 1";

        public static readonly string PLAYER_NAME = "The Player";
        public static readonly string TEST_CRATE_NAME = "test crate textured cube";
        public static readonly string TREE_NAME = "tree";
        public static readonly string GROUND_NAME = "ground";
        public static readonly string SKY_PARENT_NAME = "SkyParent";

        public static readonly string SKYBOX_BACK_NAME = "back";
        public static readonly string SKYBOX_LEFT_NAME = "left";
        public static readonly string SKYBOX_RIGHT_NAME = "right";
        public static readonly string SKYBOX_FRONT_NAME = "front";
        public static readonly string SKYBOX_SKY_NAME = "sky";

        public static readonly string HUD_NAME = "HUD";
        public static readonly string STATS_OVERLAY_NAME = "Stats Overlay";
        #endregion

        #region Cameras
        public static readonly string CAMERA_NAME_RAIL = "Rail";
        public static readonly string CAMERA_NAME_THIRD_PERSON = "Third person";
        public static readonly string CAMERA_NAME_FIRST_PERSON = "First person";
        public static readonly string CAMERA_NAME_PIP = "PIP";
        public static readonly string CAMERA_NAME_STATIC_BIRDS_EYE = "Static birds-eye";
        public static readonly string CAMERA_NAME_INTRO_CURVE = "Intro curve";
        public static readonly string CAMERA_IMPULSE_CHANNEL = "camera/impulse";
        public static readonly string CAMERA_NAME_FIRST_PERSON_PARENT = CAMERA_NAME_FIRST_PERSON + "parent";

        #endregion

        #region UI Text

        public static readonly string UI_STATS_HEADER_DRAW = "Draw Stats:";
        public static readonly string UI_STATS_HEADER_CAMERA = "Camera Stats:";

        public static readonly string UI_STATS_RENDERER_COUNT_LABEL =
            " - Renderer Count: ";

        public static readonly string UI_STATS_CAMERA_NAME_LABEL =
            " - Camera [name]: ";

        public static readonly string UI_STATS_CAMERA_POSITION_LABEL =
            " - Camera [Position]: ";

        public static readonly string UI_STATS_CAMERA_FORWARD_LABEL =
            " - Camera [Forward]: ";

        public static readonly string UI_HUD_DISTANCE_PREFIX = "Dist: ";
        public static readonly string UI_HUD_DISTANCE_SUFFIX_METRES = " m";

        public static readonly string UI_HUD_HEALTH_LABEL = "Health:   ";

        #endregion

        #region Other

        public static readonly string GAME_WINDOW_TITLE = "My Amazing Game";
        public static readonly string GAMEPAD_P1_NAME = "Gamepad P1";

        public static readonly string ORCHESTRATION_DEMO_CRATE_BOUNCE =
            "Demo_CrateBounce";

        public static readonly string DAMAGE_SOURCE_DEBUG_GUN = "DebugGun";

        #endregion


        #region Level
        public static readonly string LEVEL_1_NAME = "outdoors - level 1";
       

        #endregion

    }
}
