﻿using System.Collections.Generic;
using System.Linq;

namespace DirectNXAML.DrawData
{
    /// <summary>
    /// 3D vertex in float
    /// </summary>
    public class FVertex3D : Primitive
    {
        public new static readonly int Stride = 12;
        // inner struct: POD
        private struct CoreData
        {
            internal float3 pos;// => new(0f, 0f, fZ);       // x, y, z
            internal float3 nor;// => new(0f, 0f, -1f );     // NOR
            internal float2 tex;// => new (0, 0f );          // TEX
            internal float4 col;// => new(1f, 1f, 1f, 1f );  // rgba
        }
        CoreData m_c;
        public FVertex3D()
        {
            m_c.pos = new(0f, 0f, Fz);      // x, y, z
            m_c.nor = new(0f, 0f, -1f);     // NOR
            m_c.tex = new(0f, 0f);          // TEX
            m_c.col = new(1f, 1f, 1f, 1f);  // rgba
        }
        public FVertex3D(in FVertex3D _p) : this()
        {
            m_c.pos = _p.Pos;
        }
        // at least, two float should be specified.
        public FVertex3D(float _x, float _y, float _r = 1.0f, float _g = 1.0f, float _b = 1.0f) : this()
        {
            m_c.pos.X = _x;
            m_c.pos.Y = _y;
            m_c.col.X = _r;
            m_c.col.Y = _g;
            m_c.col.Z = _b;
        }
        public FVertex3D(float3 _pos) : this()
        {
            m_c.pos = _pos;
        }
        public FVertex3D(float3 _pos, float3 _nor) : this()
        {
            m_c.pos = _pos;
            m_c.nor = _nor;
        }
        public FVertex3D(float3 _pos, float4 _col) : this()
        {
            m_c.pos = _pos;
            m_c.col = _col;
        }
        public FVertex3D(float3 _pos, float3 _nor, float2 _tex) : this()
        {
            m_c.pos = _pos;
            m_c.nor = _nor;
            m_c.tex = _tex;
        }
        public FVertex3D(float3 _pos, float3 _nor, float2 _tex, float4 _col) : this()
        {
            m_c.pos = _pos;
            m_c.nor = _nor;
            m_c.tex = _tex;
            m_c.col = _col;
        }
        public float X { get { return m_c.pos.X; } set { m_c.pos.X = value; } }
        public float Y { get { return m_c.pos.Y; } set { m_c.pos.Y = value; } }
        public float Z { get { return m_c.pos.Z; } set { m_c.pos.Z = value; } }
        public float Nx { get { return m_c.nor.X; } set { m_c.nor.X = value; } }
        public float Ny { get { return m_c.nor.Y; } set { m_c.nor.Y = value; } }
        public float Nz { get { return m_c.nor.Z; } set { m_c.nor.Z = value; } }
        public float Tx { get { return m_c.tex.X; } set { m_c.tex.X = value; } }
        public float Ty { get { return m_c.tex.Y; } set { m_c.tex.Y = value; } }
        public float R { get { return m_c.col.X; } set { m_c.col.X = value; } }
        public float G { get { return m_c.col.Y; } set { m_c.col.Y = value; } }
        public float B { get { return m_c.col.Z; } set { m_c.col.Z = value; } }
        public float A { get { return m_c.col.W; } set { m_c.col.W = value; } }
        public float3 Pos { get { return m_c.pos; } set { m_c.pos = value; } }
        public float3 Nor { get { return m_c.nor; } set { m_c.nor = value; } }
        public float2 Tex { get { return m_c.tex; } set { m_c.tex = value; } }
        public float4 Col { get { return m_c.col; } set { m_c.col = value; } }
        public void SetPos(float _x, float _y, float _z = Fz)
        {
            m_c.pos.X = _x; m_c.pos.Y = _y; m_c.pos.Z = _z;
        }
        public void SetPos(float3 _p)
        {
            m_c.pos = _p;
        }
        public void SetNor(float _x, float _y, float _z = -1.0f)
        {
            m_c.nor.X = _x; m_c.nor.Y = _y; m_c.nor.Z = _z;
        }
        public void SetNor(float3 _n)
        {
            m_c.nor = _n;
        }
        public void SetTex(float _x, float _y)
        {
            m_c.tex.X = _x; m_c.tex.Y = _y;
        }
        public void SetTex(float2 _t)
        {
            m_c.tex = _t;
        }
        public void SetCol(float _r, float _g, float _b, float _a = 1.0f)
        {
            m_c.col.X = _r; m_c.col.Y = _g; m_c.col.Z = _b; m_c.col.W = _a;
        }
        public override void SetCol(float4 _c)
        {
            m_c.col = _c;
        }
        public override int ByteSize { get => sizeof(float) * Stride; }
        public override float[] ToFloatArray()
        {
            // here it makes float[12]
            return new float[]
            {
                m_c.pos.X, m_c.pos.Y, m_c.pos.Z,
                m_c.nor.X, m_c.nor.Y, m_c.nor.Z,
                m_c.tex.X, m_c.tex.Y,
                m_c.col.X, m_c.col.Y, m_c.col.Z, m_c.col.W
            };
        }
        public List<float> ToList()
        {
            return this.ToFloatArray().ToList();
        }
    }
}