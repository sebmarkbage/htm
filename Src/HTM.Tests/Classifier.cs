using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace HTM.Tests
{
	public abstract class Classifier
	{
		protected abstract IClassifier Create();

		[TestMethod]
		public void Test()
		{
			var setA = new bool[] { false, false, true, true };
			var setB = new bool[] { true, true , false,false};
			var setSimilairToA = new bool[] { false, true, true, true };
			var setSimilairToB = new bool[] { true, true, true, false };
			IClassifier classifier = Create();
			classifier.Train("a", setA);
			classifier.Train("b", setB);
			var shouldBeA = classifier.FindMatches(setSimilairToA).OrderByDescending(m => m.Strength).First();
			var shouldBeB = classifier.FindMatches(setSimilairToB).OrderByDescending(m => m.Strength).First();
			Console.WriteLine("Should be A strength {0}", shouldBeA.Strength);
			Console.WriteLine("Should be B strength {0}", shouldBeB.Strength);
			Assert.AreEqual(shouldBeA.Identifier, "a");
			Assert.AreEqual(shouldBeB.Identifier, "b");
		}

		[TestMethod]
		public void TestData()
		{
			var trainingData = ReadTestData("../../../../Data/train_sensor.txt", "../../../../Data/train_category.txt");

			var floatPrecision = 50;

			var cl = Create();

			foreach (var frame in trainingData)
			{
				var bits = AsBitArray(BitMapper.Map(frame.Columns, -0.6f, 2.21f, floatPrecision), frame.Columns.Length * floatPrecision);
				cl.Train(frame.Category.ToString(), bits);
			}


			var testData = ReadTestData("../../../../Data/test_sensor.txt", "../../../../Data/test_category.txt");

			var acurate = 0;
			var total = 0;

			var accuracy = new Dictionary<string, Accuracy>();

			foreach (var frame in testData)
			{
				var bits = AsBitArray(BitMapper.Map(frame.Columns, -0.6f, 2.21f, floatPrecision), frame.Columns.Length * floatPrecision);

				var matches = cl.FindMatches(bits)
					.OrderByDescending(c => c.Strength)
					.Take(2)
					.ToArray();

				var cat = frame.Category.ToString();
				if (matches[0].Identifier == cat) acurate++;
				total++;

				Accuracy v;
				if (!accuracy.TryGetValue(cat, out v))
					accuracy.Add(cat, v = new Accuracy());

				if (matches[0].Identifier == frame.Category.ToString()) v.acurate++;
				v.total++;

				Console.WriteLine("Expected: {0}  Matched: {1} ({3:p})  Runner Up: {2} ({4:p})", frame.Category, matches[0].Identifier, matches[1].Identifier, matches[0].Strength, matches[1].Strength);
			}
			Console.WriteLine("Accuracy: {0:p}", ((float)acurate) / ((float)total));

			foreach (var pair in accuracy)
				Console.WriteLine("Accuracy {1}: {0:p}", ((float)pair.Value.acurate) / ((float)pair.Value.total), pair.Key);
		}

		private bool[] AsBitArray(Func<int, bool> converter, int count)
		{
			var bits = new bool[count];
			for (var i = 0; i < count; i++)
				bits[i] = converter(i);
			return bits;
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
