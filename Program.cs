using System.Diagnostics;

namespace NonRedundantCover
{
    internal class Program
    {
        public static string fileDetail = "n-n";

        static void Main(string[] args)
        {
            //INPUT
            //Table Schema T
            //NFS nfs(Ts) with Ts in T
            //Set Sigma of (UCs and) FDs over T
            //OUTPUT
            //A non redundant cover SigmaC of Sigma

            Console.WriteLine("Generating Functional Dependency sets and covers");
            GenerateSchemaAndCovers();
            Console.WriteLine("Preparing data for graphing");
            PrepDataForGraphing();

            Console.WriteLine("Program finished, hit enter to exit.");
            Console.ReadLine();
        }

        static void GenerateSchemaAndCovers()
        {
            int minimumSchemaSize = 3;  //The minimum number of attributes in our schema
            int maximumSchemaSize = 20;  //The maximum number of attributes in our schema
            Random rng = new Random();
            int min = 1;    //The minimum number of functional dependencies in the sigma we generate  as a multiplier for the size of the table schema (n)
            int max = 1;  //The maximum number of functional dependencies as a multiplier for the size of the table schema (n)
            int i = 50;

            using (var writer = new CsvWriter("raw_output " + fileDetail + ".csv"))
            {
                //Put a quick header in our file
                List<string> header = new List<string>();
                header.Add("n");
                header.Add("k");
                header.Add("Sigma_size");
                header.Add("Time_(s)");
                header.Add("Percentage_Size");
                writer.WriteRow(header);
                for (int n = minimumSchemaSize; n <= maximumSchemaSize; n++)
                {
                    Console.WriteLine(string.Format("Beginning schemas of attribute count: {0}", n));
                    for (int k = 0; k <= n; k++)
                    {
                        //Generate a schema with n attributes, a random k of which are in the null free subschema
                        Schema instance = new Schema();
                        //Generate the attributes
                        for (int attribute = 1; attribute <= n; attribute++)
                        {
                            instance.Attributes.Add(attribute.ToString());
                        }
                        //Pick a random k of them to be in the null free sub schema
                        instance.NullFreeSubSchema.AddRange(instance.Attributes.OrderBy(a => rng.Next(int.MaxValue)).Take(k));

                        //1. loop i times
                        //2. Generate a sigma sized between min and max
                        //3: Calculate the non redundant cover for sigma
                        //4: Record n, k, and the time and %size
                        for (int l = 1; l <= i; l++)    //This is equivalent to for(l = 0; l < i; l++) which matches my assumption about i being the number of iterations per condition and l just a counter
                        {
                            instance.Closures = new Dictionary<string, List<string>>(); //Clear out any closures computed with an earlier set of fds
                            instance.FunctionalDependencies = GenerateRandomSigma(instance.Attributes, min * instance.Attributes.Count, max * instance.Attributes.Count, rng);
                            //instance.FunctionalDependencies = GenerateRandomSigma(instance.Attributes, instance.Attributes.Count, (int)Math.Pow(2, instance.Attributes.Count), rng);
                            //instance.FunctionalDependencies = GenerateRandomSigma(instance.Attributes, 5, 100, rng);
                            ComputeCover(instance); //We don't actually care about the output of this method, it adds the data we care about (size and computation time) to instance anyway
                            //record in some fashion
                            List<string> row = new List<string>();
                            row.Add(n.ToString());
                            row.Add(k.ToString());
                            row.Add(instance.FunctionalDependencies.Count.ToString());
                            row.Add(instance.CoverComputationTime.ToString());
                            row.Add(instance.CoverPercentSize.ToString());
                            writer.WriteRow(row);
                        }
                    }
                }
            }
        }

