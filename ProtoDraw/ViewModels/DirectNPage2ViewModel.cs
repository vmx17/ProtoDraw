﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DirectN;
using DirectNXAML.DrawData;
using DirectNXAML.Helpers;
using DirectNXAML.Model;
using DirectNXAML.Renderers;
using JeremyAnsel.DirectX.DXMath;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
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

namespace DirectNXAML.ViewModels
{
    internal class DirectNPage2ViewModel : ObservableObject
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

        /// <summary>
        /// temporary line object
        /// </summary>
        private FLine3D m_lin;

        /// <summary>
        /// Constructor
        /// </summary>
        internal DirectNPage2ViewModel()
        {
            m_renderer = new Dx11Renderer2();
            //SCPSize_Changed += m_renderer.Panel_SizeChanged;
            SetState_DrawLineCommand += SetState_DrawLine;
            SetState_SelectCommand += SetState_Select;

            ShaderPanel_SizeChangedCommand = new RelayCommand<SizeChangedEventArgs>(ShaderPanel_SizeChanged);
            ShaderPanel_PointerMovedCommand = new RelayCommand<PointerRoutedEventArgs>(ShaderPanel_PointerMoved);
			ShaderPanel_PointerPressedCommand = new RelayCommand<PointerRoutedEventArgs>(ShaderPanel_PointerPressed);
			ShaderPanel_PointerReleasedCommand = new RelayCommand<PointerRoutedEventArgs>(ShaderPanel_PointerReleased);
            ColorData.ResetLineColor();
            UpdateVertexCountDisplay();
            SetStateName(ELineGetState.none);  // initial mode.
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
                SetStateName(ELineGetState.Begin);
            }
        }
        internal RoutedEventHandler SetState_SelectCommand { get; private set; }
        private void SetState_Select(object sender, RoutedEventArgs e)
        {
            // reset any state machine before here
            SetStateName(ELineGetState.none);
        }
        internal ICommand ShaderPanel_SizeChangedCommand { get; private set; }
        private void ShaderPanel_SizeChanged(SizeChangedEventArgs args)
        {
            SetLocalSizeText();
            SetActualSizeText();
            var s = args.NewSize;
            RenderWidth = s.Width;
            RenderHeight = s.Height;
            SCPSize_Changed?.Invoke(this, args);
        }

        // for just a test drawing
        double m_absX, m_absY;
        double m_cx, m_cy;
        double m_nowX, m_nowY;  // position on local screen
        MathNet.Numerics.LinearAlgebra.Matrix<Single> m_projection, m_inversedProjection;
        public ICommand ShaderPanel_PointerMovedCommand { get; private set; }
		private void ShaderPanel_PointerMoved(PointerRoutedEventArgs args)
        {
            SetLocalPointerText();
            SetNormalizedPointerPosition();
            args.Handled = true;

            // should elevate to Model layer
            if (m_state == ELineGetState.Pressed)
            {
                // 2d translate (absolute: store data)
                m_absX = m_local_point.X - m_cx + m_renderer.EyePosition.X;
                m_absY = m_cy - m_local_point.Y + m_renderer.EyePosition.Y;
                SetWorldPositionText();
                // current point (local: center origin)
                m_nowX = m_absX - m_renderer.EyePosition.X;
                m_nowY = m_absY - m_renderer.EyePosition.Y;
                // projection
                var a = MatrixVectorOperation.Multiply(m_renderer.Projection, new Vector4((float)m_nowX, (float)m_nowY, 0.0f, 1.0f));

                ((App)Application.Current).DrawManager.DelLast();
                m_lin.Ep.X = (float)m_absX;
                m_lin.Ep.Y = (float)m_absY;
                ((App)Application.Current).DrawManager.AddLast(m_lin);
                SetLineText();
                m_renderer.UpdateVertexBuffer();
            }
        }

		internal ICommand ShaderPanel_PointerPressedCommand { get; private set; }
		private void ShaderPanel_PointerPressed(PointerRoutedEventArgs args)
        {
            SetNormalizedPointerPressed();
            args.Handled = true;

            // should elevate to Model layer
            if (m_state == ELineGetState.Begin)
            {
                ColorData.SetLine(ColorData.RubberLine);

                m_cx = ActualWidth / 2; m_cy = ActualHeight / 2;
                // 2d translate (absolute: store data)
                m_absX = m_pressed_point.X - m_cx + m_renderer.EyePosition.X;
                m_absY = m_cy - m_pressed_point.Y + m_renderer.EyePosition.Y;
                SetWorldPositionText();
                var a = MatrixVectorOperation.Multiply(m_renderer.Projection, new Vector4((float)m_absX, (float)m_absY, 0.0f, 1.0f));
                m_nowX = a.X;
                m_nowY = a.Y;
                SetProjectedPositionText();
                // projection
                var arr = m_renderer.Projection.ToArray();
                var M = Matrix<float>.Build;
                m_inversedProjection = M.Dense(4, 4, arr).Inverse();
                float[] b = { (float)m_nowX, (float)m_nowY, 0.0f, 1.0f };
                var V = MathNet.Numerics.LinearAlgebra.Vector<float>.Build;
                var x = m_inversedProjection * V.Dense(b);

                m_lin = new FLine3D();
                m_lin.Sp.X = m_lin.Ep.X = (float)m_absX;
                m_lin.Sp.Y = m_lin.Ep.Y = (float)m_absY;

                m_lin.SetCol(ColorData.Line);   // blue rubber
                ((App)Application.Current).DrawManager.AddLast(m_lin);
                SetLineText();
                UpdateVertexCountDisplay();
                m_renderer.UpdateVertexBuffer();
                SetStateName(ELineGetState.Pressed);
            }
        }

		internal ICommand ShaderPanel_PointerReleasedCommand { get; private set; }
		private void ShaderPanel_PointerReleased(PointerRoutedEventArgs args)
        {
            SetNormalizedPointerReleased();
            args.Handled = true;

            // should elevate to Model layer
            if (m_state == ELineGetState.Pressed)
            {
                ColorData.SetLine(ColorData.FixedLine);

                // 2d translate (absolute: store data)
                m_absX = m_local_point.X - m_cx + m_renderer.EyePosition.X;
                m_absY = m_cy - m_local_point.Y + m_renderer.EyePosition.Y;
                SetWorldPositionText();
                // projection
                var a = MatrixVectorOperation.Multiply(m_renderer.Projection, new Vector4((float)m_absX, (float)m_absY, 0.0f, 1.0f));
                SetProjectedPositionText();

                ((App)Application.Current).DrawManager.DelLast();
                m_lin.Ep.X = (float)m_absX;
                m_lin.Ep.Y = (float)m_absY;
                m_lin.SetCol(ColorData.Line); // white : Rocked
                ((App)Application.Current).DrawManager.AddLast(m_lin);
                SetLineText();
                m_renderer.UpdateVertexBuffer();
                SetStateName(ELineGetState.Begin);
            }
        }

        private void CancelLineDrawing()
        {
            if (m_state == ELineGetState.Pressed)
            {
                ((App)Application.Current).DrawManager.DelLast();
                m_lin.Clear();
                SetStateName(ELineGetState.none);
            }
        }
        #endregion

        #region for display
        int m_vertex_count = 0;
        private string m_vertex_count_text = "Vertecies: ";
        internal string VertexCountText { get => m_vertex_count_text; set => SetProperty(ref m_vertex_count_text, value); }
        public int VertexCount { get => m_vertex_count; set => SetProperty(ref m_vertex_count, value); }
        private void UpdateVertexCountDisplay()
        {
            VertexCount = ((App)Application.Current).DrawManager.VertexData.Length;
            VertexCountText = "Vertecies: " + VertexCount.ToString();
        }

        private string m_actual_size_text = "Actual size (w x h): ";
        internal string ActualSizeText { get => m_actual_size_text; set => SetProperty(ref m_actual_size_text, value); }
        private void SetActualSizeText()
        {
            StringBuilder sb = new StringBuilder("Actual size (w x h): ");
            sb.Append(SwapChainActualSize.X.ToString("F3", CultureInfo.InvariantCulture))
                .Append(" x ")
                .Append(SwapChainActualSize.Y.ToString("F3", CultureInfo.InvariantCulture));
            ActualSizeText = sb.ToString();
            sb.Clear();
        }

        string m_local_pointer_text = "Local Pointer:";
        internal string LocalPointerText { get => m_local_pointer_text; set => SetProperty(ref m_local_pointer_text, value); }
        private void SetLocalPointerText()
        {
            var sb = new StringBuilder("Local Pointer:(");
            sb.Append(m_local_point.X.ToString("F3", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_local_point.Y.ToString("F3", CultureInfo.InvariantCulture))
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

        string m_normalized_pointer_pressed_text = "Normalized Pressed";
        internal string NormalizedPointerPressedText { get => m_normalized_pointer_pressed_text; set => SetProperty(ref m_normalized_pointer_pressed_text, value); }
        private void SetNormalizedPointerPressed()
        {
            m_normalized_pressed_point.X = m_pressed_point.X / ActualWidth;
            m_normalized_pressed_point.Y = m_pressed_point.Y / ActualHeight;
            StringBuilder sb = new StringBuilder("Normalized Pressed:(");
            sb.Append(m_normalized_pressed_point.X.ToString("F3", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_normalized_pressed_point.Y.ToString("F3", CultureInfo.InvariantCulture))
                .Append(") ");
            NormalizedPointerPressedText = sb.ToString();
            sb.Clear();
        }

        string m_normalized_pointer_released_text = "Normalized Released";
        internal string NormalizedPointerReleasedText { get => m_normalized_pointer_released_text; set => SetProperty(ref m_normalized_pointer_released_text, value); }
        private void SetNormalizedPointerReleased()
        {
            m_normalized_released_point.X = m_released_point.X / ActualWidth;
            m_normalized_released_point.Y = m_released_point.Y / ActualHeight;
            StringBuilder sb = new StringBuilder("Normalized Released:(");
            sb.Append(m_normalized_released_point.X.ToString("F3", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_normalized_released_point.Y.ToString("F3", CultureInfo.InvariantCulture))
                .Append(") ");
            NormalizedPointerReleasedText = sb.ToString();
        }

        string m_local_size_text = "Local Size: ";
        internal string LocalSizeText { get => m_local_size_text; set => SetProperty(ref m_local_size_text, value); }
        private void SetLocalSizeText()
        {
            StringBuilder sb = new StringBuilder("Local Size: (");
            sb.Append(m_local_width.ToString("F3", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_local_height.ToString("F3", CultureInfo.InvariantCulture))
                .Append(") ");
            LocalSizeText = sb.ToString();
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
        private void SetStateName(ELineGetState _s)
        {
            m_state = _s;
            StateName = "State: " + System.Enum.GetName(typeof(ELineGetState), m_state);
        }

        private string m_world_position_text = "World Position: ";
        internal string WorldPositionText { get => m_world_position_text; set => SetProperty(ref m_world_position_text, value); }
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

        private string m_projected_position_text = "Projected Position: ";
        internal string ProjectedPositionText { get => m_projected_position_text; set => SetProperty(ref m_projected_position_text, value); }
        private void SetProjectedPositionText()
        {
            StringBuilder sb = new StringBuilder("Projected Position: (");
            sb.Append(m_nowX.ToString("F3", CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(m_nowY.ToString("F3", CultureInfo.InvariantCulture))
                .Append(") ");
            ProjectedPositionText = sb.ToString();
            sb.Clear();
        }
        #endregion

        #region interface variables / events
        public event SizeChangedEventHandler SCPSize_Changed;

        internal WeakReference<Page> ViewPage { get; set; }
        internal WeakReference<SwapChainPanel> ViewSwapChainPanel { get; set; }

        internal double RenderHeight { get; set; }
        internal double RenderWidth { get; set; }
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
