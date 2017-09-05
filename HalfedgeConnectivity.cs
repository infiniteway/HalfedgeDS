using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace HalfedgeDS
{
    /// <summary>
    /// 
    /// A simple C# implementation for OpenMesh
    /// 
    /// TODO: try write a C# wrapper for OpenMesh dll for optimizing.
    /// 
    /// Basic Concepts for this Half-edge Data Structure
    /// 1. The handle of each mesh core traits such as Vertex, Edge(Half-edge), Face is the index of the item in the List of Vertices, Edges, Faces.
    /// 2. Most of the Methods use a handle as a input parameter and return a handle.
    /// 3. The handle input parameter's name is suffixed with 'h' shorting for "handle" such as heh(Half-edge Handle), vh(Vertex Handle), fh(Face Handle), eh(Edge Handle).
    /// </summary>
    public class PolyConnectivity : ArrayKernel   
    {
        // struct definition only used at add face
        #region Add Face Cache Region
        struct EdgeInfo
        {
            public int heh;
            public bool is_new;
            public bool needs_adjust;
        };
        struct NextPair
        {
            public int heh0;
            public int heh1;
        }
        EdgeInfo[] m_edgeData = new EdgeInfo[3];
        NextPair[] m_nextCache = new NextPair[3 * 6];
        int[] m_vertexIdxCache = new int[3];
        #endregion
        public PolyConnectivity()
        {

        }

        public int AddVertex()
        {
            return NewVertex();
        }

        /// <summary>
        /// Add faces from patch of faces
        /// </summary>
        /// <param name="faces">Patch of faces</param>
        /// <returns>Array of added faces' handle</returns>
        public int[] AddFaces(IEnumerable<IEnumerable<int>> faces)
        {
            return faces.Select(f => this.AddFace(f.ToArray())).ToArray();
        }

        /// <summary>
        /// Add faces from patch of faces which composed with same stride
        /// </summary>
        /// <param name="faces">Patch of faces</param>
        /// <param name="stride">edge count of each face</param>
        /// <returns>Array of added faces' handle</returns>
        public void AddFaces(int[] faces, int stride = 3)
        {
            Debug.Assert(stride >= 3);
            if (m_vertexIdxCache.Length < stride)
                m_vertexIdxCache = new int[stride];

            for(int i = 0; i < faces.Length; i += 3)
            {
                for (int j = 0; j < stride; j++)
                    m_vertexIdxCache[j] = faces[i + j];
                AddFace(m_vertexIdxCache);
            }           
        }

        /// <summary>
        /// Add a triangle face in CCW order.
        /// </summary>
        /// <param name="v0">first vertex index of the face</param>
        /// <param name="v1">second vertex index of the face</param>
        /// <param name="v2">third vertex index of the face</param>
        /// <returns>New Face Handle</returns>
        public int AddFace(int v0, int v1, int v2)
        {
            m_vertexIdxCache[0] = v0;
            m_vertexIdxCache[1] = v1;
            m_vertexIdxCache[2] = v2;
            return AddFace(m_vertexIdxCache);
        }

        /// <summary>
        /// Add a face from the given vertex's index in CCW order
        /// </summary>
        /// <param name="vertexIdx">the index of vertices comprised the face</param>
        /// <returns>New Face Handle</returns>
        public int AddFace(int[] vertexIdx)
        {            
            int i, ii, n = vertexIdx.Length,
                    inner_next, inner_prev,
                    outer_next, outer_prev,
                    boundary_next, boundary_prev,
                    patch_start, patch_end;

            int nextCacheCount = 0;
            if (n < 3) return -1;
            if(n != m_edgeData.Length)
            {
                //reallocate cache
                m_edgeData = new EdgeInfo[n];
                m_nextCache = new NextPair[6 * n];
            }

            for (i = 0, ii = 1; i < n; ++i, ++ii, ii %= n)
            {
                if (!IsBoundaryVertex(vertexIdx[i]))
                    return InvalidHandle;

                m_edgeData[i].heh = FindHalfedge(vertexIdx[i], vertexIdx[ii]);
                m_edgeData[i].is_new = !m_edgeData[i].heh.isValidHandle();
                m_edgeData[i].needs_adjust = false;
                if (!m_edgeData[i].is_new && !IsBoundaryHalfedge(m_edgeData[i].heh))
                    return InvalidHandle;
            }  

            for(i = 0, ii = 1; i < n; ++i, ++ii, ii %= n)
            {
                if(!m_edgeData[i].is_new && !m_edgeData[ii].is_new)
                {
                    inner_prev = m_edgeData[i].heh;
                    inner_next = m_edgeData[ii].heh;

                    if(GetNextHalfedgeH(inner_prev) != inner_next)
                    {
                        outer_prev = GetOppositeHalfedgeH(inner_next);
                        outer_next = GetOppositeHalfedgeH(inner_prev);
                        boundary_prev = outer_prev;
                        do
                            boundary_prev = GetOppositeHalfedgeH(GetNextHalfedgeH(boundary_prev));
                        while (!IsBoundaryHalfedge(boundary_prev));
                        boundary_next = GetNextHalfedgeH(boundary_prev);

                        if (boundary_prev == inner_prev)
                            return InvalidHandle;

                        Debug.Assert(IsBoundaryHalfedge(boundary_prev));
                        Debug.Assert(IsBoundaryHalfedge(boundary_next));

                        patch_start = GetNextHalfedgeH(inner_prev);
                        patch_end = GetPrevHalfedgeH(inner_next);

                        Debug.Assert(boundary_prev.isValidHandle());
                        Debug.Assert(patch_start.isValidHandle());
                        Debug.Assert(patch_end.isValidHandle());
                        Debug.Assert(boundary_next.isValidHandle());
                        Debug.Assert(inner_prev.isValidHandle());
                        Debug.Assert(inner_next.isValidHandle());

                        m_nextCache[nextCacheCount].heh0 = boundary_prev;
                        m_nextCache[nextCacheCount].heh1 = patch_start;
                        nextCacheCount++;

                        m_nextCache[nextCacheCount].heh0 = patch_end;
                        m_nextCache[nextCacheCount].heh1 = boundary_next;
                        nextCacheCount++;

                        m_nextCache[nextCacheCount].heh0 = inner_prev;
                        m_nextCache[nextCacheCount].heh1 = inner_next;
                        nextCacheCount++;
                    }
                }
            }

            for (i = 0, ii = 1; i < n; ++i, ++ii, ii %= n)
            {
                if (m_edgeData[i].is_new)
                    m_edgeData[i].heh = NewEdge(vertexIdx[i], vertexIdx[ii]);
            }

            int fh = NewFace();            
            SetHalfedgeFaceH(fh, m_edgeData[n - 1].heh);
            for(i = 0, ii = 1; i < n; ++i, ++ii, ii %= n)
            {
                var vh = vertexIdx[ii];

                inner_prev = m_edgeData[i].heh;
                inner_next = m_edgeData[ii].heh;

                Debug.Assert(inner_prev.isValidHandle());
                Debug.Assert(inner_next.isValidHandle());

                int id = 0;
                if (m_edgeData[i].is_new) id |= 1;
                if (m_edgeData[ii].is_new) id |= 2;

                if (id > 0)
                {
                    outer_prev = GetOppositeHalfedgeH(inner_next);
                    outer_next = GetOppositeHalfedgeH(inner_prev);

                    Debug.Assert(outer_prev.isValidHandle());
                    Debug.Assert(outer_next.isValidHandle());

                    // set outer links
                    switch (id)
                    {
                        case 1: // prev is new, next is old
                            boundary_prev = GetPrevHalfedgeH(inner_next);
                            Debug.Assert(boundary_prev.isValidHandle());
                            m_nextCache[nextCacheCount].heh0 = boundary_prev;
                            m_nextCache[nextCacheCount].heh1 = outer_next;
                            nextCacheCount++;
                            SetHalfedgeVertexH(vh, outer_next);
                            break;

                        case 2: // next is new, prev is old
                            boundary_next = GetNextHalfedgeH(inner_prev);
                            Debug.Assert(boundary_next.isValidHandle());
                            m_nextCache[nextCacheCount].heh0 = outer_prev;
                            m_nextCache[nextCacheCount].heh1 = boundary_next;
                            nextCacheCount++;
                            SetHalfedgeVertexH(vh, boundary_next);
                            break;

                        case 3: // both are new
                            if (!GetHalfedgeVertexH(vh).isValidHandle())
                            {
                                SetHalfedgeVertexH(vh, outer_next);
                                m_nextCache[nextCacheCount].heh0 = outer_prev;
                                m_nextCache[nextCacheCount].heh1 = outer_next;
                                nextCacheCount++;
                            }
                            else
                            {
                                boundary_next = GetHalfedgeVertexH(vh);
                                boundary_prev = GetPrevHalfedgeH(boundary_next);
                                Debug.Assert(boundary_prev.isValidHandle());
                                Debug.Assert(boundary_next.isValidHandle());
                                m_nextCache[nextCacheCount].heh0 = boundary_prev;
                                m_nextCache[nextCacheCount].heh1 = outer_next;
                                nextCacheCount++;

                                m_nextCache[nextCacheCount].heh0 = outer_prev;
                                m_nextCache[nextCacheCount].heh1 = boundary_next;
                                nextCacheCount++;
                            }
                            break;
                    }

                    // set inner link
                    m_nextCache[nextCacheCount].heh0 = inner_prev;
                    m_nextCache[nextCacheCount].heh1 = inner_next;
                    nextCacheCount++;
                }
                else
                    m_edgeData[ii].needs_adjust = (GetHalfedgeVertexH(vh) == inner_next);

                // set face handle
                SetFaceH(m_edgeData[i].heh, fh);
            }

            // process next halfedge cache
            for (i = 0; i < nextCacheCount; ++i)
            {
                SetNextHalfedgeH(m_nextCache[i].heh0, m_nextCache[i].heh1);
                // set prev half edge if it has
            }

            // adjust vertices' halfedge handle
            for (i = 0; i < n; ++i)
                if (m_edgeData[i].needs_adjust)
                    AdjustOutgoingHalfedge(vertexIdx[i]);

            return fh;
        }

        /// <summary>
        /// Traverses CW around the vertex, return the half-edge handles originated from the given vertex
        /// </summary>
        /// <param name="vh">Vertex Handle</param>
        /// <returns>An enumerable of half-edge handles originated from the given vertex</returns>
        public IEnumerable<int> GetVertexCirculartor(int vh)
        {
            int heh = GetHalfedgeVertexH(vh);
            int theh = heh;
            do
            {
                if (theh < 0 || theh > edgeCount * 2) { yield break; }
                yield return theh;
                theh = GetNextHalfedgeH(GetOppositeHalfedgeH(theh));
            } while (theh != heh);
        }

        /// <summary>
        /// Traverses CW around the pointing vertex of a half-edge, return the incident half-edge handles of the given vertex
        /// </summary>
        /// <param name="heh">Half-edge Handle, the returned enumerable will start with this handle</param>
        /// <returns>An enumerable of half-edge handles incident to the pointing vertex of the given half-edge</returns>
        public IEnumerable<int> GetHalfedgeCirculator(int heh)
        {
            int theh = heh;
            do
            {
                if (theh < 0 || theh > edgeCount * 2) { yield break; }
                yield return theh;
                theh = GetOppositeHalfedgeH(GetNextHalfedgeH(theh));
            } while (theh != heh);
        }

        /// <summary>
        /// Traverse CCW around the adjacent face of o half-edge, return the half-edge handles of the face
        /// </summary>
        /// <param name="heh">Half-edge Handle</param>
        /// <returns>An enumerable of Half-edge handles incident to the adjacent face</returns>
        public IEnumerable<int> GetFaceCirculator(int heh)
        {
            int theh = heh;
            do
            {
                if (theh < 0 || theh > edgeCount * 2) { yield break; }
                yield return theh;
                theh = GetNextHalfedgeH(theh);
            } while (theh != heh);
        }

        /// <summary>
        /// Get the vertex handles of the face ordered in CCW
        /// </summary>
        /// <param name="fh">Face Handle</param>
        /// <returns>An enumerable of vertex handle</returns>
        public IEnumerable<int> GetFaceVertexIter(int fh)
        {
            int heh = GetHalfedgeFaceH(fh);
            int theh = heh;
            do
            {
                if (theh < 0 || theh > edgeCount * 2) { yield break; }
                yield return GetEndVertexH(theh);
                theh = GetNextHalfedgeH(theh);
            } while (theh != heh);
        }

        /// <summary>
        /// Get the half-edge handles of the face ordered in CCW
        /// </summary>
        /// <param name="fh">Face Handle</param>
        /// <returns>An enumerable of half-edge handles</returns>
        public IEnumerable<int> GetFaceHalfedgeIter(int fh)
        {
            return GetFaceCirculator(GetHalfedgeFaceH(fh));
        }

        /// <summary>
        /// Get the neighbour vertex handles (A.K.A 1-ring vertices) around the vertex ordered in CW
        /// </summary>
        /// <param name="vh">Vertex Handle</param>
        /// <returns>An enumerable of vertex handle</returns>
        public IEnumerable<int> GetVertexVertexIter(int vh)
        {
            int heh = GetHalfedgeVertexH(vh);
            int theh = heh;
            do
            {
                if (theh < 0 || theh > edgeCount * 2) { yield break; }
                yield return GetEndVertexH(theh);
                theh = GetNextHalfedgeH(GetOppositeHalfedgeH(theh));
            } while (theh != heh);
        }

        /// <summary>
        /// Get the outgoing half-edges of the given vertex ordered in CW
        /// </summary>
        /// <param name="vh">Vertex Handle</param>
        /// <returns>An enumerable of half-edge handle</returns>
        public IEnumerable<int> GetVertexOHalfedgeIter(int vh)
        {
            return GetVertexCirculartor(vh);
        }

        /// <summary>
        /// Get the incoming half-edges of the given vertex ordered in CW
        /// </summary>
        /// <param name="vh">Vertex Handle</param>
        /// <returns>An enumerable of half-edge handle</returns>
        public IEnumerable<int> GetVertexIHalfedgeIter(int vh)
        {
            return GetHalfedgeCirculator(GetOppositeHalfedgeH(GetHalfedgeVertexH(vh)));
        }

        public void AdjustOutgoingHalfedge(int vh)
        {
            var vit = GetVertexOHalfedgeIter(vh);
            foreach( var heh in vit)
            {
                if(IsBoundaryHalfedge(heh))
                {
                    SetHalfedgeVertexH(vh, heh);
                    break;
                }
            }
        }

        /// <summary>
        /// Find the half-edge handle from one vertex to another
        /// </summary>
        /// <param name="vh0">From Vertex Handle</param>
        /// <param name="vh1">To Vertex Handle</param>
        /// <returns>Half-edge Handle</returns>
        public int FindHalfedge(int vh0, int vh1)
        {
            var vit = GetVertexOHalfedgeIter(vh0);
            foreach(var heh in vit)
            {
                if (!heh.isValidHandle())
                    return InvalidHandle;
                if (GetEndVertexH(heh) == vh1)
                    return heh;
            }
            return InvalidHandle;
        }
    }
}
