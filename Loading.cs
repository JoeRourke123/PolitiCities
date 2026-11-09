using ColossalFramework.Plugins;
using ICities;
using PolitiCities.Politics;
using UnityEngine;

namespace PolitiCities
{
    public class PolitiCitiesLoading : LoadingExtensionBase
    {
        private GameObject hotkeyObject;

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "PolitiCities: level loaded, mode " + mode + " (press P for council panel)");
            Debug.Log("[PolitiCities] level loaded, mode " + mode);

            if (hotkeyObject == null)
            {
                hotkeyObject = new GameObject("PolitiCitiesHotkeys");
                hotkeyObject.AddComponent<HotkeyHandler>();
            }
        }

        public override void OnLevelUnloading()
        {
            if (hotkeyObject != null)
            {
                Object.Destroy(hotkeyObject);
                hotkeyObject = null;
            }
            base.OnLevelUnloading();
        }
    }
}