        static void PrepDataForGraphing()
        {
            //Here, we read the file containing the output of the previous part
            //And generate the averages that we end up putting into our graphs
            //We spit that out to a csv file, ready to be put into matlab or whatever graphing application we use

            //Let us consider the 4 graphs we wish to present:

            //Graph 1 is a 3D graph, showing the average time to compute sigmaC as a function of the total number of attributes and the number of attributes in the nfs
            //Graph 2 is the same, except it shows the average of the percentage of the size of Sigma that is SigmaC
            //Graph 3 is a 2D graph that shows an average of the time to compute SigmaC
            //Graph 4 is the same as Graph 3, except that it shows the average size of SigmaC as a percentage of Sigma

            List<PartIIOut> rows = new List<PartIIOut>();

            using (CsvReader reader = new CsvReader("raw_output " + fileDetail + ".csv"))
            {
                CsvRow row = new CsvRow();
                bool readingHeader = true;
                while (reader.ReadRow(row))
                {
                    if (readingHeader)
                    {
                        readingHeader = false;
                    }
                    else
                    {
                        PartIIOut rowData = new PartIIOut();
                        rowData.n = int.Parse(row[0]);
                        rowData.k = int.Parse(row[1]);
                        rowData.SigmaSize = int.Parse(row[2]);
                        rowData.Time = decimal.Parse(row[3]);
                        rowData.PercentSize = decimal.Parse(row[4]);
                        rows.Add(rowData);
                    }
                }
            }

            var groupedRows = rows.GroupBy(r => new { r.n, r.k });

            //Now, to output for the 3d graphs, we need to pivot this data into the format used for graphing
            int minN = rows.Min(r => r.n);
            int maxN = rows.Max(r => r.n);
            int minK = rows.Min(r => r.k);
            int maxK = rows.Max(r => r.k);
            decimal[][] pivotDataTime = new decimal[maxN - minN + 1][];
            decimal[][] pivotDataSize = new decimal[maxN - minN + 1][];
            for (int i = 0; i <= maxN - minN; i++)
            {
                pivotDataTime[i] = new decimal[maxK - minK + 1];
                pivotDataSize[i] = new decimal[maxK - minK + 1];
            }
            foreach (var groupedRow in groupedRows)
            {
                pivotDataTime[groupedRow.Key.n - minN][groupedRow.Key.k - minK] = groupedRow.Sum(r => r.Time) * (1.0M / groupedRow.Count());
                pivotDataSize[groupedRow.Key.n - minN][groupedRow.Key.k - minK] = groupedRow.Sum(r => r.PercentSize) * (1.0M / groupedRow.Count());
            }

            //Output the time data
            using (var writer = new CsvWriter("TimeData " + fileDetail + ".csv"))
            {
                //write the k header
                List<string> header = new List<string>();
                header.Add("0");
                for (int k = minK; k <= maxK - minK; k++)
                {
                    header.Add(k.ToString());
                }
                writer.WriteRow(header);
                //write the n rows
                for (int n = minN; n <= maxN; n++)
                {
                    List<string> writeRow = new List<string>();
                    writeRow.Add(n.ToString());
                    for (int k = minK; k <= maxK; k++)
                    {
                        writeRow.Add(pivotDataTime[n - minN][k - minK].ToString());
                    }
                    writer.WriteRow(writeRow);
                }
            }

            Console.WriteLine("Writing Size data.");

            //Output the size data
            using (var writer = new CsvWriter("SizeData " + fileDetail + ".csv"))
            {
                //write the k header
                List<string> header = new List<string>();
                header.Add("0");
                for (int k = minK; k <= maxK; k++)
                {
                    header.Add(k.ToString());
                }
                writer.WriteRow(header);
                //write the n rows
                for (int n = minN; n <= maxN; n++)
                {
                    List<string> writeRow = new List<string>();
                    writeRow.Add(n.ToString());
                    for (int k = minK; k <= maxK; k++)
                    {
                        writeRow.Add(pivotDataSize[n - minN][k - minK].ToString());
                    }
                    writer.WriteRow(writeRow);
                }
            }

            //Now we build the data for the 2d graph

            using (var writer = new CsvWriter("2DData " + fileDetail + ".csv"))
            {
                List<string> header = new List<string>();
                header.Add("n");
                header.Add("Size");
                header.Add("Time");
                writer.WriteRow(header);
                foreach (var group in rows.GroupBy(r => r.n))
                {
                    int n = group.Key;
                    int i = group.Count() / n + 1;
                    decimal multiplier = 1.0M / ((n + 1) * i);
                    List<string> row = new List<string>();
                    row.Add(group.Key.ToString());
                    row.Add((group.Sum(g => g.PercentSize) * multiplier).ToString());
                    row.Add((group.Sum(g => g.Time) * multiplier).ToString());
                    writer.WriteRow(row);
                }
            }
        }

