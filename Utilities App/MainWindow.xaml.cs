using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using UtilitiesApp.Services;
using UtilitiesApp.ViewModels;

namespace UtilitiesApp
{
    public partial class MainWindow : Window
    {
        private const double ExpandedMenuWidth = 252;
        private const double CollapsedMenuWidth = 76;

        public MainWindow()
        {
            InitializeComponent();

            var dialogService = new DialogService();
            var viewModel = new MainViewModel(new FileRenamer(), dialogService, new YouTubeDownloader());
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsMenuExpanded))
                {
                    AnimateMenu(viewModel.IsMenuExpanded);
                }
            };
            DataContext = viewModel;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            UseDarkTitleBar();
        }

        private void UseDarkTitleBar()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            int useDark = 1;
            DwmSetWindowAttribute(handle, 20, ref useDark, sizeof(int));
            DwmSetWindowAttribute(handle, 19, ref useDark, sizeof(int));
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

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
