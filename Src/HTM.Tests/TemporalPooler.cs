
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTM.Tests
{
	[TestClass]
	public class TemporalPoolerTests
	{
		[TestMethod]
		public void CanInitializeNetwork()
		{
			new TemporalPooler(
				columnCount: 100,
				cellsPerColumn: 4
			);
		}

		[TestMethod]
		public void CanStepWithoutActiveInput()
		{
			Func<int, bool> input = (i) => false;
			var sp = new TemporalPooler(
				columnCount: 100,
				cellsPerColumn: 4
			);
			sp.Feed(input);
		}

		[TestMethod]
		public void CanStepWithAllActiveInput()
		{
			Func<int, bool> input = (i) => true;
			var sp = new TemporalPooler(
				columnCount: 100,
				cellsPerColumn: 4
			);
			sp.Feed(input);
		}

		[TestMethod]
		public void CanStepTwiceWithHalfActiveInput()
		{
			Func<int, bool> input = (i) => i % 2 == 0;
			var sp = new TemporalPooler(
				columnCount: 100,
				cellsPerColumn: 4
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
			var sp = new TemporalPooler(
				columnCount: 100,
				cellsPerColumn: 4,
				newSynapseCount: 4
			);
			for (var i = 0; i < 200; i++)
			{
				var start = DateTime.Now;
				sp.Feed(input);
				var end = DateTime.Now;
				foreach (var b in sp)
					if (b)
						isActive = true;
				Console.WriteLine(end - start);
			}
			Assert.IsTrue(isActive);
		}
	}
}
