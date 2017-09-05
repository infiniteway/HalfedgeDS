using System;
using System.Collections.Generic;
using UnityEngine;

namespace HalfedgeDS
{
    using Smooth;
    
    public class HalfedgeUtils
    {
        /// <summary>
        /// Split edge in half
        /// </summary>
        /// <param name="m">Polymesh</param>
        /// <param name="eh">Edge Handle</param>
        /// <returns>New Half-edge Handle for the new Edge</returns>
        public static int SplitEdge(PolyMesh m, int eh)
        {
            int heh = m.GetHalfedgeEdgeH(eh, 0);
			int opp_heh = m.GetHalfedgeEdgeH(eh, 1);

            int new_heh, opp_new_heh, t_heh;
            int vh, vh1 = m.GetEndVertexH(heh);

            Vector3 midP = m.GetPoint(vh1);
            midP += m.GetPoint(m.GetEndVertexH(opp_heh));
            midP *= 0.5f;

            // new vertex
            vh = m.AddVertex(midP);

            // Re-link mesh entities
            if (m.IsBoundaryEdge(eh))
            {
                for (t_heh = heh;
                    m.GetNextHalfedgeH(t_heh) != opp_heh;
                    t_heh = m.GetOppositeHalfedgeH(m.GetNextHalfedgeH(t_heh)))
                {
                }
            }
            else
            {
                for (t_heh = m.GetNextHalfedgeH(opp_heh);
                    m.GetNextHalfedgeH(t_heh) != opp_heh;
                    t_heh = m.GetNextHalfedgeH(t_heh))
                {
                }
            }

            new_heh = m.NewEdge(vh, vh1);
            opp_new_heh = m.GetOppositeHalfedgeH(new_heh);
            m.SetVertexH(heh, vh);

            m.SetNextHalfedgeH(t_heh, opp_new_heh);
            m.SetNextHalfedgeH(new_heh, m.GetNextHalfedgeH(heh));
            m.SetNextHalfedgeH(heh, new_heh);
            m.SetNextHalfedgeH(opp_new_heh, opp_heh);

            if (m.GetFaceH(opp_heh).isValidHandle())
            {
                m.SetFaceH(opp_new_heh, m.GetFaceH(opp_heh));
                m.SetHalfedgeFaceH(m.GetFaceH(opp_new_heh), opp_new_heh);
            }

            m.SetFaceH(new_heh, m.GetFaceH(heh));
            m.SetHalfedgeVertexH(vh, new_heh);
            m.SetHalfedgeFaceH(m.GetFaceH(heh), heh);
            m.SetHalfedgeVertexH(vh1, opp_new_heh);

            // Never forget this, when playing with the topology
            m.AdjustOutgoingHalfedge(vh);
            m.AdjustOutgoingHalfedge(vh1);

            return new_heh;
        }

        /// <summary>
        /// Split the face with Corner Cutting 
        /// </summary>
        /// <param name="m">Polymesh</param>
        /// <param name="heh">Half-edge for the Face</param>
        /// <returns>New Face Handle</returns>
        public static int CornerCutting(PolyMesh m, int heh)
        {
            int heh1 = heh,
			heh5 = heh1,
			heh6 = m.GetNextHalfedgeH(heh1);

            // Cycle around the polygon to find correct Halfedge
            for (; m.GetNextHalfedgeH(m.GetNextHalfedgeH(heh5)) != heh1;
                heh5 = m.GetNextHalfedgeH(heh5))
            {
            }

            int vh1 = m.GetEndVertexH(heh1), 
                vh2 = m.GetEndVertexH(heh5);

            int heh2 = m.GetNextHalfedgeH(heh5),
			heh3 = m.NewEdge(vh1, vh2),
			heh4 = m.GetOppositeHalfedgeH(heh3);

            /* Intermediate result
            *
            *            *
            *         5 /|\
            *          /_  \
            *    vh2> *     *
            *        /|\3   |\
            *     2 /_  \|4   \
            *      *----\*----\*
            *          1 ^   6
            *            vh1 (adjust_outgoing half - edge!)
            */

            // Old and new Face
            int fh_old = m.GetFaceH(heh6);
            int fh_new = m.NewFace();

            // Re-Set Handles around old Face
            m.SetNextHalfedgeH(heh4, heh6);
            m.SetNextHalfedgeH(heh5, heh4);

            m.SetFaceH(heh4, fh_old);
            m.SetFaceH(heh5, fh_old);
            m.SetFaceH(heh6, fh_old);
            m.SetHalfedgeFaceH(fh_old, heh4);

            // Re-Set Handles around new Face
            m.SetNextHalfedgeH(heh1, heh3);
            m.SetNextHalfedgeH(heh3, heh2);

            m.SetFaceH(heh1, fh_new);
            m.SetFaceH(heh2, fh_new);
            m.SetFaceH(heh3, fh_new);

            m.SetHalfedgeFaceH(fh_new, heh1);
            return fh_new;
        }

        public static Vector3 ComputeMidPoint(PolyMesh m, int heh)
        {
            var opp_heh = m.GetOppositeHalfedgeH(heh);
            Vector3 pos = m.GetPoint(m.GetEndVertexH(heh));
            pos += m.GetPoint(m.GetEndVertexH(opp_heh));
            if (m.IsBoundaryHalfedge(heh) || m.IsBoundaryHalfedge(opp_heh))
                return pos * 0.5f;
            else
            {
                pos *= 3.0f;
                pos += m.GetPoint(m.GetEndVertexH(m.GetNextHalfedgeH(heh)));
                pos += m.GetPoint(m.GetEndVertexH(m.GetNextHalfedgeH(opp_heh)));
                pos *= 0.125f;
            }
            return pos;
        }

        /// TODO: make the smooth generic with the subdivision type
        /// <summary>
        /// Smooth the vertex with loop subdivision
        /// </summary>
        /// <param name="m">Polymesh</param>
        /// <param name="vh">Vertex Handle to smooth</param>
        /// <param name="p">new position for the vertex</param>
        /// <returns>false indicates that the vertex is not in the Polymesh which can be removed</returns>
        public static bool Smooth(PolyMesh m, int vh, out Vector3 p)
        {
            p = Vector3.zero;
            if(m.IsBoundaryVertex(vh))
            {
                int heh = m.GetHalfedgeVertexH(vh), prev_heh;
                if (heh.isValidHandle())
                {
                    Debug.Assert(m.IsBoundaryEdge(m.GetEdgeH(heh)));
                    prev_heh = m.GetPrevHalfedgeH(heh);

                    int to_vh = m.GetEndVertexH(heh),
                        from_vh = m.GetStartVertexH(prev_heh);

                    p = m.GetPoint(vh);
                    p *= 6.0f;
                    p += m.GetPoint(to_vh);
                    p += m.GetPoint(from_vh);
                    p *= 0.125f;
                }
                else
                    return false;
            }
            else
            {
                var vit = m.GetVertexVertexIter(vh);
                int valence = 0;
                foreach (var vvh in vit)
                {
                    valence++;
                    p += m.GetPoint(vvh);
                }
                var w = SmoothUtils.loopWeights[valence];
                p *= w.second;
                p += m.GetPoint(vh) * w.first;
            }
            return true;
        }     
    }
}

