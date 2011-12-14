using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTM.Tests
{
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public class SpatialPoolerTests
	{
		[TestMethod]
		public void CanInitializeNetwork()
		{
			new SpatialPooler(
				inputCount: 100,
				columnCount: 100,
				desiredLocalActivity: 10,
				minOverlap: 10,
				maxSynapsesPerColumn: 20
			);
		}

		[TestMethod]
		public void CanStepWithoutActiveInput()
		{
			Func<int, bool> input = (i) => false;
			var sp = new SpatialPooler(
				inputCount: 100,
				columnCount: 100,
				desiredLocalActivity: 10,
				minOverlap: 10,
				maxSynapsesPerColumn: 20
			);
			sp.Feed(input);
		}

		[TestMethod]
		public void CanStepWithAllActiveInput()
		{
			Func<int, bool> input = (i) => true;
			var sp = new SpatialPooler(
				inputCount: 100,
				columnCount: 100,
				desiredLocalActivity: 10,
				minOverlap: 10,
				maxSynapsesPerColumn: 20
			);
			sp.Feed(input);
		}

		[TestMethod]
		public void CanStepTwiceWithHalfActiveInput()
		{
			Func<int, bool> input = (i) => i % 2 == 0;
			Action<int, bool> output = (i, b) => { Console.WriteLine(b); };
			var sp = new SpatialPooler(
				inputCount: 100,
				columnCount: 100,
				desiredLocalActivity: 10,
				minOverlap: 2,
				maxSynapsesPerColumn: 20
			);
			sp.Feed(input);
			foreach (var b in sp) Console.WriteLine(b);
			sp.Feed(input);
			foreach (var b in sp) Console.WriteLine(b);
		}

		[TestMethod]
		public void StepWithHalfActiveInputShouldGenerateAtleastOneActiveOutputColumn()
		{
			Func<int, bool> input = (i) => i % 2 == 0;
			bool isActive = false;
			var sp = new SpatialPooler(
				inputCount: 100,
				columnCount: 100,
				desiredLocalActivity: 10,
				minOverlap: 2,
				maxSynapsesPerColumn: 20
			);
			for (var i = 0; i < 1000; i++)
			{
				sp.Feed(input);
				foreach (var b in sp) if (b) isActive = true;
			}
			Assert.IsTrue(isActive);
		}

		[TestMethod]
		public void CountActiveColumnsPerStep()
		{
			int activeCount = 0;
			Func<int, bool> input = (i) => i % 2 == 0;
			var sp = new SpatialPooler(
				inputCount: 100,
				columnCount: 100,
				desiredLocalActivity: 10,
				minOverlap: 2,
				maxSynapsesPerColumn: 20
			);
			for (var i = 0; i < 1000; i++)
			{
				activeCount = 0;
				sp.Feed(input);
				foreach (var b in sp)
					if (b)
						activeCount++;
				Console.WriteLine("Step #{0}: {1}", i, activeCount);
			}
		}

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

			var values = new bool[100];
 			for (var i = 0; i < 100; i++)
				values[i] = i < 5;

			for (var t = 0; t < 10000; t++)
			{
				var activeCount = 0;

				Func<int, bool> input = (i) => values[i];

				sp.Feed(input);

				foreach (var b in sp)
					if (b)
						activeCount++;

				Console.Write("Input data: " + String.Join("", values.Select(v => v ? "1" : "0")));
				Console.WriteLine("\tStep #{0}: {2} (Count: {1})", t, activeCount, String.Join("", sp.Select(v => v ? "1" : "0")));

				var source = values.ToArray();
				Array.Copy(source, 1, values, 0, 99);
				values[values.Length - 1] = source[0];
			}
		}


	}
}
