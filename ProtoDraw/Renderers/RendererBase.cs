﻿using DirectN;
using JeremyAnsel.DirectX.DXMath;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DirectNXAML.Renderers
{
    public abstract class RendererBase : IRenderBase, IDisposable
    {
        protected object m_CriticalLock = new();
        protected RendererBase() {; }

        // why does arguments define the abstract/virtual type
        public abstract void Dispose();
        public abstract void Initialize(uint _width, uint _height);
        public abstract void StartRendering();
        public abstract void StopRendering();
        public abstract bool Render();

        public abstract void SetSwapChainPanel(SwapChainPanel panel) ;
        public abstract void Panel_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e) ;
        public abstract void SetBGColor(float _r, float _g, float _b, float _a = 1.0f) ;


        protected D2D_MATRIX_4X4_F m_transform, m_projection;
        public virtual D2D_MATRIX_4X4_F Transform { get => m_transform; set => m_transform = value; }
        public virtual D2D_MATRIX_4X4_F Projection { get => m_projection; set => m_projection = value; }

        protected float m_aspectRatio = 1.0f;
        protected XMVector m_eyePosition = new(0, 0, 1500, 1);  // view point
        protected XMVector m_eyeDirection = new(0, 0, 1, 1);    // target
        protected XMVector m_forcusPosition = new(0, 0, 0, 1);    // target
        protected XMVector m_upDirection = new(0, 1, 0, 1);     // up

        public virtual float AspectRatio { get => m_aspectRatio; set => m_aspectRatio = value; }
        public virtual XMVector EyePosition { get => m_eyePosition; set => m_eyePosition = value; }
        public virtual XMVector EyeDirection { get => m_eyeDirection; set => m_eyeDirection = value; }
        public virtual XMVector ForcusPosition { get => m_forcusPosition; set => m_forcusPosition = value; }
        public virtual XMVector UpDirection { get => m_upDirection; set => m_upDirection = value; }

        public virtual void UpdateVertexBuffer() {; }

        [DllImport("kernel32", ExactSpelling = true, EntryPoint = "RtlMoveMemory")]
        protected static extern void CopyMemory(IntPtr destination, IntPtr source, IntPtr length);
    }
}