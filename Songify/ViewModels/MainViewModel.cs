using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Songify.Models;

namespace Songify.ViewModels
{
    public class MainViewModel : Screen
    {
        private readonly string COVERWIDTH = "1.3*";

        private string _coverWidth = "0";
        
        private SourceModel _selectedSource;

        public string CoverWidth
        {
            get => _coverWidth;
            set { 
                _coverWidth = value;
                NotifyOfPropertyChange(() => CoverWidth);
            }
        }

        public BindableCollection<SourceModel> Sources { get; set; } = new();

        public SourceModel SelectedSource
        {
            get => _selectedSource;
            set
            {
                _selectedSource = value;
                NotifyOfPropertyChange(() => SelectedSource);
            }
        }

        public void ShowCover()
        {
            CoverWidth = COVERWIDTH;
        }

        public void HideCover()
        {
            CoverWidth = "0";
        }

        public MainViewModel()
        {
            
        }
    }
}
