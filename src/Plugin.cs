using BepInEx;
using Ultracam.CameraTools;
using UnityEngine.SceneManagement;

namespace HydraDynamics
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("Hydraxous.ULTRAKILL.Configgy", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        Configgy.ConfigBuilder configBuilder;

        public const string PLUGIN_NAME = "Ultracam";
        public const string PLUGIN_VERSION = "1.0.0";
        public const string PLUGIN_GUID = "Hydraxous.ULTRAKILL.Ultracam";

        private void Awake()
        {
            configBuilder = new Configgy.ConfigBuilder();
            configBuilder.Build();
            
            Logger.LogInfo($"Plugin {PLUGIN_NAME} is loaded!");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (SceneHelper.CurrentScene != "Main Menu")
                return;
         
            CameraHandler.Load();
        }
    }
}
