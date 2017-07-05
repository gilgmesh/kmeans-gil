using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MvcApplication1.Controllers;

namespace MvcApplication1.Kmeans
{
    public class Kmeans
    {
        public void ClusterMain()
        {

            var coords = ReadPoints("data.txt");
            var coordinates = coords as ClusteringController.Coordinate[] ?? coords.ToArray();

            double averageDistanceToCentroids;
            ClusteringController.Coordinate[] centroids;
            var clustering = K_means(coordinates, out centroids, out averageDistanceToCentroids);
        }

        public IEnumerable<int> K_means(
            IReadOnlyList<ClusteringController.Coordinate> coordinates,
            out ClusteringController.Coordinate[] centroids,
            out double averageDistanceToCentroids)
        {
            // this method find the best K,  by diminishing marginal gain

            const double minimumDrop = 0.02;
            const double maxMarginalDrop = 0.95;

            var k = 2;
            var bestK = 0;

            int iterations;

            var centroidDictionary = new Dictionary<int, ClusteringController.Coordinate[]>();
            var distanceDictionary = new Dictionary<int, double>();
            var clusteringDictionary = new Dictionary<int, int[]>();

            var done = false;

            var clustering = K_means(k, coordinates, out iterations, out centroids, out averageDistanceToCentroids).ToArray();
            distanceDictionary.Add(k, averageDistanceToCentroids);
            centroidDictionary.Add(k, centroids);
            clusteringDictionary.Add(k, clustering);

            var currentDrop = minimumDrop;

            do
            {
                k++;
                var preiousDrop = currentDrop;

                ClusteringController.Coordinate[] nextCentroids;
                double nextAverageDistanceToCentroids;

                var nextClustering =
                    K_means(k, coordinates, out iterations, out nextCentroids, out nextAverageDistanceToCentroids).ToArray();
                distanceDictionary.Add(k, nextAverageDistanceToCentroids);
                centroidDictionary.Add(k, nextCentroids);
                clusteringDictionary.Add(k, nextClustering);

                currentDrop = nextAverageDistanceToCentroids/distanceDictionary[2];
                var marginalDrop = currentDrop/preiousDrop;

                if ((marginalDrop < maxMarginalDrop) || (marginalDrop > 1.0))
                    continue;

                done = true;
                bestK = k - 1;
            } while (!done);

            centroids = centroidDictionary[bestK];
            averageDistanceToCentroids = distanceDictionary[bestK];
            return clusteringDictionary[bestK];
        }

        private static IEnumerable<int> K_means(
            int k,
            IReadOnlyList<ClusteringController.Coordinate> coords,
            out int actualIterations,
            out ClusteringController.Coordinate[] centroids,
            out double averageDistanceToCentroids)
        {
            const int maxTriesTillDone = 2; // 100;
            var triesTillDone = maxTriesTillDone;
            Console.WriteLine("k = {0}", k);
            Console.WriteLine("   till done : {0}", triesTillDone);

            int iterations;

            var clusterring = KmeansInner(k, coords, out iterations, out centroids, out averageDistanceToCentroids).ToArray();

            // run a few times to try and get a better final result, and not be stuck in local maxima
            do {
                double newAverageDistanceToCentroids;
                ClusteringController.Coordinate[] newCentroids;

                var newClusterring = KmeansInner(k, coords, out iterations, out newCentroids, out newAverageDistanceToCentroids).ToArray();

                if (newAverageDistanceToCentroids >= averageDistanceToCentroids)
                {
                    triesTillDone--;
                    Console.WriteLine("   till done : {0} ; ave. dist. = {1}", triesTillDone, averageDistanceToCentroids);
                }
                else
                {
                    {
                        triesTillDone = maxTriesTillDone;
                        Console.WriteLine("   till done : {0} ; ave. dist. = {1}", triesTillDone, averageDistanceToCentroids);
                        centroids = new List<ClusteringController.Coordinate>(newCentroids).ToArray();
                        averageDistanceToCentroids = newAverageDistanceToCentroids;
                        clusterring = new List<int>(newClusterring).ToArray();
                    }
                }
            }
            while (triesTillDone > 0);

            actualIterations = iterations;
            return clusterring;
        }

        private static IEnumerable<int> KmeansInner(
            int k, 
            IEnumerable<ClusteringController.Coordinate> coords, 
            out int actualIterations,
            out ClusteringController.Coordinate[] outCentroids,
            out double averageDistanceToCentroids)
        {
            const int maxIterations = 1000;

            var points = coords as ClusteringController.Coordinate[] ?? coords.ToArray();
            
            var centroids = GetInitialCentroids(k, points);
            var clusterAssignment = points.Select(p => FindNearestCluster(p, centroids)).ToArray();
            var newClusterAssignment = new List<int>(clusterAssignment).ToArray();
            ClusteringController.Coordinate[] updatedCentroids;

            var counter = 0;
            do
            {
                counter++;
                clusterAssignment = new List<int>(newClusterAssignment).ToArray();

                updatedCentroids = RecomputeCentroids(k, points, clusterAssignment);
                newClusterAssignment = points.Select(p => FindNearestCluster(p, updatedCentroids)).ToArray();
            }
            while ((counter < maxIterations) && !SameList(clusterAssignment, newClusterAssignment));

            actualIterations = counter;
            outCentroids = new List<ClusteringController.Coordinate>(centroids).ToArray();
            averageDistanceToCentroids = ComputeAverageDistanceToCentroids(points, centroids, newClusterAssignment);
            return newClusterAssignment;
        }

