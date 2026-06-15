using System.Collections.Generic;
using System.Windows.Media;

namespace Songify_Slim.Util.General
{
    internal sealed class ServiceIndicatorState
    {
        public ServiceIndicatorState(
            bool isSelected,
            bool isConnecting,
            bool isConnected,
            string connectedStatusText = "Connected",
            string disconnectedStatusText = "Disconnected",
            bool showInactiveStatusWhenUnselected = true,
            string inactiveStatusText = "Inactive")
        {
            IsSelected = isSelected;
            IsConnecting = isConnecting;
            IsConnected = isConnected;
            ConnectedStatusText = connectedStatusText;
            DisconnectedStatusText = disconnectedStatusText;
            ShowInactiveStatusWhenUnselected = showInactiveStatusWhenUnselected;
            InactiveStatusText = inactiveStatusText;
        }

        public bool IsSelected { get; }
        public bool IsConnecting { get; }
        public bool IsConnected { get; }
        public string ConnectedStatusText { get; }
        public string DisconnectedStatusText { get; }
        public bool ShowInactiveStatusWhenUnselected { get; }
        public string InactiveStatusText { get; }

        public string StatusText =>
            IsConnecting
                ? "Connecting"
                : !IsSelected && ShowInactiveStatusWhenUnselected
                    ? InactiveStatusText
                    : IsConnected
                        ? ConnectedStatusText
                        : DisconnectedStatusText;

        public Brush Foreground =>
            !IsSelected
                ? Brushes.Gray
                : IsConnecting
                    ? Brushes.DarkOrange
                    : IsConnected
                        ? Brushes.GreenYellow
                        : Brushes.IndianRed;

        public List<(string Label, string Value)> BuildRows(params (string Label, string Value)[] additionalRows)
        {
            List<(string Label, string Value)> rows =
            [
                ("Status", StatusText),
                ("Selected", IsSelected ? "Yes" : "No")
            ];

            rows.AddRange(additionalRows);
            return rows;
        }
    }
}