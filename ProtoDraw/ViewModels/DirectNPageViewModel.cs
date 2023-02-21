using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DirectN;
using JeremyAnsel.DirectX.DXMath;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Windows.Input;
using System.Drawing;
using Microsoft.UI.Xaml.Controls;
using DirectNXAML.DrawData;
using DirectNXAML.Helpers;
using DirectNXAML.Model;
using DirectNXAML.Renderers;
using DirectNXAML.Services.Enums;
using static PInvoke.Kernel32;
using Microsoft.UI.Input;
using System.Runtime.InteropServices;

namespace DirectNXAML.ViewModels
{
    internal class DirectNPageViewModel : ObservableObject
	{
        /// <summary>
        /// the Renderer
        /// </summary>
		RendererBase m_renderer = null;
        internal RendererBase PageRenderer { get { return m_renderer; } set { m_renderer = value; } }

        // for a simple line drawing state transition (should elevate to Model layer)
        enum ELineGetState : int
        {
            none = -1,
            Begin = 0,
			Pressed,
            maxEnum
        }
        ELineGetState m_state = ELineGetState.none;

        internal delegate void SetCursor(int _x, int _y);
        internal SetCursor SetCursorMethods;

        /// <summary>
        /// temporary line object
        /// </summary>
        private FLine3D m_lin;

        /// <summary>
        /// Constructor
        /// </summary>
        internal DirectNPageViewModel()
        {
            m_renderer = new Dx11Renderer();
            SetState_DrawLineCommand += SetState_DrawLine;
            SetState_SelectCommand += SetState_Select;

            ShaderPanel_SizeChangedCommand = new RelayCommand<SizeChangedEventArgs>(ShaderPanel_SizeChanged);
            ShaderPanel_PointerMovedCommand = new RelayCommand<PointerRoutedEventArgs>(ShaderPanel_PointerMoved);
			ShaderPanel_PointerPressedCommand = new RelayCommand<PointerRoutedEventArgs>(ShaderPanel_PointerPressed);
			ShaderPanel_PointerReleasedCommand = new RelayCommand<PointerRoutedEventArgs>(ShaderPanel_PointerReleased);
            ShaderPanel_PointerWheelChangedCommand  =  new RelayCommand<PointerRoutedEventArgs>(ShaderPanel_PointerWheelChanged);
            ColorData.ResetLineColor();
            UpdateVertexCountDisplay();
            SetLineState(ELineGetState.none);  // initial mode.
            SetWorldOriginPositionText();
        }
        public void Dispose()
        {
            m_renderer.StopRendering();
            SCPSize_Changed -= m_renderer.Panel_SizeChanged;
        }

        #region line draw state machine
        internal RoutedEventHandler SetState_DrawLineCommand { get; private set; }
        private void SetState_DrawLine(object sender, RoutedEventArgs e)
		{
            if (m_state == ELineGetState.none)
            {
                SetLineState(ELineGetState.Begin);
                SetWheelScale();
            }
        }
        internal RoutedEventHandler SetState_SelectCommand { get; private set; }
        private void SetState_Select(object sender, RoutedEventArgs e)
        {
            // reset any state machine before here
            SetLineState(ELineGetState.none);
        }
        internal ICommand ShaderPanel_SizeChangedCommand { get; private set; }
        private void ShaderPanel_SizeChanged(SizeChangedEventArgs args)
        {
            SetActualSizeText();
            SCPSize_Changed?.Invoke(this, args);
        }

        /// <summary>
        /// absolute current pointer position (on real)
        /// </summary>
        double m_absX = 0, m_absY = 0;
        /// <summary>
        /// relative position of world origin (on real)
        /// </summary>
        double m_orgX = 0, m_orgY = 0;
        /// <summary>
        /// centralized pointer position on SwapChainPanel (on screen)
        /// </summary>
        double m_cX = 0, m_cY = 0;

