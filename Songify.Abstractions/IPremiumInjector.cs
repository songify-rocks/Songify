using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Songify.Abstractions
{
    public interface IPremiumInjector
    {
        void InjectSettingsTab(System.Windows.Controls.TabControl settingsTabControl);
        void InjectMainUi(IMainWindowApi mainWindow);
        void HandleWebsocketMessage(string message, object socket);
        bool IsPremiumUser(string userid);
    }
}
