using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace HTM.Tests
{
	[TestClass]
	public class TreeTests
	{
		[TestMethod]
		public void TestData()
		{
			var trainingData = ReadTestData("../../../../Data/train_sensor.txt", "../../../../Data/train_category.txt");

			var columnCount = 400;
			var cellsPerColumn = 2;

			var floatPrecision = 50;

			var node = new Tree(
				new [] { 32 },
				32 * floatPrecision,
				columnCount,
				cellsPerColumn: cellsPerColumn,
				desiredLocalActivity: 50,
				minOverlap: 4,
				maxSynapsesPerColumn: 20,
				newSynapseCount: 4
			);

			var cl = new AlsingClassifier();

			foreach (var frame in trainingData)
				node.Feed(BitMapper.Map(frame.Columns, -0.6f, 2.21f, floatPrecision));

			using (var log = new StreamWriter("../../../../Data/train_result.txt", false, Encoding.ASCII))
			foreach (var frame in trainingData)
			{
				node.Feed(BitMapper.Map(frame.Columns, -0.6f, 2.21f, floatPrecision));
				cl.Train(frame.Category.ToString(), node.ToArray());
				log.WriteLine(node.Select(v => v ? '1' : '0').ToArray());
			}

			node.Learning = false;

			var testData = ReadTestData("../../../../Data/test_sensor.txt", "../../../../Data/test_category.txt");

			var acurate = 0;
			var total = 0;

			var accuracy = new Dictionary<string, Accuracy>();

			using (var log = new StreamWriter("../../../../Data/test_result.txt", false, Encoding.ASCII))
			foreach (var frame in testData)
			{
				var activeCount = 0;

				Func<int, bool> input = BitMapper.Map(frame.Columns, -0.6f, 2.21f, floatPrecision);
				node.Feed(input);

				foreach (var b in node)
					if (b)
						activeCount++;

				var matches = cl.FindMatches(node.ToArray())
					.OrderByDescending(c => c.Strength)
					.Take(2)
					.ToArray();

				log.WriteLine(node.Select(d => d ? '1' : '0').ToArray());

				/*for (var i = 0; i < 32 * 50; i++)
					Console.Write(input(i) ? "1" : "0");
				Console.WriteLine();

				Console.WriteLine(String.Join("", outputData.Select(v => v ? "1" : "0")));*/

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
			Console.WriteLine("Accuracy: {0:p}", ((float)acurate) / ((float)total));

			foreach (var pair in accuracy)
				Console.WriteLine("Accuracy {1}: {0:p}", ((float)pair.Value.acurate) / ((float)pair.Value.total), pair.Key);
		}

		public class Accuracy
		{
			public int total;
			public int acurate;
		}

		IEnumerable<TestFrame> ReadTestData(string sensorFile, string categoryFile)
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
						Category = int.Parse(c, ic),
						Columns = s.Split(' ', '\t').Select(v => float.Parse(v, ic)).ToArray()
					};
				}
			}
		}

		class TestFrame
		{
			public int Category;
			public float[] Columns;
		}
	}
}