        public ICommand ShaderPanel_PointerMovedCommand { get; private set; }
		private void ShaderPanel_PointerMoved(PointerRoutedEventArgs args)
        {
            GetMouseButton(args);
            SetLocalPointerText();
            //m_cx = ActualWidth / 2; m_cy = ActualHeight / 2;
            m_cX = m_local_point.X - ActualWidth / 2;
            m_cY = ActualHeight / 2 - m_local_point.Y;
            SetCentralizedPositionText();
            SetNormalizedPointerPosition();
            args.Handled = true;

            // should elevate to Model layer
            if (m_state == ELineGetState.Pressed)
            {
                if (m_left_pressed)
                {
                    // 2d translate (absolute: store data)
                    //m_absX = (m_cX + (double)m_renderer.EyePosition.X) * (double)m_viewScale;
                    //m_absY = (m_cY + (double)m_renderer.EyePosition.Y) * (double)m_viewScale;
                    m_absX = m_cX * m_viewScale - m_orgX;
                    m_absY = m_cY * m_viewScale - m_orgY;
                    SetWorldPositionText();

                    ((App)Application.Current).DrawManager.DelLast();
                    m_lin.Ep.X = (float)m_absX;
                    m_lin.Ep.Y = (float)m_absY;
                    ((App)Application.Current).DrawManager.AddLast(m_lin);
                    SetLineText();
                    m_renderer.UpdateVertexBuffer();
                }
                else
                {
                    // cancel
                    ((App)Application.Current).DrawManager.DelLast();
                    SetLineState(ELineGetState.Begin);
                }
            }
        }

		internal ICommand ShaderPanel_PointerPressedCommand { get; private set; }
		private void ShaderPanel_PointerPressed(PointerRoutedEventArgs args)
        {
            GetMouseButton(args);
            args.Handled = true;

            // Centerlized Current Position
            //m_cx = ActualWidth / 2; m_cy = ActualHeight / 2;
            m_cX = m_pressed_point.X - ActualWidth / 2;
            m_cY = ActualHeight / 2 - m_pressed_point.Y;
            SetCentralizedPositionText();
            // 2d translate (absolute: store data)
            // to see larger, m_viewScale get smaller.
            //m_absX = m_cX * m_viewScale - (double)m_renderer.EyePosition.X;
            //m_absY = m_cY * m_viewScale - (double)m_renderer.EyePosition.Y;
            m_absX = m_cX * m_viewScale - m_orgX;
            m_absY = m_cY * m_viewScale - m_orgY;
            SetWorldPositionText();

            // should elevate to Model layer
            if (m_state == ELineGetState.Begin)
            {
                if (m_left_pressed)
                {
                    ColorData.SetLine(ColorData.RubberLine);

                    m_lin = new FLine3D();
                    m_lin.Sp.X = m_lin.Ep.X = (float)m_absX;
                    m_lin.Sp.Y = m_lin.Ep.Y = (float)m_absY;

                    m_lin.SetCol(ColorData.Line);   // blue rubber
                    ((App)Application.Current).DrawManager.AddLast(m_lin);
                    SetLineText();
                    UpdateVertexCountDisplay();
                    m_renderer.UpdateVertexBuffer();
                    SetLineState(ELineGetState.Pressed);
                }
            }
            if (m_middle_pressed)
            {
                m_renderer.EyePosition = new((float)m_absX, (float)m_absY, m_renderer.EyePosition.Z, m_renderer.EyePosition.W);
                SetCursorMethods?.Invoke((int)ActualWidth / 2, (int)ActualHeight / 2);
                m_orgX -= m_cX * m_viewScale;
                m_orgY -= m_cY * m_viewScale;
                SetWorldOriginPositionText();
                //(Application.Current as App).CurrentWindow.CoreWindow.PointerPosition = new(m_renderer.EyePosition.X, m_renderer.EyePosition.Y);
                //(Application.Current as App).CurrentWindow.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Cross, 0);
            }
        }

