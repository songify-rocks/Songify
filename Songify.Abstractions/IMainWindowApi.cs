using System.Windows.Controls;

namespace Songify.Abstractions
{
    public interface IMainWindowApi
    {
        Button BtnSupportUs { get; }
        string WindowTitle { get; set; }
    }
}

