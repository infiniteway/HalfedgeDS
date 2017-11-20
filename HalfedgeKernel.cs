using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

namespace HalfedgeDS
{
    public static class Extension
    {
        public static bool isValidHandle(this int handle) { return (handle >= 0); }
    }

    public class BaseHandle
    {
        int idx = -1;
    }

    public class HalfedgeH : BaseHandle { }
    public class EdgeH : BaseHandle { }
    public class VertexH : BaseHandle { }
    public class FaceH : BaseHandle { }

    internal class Halfedge
    {
        public int next = -1;       // next half-edge handle
        public int prev = -1;       // prev half-edge handle
        public int fh = -1;         // adjacent face handle
        public int vh = -1;         // start/pointing vertex handle of this half-edge 
    }

    internal class Edge
    {
        // the pair half-edges of this edge
        public Halfedge[] halfedges = new Halfedge[2] { new Halfedge(), new Halfedge() };
    }

    internal class Face
    {
        public int heh = -1;        // first half-edge handle of this face
    }

    internal class Vertex
    {
        public int heh = -1;        // the outgoing half-edge handle of this vertex
    }

    public class BaseKernel : IDisposable
    {
        protected PropertyContainer m_vprops = new PropertyContainer();
        protected PropertyContainer m_eprops = new PropertyContainer();
        protected PropertyContainer m_hprops = new PropertyContainer();
        protected PropertyContainer m_fprops = new PropertyContainer();

        public virtual int vertexCount { get { return 0; } private set { } }
        public virtual int halfedgeCount { get { return 0; } private set { } }
        public virtual int edgeCount { get { return 0; } private set { } }
        public virtual int faceCount { get { return 0; } private set { } }

        protected void ResizeVProps(int n) { m_vprops.Resize(n); }
        protected void ResizeEProps(int n) { m_eprops.Resize(n); ResizeHProps(n * 2); }
        protected void ResizeHProps(int n) { m_hprops.Resize(n); }
        protected void ResizeFProps(int n) { m_fprops.Resize(n); }

        protected void ReserveVProps(int n) { m_vprops.Reserve(n); }
        protected void ReserveEProps(int n) { m_eprops.Reserve(n); ReserveHProps(n * 2); }
        protected void ReserveHProps(int n) { m_hprops.Reserve(n); }
        protected void ReserveFProps(int n) { m_fprops.Reserve(n); }

        protected const int InvalidHandle = -1;

        public virtual void Clear()
        {
            m_vprops.Clear();
            m_eprops.Clear();
            m_hprops.Clear();
            m_fprops.Clear();
        }

        public virtual void Dispose()
        {
            m_vprops.Dispose();
            m_eprops.Dispose();
            m_hprops.Dispose();
            m_fprops.Dispose();
        }

        /// <summary>
        /// Add Vertex Property for the given property handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="h">Vertex Property Handle</param>
        public void AddProperty<T>(VPropH<T> ph) where T : new()
        {
            m_vprops.AddProperty(ph);
            m_vprops.Resize(vertexCount);
        }

        /// <summary>
        /// Add Edge Property for the given property handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="h">Edge Property Handle</param>
        public void AddProperty<T>(EPropH<T> ph) where T : new()
        {
            m_eprops.AddProperty(ph);
            m_eprops.Resize(edgeCount);
        }

        /// <summary>
        /// Add Half edge property for the given property handle
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ph">Half edg Property Handle</param>
        public void AddProperty<T>(HPropH<T> ph) where T : new()
        {
            m_hprops.AddProperty(ph);
            m_hprops.Resize(edgeCount * 2);
        }

        /// <summary>
        /// Add Face Property for the given property handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="h">Face Property Handle</param>
        public void AddProperty<T>(FPropH<T> ph) where T : new()
        {
            m_fprops.AddProperty(ph);
            m_fprops.Resize(faceCount);
        }

        /// <summary>
        /// Remove Vertex Property for the given property handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="h">Vertex Property Handle</param>
        public void RemoveProperty<T>(VPropH<T> ph)
        {
            m_vprops.RemoveProperty(ph);
        }