		internal ICommand ShaderPanel_PointerReleasedCommand { get; private set; }
		private void ShaderPanel_PointerReleased(PointerRoutedEventArgs args)
        {
            GetMouseButton(args);
            args.Handled = true;
            SetLocalPointerText();
            //m_cx = ActualWidth / 2; m_cy = ActualHeight / 2;
            m_cX = m_released_point.X - ActualWidth / 2;
            m_cY = ActualHeight / 2 - m_released_point.Y;
            SetCentralizedPositionText();

            // should elevate to Model layer
            if (m_state == ELineGetState.Pressed)
            {
                ColorData.SetLine(ColorData.FixedLine);

                // 2d translate (absolute: store data)
                //m_absX = (m_cX + (double)m_renderer.EyePosition.X) * (double)m_viewScale;
                //m_absY = (m_cY + (double)m_renderer.EyePosition.Y) * (double)m_viewScale;
                m_absX = m_cX * m_viewScale - m_orgX;
                m_absY = m_cY * m_viewScale - m_orgY;
                SetWorldPositionText();

                ((App)Application.Current).DrawManager.DelLast();
                m_lin.Ep.X = (float)m_absX;
                m_lin.Ep.Y = (float)m_absY;
                m_lin.SetCol(ColorData.Line); // white : Rocked
                ((App)Application.Current).DrawManager.AddLast(m_lin);
                SetLineText();
                m_renderer.UpdateVertexBuffer();
                SetLineState(ELineGetState.Begin);
            }
        }
        
        /// <summary>
        /// cancel drawing to go another state machine
        /// </summary>
        private void CancelLineDrawing()
        {
            if (m_state == ELineGetState.Pressed)
            {
                ((App)Application.Current).DrawManager.DelLast();
                m_lin.Clear();
                SetLineState(ELineGetState.none);
            }
        }


        /// <summary>
        /// recognize mouse button
        /// </summary>
        /// <param name="_args"></param>
        private void GetMouseButton(in PointerRoutedEventArgs _args)
        {
            Pointer ptr = _args.Pointer;
            if (ptr.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                // To get mouse state, we need extended pointer details.
                // We get the pointer info through the getCurrentPoint method
                // of the event argument. 
                Microsoft.UI.Input.PointerPoint ptrPt = _args.GetCurrentPoint(m_renderer.SCPanel);
                IsLeftButtonPressed = ptrPt.Properties.IsLeftButtonPressed;
                IsMiddleButtonPressed = ptrPt.Properties.IsMiddleButtonPressed;
                IsRightButtonPressed = ptrPt.Properties.IsRightButtonPressed;
            }
        }
        internal ICommand ShaderPanel_PointerWheelChangedCommand;
        public void ShaderPanel_PointerWheelChanged(PointerRoutedEventArgs _args)
        {
            SetWheelScale();
        }

        private double m_viewScale = 1.0;
        private void SetWheelScale()
        {
            double d = Math.Round(1.0 - (double)m_mouse_wheel_delta / 1000.0, 3, MidpointRounding.AwayFromZero);
            m_viewScale = (d <= 0.001) ? 0.001 : d;
            m_renderer.ViewScale = (float)m_viewScale;
            SetViewScaleText();
        }
        #endregion

        #region for display

        bool m_left_pressed = false;
        bool m_middle_pressed = false;
        bool m_right_pressed = false;
        private EMouseButtonStatus m_mouse_status;
        internal EMouseButtonStatus MouseStatus { get => m_mouse_status; private set => SetProperty(ref m_mouse_status, value); }
        internal bool IsLeftButtonPressed
        {
            get => m_left_pressed;
            set
            {
                if (m_left_pressed != value)
                {
                    SetProperty(ref m_left_pressed, value);
                    m_mouse_status = (EMouseButtonStatus)Helpers.MouseStatus.GetButtons(m_left_pressed, m_middle_pressed, IsRightButtonPressed);
                }
            }
        }
        internal bool IsMiddleButtonPressed
        {
            get => m_middle_pressed;
            set
            {
                if (m_middle_pressed != value)
                {
                    SetProperty(ref m_middle_pressed, value);
                    m_mouse_status = (EMouseButtonStatus)Helpers.MouseStatus.GetButtons(m_left_pressed, m_middle_pressed, m_right_pressed);
                }
            }
        }

