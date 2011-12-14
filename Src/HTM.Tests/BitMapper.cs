using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTM.Tests
{
	[TestClass]
	public class BitMapperTests
	{
		[TestMethod]
		public void OutputShouldNotBeZero()
		{
			var values = new [] {
				0.5f, 0.1f, 0.0f, 1.0f, 0.7f, 0.66f, 0.99f
			};
			var map = BitMapper.Map(values, 0f, 1f, 50);
			
			var atleastOneIsTrue = false;
			for (int i = 0, l = values.Length * 50; i < l; i++)
			{
				var b = map(i);
				Console.Write(b ? "1" : "0");
				if (i % 50 == 49) Console.WriteLine();
				if (b) atleastOneIsTrue = true;
			}

			Assert.IsTrue(atleastOneIsTrue, "At least one output bit should be true.");
		}
	}
}