        /// <summary>
        /// Remove Edge Property for the given property handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="h">Edge Property Handle</param>
        public void RemoveProperty<T>(EPropH<T> ph)
        {
            m_eprops.RemoveProperty(ph);
        }

        public void RemoveProperty<T>(HPropH<T> ph)
        {
            m_hprops.RemoveProperty(ph);
        }

        /// <summary>
        /// Remove Face Property for the given property handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="h">Face Property Handle</param>
        public void RemoveProperty<T>(FPropH<T> ph)
        {
            m_fprops.RemoveProperty(ph);
        }

        public Property<T> GetProperty<T>(VPropH<T> ph) where T : new()
        {
            return m_vprops.GetProperty(ph);
        }

        /// <summary>
        /// Get Vertex Property for the given property handle and vertex handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="ph">Vertex Property Handle</param>
        /// <param name="vh">Vertex Handle</param>
        /// <returns>Property Value</returns>
        public T GetProperty<T>(VPropH<T> ph, int vh) where T : new()
        {
            return m_vprops.GetProperty(ph)[vh];
        }

        /// <summary>
        /// Set Vertex Property for the given property handle and vertex handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="ph">Vertex Property Handle</param>
        /// <param name="vh">Vertex Handle</param>
        /// <param name="p">Property Value</param>
        public void SetProperty<T>(VPropH<T> ph, int vh, T p) where T : new()
        {
            m_vprops.GetProperty(ph)[vh] = p;
        }

        public Property<T> GetProperty<T>(EPropH<T> ph) where T : new()
        {
            return m_eprops.GetProperty(ph);
        }

        /// <summary>
        /// Get Edge Property for the given property handle and edge handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="ph">Edge Property Handle</param>
        /// <param name="eh">Edge Handle</param>
        /// <returns>Property Value</returns>
        public T GetProperty<T>(EPropH<T> ph, int eh) where T : new()
        {
            return m_eprops.GetProperty(ph)[eh];
        }

        /// <summary>
        /// Set Edge Property for the given property handle and edge handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="ph">Edge Property Handle</param>
        /// <param name="eh">Edge Handle</param>
        /// <param name="p">Property Value</param>
        public void SetProperty<T>(EPropH<T> ph, int eh, T p) where T : new()
        {
            m_eprops.GetProperty(ph)[eh] = p;
        }

        public Property<T> GetProperty<T>(HPropH<T> ph) where T : new()
        {
            return m_hprops.GetProperty(ph);
        }

        public T GetProperty<T>(HPropH<T> ph, int heh) where T : new()
        {
            return m_hprops.GetProperty(ph)[heh];
        }

        public void SetProperty<T>(HPropH<T> ph, int heh, T p) where T : new()
        {
            m_hprops.GetProperty(ph)[heh] = p;
        }

        public Property<T> GetProperty<T>(FPropH<T> ph) where T : new()
        {
            return m_fprops.GetProperty(ph);
        }

        /// <summary>
        /// Get Face Property for the given property handle and face handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="ph">Face Property Handle</param>
        /// <param name="fh">Face Handle</param>
        /// <returns>Property Value</returns>
        public T GetProperty<T>(FPropH<T> ph, int fh) where T : new()
        {
            return m_fprops.GetProperty(ph)[fh];
        }

        /// <summary>
        /// Set Face Property for the given property handle and face handle
        /// </summary>
        /// <typeparam name="T">the template type of the property. eg UnityEngine.Vector3</typeparam>
        /// <param name="ph">Face Property Handle</param>
        /// <param name="fh">Face Handle</param>
        /// <param name="p">Property Value</param>
        public void SetProperty<T>(FPropH<T> ph, int fh, T p) where T : new()
        {
            m_fprops.GetProperty(ph)[fh] = p;
        }
    }

    public class ArrayKernel : BaseKernel
    {
        private SectionList<Vertex> m_vertices = new SectionList<Vertex>();
        private SectionList<Edge> m_edges = new SectionList<Edge>();
        private SectionList<Face> m_faces = new SectionList<Face>();

