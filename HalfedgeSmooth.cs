using System;
using System.Collections;
using System.Collections.Generic;

namespace HalfedgeDS.Smooth
{
    public struct Weight
    {
        public float first;
        public float second;
    }

    public class ComputeWeights
    {
        protected int valence = -1;
        public virtual IEnumerable<Weight> GetWeightIter(int maxValence)
        {
            throw new NotImplementedException();
        }
    }

    public class LoopComputeWeights : ComputeWeights
    {
        public override IEnumerable<Weight> GetWeightIter(int maxValence)
        {
            while(valence < maxValence)
            {
                if (++valence > 0)
                {
                    double inv_v = 1.0 / (double)valence;
                    double t = (3.0 + 2.0 * Math.Cos((float)(2.0 * Math.PI * inv_v)));
                    double alpha = (40.0 - t * t) / 64.0;
                    yield return new Weight { first = (float)(1.0 - alpha), second = (float)(inv_v * alpha) };
                }
                else
                    yield return new Weight { first = 0.0f, second = 0.0f };
            }
        }
    }

    public class Weights<T> where T : ComputeWeights, new()
    {
        private List<Weight> m_weights;

        public int Count { get { return m_weights != null ? m_weights.Count : 0; } }

        public Weight this[int d]
        {
            get { return m_weights[d]; }
        }

        public Weights(int maxValence = 20)
        {
            Precompute(maxValence);
        }

        public IEnumerable<Weight> GetWeight()
        {
            for (int i = 0; i < m_weights.Count; ++i)
                yield return m_weights[i];
        }

        private void Precompute(int valence = 10)
        {
            m_weights = new List<Weight>(valence);
            var iter = new T().GetWeightIter(valence);
            foreach (var v in iter)
                m_weights.Add(v);
        }
    }

    public class SmoothUtils
    {
        private static Weights<LoopComputeWeights> m_loopWeights = null;
        public static Weights<LoopComputeWeights> loopWeights
        {
            get
            {
                if (m_loopWeights == null)
                    m_loopWeights = new Weights<LoopComputeWeights>();
                return m_loopWeights;
            }
        }
    }
}