        static List<Tuple<List<string>, List<string>>> GenerateRandomSigma(List<string> T, int min, int max, Random rng)
        {
            List<Tuple<List<string>, List<string>>> sigma = new List<Tuple<List<string>, List<string>>>();

            int fdsToGenerate = rng.Next(max + 1 - min) + min;
            int fdsGenerated = 0;
            if (fdsToGenerate < 1)
            {
                Console.WriteLine("Something has gone wrong, it wants to generate no functional dependencies.");
            }
            int failureCount = 0;
            while (fdsGenerated < fdsToGenerate && failureCount < 10)   //If we have failed to generate a functional dependency a bunch of times, we've either filled or close to filled the selection space
            {
                var newFD = RandomFD(T, rng);
                //make sure the FD doesn't already exist
                bool alreadyExists = false;
                foreach (var fd in sigma)
                {
                    if (newFD.Item1.Count == fd.Item1.Count && newFD.Item2.Count == fd.Item2.Count)
                    {
                        //there is a functional dependency of the same size as the one we just generated, better check it out
                        if (newFD.Item1.All(l => fd.Item1.Any(f => f == l)) && newFD.Item2.All(r => fd.Item2.Any(f => f == r)))
                        {
                            //All the elements on both sides are present, and since they are the same size, this means that they are identical
                            alreadyExists = true;
                        }
                    }
                }
                //Add it to sigma
                if (!alreadyExists)
                {
                    sigma.Add(newFD);
                    fdsGenerated++;
                    failureCount = 0;
                }
                else
                {
                    failureCount++;
                }
            }

            return sigma;
        }

        static Tuple<List<string>, List<string>> RandomFD(List<string> T, Random rng)
        {
            var randomPick = T.OrderBy(t => rng.Next(int.MaxValue)).ToList();   //Need to use a toList here, otherwise it uses a different list for lhs and rhs
            //Pick a random left hand side of up to T.Count - 1 elements (I think it would be best if this is weighted towards the low end)
            int lhsCount = rng.Next(T.Count - 1) + 1;
            List<string> lhs = randomPick.Take(lhsCount).OrderBy(a => a).ToList();
            //Pick a random right hand side of up to T.Count - lhs.Count elements (again, weighted towards the low end)
            int rhsCount = rng.Next(T.Count - lhsCount) + 1;
            List<string> rhs = randomPick.Skip(lhsCount).Take(rhsCount).OrderBy(a => a).ToList();

            return new Tuple<List<string>, List<string>>(lhs, rhs);
        }

        static List<Tuple<List<string>, List<string>>> ComputeCover(Schema instance)
        {
            var stopWatch = Stopwatch.StartNew();
            List<string> T = instance.Attributes;
            List<string> Ts = instance.NullFreeSubSchema;
            List<Tuple<List<string>, List<string>>> sigma = instance.FunctionalDependencies;
            //We want to begin by computing the closure with the default sigma for each element (we compare this with our covers closures);
            //First, we check to make sure this instance has not already had its closures calculated
            foreach (string attribute in T)
            {
                instance.Closures.Add(attribute, DetermineClosure(instance.Attributes, instance.NullFreeSubSchema, instance.FunctionalDependencies, attribute));
            }

            //Line 2: Sigmac <- Sigma
            List<Tuple<List<string>, List<string>>> sigmaC = CopySigma(sigma);

            //Line 3 and 7: For all sig in SigmaC do
            int indexToRemove = 0;
            while (indexToRemove < sigmaC.Count)
            {
                //Line 4 & 6: if (SigmaC - {sig}) |=Ts sig then (If SigmaC still implies (in the presence of Ts) sig then)
                //I guess in order to do this, we create a copy of sigmaC, sans sig
                var sansSig = CopySigma(sigmaC);
                sansSig.RemoveAt(indexToRemove);
                //Then, using the closure algorithm, we compute the closure of all attributes in T given the new set of functional dependencies
                Dictionary<string, List<string>> sansSigClosures = new Dictionary<string, List<string>>();
                foreach (string attribute in T)
                {
                    sansSigClosures.Add(attribute, DetermineClosure(T, Ts, sansSig, attribute));
                }
                //Then we check that the set of closures we computed matches the original set of closures
                if (sansSigClosures.Count == instance.Closures.Count)
                {
                    bool allMatch = true;
                    foreach (var attribute in instance.Closures.Keys)
                    {
                        foreach (var beta in instance.Closures[attribute])
                        {
                            if (!sansSigClosures[attribute].Any(ss => ss == beta))
                            {
                                allMatch = false;
                            }
                        }
                    }
                    //All the closures match, sig was a redundant FD
                    if (allMatch)
                    {
                        //Line 5: SigmaC <- SigmaC - {sig};
                        sigmaC = sansSig;
                    }
                    else
                    {
                        indexToRemove++;
                    }
                }
                else
                {
                    indexToRemove++; //if we removed one, the remaining entries shuffle down, so we don't need to increment indexToRemove. If we don't remove one, increment it
                }
            }
            instance.CoverComputationTime = stopWatch.ElapsedMilliseconds / 1000.0M;
            instance.CoverPercentSize = ((decimal)sigmaC.Count / (decimal)sigma.Count) * 100.0M;
            //Line 8: return SigmaC
            return sigmaC;
        }