        internal bool IsRightButtonPressed
        {
            get => m_right_pressed;
            set
            {
                if (m_right_pressed != value)
                {
                    SetProperty(ref m_right_pressed, value);
                    m_mouse_status = (EMouseButtonStatus)Helpers.MouseStatus.GetButtons(m_left_pressed, m_middle_pressed, m_right_pressed);
                }
            }
        }
        private int m_mouse_wheel_delta = 0;
        internal int MouseWheelDelta
        {
            get => m_mouse_wheel_delta;
            set => m_mouse_wheel_delta = value;
        }

        int m_vertex_count = 0;
        private string m_vertex_count_text = "Number of Vertecies: ";
        internal string VertexCountText { get => m_vertex_count_text; set => SetProperty(ref m_vertex_count_text, value); }
        public int VertexCount { get => m_vertex_count; set => SetProperty(ref m_vertex_count, value); }
        private void UpdateVertexCountDisplay()
        {
            VertexCount = ((App)Application.Current).DrawManager.VertexData.Length;
            VertexCountText = "Number of Vertecies: " + VertexCount.ToString();
        }

        private string m_actual_size_text = "SCP Actual size (W x H): ";
        internal string ActualSizeText { get => m_actual_size_text; set => SetProperty(ref m_actual_size_text, value); }
        private void SetActualSizeText()
        {
            StringBuilder sb = new StringBuilder("SCP Actual size (W x H): ");
            sb.Append(SwapChainActualSize.X.ToString("F0", CultureInfo.InvariantCulture))
                .Append(" x ")
                .Append(SwapChainActualSize.Y.ToString("F0", CultureInfo.InvariantCulture));
            ActualSizeText = sb.ToString();
            sb.Clear();
        }