        public override int vertexCount { get { return m_vertices.Count; } }
        public override int edgeCount { get { return m_edges.Count; } }
        public override int faceCount { get { return m_faces.Count; } }

        public override void Clear()
        {
            base.Clear();
            m_vertices.Clear();
            m_edges.Clear();
            m_faces.Clear();
        }

        public override void Dispose()
        {
            m_vertices.Dispose();
            m_edges.Dispose();
            m_faces.Dispose();
            base.Dispose();
        }

        public void ReserveVertex(int cap)
        {
            if (cap < m_vertices.Capacity)
                return;
            m_vertices.Capacity = cap;
            ReserveVProps(m_vertices.Capacity);
        }

        public void ReserveEdge(int cap)
        {
            if (cap < m_edges.Capacity)
                return;
            m_edges.Capacity = cap;
            ReserveEProps(m_edges.Capacity);
        }

        public void ReserveFace(int cap)
        {
            if (cap < m_faces.Capacity)
                return;
            m_faces.Capacity = cap;
            ReserveFProps(m_faces.Capacity);
        }

        public void Reserve(int vCount, int eCount, int fCount)
        {
            ReserveVertex(vCount);
            ReserveEdge(eCount);
            ReserveFace(fCount);
        }

        public void ResizeVertex(int size)
        {
            if (size > m_vertices.Count)
            {
                int add = size - m_vertices.Count;
                for (int i = 0; i < add; ++i)
                {
                    var def = default(Vertex);
                    m_vertices.Add(def == null ? new Vertex() : def);
                }
            }
        
            ResizeVProps(m_vertices.Count);
        }

        public void ResizeEdge(int size)
        {
            if (size > m_edges.Count)
            {
                int add = size - m_edges.Count;
                for (int i = 0; i < add; ++i)
                {
                    var def = default(Edge);
                    m_edges.Add(def == null ? new Edge() : def);
                }
            }
            ResizeEProps(m_edges.Count);
        }

        public void ResizeFace(int size)
        {
            if (size > m_faces.Count)
            {
                int add = size - m_faces.Count;
                for (int i = 0; i < add; ++i)
                {
                    var def = default(Face);
                    m_faces.Add(def == null ? new Face() : def);
                }
            }
            ResizeFProps(m_faces.Count);
        }

        public void Resize(int vCount, int eCount, int fCount)
        {
            ResizeVertex(vCount);
            ResizeEdge(eCount);
            ResizeFace(fCount);
        }

        /// <summary>
        /// Get Vertex from Vertex Handle
        /// </summary>
        /// <param name="vh">Vertex Handle which is the index for vertex in m_vertices.</param>
        /// <returns>Half-edge data structure's vertex</returns>
        private Vertex GetVertex(int vh)
        {
            //Debug.Assert(vh >= 0 && vh < m_vertices.Count, "Vertex Count vs Handle: " + m_vertices.Count + " - " + vh);
            return m_vertices[vh];
        }

        /// <summary>
        /// Get Edge from Edge Handle
        /// </summary>
        /// <param name="eh">Edge Handle which is the index for edge in m_edges.</param>
        /// <returns>Half-edge data structure's edge</returns>
        private Edge GetEdge(int eh)
        {
            //Debug.Assert(eh >= 0 && eh < m_edges.Count, "Edge Count vs Handle: " + m_edges.Count + " - " + eh);
            return m_edges[eh];
        }

        /// <summary>
        /// Get Face from Face Handle
        /// </summary>
        /// <param name="fh">Face Handle which is the index for face in m_faces.</param>
        /// <returns>Half-edge data structure's face</returns>
        private Face GetFace(int fh)
        {
            //Debug.Assert(fh >= 0 || fh < m_faces.Count, "Face Count vs Handle: " + m_faces.Count + " - " + fh);
            return m_faces[fh];
        }

