using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTM
{
	class SlidingAverage
	{
		private bool[] queue;
		private int currentIndex = 0;
		private int trueItems = 0;

		public SlidingAverage(int numInterations)
		{
			queue = new bool[numInterations];
		}

		public SlidingAverage Push(bool value)
		{
			if (queue[currentIndex % queue.Length]) trueItems--;
			queue[currentIndex % queue.Length] = value;
			if (value) trueItems++;
			currentIndex++;
			return this;
		}

		public float Average()
		{
			if (currentIndex == 0) return 0;
			if (currentIndex < queue.Length) return trueItems / currentIndex;
			return trueItems / queue.Length;
		}
	}
}