        /// <summary>
        /// Algorithm references lines in the slides
        /// </summary>
        /// <param name="X">The attribute for which we compute the closure</param>
        static List<string> DetermineClosure(List<String> T, List<string> Ts, List<Tuple<List<string>, List<string>>> sigma, string X)
        {
            List<string> closure = new List<string>();
            List<string> oldClosure = new List<string>();
            //Line 2: Closure <- X
            closure.Add(X);
            //Line 3: FDList <- List of FDs in Sigma
            List<Tuple<List<string>, List<string>>> FDList = CopySigma(sigma);
            //lines 4 and 12: repeat ~ until Closure = OldClosure or FDList = []
            while (!(closure.Count == oldClosure.Count && closure.All(c => oldClosure.Any(oc => oc == c))) && !(FDList.Count == 0))
            {
                //Line 5: OldClosure <- Closure
                oldClosure.Clear();
                oldClosure.AddRange(closure);
                //Line 6 & 7: Remove all attributes in the intersection of Closure and XTs from the LHS of FDs in FDList
                //First we determine the intersection of Closure and XTs
                List<string> intersection = new List<string>();
                intersection.AddRange(Ts);
                if (!intersection.Any(i => i == X))
                {
                    intersection.Add(X);
                }
                intersection.RemoveAll(x => !closure.Any(c => c == x));
                //Then we remove everything in intersection from the FDs
                foreach (var fd in FDList)
                {
                    fd.Item1.RemoveAll(a => intersection.Any(i => i == a));
                }
                //Line 8 & 11: For all entries in FDList where 0 => Y (all lhs elements removed) do
                for (int i = 0; i < FDList.Count; i++)
                {
                    if (FDList[i].Item1.Count == 0)
                    {
                        //Line 9: Closure <- Closure U Y (add any elements from the RHS of the functional dependency that are not already in the closure, to the closure
                        closure.AddRange(FDList[i].Item2.Where(v => !closure.Any(c => c == v)));

                    }
                }
                //Line 10: FDList <- FDList - {0 => Y} (remove the entries with no LHS from the FDList (we do this outside of the loop for code simplicity)
                FDList.RemoveAll(s => s.Item1.Count == 0);
            }

            //Line 13: return(Closure);
            return closure;

        }

        /// <summary>
        /// Performes a deep copy of a list of FDs (List of Tuples of lists of strings)
        /// </summary>
        static List<Tuple<List<string>, List<string>>> CopySigma(List<Tuple<List<string>, List<string>>> sigma)
        {
            List<Tuple<List<string>, List<string>>> copy = new();
            foreach (var pairing in sigma)
            {
                List<string> left = new();
                List<string> right = new();
                foreach (string v1 in pairing.Item1)
                {
                    left.Add(new string(v1));

                }
                foreach (string v2 in pairing.Item2)
                {
                    right.Add(new string(v2));
                }
                copy.Add(new Tuple<List<string>, List<string>>(left, right));
            }
            return copy;
        }

