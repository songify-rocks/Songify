using System;
using System.Runtime.InteropServices;

namespace Songify_Slim.Util.General;

/// <summary>
/// User32 MessageBox for crash UI. WPF MessageBox can throw with WPF-UI dictionaries loaded;
/// WinForms MessageBox pulls System.Windows.Forms into the process.
/// </summary>
internal static class NativeMessageBox
{
    private const uint MbYesNo = 0x00000004;
    private const uint MbIconError = 0x00000010;
    private const uint MbIconQuestion = 0x00000020;
    private const int IdYes = 6;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    public static bool YesNo(string text, string caption, bool errorIcon)
    {
        uint type = MbYesNo | (errorIcon ? MbIconError : MbIconQuestion);
        return MessageBoxW(IntPtr.Zero, text, caption, type) == IdYes;
    }
}
