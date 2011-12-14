using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HTM.Runner
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Usage: htm TRAINING-SENSOR-FILE TRAINING-CATEGORY-FILE TEST-SENSOR-FILE TEST-CATEGORY-FILE [spatialonly] [PARAMETER VALUE [PARAMETER VALUE ...]]");
				Console.WriteLine("Parameters (Default Value):");
				Console.WriteLine("nodesPerLevel (1)");
				Console.WriteLine("columnCount (100)");
				Console.WriteLine("cellsPerColumn (4)");
				Console.WriteLine("desiredLocalActivity (10)");
				Console.WriteLine("minOverlap (2)");
				Console.WriteLine("maxSynapsesPerColumn (2)");
				Console.WriteLine("connectedPerm (0.2)");
				Console.WriteLine("permanenceInc (0.01)");
				Console.WriteLine("permanenceDec (0.01)");
				Console.WriteLine("slidingWindowLength (1000)");
				Console.WriteLine("activationThreshold (1)");
				Console.WriteLine("minThreshold (1)");
				Console.WriteLine("learningRadius (10)");
				Console.WriteLine("newSynapseCount (10)");
				Console.WriteLine("floatPrecision (50)");
				Console.WriteLine("warmUps (1)");
				Console.WriteLine();
				Console.WriteLine("trainingLog (NONE) - File name to log training output");
				Console.WriteLine("testLog (NONE) - File name to log test output");
				return;
			}

			string trainingLog = null;
			string testLog = null;

			int floatPrecision = 50;

			int columnCount = 100, cellsPerColumn = 4, desiredLocalActivity = 10, minOverlap = 2, maxSynapsesPerColumn = 2;
			float connectedPerm = 0.2f, permanenceInc = 0.01f, permanenceDec = 0.01f;
			int slidingWindowLength = 1000, activationThreshold = 1;
			int minThreshold = 1;
			int learningRadius = 10, newSynapseCount = 10;
			int warmUps = 1;

			bool spatialOnly = false;

			int[] nodesPerLevel = new[] { 1 };

			var ci = System.Globalization.CultureInfo.InvariantCulture;

			for (var i = 4; i < args.Length; i++)
			{
				var cmd = args[i].ToLowerInvariant();
				switch (cmd)
				{
					case "nodesperlevel":
						var levels = new List<int>();
						int v;
						while (++i < args.Length)
						{
							if (!int.TryParse(args[i], out v))
							{
								i--;
								break;
							}
							levels.Add(v);
						}
						nodesPerLevel = levels.ToArray();
						break;
					case "warmups":
						warmUps = int.Parse(args[++i], ci);
						break;
					case "columncount":
						columnCount = int.Parse(args[++i], ci);
						break;
					case "cellspercolumn":
						cellsPerColumn = int.Parse(args[++i], ci);
						break;
					case "desiredlocalactivity":
						desiredLocalActivity = int.Parse(args[++i], ci);
						break;
					case "minoverlap":
						minOverlap = int.Parse(args[++i], ci);
						break;
					case "maxsynapsespercolumn":
						maxSynapsesPerColumn = int.Parse(args[++i], ci);
						break;
					case "connectedperm":
						connectedPerm = float.Parse(args[++i], ci);
						break;
					case "permanenceinc":
						permanenceInc = float.Parse(args[++i], ci);
						break;
					case "permanencedec":
						permanenceDec = float.Parse(args[++i], ci);
						break;
					case "slidingwindowlength":
						slidingWindowLength = int.Parse(args[++i], ci);
						break;
					case "activationthreshold":
						activationThreshold = int.Parse(args[++i], ci);
						break;
					case "minthreshold":
						minThreshold = int.Parse(args[++i], ci);
						break;
					case "learningradius":
						learningRadius = int.Parse(args[++i], ci);
						break;
					case "newsynapsecount":
						newSynapseCount = int.Parse(args[++i], ci);
						break;
					case "floatprecision":
						floatPrecision = int.Parse(args[++i], ci);
						break;
					case "traininglog":
						trainingLog = args[++i];
						break;
					case "testlog":
						testLog = args[++i];
						break;
					case "spatialonly":
						spatialOnly = true;
						break;
					default:
						Console.WriteLine("Unknown argument: " + args[i]);
						return;
				}
			}

			var trainingData = ReadTestData(args[0], args[1]);
			var testData = ReadTestData(args[2], args[3]);

			var allValues = trainingData.SelectMany(r => r.Columns)
				.Union(trainingData.SelectMany(r => r.Columns));

			float min = allValues.Min(), max = allValues.Max();

			object node;

			if (spatialOnly)
			{
				node = new SpatialPooler(
					trainingData.First().Columns.Length * floatPrecision,
					columnCount, desiredLocalActivity, minOverlap, maxSynapsesPerColumn,
					connectedPerm, permanenceInc, permanenceDec,
					slidingWindowLength
				);
			}
			else
			{
				node = new Tree(
					nodesPerLevel,
					trainingData.First().Columns.Length * floatPrecision,
					columnCount, cellsPerColumn, desiredLocalActivity, minOverlap, maxSynapsesPerColumn,
					connectedPerm, permanenceInc, permanenceDec,
					slidingWindowLength, activationThreshold,
					minThreshold,
					learningRadius, newSynapseCount
				);
			}

			TestData(node, trainingData, testData, warmUps, trainingLog, testLog, c => BitMapper.Map(c, min, max, floatPrecision));
		}

		static void TestData(dynamic node, IEnumerable<TestFrame> trainingData, IEnumerable<TestFrame> testData, int warmUps, string trainingLog, string testLog, Func<float[], Func<int, bool>> floatMapper)
		{
			var enode = node as IEnumerable<bool>;
			
			var cl = new AlsingClassifier();

			var warmupStart = DateTime.Now;

			for (var w = 0; w < warmUps; w++)
				foreach (var frame in trainingData)
					node.Feed(floatMapper(frame.Columns));
			
			var warmupEnd = DateTime.Now;

			var trainingStart = DateTime.Now;

			using (var log = trainingLog == null ? null : new StreamWriter(trainingLog, false, Encoding.ASCII))
				foreach (var frame in trainingData)
				{
					node.Feed(floatMapper(frame.Columns));
					cl.Train(frame.Category.ToString(), enode.ToArray());

					if (log != null) log.WriteLine(String.Join(" ", enode.Select(d => d ? '1' : '0')));
				}

			var trainingEnd = DateTime.Now;

			node.Learning = false;

			var acurate = 0;
			var total = 0;

			var accuracy = new Dictionary<string, Accuracy>();

			var testStart = DateTime.Now;

			using (var log = testLog == null ? null : new StreamWriter(testLog, false, Encoding.ASCII))
				foreach (var frame in testData)
				{
					var activeCount = 0;

					Func<int, bool> input = floatMapper(frame.Columns);
					node.Feed(input);

					foreach (var b in enode)
						if (b)
							activeCount++;

					var matches = cl.FindMatches(enode.ToArray())
						.OrderByDescending(c => c.Strength)
						.Take(2)
						.ToArray();

					if (log != null) log.WriteLine(String.Join(" ", enode.Select(d => d ? '1' : '0')));

					var cat = frame.Category.ToString();
					if (matches[0].Identifier == cat) acurate++;
					total++;

					Accuracy v;
					if (!accuracy.TryGetValue(cat, out v))
						accuracy.Add(cat, v = new Accuracy());

					if (matches[0].Identifier == frame.Category.ToString()) v.acurate++;
					v.total++;

					Console.WriteLine("Expected: {0}  Matched: {1} ({3:p})  Runner Up: {2} ({4:p})  Activity Count: {5}", frame.Category, matches[0].Identifier, matches[1].Identifier, matches[0].Strength, matches[1].Strength, activeCount);
				}

			var testEnd = DateTime.Now;

			Console.WriteLine("Accuracy: {0:p}", ((float)acurate) / ((float)total));

			foreach (var pair in accuracy)
				Console.WriteLine("Accuracy {1}: {0:p}", ((float)pair.Value.acurate) / ((float)pair.Value.total), pair.Key);

			Console.WriteLine();
			Console.WriteLine("Warm up:\t{0:g}", warmupEnd - warmupStart);
			Console.WriteLine("Training:\t{0:g}", trainingEnd - trainingStart);
			Console.WriteLine("Test:\t{0:g}", testEnd - testStart);
		}

		public class Accuracy
		{
			public int total;
			public int acurate;
		}

		static IEnumerable<TestFrame> ReadTestData(string sensorFile, string categoryFile)
		{
			var ic = System.Globalization.CultureInfo.InvariantCulture;
			using (var sensor = new StreamReader(sensorFile, Encoding.ASCII))
			using (var category = new StreamReader(categoryFile, Encoding.ASCII))
			{
				string s, c;
				while ((s = sensor.ReadLine()) != null && (c = category.ReadLine()) != null)
				{
					yield return new TestFrame
					{
						Category = c,
						Columns = s.Split(' ', '\t').Select(v => float.Parse(v, ic)).ToArray()
					};
				}
			}
		}

		class TestFrame
		{
			public string Category;
			public float[] Columns;
		}

	}
}
