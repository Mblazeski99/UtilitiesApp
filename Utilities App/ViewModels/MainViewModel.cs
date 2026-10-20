using System.Collections.ObjectModel;
using System.Windows.Input;
using UtilitiesApp.Services;

namespace UtilitiesApp.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private readonly ObservableCollection<NavigationItem> _navigationItems = [];
        private bool _isMenuExpanded = true;
        private string _currentTitle = "";
        private object? _currentViewModel;

        public MainViewModel(FileRenamer fileRenamer, IDialogService dialogService)
        {
            ToggleMenuCommand = new RelayCommand(ToggleMenu);
            NavigateCommand = new RelayCommand(Navigate);

            var renamer = new NavigationItem("Renamer", "\uE8AC", new RenamerViewModel(fileRenamer, dialogService));
            _navigationItems.Add(renamer);
            Navigate(renamer);
        }

        public bool IsMenuExpanded
        {
            get => _isMenuExpanded;
            private set => SetProperty(ref _isMenuExpanded, value);
        }

        public string CurrentTitle
        {
            get => _currentTitle;
            private set => SetProperty(ref _currentTitle, value);
        }

        public object? CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        public IReadOnlyList<NavigationItem> NavigationItems => _navigationItems;

        public ICommand ToggleMenuCommand { get; }

        public ICommand NavigateCommand { get; }

        private void ToggleMenu()
        {
            IsMenuExpanded = !IsMenuExpanded;
        }

        private void Navigate(object? parameter)
        {
            if (parameter is not NavigationItem item)
            {
                return;
            }

            foreach (NavigationItem navigationItem in _navigationItems)
            {
                navigationItem.IsSelected = navigationItem == item;
            }

            CurrentTitle = item.Title;
            CurrentViewModel = item.ViewModel;
        }
    }
}
