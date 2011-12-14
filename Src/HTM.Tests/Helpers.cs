using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTM.Tests
{
	public static class Helpers
	{
		public static void Shift(this bool[] values)
		{
			var source = (bool[])values.Clone();
			Array.Copy(source, 1, values, 0, values.Length - 1);
			values[values.Length - 1] = source[0];
		}
	}
}