        /// <summary>
        /// Get Half-edge from Half-edge Handle
        /// </summary>
        /// <param name="heh">Half-edge Handle which is the index for half-edge in m_edges.</param>
        /// <returns></returns>
        private Halfedge GetHalfedge(int heh)
        {
            //Debug.Assert(heh >= 0 && heh < m_edges.Count * 2, "Halfedge Count vs Handle: " + m_edges.Count * 2 + " - " + heh);
            return m_edges[heh >> 1].halfedges[heh & 1];
        }

        /// <summary>
        /// Allocate new Vertex and resize vertex assigned properties.
        /// </summary>
        /// <returns>New Vertex Handle</returns>
        public int NewVertex()
        {
            m_vertices.Add(new Vertex());
            ResizeVProps(vertexCount);
            return m_vertices.Count - 1;
        }

        /// <summary>
        /// Allocate new Edge and resize edge's assigned properties.
        /// </summary>
        /// <param name="startVh"></param>
        /// <param name="endVh"></param>
        /// <returns>New Edge Handle</returns>
        public int NewEdge(int startVh, int endVh)
        {
            var edge = new Edge();
            m_edges.Add(edge);
            ResizeEProps(edgeCount);
            edge.halfedges[0].vh = endVh;
            edge.halfedges[1].vh = startVh;
            return (m_edges.Count - 1) * 2;
        }

        /// <summary>
        /// Allocate new Face and resize face's assigned properties.
        /// </summary>
        /// <returns>New Face Handle</returns>
        public int NewFace()
        {
            m_faces.Add(new Face());
            ResizeFProps(faceCount);
            return m_faces.Count - 1;
        }

        public bool IsBoundaryVertex(int vh)
        {
            int heh = GetHalfedgeVertexH(vh);
            if (heh.isValidHandle() && GetFaceH(heh).isValidHandle())
                return false;
            return true;
        }

        public bool IsBoundaryHalfedge(int heh)
        {
            return !GetFaceH(heh).isValidHandle();
        }

        public bool IsBoundaryEdge(int eh)
        {
            var e = GetEdge(eh);
            return !(e.halfedges[0].fh.isValidHandle() && e.halfedges[1].fh.isValidHandle());
        }

#region Quick Access Helpers for Halfedge Data Structure
#region Get Methods
        public int GetStartVertexH(int heh) { return GetEndVertexH(GetOppositeHalfedgeH(heh)); }
        public int GetEndVertexH(int heh) { return GetHalfedge(heh).vh; }
        public int GetEdgeH(int heh) { return (heh >> 1); }
        public int GetNextHalfedgeH(int heh) { return GetHalfedge(heh).next; }
        public int GetPrevHalfedgeH(int heh) { return GetHalfedge(heh).prev; }
        public int GetOppositeHalfedgeH(int heh) { return (heh ^ 1); }
        public int GetFaceH(int heh) { return GetHalfedge(heh).fh; }
        public int GetOppositeFaceH(int heh) { return GetFaceH(GetOppositeHalfedgeH(heh)); }
        public int GetHalfedgeVertexH(int vh) { return GetVertex(vh).heh; }
        public int GetHalfedgeEdgeH(int eh, int i) { return (eh * 2 + i); }
        public int GetHalfedgeFaceH(int fh) { return GetFace(fh).heh; }
        public int GetHalfedgeHalfedgeH(int heh, int i) { return heh - (heh & 1) + i; }
#endregion

#region Set Methods
        public void SetVertexH(int heh, int vh) { GetHalfedge(heh).vh = vh; }
        public void SetFaceH(int heh, int fh) { GetHalfedge(heh).fh = fh; }
        public void SetNextHalfedgeH(int heh, int next_heh) { GetHalfedge(heh).next = next_heh; SetPrevHalfedgeH(next_heh, heh); }
        public void SetPrevHalfedgeH(int heh, int prev_heh) { GetHalfedge(heh).prev = prev_heh; }
        public void SetHalfedgeFaceH(int fh, int heh) { GetFace(fh).heh = heh; }
        public void SetHalfedgeVertexH(int vh, int heh) { GetVertex(vh).heh = heh; }
#endregion
#endregion
    }

}