        private static double ComputeAverageDistanceToCentroids(
            IReadOnlyList<ClusteringController.Coordinate> points, 
            IReadOnlyList<ClusteringController.Coordinate> centroids, 
            IReadOnlyList<int> clusterAssignment)
        {
            if (centroids == null) 
                throw new ArgumentNullException("centroids");

            var spaceSize = points.Count;

            var sumDistances = 0.0;
            for (var i = 0; i < spaceSize; i++)
                sumDistances += Math.Sqrt(DistanceToClusterSquared(points[i], centroids[clusterAssignment[i]]));

            var result = sumDistances / spaceSize;

            return result;
        }

        private static bool SameList(IReadOnlyList<int> clusterAssignment, IReadOnlyList<int> newClusterAssignments)
        {
            var clusterLength = clusterAssignment.Count;
            if (clusterLength != newClusterAssignments.Count)
                return false;

            var result = true;
            var counter = 0;
            while (result && (counter < clusterLength))
            {
                result = clusterAssignment[counter] == newClusterAssignments[counter];

                counter++;
            }
            
            return result;
        }

        private static ClusteringController.Coordinate[] RecomputeCentroids(int k, IEnumerable<ClusteringController.Coordinate> points, IReadOnlyList<int> clusterAssignment)
        {
            var centroids = new ClusteringController.Coordinate[k];
            var sums = new ClusteringController.Coordinate[k];
            var counts = new int[k];
            var counter = -1;

            for (var i = 0; i < k; i++)
            {
                centroids[i] = new ClusteringController.Coordinate { X = 0.0, Y = 0.0 };
                sums[i] = new ClusteringController.Coordinate { X = 0.0, Y = 0.0 };
            }

            foreach (var point in points)
            {
                counter++;

                var clusterNumber = clusterAssignment[counter];

                counts[clusterNumber]++;
                sums[clusterNumber].X += point.X;
                sums[clusterNumber].Y += point.Y;
            }

            for (var i = 0; i < k; i++)
            {
                centroids[i].X = sums[i].X / counts[i];
                centroids[i].Y = sums[i].Y / counts[i];
            }

            return centroids;
        }

        private static double DistanceToClusterSquared(ClusteringController.Coordinate coordinate, ClusteringController.Coordinate centroid)
        {
            var result = Math.Pow(centroid.X - coordinate.X, 2.0) + Math.Pow(centroid.Y - coordinate.Y, 2.0);

            return result;
        }

        private static int FindNearestCluster(ClusteringController.Coordinate coordinate, ClusteringController.Coordinate[] centroids)
        {
            var minCluster = -1;
            var minDistance = double.MaxValue;
            var counter = -1;
            foreach (var centroid in centroids)
            {
                counter++;

                var distance = DistanceToClusterSquared(coordinate, centroid);

                if (distance > minDistance) 
                    continue;

                minDistance = distance;
                minCluster = counter;
            }

            return minCluster;
        }

        private static ClusteringController.Coordinate[] GetInitialCentroids(int k, IReadOnlyList<ClusteringController.Coordinate> coordinates)
        {
            // trying something different, for k=4
            //    I will add "heat" concept later if time allows, for now disable...
            /*
            if (k == 4)
            {
                var xMin = coordinates.Min(c => c.X);
                var xMax = coordinates.Max(c => c.X);

                var yMin = coordinates.Min(c => c.Y);
                var yMax = coordinates.Max(c => c.Y);

                var xLength = xMax - xMin;
                var yLength = yMax - yMin;

                var xFraction = 0.15*xLength;
                var yFraction = 0.15*yLength;

                var centroids4 = new Coordinate[4];
                centroids4[0] = new Coordinate
                {
                    X = xMin + xFraction,
                    Y = yMin + yFraction
                };

                centroids4[1] = new Coordinate
                {
                    X = xMax - xFraction,
                    Y = yMin + yFraction
                };

                centroids4[2] = new Coordinate
                {
                    X = xMax - xFraction,
                    Y = yMax - yFraction
                };

                centroids4[3] = new Coordinate
                {
                    X = xMin + xFraction,
                    Y = yMax - yFraction
                };

                return centroids4;
            }
             * */

            var spaceSize = coordinates.Count;
            var starterNodes = new List<int>();

            var rnd = new Random();

            do
            {
                var nextCandidate = rnd.Next(0, spaceSize);
                if (!starterNodes.Contains(nextCandidate))
                    starterNodes.Add(nextCandidate);
            } while (starterNodes.Count < k);

            var starterNodesArray = starterNodes.ToArray();
            var centroids = new ClusteringController.Coordinate[k];
            for (var i = 0; i < k; i++)
                centroids[i] = coordinates[starterNodesArray[i]];

            return centroids;
        }

        public IEnumerable<ClusteringController.Coordinate> ReadPoints(string fileName)
        {
            var coords = new List<ClusteringController.Coordinate>();

            using (var reader = new StreamReader(fileName))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;

                    var items = line.Split(',');

                    var coord = new ClusteringController.Coordinate { X = Convert.ToDouble(items[0]), Y = Convert.ToDouble(items[1])};

                    coords.Add(coord);
                }
            }

            return coords;
        }
    }
}
