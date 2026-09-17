using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Songify_Slim.Util.General;

/// <summary>WPF folder picker (Vista <see cref="OpenFolderDialog"/>). Do not construct until the user clicks Browse.</summary>
internal static class FolderPicker
{
    public static string PickFolder(DependencyObject owner, string title, string initialDirectory = null)
    {
        Window window = owner as Window ?? (owner != null ? Window.GetWindow(owner) : null);
        return PickFolder(window, title, initialDirectory);
    }

    public static string PickFolder(Window owner, string title, string initialDirectory = null)
    {
        OpenFolderDialog dialog = new()
        {
            Title = title ?? "",
            Multiselect = false
        };

        string start = ResolveExistingDirectory(initialDirectory);
        if (start != null)
            dialog.InitialDirectory = start;

        bool? ok = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
        return ok == true && !string.IsNullOrWhiteSpace(dialog.FolderName)
            ? dialog.FolderName
            : null;
    }

    private static string ResolveExistingDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        if (Directory.Exists(path))
            return path;

        try
        {
            string parent = Path.GetDirectoryName(path);
            return !string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent) ? parent : null;
        }
        catch
        {
            return null;
        }
    }
}
