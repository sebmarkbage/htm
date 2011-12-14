using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace HTM.Tests
{
	[TestClass]
	public class SpatialPoolerWithClassifier
	{
		[TestMethod]
		public void CountActiveColumnsPerStepWithMovingInput()
		{
			var sp = new SpatialPooler(
				inputCount: 100,
				columnCount: 100,
				desiredLocalActivity: 5,
				minOverlap: 2,
				maxSynapsesPerColumn: 20
			);

			var cl = new AlsingClassifier();

			var values = new bool[100];
			for (var i = 0; i < 100; i++)
				values[i] = i < 5;


			for (var t = 0; t < 10000; t++)
			{
				Func<int, bool> input = (i) => values[i];

				sp.Feed(input);

				values.Shift();
			}

			sp.Learning = false;

			for (var t = 0; t < 100; t++)
			{
				Func<int, bool> input = (i) => values[i];

				sp.Feed(input);

				cl.Train((t / 10).ToString(), sp.ToArray());
				values.Shift();
			}

			for (var i = 0; i < 100; i++)
				values[i] = i < 6;

			var rnd = new Random();

			for (var t = 0; t < 1000; t++)
			{
				var activeCount = 0;

				var noiseInput = (bool[])values.Clone();
				for (var i = 0; i < 100; i++)
					if (rnd.NextDouble() < 0.05)
						noiseInput[i] = !noiseInput[i];

				Func<int, bool> input = (i) => noiseInput[i];

				sp.Feed(input);

				foreach (var b in sp)
					if (b)
						activeCount++;

				var matches = cl.FindMatches(sp.ToArray())
					.OrderByDescending(c => c.Strength)
					.Select(m => m.Identifier + ": " + Math.Round(m.Strength * 100, 2) + "%");

				Console.Write(String.Join("", sp.Select(v => v ? "1" : "0")) + "\t");

				Console.WriteLine(String.Join("\t", matches));

				values.Shift();
			}
		}

		[TestMethod]
		public void TestData()
		{
			var trainingData = ReadTestData("../../../../Data/train_sensor.txt", "../../../../Data/train_category.txt");

			var columnCount = 300;

			var sp = new SpatialPooler(
				inputCount: 32 * 50,
				columnCount: columnCount,
				desiredLocalActivity: 10,
				minOverlap: 2,
				maxSynapsesPerColumn: 20
			);

			var cl = new AlsingClassifier();

			foreach (var frame in trainingData)
				sp.Feed(BitMapper.Map(frame.Columns, -0.6f, 2.1f, 50));

			using (var log = new StreamWriter("../../../../Data/train_result_spatial.txt", false, Encoding.ASCII))
			foreach (var frame in trainingData)
			{
				sp.Feed(BitMapper.Map(frame.Columns, -0.6f, 2.1f, 50));
				cl.Train(frame.Category.ToString(), sp.ToArray());
				log.WriteLine(String.Join(" ", sp.Select(v => v ? '1' : '0')));
			}

			sp.Learning = false;

			var testData = ReadTestData("../../../../Data/test_sensor.txt", "../../../../Data/test_category.txt");

			using (var log = new StreamWriter("../../../../Data/test_result_spatial.txt", false, Encoding.ASCII))
			foreach (var frame in testData)
			{
				var activeCount = 0;

				Func<int, bool> input = BitMapper.Map(frame.Columns, -0.6f, 2.1f, 50);

				sp.Feed(input);

				foreach (var b in sp)
					if (b)
						activeCount++;

				log.WriteLine(String.Join(" ", sp.Select(v => v ? '1' : '0')));

				var matches = cl.FindMatches(sp.ToArray())
					.OrderByDescending(c => c.Strength)
					.Take(2)
					.ToArray();

				/*for (var i = 0; i < 32 * 50; i++)
					Console.Write(input(i) ? "1" : "0");
				Console.WriteLine();

				Console.WriteLine(String.Join("", outputData.Select(v => v ? "1" : "0")));*/

				Console.WriteLine("Expected: {0}  Matched: {1} ({3:p})  Runner Up: {2} ({4:p})  Activity Count: {5}", frame.Category, matches[0].Identifier, matches[1].Identifier, matches[0].Strength, matches[1].Strength, activeCount);
			}
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
