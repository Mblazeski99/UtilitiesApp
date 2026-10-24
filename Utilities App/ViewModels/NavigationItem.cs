namespace UtilitiesApp.ViewModels
{
    public sealed class NavigationItem : ObservableObject
    {
        public NavigationItem(string title, string description, string glyph, object viewModel)
        {
            Title = title;
            Description = description;
            Glyph = glyph;
            ViewModel = viewModel;
        }

        public string Title { get; }

        public string Description { get; }

        public string Glyph { get; }

        public object ViewModel { get; }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
