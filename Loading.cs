using ColossalFramework.Plugins;
using ICities;
using UnityEngine;

namespace PolitiCities
{
    public class PolitiCitiesLoading : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "PolitiCities: level loaded, mode " + mode);
            Debug.Log("[PolitiCities] level loaded, mode " + mode);
        }
    }
}
