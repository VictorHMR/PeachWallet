using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Utils
{
    public static class SettingsManager
    {
        public static int Teste
        {
            get
            {
                if (Preferences.ContainsKey("Teste"))
                {
                    return Preferences.Get("Teste", 0);
                }
                return 0;
            }
            set
            {
                Preferences.Set("Teste", value);
            }
        }

        public static void ResetSettings()
        {
            Preferences.Clear();
        }
    }
}
