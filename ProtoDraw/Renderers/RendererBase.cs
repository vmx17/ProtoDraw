using DirectN;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;

namespace DirectNXAML.Renderers
{
    public abstract class RendererBase : IRendererBase, IDisposable
    {
        protected object m_CriticalLock = new();
        protected RendererBase() {; }

        // why does arguments define the abstract/virtual type
        public abstract void Dispose();
        public abstract void Initialize(uint _width, uint _height);
        public abstract void StartRendering();
        public abstract void StopRendering();
        public abstract bool Render();

        public abstract void SetSwapChainPanel(SwapChainPanel panel);
        public abstract void Panel_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e);

        protected float[] m_renderBackgroundColor = new float[] { 0.025f, 0.025f, 0.025f, 1 };
        public virtual void SetBGColor(float _r, float _g, float _b, float _a = 1.0f)
        {
            StopRendering();
            lock (m_CriticalLock)
            {
                m_renderBackgroundColor[0] = _r;
                m_renderBackgroundColor[1] = _g;
                m_renderBackgroundColor[2] = _b;
                m_renderBackgroundColor[3] = _a;
            }
            StartRendering();
        }

        public void SetBGColor(Windows.UI.Color _col)
        {
            StopRendering();
            lock (m_CriticalLock)
            {
                m_renderBackgroundColor[0] = ((float)_col.R) / 256f;
                m_renderBackgroundColor[1] = ((float)_col.G) / 256f;
                m_renderBackgroundColor[2] = ((float)_col.B) / 256f;
                m_renderBackgroundColor[3] = ((float)_col.A) / 256f;
            }
            StartRendering();
        }

        protected D2D_MATRIX_4X4_F m_transform, m_projection;
        public virtual D2D_MATRIX_4X4_F Transform { get => m_transform; set => m_transform = value; }
        public virtual D2D_MATRIX_4X4_F Projection { get => m_projection; set => m_projection = value; }

        public virtual void UpdateVertexBuffer() {; }

        [DllImport("kernel32", ExactSpelling = true, EntryPoint = "RtlMoveMemory")]
        protected static extern void CopyMemory(IntPtr destination, IntPtr source, IntPtr length);
    }
}
