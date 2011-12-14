using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTM
{
	public static class BitMapper
	{
		public static Func<int, bool> Map(float[] values, float min, float max, int bitsPerValue)
		{
			var range = max - min;
			return i =>
			{
				var o = i / bitsPerValue;
				var v = (values[o] - min) / range;
				//return v > 0.75;
				var x = (float)(i - (o * bitsPerValue)) / ((float)bitsPerValue - 1);
				return (v - 0.1 <= x && v + 0.1 >= x);
				//return (v - 0.04 <= x && v + 0.04 >= x);
			};
		}
	}
}
