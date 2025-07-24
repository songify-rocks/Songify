using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Songify.Abstractions;

namespace Songify_Slim
{
    public class PremiumInjectorStub : IPremiumInjector
    {
        public void InjectSettingsTab(System.Windows.Controls.TabControl settingsTabControl) { }
        public void InjectMainUi(IMainWindowApi mainWindow) { }
        public bool IsPremiumUser(string userId) => false;
        public void HandleWebsocketMessage(string message, object socket) { }
    }

}
