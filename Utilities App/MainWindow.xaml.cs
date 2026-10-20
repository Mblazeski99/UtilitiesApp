using System.Windows;
using System.Windows.Media.Animation;
using UtilitiesApp.Services;
using UtilitiesApp.ViewModels;

namespace UtilitiesApp
{
    public partial class MainWindow : Window
    {
        private const double ExpandedMenuWidth = 240;
        private const double CollapsedMenuWidth = 72;

        public MainWindow()
        {
            InitializeComponent();

            var viewModel = new MainViewModel(new FileRenamer(), new DialogService());
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsMenuExpanded))
                {
                    AnimateMenu(viewModel.IsMenuExpanded);
                }
            };
            DataContext = viewModel;
        }

        private void AnimateMenu(bool expanded)
        {
            var animation = new DoubleAnimation
            {
                To = expanded ? ExpandedMenuWidth : CollapsedMenuWidth,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            NavigationMenu.BeginAnimation(WidthProperty, animation);
        }
    }
}
