using DirectNXAML.DrawData;
using DirectNXAML.Renderers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PInvoke;
using System;
using System.IO;
using System.Runtime.InteropServices;
using static DirectNXAML.ViewModels.DirectNPageViewModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DirectNXAML.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DirectNPage : Page
    {
        public DirectNPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
        }

        private void DirectNPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (viewModel.PageRenderer != null)
            {
                this.UpdateLayout();
                try
                {
                    viewModel.PageRenderer.Initialize((uint)_scp.ActualWidth, (uint)_scp.ActualHeight);
                }
                catch
                {
                    throw new InvalidProgramException("Error at initializsizng renderer.");
                }

                viewModel.PageRenderer.SetSwapChainPanel(_scp);
                viewModel.SetCursorMethods += new SetCursor(this.SetCursorPosition);
                viewModel.SCPSize_Changed += viewModel.PageRenderer.Panel_SizeChanged;
                viewModel.PageRenderer.StartRendering();
            }
            else
            {
                throw new InvalidDataException("The last renderer is invalid.");
            }
        }
        private void DirectNPage_Unloaded(object sender, RoutedEventArgs e)
        {
            viewModel.PageRenderer.StopRendering();
            viewModel.SetCursorMethods -= new SetCursor(this.SetCursorPosition);
        }
        private void SetBG_White(object sender, RoutedEventArgs e)
        {
            viewModel.PageRenderer?.SetBGColor(1, 1, 1);
        }
        private void SetBG_Black(object sender, RoutedEventArgs e)
        {
            viewModel.PageRenderer?.SetBGColor(0, 0, 0);
        }
        bool m_can_get_point = false;
        private void SwapChainPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            m_can_get_point = true;
        }

        private void SwapChainPanel_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            m_can_get_point = false;
        }

        private void SwapChainPanel_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            m_can_get_point = false;
        }

        private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            viewModel.LocalWidth = e.NewSize.Width;
            viewModel.LocalHeight = e.NewSize.Height;
            viewModel.ActualWidth = (sender as SwapChainPanel).ActualWidth;
            viewModel.ActualHeight = (sender as SwapChainPanel).ActualHeight;
            viewModel.SwapChainActualSize = (sender as SwapChainPanel).ActualSize;
            viewModel.ShaderPanel_SizeChangedCommand.Execute(e);
        }

        private void SwapChainPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (m_can_get_point)
            {
                viewModel.LocalPointerPoint = e.GetCurrentPoint(sender as SwapChainPanel).Position;
            }
            viewModel.ShaderPanel_PointerMovedCommand.Execute(e);
        }

        private void SwapChainPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (m_can_get_point)
            {
                viewModel.PressedPoint = e.GetCurrentPoint(sender as SwapChainPanel).Position;
            }
            viewModel.ShaderPanel_PointerPressedCommand.Execute(e);
        }

        private void SwapChainPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (m_can_get_point)
            {
                viewModel.ReleasedPoint = e.GetCurrentPoint(sender as SwapChainPanel).Position;
            }
            viewModel.ShaderPanel_PointerReleasedCommand.Execute(e);
        }
        private void SwapChainPanel_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (m_can_get_point)
            {
                var p = e.GetCurrentPoint(_scp);
                var wdelta = p.Properties.MouseWheelDelta;
                viewModel.MouseWheelDelta += wdelta;    // here is total
                viewModel.ShaderPanel_PointerWheelChangedCommand.Execute(e); //here is delta
            }
        }

        [DllImport("User32.dll", ExactSpelling = true, EntryPoint = "SetCursorPos", CharSet = CharSet.Unicode)]
        public static extern bool MoveCursor(int X, int Y);
        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool ClientToScreen(IntPtr hWnd, ref Windows.Graphics.PointInt32 lpPoint);

        /// <summary>
        /// Set cursor to specified position on SwawpChainPanel
        /// https://github.com/microsoft/microsoft-ui-xaml/issues/5897
        /// This should do the trick when null is passed as the parameter: https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.uielement.transformtovisual?view=winrt-20348
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public void SetCursorPosition(int _x, int _y)
        {
            Windows.Graphics.PointInt32 pt = new Windows.Graphics.PointInt32(_x, _y);
            ClientToScreen(((App)Application.Current).hWndCurrent, ref pt);
            //GeneralTransform gt = _scp.TransformToVisual((UIElement)this.Content);
            GeneralTransform gt = _scp.TransformToVisual(null); // <-- This is it!
            Windows.Foundation.Point ptNew = gt.TransformPoint(new Windows.Foundation.Point(0, 0));
            pt.X += (int)ptNew.X;// position of control swapchainpanel 321
            pt.Y += (int)ptNew.Y;
            MoveCursor(pt.X, pt.Y);
        }
    }
}
