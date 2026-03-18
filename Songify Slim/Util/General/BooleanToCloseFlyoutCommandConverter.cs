using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace Songify_Slim.Util.General;

public sealed class BooleanToCloseFlyoutCommandConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // In XAML we bind to the TemplatedParent (Flyout instance).
        if (value is not Flyout flyout)
            return null;

        return new CloseFlyoutCommand(flyout);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private sealed class CloseFlyoutCommand : ICommand
    {
        private readonly WeakReference<Flyout> _flyoutRef;

        public CloseFlyoutCommand(Flyout flyout) => _flyoutRef = new WeakReference<Flyout>(flyout);

        public bool CanExecute(object parameter)
            => _flyoutRef.TryGetTarget(out Flyout flyout) && flyout.IsOpen;

        public void Execute(object parameter)
        {
            if (_flyoutRef.TryGetTarget(out Flyout flyout))
                flyout.IsOpen = false;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}