        string m_local_pointer_text = "Local Pointer:";
        internal string LocalPointerText { get => m_local_pointer_text; set => SetProperty(ref m_local_pointer_text, value); }
        private void SetLocalPointerText()
        {
            var sb = new StringBuilder("Local Pointer:(");
            sb.Append(m_local_point.X.ToString("F0", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_local_point.Y.ToString("F0", CultureInfo.InvariantCulture))
                .Append(") ");
            LocalPointerText = sb.ToString();
            sb.Clear();
        }

        string m_normalized_pointer_text= "Normalized Pointer:";
        internal string NormalizedPointerText { get => m_normalized_pointer_text; set => SetProperty(ref m_normalized_pointer_text, value); }
        private void SetNormalizedPointerPosition()
        {
            m_normalized_local_point.X = m_local_point.X / ActualWidth;
            m_normalized_local_point.Y = m_local_point.Y / ActualHeight;
            StringBuilder sb = new StringBuilder("Normalized Pointer:(");
            sb.Append(m_normalized_local_point.X.ToString("F3", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_normalized_local_point.Y.ToString("F3", CultureInfo.InvariantCulture))
                .Append(") ");
            NormalizedPointerText = sb.ToString();
            sb.Clear();
        }

        string m_view_scale_text = "View Scale: ";
        internal string ViewScaleText { get => m_view_scale_text; set => SetProperty(ref m_view_scale_text, value); }
        private void SetViewScaleText()
        {
            StringBuilder sb = new StringBuilder("View Scale: ");
            var a = 1000 / m_viewScale;
            if (a < 1000)
            {
                sb.Append(a.ToString("F0", CultureInfo.InvariantCulture)).Append("/1000");
            }
            else
            {
                a = 1 / m_viewScale;
                sb.Append(a.ToString("F3", CultureInfo.InvariantCulture));
            }
            ViewScaleText = sb.ToString();
            sb.Clear();
        }

        string m_drawing_line_text = "Line: ";
        internal string LineText { get => m_drawing_line_text; set => SetProperty(ref m_drawing_line_text, value); }
        private void SetLineText()
        {
            StringBuilder sb = new StringBuilder("Line: (");
            sb.Append(m_lin.Sp.X.ToString("F3", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_lin.Sp.Y.ToString("F3", CultureInfo.InvariantCulture))
                .Append(")-(")
                .Append(m_lin.Ep.X.ToString("F3", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_lin.Ep.Y.ToString("F3", CultureInfo.InvariantCulture))
                .Append(")");
            LineText = sb.ToString();
            sb.Clear();
        }

        string m_state_name_text = "State: ";
        internal string StateName { get => m_state_name_text; set => SetProperty(ref m_state_name_text, value); }
        private void SetLineState(ELineGetState _s)
        {
            m_state = _s;
            StateName = "State: " + System.Enum.GetName(typeof(ELineGetState), m_state);
        }

        private string m_centralized_position_text = "Centralized Position: ";
        internal string CentralizedPositionText { get => m_centralized_position_text; set => SetProperty(ref m_centralized_position_text, value); }
        /// <summary>
        /// show centralized position: m_cx, m_cy
        /// </summary>
        private void SetCentralizedPositionText()
        {
            StringBuilder sb = new StringBuilder("Centralized Position: (");
            sb.Append(m_cX.ToString("F0", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_cY.ToString("F0", CultureInfo.InvariantCulture))
                .Append(") ");
            CentralizedPositionText = sb.ToString();
            sb.Clear();
        }
        private string m_world_orign_position_text = "World Orgin: ";
        internal string WorldOriginPositionText { get => m_world_orign_position_text; set => SetProperty(ref m_world_orign_position_text, value); }
        /// <summary>
        /// show world origin position: m_orgX, m_orgY
        /// </summary>
        private void SetWorldOriginPositionText()
        {
            StringBuilder sb = new StringBuilder("World Orgin: (");
            sb.Append(m_orgX.ToString("F3", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_orgY.ToString("F3", CultureInfo.InvariantCulture))
                .Append(") ");
            WorldOriginPositionText = sb.ToString();
            sb.Clear();
        }
        private string m_world_position_text = "World Position: ";
        internal string WorldPositionText { get => m_world_position_text; set => SetProperty(ref m_world_position_text, value); }
        /// <summary>
        /// show world absolute position: m_absX, m_absY
        /// </summary>
        private void SetWorldPositionText()
        {
            StringBuilder sb = new StringBuilder("World Position: (");
            sb.Append(m_absX.ToString("F3", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_absY.ToString("F3", CultureInfo.InvariantCulture))
                .Append(") ");
            WorldPositionText = sb.ToString();
            sb.Clear();
        }
        #endregion

        #region interface variables / events
        public event SizeChangedEventHandler SCPSize_Changed;
        internal double ActualHeight { get; set; }
        internal double ActualWidth { get; set; }
        internal Vector2 SwapChainActualSize { get; set; }

        double m_local_height, m_local_width;
		internal double LocalHeight { get => m_local_height; set => m_local_height = value; }
		internal double LocalWidth { get => m_local_width; set => m_local_width = value; }

		Windows.Foundation.Point m_local_point, m_normalized_local_point;
		internal Windows.Foundation.Point LocalPointerPoint { get => m_local_point; set => m_local_point = value; }
        internal Windows.Foundation.Point NormalizedPointerPoint { get => m_normalized_local_point; set =>  m_normalized_local_point = value; }

        Windows.Foundation.Point m_pressed_point, m_normalized_pressed_point;
        internal Windows.Foundation.Point PressedPoint { get => m_pressed_point; set => m_pressed_point = value; }
        internal Windows.Foundation.Point NormalizedPressedPoint { get => m_normalized_pressed_point; set => m_normalized_pressed_point = value; }
		
		Windows.Foundation.Point m_released_point, m_normalized_released_point;
        internal Windows.Foundation.Point ReleasedPoint { get => m_released_point; set => m_released_point = value; }
        internal Windows.Foundation.Point NormalizedReleasedPoint { get => m_normalized_released_point; set => m_normalized_released_point = value; }
#endregion
    }
}