        static Schema BuildSchemaInstanceFromUserInput()
        {
            Schema instance = new Schema();

            //Retrieve from the user the list of attributes
            Console.WriteLine("Enter attribute names, one per line. Enter an empty line when done adding attributes.");
            bool doneAddingAttributes = false;
            while (!doneAddingAttributes)
            {
                string? line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    doneAddingAttributes = true;
                }
                else
                {
                    instance.Attributes.Add(line.Trim());
                }
            }
            //Retrieve from the user the null free subschema
            instance.ListAttributes();
            Console.WriteLine("From the attributes listed, enter the attributes which are in the null free subschema, one per line. Empty line to move to the next step.");
            bool doneAddingNFS = false;
            while (!doneAddingNFS)
            {
                string? line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    doneAddingNFS = true;
                }
                else
                {
                    if (instance.Attributes.Any(a => a == line.Trim()))
                    {
                        instance.NullFreeSubSchema.Add(line.Trim());
                    }
                    else
                    {
                        Console.WriteLine("Error: Attribute " + line.Trim() + " not found.");
                    }
                }
            }
            //Retrieve from the user the functional dependencies
            Console.WriteLine("Enter the functional dependencies one per line in the following format:");
            Console.WriteLine("A, B => C, D");
            Console.WriteLine("Comma seperated attributes. Empty line to move the the next step.");
            bool doneAddingFDs = false;
            string[] splitter = { "=>" };
            char[] subSplitter = { ',' };
            while (!doneAddingFDs)
            {
                string? line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    doneAddingFDs = true;
                }
                else
                {
                    bool badLine = false;
                    string[] sides = line.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                    string[] leftAttributes = sides[0].Split(subSplitter, StringSplitOptions.RemoveEmptyEntries);
                    string[] rightAttributes = sides[1].Split(subSplitter, StringSplitOptions.RemoveEmptyEntries);
                    //Make sure that a valid FD has been entered
                    if (leftAttributes.Count() > 0 && rightAttributes.Count() > 0)
                    {
                        List<string> finalLeft = new List<string>();
                        List<string> finalRight = new List<string>();
                        foreach (var attribute in leftAttributes)
                        {
                            if (instance.Attributes.Any(a => a == attribute.Trim()))
                            {
                                finalLeft.Add(attribute.Trim());
                            }
                            else
                            {
                                badLine = true;
                                break;
                            }
                        }
                        foreach (var attribute in rightAttributes)
                        {
                            if (instance.Attributes.Any(a => a == attribute.Trim()))
                            {
                                finalRight.Add(attribute.Trim());
                            }
                            else
                            {
                                badLine = true;
                                break;
                            }
                        }
                        //All the attributes specified were present in the schema, so add the FD
                        if (!badLine)
                        {
                            instance.FunctionalDependencies.Add(new Tuple<List<string>, List<string>>(finalLeft, finalRight));
                        }
                    }
                    else
                    {
                        badLine = true;
                    }

                    if (badLine)
                    {
                        Console.WriteLine("Error: Invalid FD");
                    }
                }
            }
            //Add the UQs
            bool doneAddingUQs = false;
            Console.WriteLine("Enter the uniqueness constraints, one per line, in the format A, B. Empty line moves to the next step.");
            while (!doneAddingUQs)
            {
                string? line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    doneAddingUQs = true;
                }
                else
                {
                    List<string> finalAttributes = new List<string>();
                    foreach (string attribute in line.Split(subSplitter, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (instance.Attributes.Any(a => a == attribute.Trim()))
                        {
                            finalAttributes.Add(attribute.Trim());
                        }
                        else
                        {
                            Console.WriteLine("Error: Invalid uniqueness constraint.");
                            break;
                        }
                    }
                    //if we've made it to here, it's a valid UQ, add it to the Schema
                    instance.UniquenessConstraints.Add(finalAttributes);
                }
            }
            //Write the whole thing out to make sure we're happy
            Console.WriteLine("Schema instance created. The schema is as follows:");
            instance.Print();

            return instance;
        }

        /// <summary>
        /// Builds a schema from user input, then generates a non redundant cover, which it displays to the screen
        /// </summary>
        static void UserInputCoverTest()
        {
            Schema instance = BuildSchemaInstanceFromUserInput();

            var nonRedundantCover = ComputeCover(instance);
            Console.WriteLine(string.Empty);
            Console.WriteLine("The FDs in the non redundant cover are:");
            foreach (var fd in nonRedundantCover)
            {
                Console.WriteLine(fd.Item1.Aggregate((x, y) => x + ", " + y) + " => " + fd.Item2.Aggregate((x, y) => x + ", " + y));
            }
            Console.WriteLine(string.Format("It took {0} seconds to compute the cover.", instance.CoverComputationTime));
        }

        /// <summary>
        /// Builds a schema from user input, then prompts the user to specify an attribute, for which it computes the closure
        /// </summary>
        static void UserInputClosureTest()
        {
            Schema instance = BuildSchemaInstanceFromUserInput();

            Console.WriteLine("Enter the attribute for which to compute the closure:");
            string? attribute = Console.ReadLine();
            if (!string.IsNullOrEmpty(attribute) && instance.Attributes.Any(a => a == attribute.Trim()))
            {
                var closure = DetermineClosure(instance.Attributes, instance.NullFreeSubSchema, instance.FunctionalDependencies, attribute.Trim());
                Console.WriteLine("The closure for attribute " + attribute.Trim() + " is:");
                Console.WriteLine(closure.Aggregate((x, y) => x + ", " + y));
            }
            else
            {
                Console.WriteLine("Attribute not found.");
            }
        }
    }

    public class PartIIOut
    {
        public int n { get; set; }
        public int k { get; set; }
        public int SigmaSize { get; set; }
        public decimal Time { get; set; }
        public decimal PercentSize { get; set; }
    }

    /// <summary>
    /// In this class, the summations form an average over both k and i
    /// </summary>
    public class PartIIIOut2D
    {
        public int N { get; set; }
        public decimal TimeSummation { get; set; }
        public decimal SizeSummation { get; set; }
    }
}