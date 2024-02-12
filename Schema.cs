using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonRedundantCover
{
    public class Schema
    {
        public Schema()
        {
            Attributes = new List<string>();
            NullFreeSubSchema = new List<string>();
            FunctionalDependencies = new List<Tuple<List<string>, List<string>>>();
            UniquenessConstraints = new List<List<string>>();
            Closures = new Dictionary<string, List<string>>();
        }

        public void ListAttributes()
        {
            if (Attributes.Count > 0)
            {
                Console.WriteLine("The list of attributes is:");
                Console.WriteLine(Attributes.Aggregate((x, y) => x + ", " + y));
            }
        }

        public void ListNFS()
        {
            if (NullFreeSubSchema.Count > 0)
            {
                Console.WriteLine("The NFS is:");
                Console.WriteLine(NullFreeSubSchema.Aggregate((x, y) => x + ", " + y));
            }
        }

        public void ListFDs()
        {
            if (FunctionalDependencies.Count > 0)
            {
                Console.WriteLine("The FDs are:");
                foreach (var fd in FunctionalDependencies)
                {
                    Console.WriteLine(fd.Item1.Aggregate((x, y) => x + ", " + y) + " => " + fd.Item2.Aggregate((x, y) => x + ", " + y));
                }
            }
        }

        public void ListUQs()
        {
            if (UniquenessConstraints.Count > 0)
            {
                Console.WriteLine("The Uniqueness Contraints are:");
                foreach (var uq in UniquenessConstraints)
                {
                    Console.WriteLine(uq.Aggregate((x, y) => x + ", " + y));
                }
            }
        }

        public void Print()
        {
            ListAttributes();
            ListNFS();
            ListFDs();
            ListUQs();
        }
        public List<string> Attributes { get; set; }
        public List<string> NullFreeSubSchema { get; set; }
        public List<Tuple<List<string>, List<string>>> FunctionalDependencies { get; set; }
        public List<List<String>> UniquenessConstraints { get; set; }
        public Dictionary<string, List<string>> Closures { get; set; }

        /// <summary>
        /// The time (in seconds) it took to compute the cover for this schema instance
        /// </summary>
        public decimal CoverComputationTime { get; set; }

        /// <summary>
        /// The size of the non redundant cover as a percentage of the size of Sigma
        /// </summary>
        public decimal CoverPercentSize { get; set; }
    }
}
