using System;
using System.Collections.Generic;
using UnityEngine;

namespace HalfedgeDS
{
    public interface IAttribBase
    {
    }

    public interface IAttribPointV<P> : IAttribBase
    {
        VPropH<P> pointPH { get; }
        P GetPoint(int vh);
        void SetPoint(int vh, P v);
    }

    public interface IAttribNormalV<N> : IAttribBase
    {
        VPropH<N> normalPH { get; }
        N GetNormal(int vh);
        void SetNormal(int vh, N v);
    }

    public interface IAttribColorV<C> : IAttribBase
    {
        VPropH<C> corlorPH { get; }
        C GetColor(int vh);
        void SetColor(int vh, C v);
    }

    public interface IAttribTexCoordV<T> : IAttribBase
    {
        VPropH<T> texCoordPH { get; }
        T GetCoord(int vh);
        void SetCoord(int vh, T v);
    }

    public interface IAttribNormalF<N> : IAttribBase
    {
        FPropH<N> faceNormalPH { get; }
        N GetFaceNormal(int vh);
        void SetFaceNormal(int vh, N v);
    }

    public class PolyMeshT<P> : PolyConnectivity, IAttribPointV<P> where P : new()
    {
        private VPropH<P> m_pointPH = null;
        public VPropH<P> pointPH
        {
            get
            {
                if(m_pointPH == null)
                {
                    m_pointPH = new VPropH<P>("<Position>", true);
                    AddProperty(m_pointPH);
                }
                if (!m_pointPH.idx.isValidHandle())
                    AddProperty(m_pointPH);
                return m_pointPH;
            }
        }

        public P GetPoint(int vh) { return GetProperty(pointPH, vh); }
        public void SetPoint(int vh, P p) { SetProperty(pointPH, vh, p); }

        public int AddVertex(P v)
        {
            var vh = AddVertex();
            SetPoint(vh, v);
            return vh;
        }
    }

    public class PolyMeshT<P, T> : PolyMeshT<P>, IAttribTexCoordV<T> where P : new() where T : new()
    {
        private VPropH<T> m_texCoordPH = null;
        public VPropH<T> texCoordPH
        {
            get
            {
                if (m_texCoordPH == null)
                {
                    m_texCoordPH = new VPropH<T>("<Texcoord>", true);
                    AddProperty(m_texCoordPH);
                }
                if (!m_texCoordPH.idx.isValidHandle())
                    AddProperty(m_texCoordPH);
                return m_texCoordPH;
            }
        }

        public T GetCoord(int vh) { return GetProperty(texCoordPH, vh); }
        public void SetCoord(int vh, T v) { SetProperty(texCoordPH, vh, v); }
    }

    public class PolyMeshT<P, T, N> : PolyMeshT<P, T>, IAttribNormalV<N> where P : new() where T : new() where N : new()
    {
        private VPropH<N> m_normalPH = null;
        public VPropH<N> normalPH
        {
            get
            {
                if (m_normalPH == null)
                {
                    m_normalPH = new VPropH<N>();
                    AddProperty(m_normalPH);
                }
                if (!m_normalPH.idx.isValidHandle())
                    AddProperty(m_normalPH);
                return m_normalPH;
            }
        }

        public N GetNormal(int vh) { return GetProperty(normalPH, vh); }
        public void SetNormal(int vh, N v) { SetProperty(normalPH, vh, v); }
    }

    public class PolyMeshT_VN_FN<P, N> : PolyMeshT<P>, IAttribNormalV<N>, IAttribNormalF<N> where P : new() where N : new()
    {
        private VPropH<N> m_normalPH = null;
        public VPropH<N> normalPH
        {
            get
            {
                if (m_normalPH == null)
                {
                    m_normalPH = new VPropH<N>("<VertexNoraml>", false);
                    AddProperty(m_normalPH);
                }
                if (!m_normalPH.idx.isValidHandle())
                    AddProperty(m_normalPH);
                return m_normalPH;
            }
        }

        public N GetNormal(int vh) { return GetProperty(normalPH, vh); }
        public void SetNormal(int vh, N v) { SetProperty(normalPH, vh, v); }

        private FPropH<N> m_faceNormalPH = null;
        public FPropH<N> faceNormalPH
        {
            get
            {
                if (m_faceNormalPH == null)
                {
                    m_faceNormalPH = new FPropH<N>("<FaceNormal>", false);
                    AddProperty(m_faceNormalPH);
                }
                if (!m_faceNormalPH.idx.isValidHandle())
                    AddProperty(m_faceNormalPH);
                return m_faceNormalPH;
            }
        }

        public N GetFaceNormal(int fh) { return GetProperty(faceNormalPH, fh); }
        public void SetFaceNormal(int fh, N v) { SetProperty(faceNormalPH, fh, v); }
    }

    public class PolyMeshT_FN<P, N> : PolyMeshT<P>, IAttribNormalF<N> where P : new() where N : new()
    {
        private FPropH<N> m_faceNormalPH = null;
        public FPropH<N> faceNormalPH
        {
            get
            {
                if (m_faceNormalPH == null)
                {
                    m_faceNormalPH = new FPropH<N>("<FaceNormal>", false);
                    AddProperty(m_faceNormalPH);
                }
                if (!m_faceNormalPH.idx.isValidHandle())
                    AddProperty(m_faceNormalPH);
                return m_faceNormalPH;
            }
        }

        public N GetFaceNormal(int fh) { return GetProperty(faceNormalPH, fh); }
        public void SetFaceNormal(int fh, N v) { SetProperty(faceNormalPH, fh, v); }
    }

    public class PolyMeshBase : PolyMeshT<Vector3>
    {

    }

    public class PolyMesh : PolyMeshT_FN<Vector3, Vector3>
    {

    }
}
