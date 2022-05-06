using System;
using System.Collections.Generic;
using System.Linq;
using Apache.Ignite.Core.Compute;

namespace Shwab.Compute
{
    public class AggregateTask : ComputeTaskSplitAdapter<string, Dictionary<int, Decimal>, Dictionary<int, Decimal>>
    {
        public override Dictionary<int, decimal> Reduce(IList<IComputeJobResult<Dictionary<int, decimal>>> results)
        {
            var res = new Dictionary<int, Decimal>();

            foreach (var item in results)
            {
                foreach (var d in item.Data)
                {
                    res[d.Key] = d.Value;
                }
            }

            return res;

        }

        protected override ICollection<IComputeJob<Dictionary<int, decimal>>> Split(int gridSize, string arg)
        {
            var jobs = new List<IComputeJob<Dictionary<int, decimal>>>();
            for (int i = 0; i < gridSize; i++)
            {
                jobs.Add(new AggregationFunc());
            }

            return jobs;
        }
    }
}