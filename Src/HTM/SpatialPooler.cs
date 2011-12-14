using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTM
{
	public class SpatialPooler : IEnumerable<bool>
	{
		static readonly Random rnd = new Random();
		int desiredLocalActivity;  // A parameter controlling the number of columns that will be winners after the inhibition step. 
		float connectedPerm;  // If the permanence value for a synapse is greater than this value, it is said to be connected. 
		float permanenceInc;  // Amount permanence values of synapses are incremented during learning. 
		float permanenceDec;  // Amount permanence values of synapses are decremented during learning. 
		int minOverlap;  // A minimum number of inputs that must be active for a column to be considered during the inhibition step. 
		int maxSynapsesPerColumn; // Maximum synapses per column

		int inputCount;
		int columnCount;

		List<Column> columns;   // List of all columns. 

		int inhibitionRadius = 0;  // Average connected receptive field size of the columns. 

		Func<int, bool> input;  //(t,j) The input to this level at time t. input(t, j) is 1 if the j'th  input is on. 
		List<Column> activeColumns;  // At (t) List of column indices that are winners due to bottom-up input. 

		public bool Learning { get; set; }

		public SpatialPooler(int inputCount, int columnCount, int desiredLocalActivity, int minOverlap, int maxSynapsesPerColumn, float connectedPerm = 0.2f, float permanenceInc = 0.01f, float permanenceDec = 0.01f, int slidingWindowLength = 1000)
		{
			Learning = true;

			this.desiredLocalActivity = desiredLocalActivity;
			this.connectedPerm = connectedPerm;
			this.permanenceInc = permanenceInc;
			this.permanenceDec = permanenceDec;
			this.minOverlap = minOverlap;
			this.maxSynapsesPerColumn = maxSynapsesPerColumn;
			
			this.inputCount = inputCount;
			this.columnCount = columnCount;

			this.columns = new List<Column>(columnCount);
			for (var i = 0; i < columnCount; i++)
				this.columns.Add(new Column(slidingWindowLength));

			Initialize();
		}

		public bool this[int index]
		{
			get
			{
				return activeColumns.Contains(columns[index]);
			}
		}

		public void Feed(Func<int, bool> input)
		{
			this.input = input;
			Phase1();
			Phase2();
			Phase3();
			// for (int i = 0, l = columns.Count; i < l; i++) output(i, activeColumns.Contains(columns[i]));
		}

		void Initialize()
		{
			var ci = 0;
			foreach (var c in columns)
			{
				for (var i = 0; i < maxSynapsesPerColumn; i++)
				{
					var newSynapse = new Synapse();
					newSynapse.sourceInput = rnd.Next(inputCount);
					newSynapse.permanence = initialPermanenceValue(ci, newSynapse.sourceInput);
					c.potentialSynapses.Add(newSynapse);
				}
				ci++;
			}
		}

		void Phase1()
		{
			foreach (var c in columns)
			{
				c.overlap = 0;
				foreach (var s in connectedSynapses(c))
					c.overlap = c.overlap + (input(s.sourceInput) ? 1 : 0);
 
				if (c.overlap < minOverlap)
					c.overlap = 0;
				else 
					c.overlap = (int)((float)c.overlap * c.boost);
			}
		}

		void Phase2() // Inhibition
		{
			activeColumns = new List<Column>();
			foreach (var c in columns)
			{
				var minLocalActivity = kthScore(neighbors(c), desiredLocalActivity);
				if (c.overlap > 0 && c.overlap >= minLocalActivity)
					activeColumns.Add(c);
			}
		}

		void Phase3() // Learning
		{
			if (!Learning) return;

			foreach (var c in activeColumns)
			{
			  foreach (var s in c.potentialSynapses)
			  {
				if (active(s))
				{
				  s.permanence += permanenceInc;
				  s.permanence = min(1.0f, s.permanence);
				}
				else 
				{
				  s.permanence -= permanenceDec;
				  s.permanence = max(0.0f, s.permanence);
				}
			  }
			}
 
			foreach (var c in columns)
			{
			  var minDutyCycle = 0.01f * maxDutyCycle(neighbors(c)); 
			  var activeDutyCycle = updateActiveDutyCycle(c);
			  c.boost = boostFunction(activeDutyCycle, minDutyCycle);
 
			  var overlapDutyCycle = updateOverlapDutyCycle(c); 
			  if (overlapDutyCycle < minDutyCycle) 
				increasePermanences(c, 0.1f * connectedPerm); 
			}

			inhibitionRadius = averageReceptiveFieldSize(); 
		}

		IEnumerable<Column> neighbors(Column c)
		{
			// A list of all the columns that are within inhibitionRadius of column c.
			int index = columns.IndexOf(c);
			for (var i = index - 1; i >= index - inhibitionRadius; i--)
				if (i >= 0)
					yield return columns[i];
			for (var i = index + 1; i <= index + inhibitionRadius; i++)
				if (i < columns.Count)
					yield return columns[i];
		}

		IEnumerable<Synapse> connectedSynapses(Column c)
		{
			// A subset of potentialSynapses(c) where the permanence value is greater than connectedPerm. These are the bottom-up inputs that are currently connected to column c.
			foreach (var s in c.potentialSynapses)
				if (s.permanence >= connectedPerm)
					yield return s;
		}

		float initialPermanenceValue(int column, int input)
		{
			//return (float) rnd.NextDouble(); // Total randomness

			float c = (float)column / (float)columnCount;
			float i = (float)input / (float)inputCount;

			float d = Math.Abs(c - i);

			return (1.0f - d) * (float)rnd.NextDouble();

			/*
			float s = 20.0f;
			float sqrt2PI = (float)Math.Sqrt(2.0 * Math.PI);

			float randomize = 0.01f;
			float sqrtSx2 = (float)Math.Pow(s, 2);

			float r = ((float)rnd.NextDouble()) * 2 - 1;
			float t = input;
			float mu = column * (inputCount - 1) / (columnCount - 1);
			return (connectedPerm) + (1.0f / (s * sqrt2PI) * (float)Math.Exp(-Math.Pow(input - mu, 2) / sqrtSx2) + r * randomize); 
			*/
		}

		bool active(Synapse s)
		{
			return input(s.sourceInput);
		}
		
		int kthScore(IEnumerable<Column> cols, int k)
		{
			// Given the list of columns, return the k'th highest overlap value.
			return cols.Select(c => c.overlap)
				.OrderByDescending(i => i)
				.Skip(k)
				.FirstOrDefault();
		}
 
		float updateActiveDutyCycle(Column c)
		{
			// Computes a moving average of how often column c has been active aft inhibition.
			return c.activeDutyCycle.Push(activeColumns.Contains(c)).Average();
		}
 
		float updateOverlapDutyCycle(Column c)
		{
			// Computes a moving average of how often column c has overlap greate than minOverlap.
			return c.overlapDutyCycle.Push(activeColumns.Contains(c)).Average();
		}
 
		int averageReceptiveFieldSize()
		{
			// The radius of the average connected receptive field size of all the colum
			// The connected receptive field size of a column includes only the conne
			// synapses (those with permanence values >= connectedPerm).  This is 
			// to determine the extent of lateral inhibition between columns.
			int totalReceptiveFieldSize = 0;
			foreach (var c in columns)
			{
				int min = int.MaxValue, max = int.MinValue;
				foreach (var s in c.potentialSynapses)
					if (s.permanence >= connectedPerm)
					{
						if (s.sourceInput < min) min = s.sourceInput;
						if (s.sourceInput > max) max = s.sourceInput;
					}
				if (min < int.MaxValue) totalReceptiveFieldSize += max - min;
			}
			return (totalReceptiveFieldSize / columns.Count) / 2;
		}
 
		float maxDutyCycle(IEnumerable<Column> cols)
		{
			// Returns the maximum active duty cycle of the columns in the given lis columns.
			float max = 0;
			foreach (var c in cols)
			{
				var v = c.activeDutyCycle.Average();
				if (v > max) max = v;
			}
			return max;
		}
 
		void increasePermanences(Column c, float s)
		{
			// Increase the permanence value of every synapse in column c by a scale factor s.
			foreach (var sy in c.potentialSynapses)
				sy.permanence = min(1.0f, sy.permanence * (1.0f + s));
		}

		float boostFunction(float activeDutyCycle, float minDutyCycle)
		{
			// Returns the boost value of a column. The boost value is a scalar >= 1. If
			// activeDutyCyle(c) is above minDutyCycle(c), the boost value is 1. The 
			// boost increases linearly once the column's activeDutyCyle starts fallin
			// below its minDutyCycle.
			if (activeDutyCycle > minDutyCycle) return 1.0f;
			return 1.0f - (activeDutyCycle - minDutyCycle);
		}

		float min(float a, float b)
		{
			return Math.Min(a, b);
		}

		float max(float a, float b)
		{
			return Math.Max(a, b);
		}

		class Column
		{
			public Column(int slidingWindowSize)
			{
				activeDutyCycle = new SlidingAverage(slidingWindowSize);
				overlapDutyCycle = new SlidingAverage(slidingWindowSize);
				potentialSynapses = new List<Synapse>();
			}

			public SlidingAverage activeDutyCycle; // A sliding average representing how often column c has been active after inhibition (e.g. over the last 1000 iterations).
			public SlidingAverage overlapDutyCycle;
			public List<Synapse> potentialSynapses; // The list of potential synapses and their permanence values.

			public int overlap;  // The spatial pooler overlap of column c with a particular  input pattern.
			public float boost;  // The boost value for column c as computed during learning - used to increase the overlap value for inactive columns. 
		}

		class Synapse
		{
			// A data structure representing a synapse - contains a permanence value and the source input index. 
			public float permanence;
			public int sourceInput;
		}

		public IEnumerator<bool> GetEnumerator()
		{
			for (var i = 0; i < columnCount; i++) yield return this[i];
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
