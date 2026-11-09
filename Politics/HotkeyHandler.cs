using UnityEngine;

namespace PolitiCities.Politics
{
    public class HotkeyHandler : MonoBehaviour
    {
        private CouncilPanel panel;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (panel == null)
                {
                    panel = new CouncilPanel();
                }
                if (panel.IsOpen)
                {
                    panel.Hide();
                }
                else
                {
                    panel.Show();
                    panel.Refresh();
                }
            }
        }
    }
}
