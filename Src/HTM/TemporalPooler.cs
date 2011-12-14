using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace HTM
{
	public class TemporalPooler : IEnumerable<bool>
	{
		static readonly Random rnd = new Random();

		int cellsPerColumn; //  Number of cells in each column. 

		float initialPerm; // Initial permanence value for a synapse. 
		float connectedPerm; // If the permanence value for a synapse is greater than this value, it is said to be connected. 
		float permanenceInc; // Amount permanence values of synapses are incremented when activity-based learning occurs. 
		float permanenceDec; // Amount permanence values of synapses are decremented when activity-based learning occurs. 

		int activationThreshold; // Activation threshold for a segment. If the number of active connected synapses in a segment is greater than activationThreshold, the segment is said to be active. 
		int minThreshold; // Minimum segment activity for learning. 
		int learningRadius; // The area around a temporal pooler cell from which it can get lateral connections.  

		int newSynapseCount; // The maximum number of synapses added to a segment during learning. 

		List<Column> columns;

		SegmentUpdateList segmentUpdateList = new SegmentUpdateList(); // A list of segmentUpdate structures. segmentUpdateList(c,i) is the list of changes for cell i in column c. 

		Func<int, bool> input;  //(this is the output of the spatial pooler). 

		readonly int t = 0; // relative time

		public bool Learning { get; set; }

		public TemporalPooler(int columnCount, int cellsPerColumn, float connectedPerm = 0.2f, float initialPerm = 0.3f, float permanenceInc = 0.01f, float permanenceDec = 0.01f, int activationThreshold = 1, int minThreshold = 1, int learningRadius = 10, int newSynapseCount = 10)
		{
			this.cellsPerColumn = cellsPerColumn;

			this.initialPerm = initialPerm;
			this.connectedPerm = connectedPerm;
			this.permanenceInc = permanenceInc;
			this.permanenceDec = permanenceDec;

			this.activationThreshold = activationThreshold;
			this.minThreshold = minThreshold;
			this.learningRadius = learningRadius;

			this.newSynapseCount = newSynapseCount;

			columns = new List<Column>(columnCount);
			for (var i = 0; i < columnCount; i++)
				columns.Add(new Column(cellsPerColumn));
		}

		public bool this[int column, int index]
		{
			get
			{
				var cell = columns[column].cells[index];
				var state = cell.GetState(t);
				return state.active || state.predictive;
			}
		}

		public void Feed(Func<int, bool> input)
		{
			this.input = input;
			Tick();
			Phase1();
			Phase2();
			Phase3();
		}

		void Tick()
		{
			for (var i = 0; i < columns.Count; i++)
			{
				var column = columns[i];
				for (var ii = 0; ii < cellsPerColumn; ii++)
					column.cells[ii].Tick();
			}
		}

		void Phase1()
		{
			foreach (var c in activeColumns(t)) 
			{ 
			  var buPredicted = false;
			  var lcChosen = false;
			  foreach (var i in c.cells)
			  {
				if (predictiveState(c, i, t-1) == true)
				{
				  var s = getActiveSegment(c, i, t-1, activeState);
				  if (s.sequenceSegment == true)
				  {
					buPredicted = true;
					setActiveState(c, i, t, true);
					if (Learning && segmentActive(s, t-1, learnState))
					{
					  lcChosen = true;
					  setLearnState(c, i, t, true);
					}
				  }
				}
			  }
 
			  if (buPredicted == false)
				  foreach (var i in c.cells)
					setActiveState(c, i, t, true);

			  if (!Learning) continue;
 
			  if (lcChosen == false)
			  {
				var i = getBestMatchingCell(c, t-1);
				setLearnState(c, i, t, true);
				var sUpdate = getSegmentActiveSynapses(c, i, null, t-1, true);
				sUpdate.sequenceSegment = true;
				segmentUpdateList.add(sUpdate);
			  }
			}
		}

		void Phase2()
		{
			foreach (var c in columns)
			foreach (var i in c.cells)
			  foreach (var s in segments(c, i)) 
				if (segmentActive(s, t, activeState))
				{
					setPredictiveState(c, i, t, true);

					if (!Learning) continue;
 
					var activeUpdate = getSegmentActiveSynapses(c, i, s, t, false);
					segmentUpdateList.add(activeUpdate);
 
					var predSegment = getBestMatchingSegment(c, i, t-1);
					var predUpdate = getSegmentActiveSynapses(c, i, predSegment, t-1, true);
					segmentUpdateList.add(predUpdate);
				}
		}

		void Phase3()
		{
			if (!Learning) return;

			foreach (var c in columns)
			foreach (var i in c.cells)
			{
			  if (learnState(c, i, t) == true)
			  {
				adaptSegments(segmentUpdateList[c, i], true);
				segmentUpdateList.delete(c, i);
			  }
			  else if (predictiveState(c, i, t) == false && predictiveState(c, i, t-1) == true)
			  {
				adaptSegments(segmentUpdateList[c, i], false); 
				segmentUpdateList.delete(c, i);
 			  }
			}
		}

		IEnumerable<Segment> segments(Column c, Cell i)
		{
			return i.currentState.dendriteSegments;
		}

		IEnumerable<Column> activeColumns(int t)
		{
			// List of column indices that are winners due to bottom-up
			for (var i = 0; i < columns.Count; i++)
				if (input(i))
					yield return columns[i];
		}

		bool activeState(Column c, Cell i, int t)
		{
			// A boolean vector with one number per cell. It represents the active state of the column c cell i at time t given the current feed-forward input and the past temporal context
			// activeState(c, i, t) is the contribution from column c cell i at time t.  If 1, the cell has current feed-forward input as well as an appropriate temporal context.
			return i.GetState(t).active;
		}

		bool predictiveState(Column c, Cell i, int t)
		{
			// A boolean vector with one number per cell. It represents the prediction of the column c cell i at time t, given the bottom-up activity of other columns and the past temporal context. 
			// predictiveState(c, i, t) is the contribution of column c cell i at time t. If 1, the cell is predicting feed-forward input in the current temporal context. 
			return i.GetState(t).predictive;
		}

		bool learnState(Column c, Cell i, int t)
		{
			//  A boolean indicating whether cell i in column c is chosen as the cell to learn on.
			return i.GetState(t).learn;
		}

		void setActiveState(Column c, Cell i, int t, bool state)
		{
			if (t != 0) throw new ArgumentOutOfRangeException("t");
			i.currentState.active = state;
		}

		void setPredictiveState(Column c, Cell i, int t, bool state)
		{
			if (t != 0) throw new ArgumentOutOfRangeException("t");
			i.currentState.predictive = state;
		}

		void setLearnState(Column c, Cell i, int t, bool state)
		{
			if (t != 0) throw new ArgumentOutOfRangeException("t");
			i.currentState.learn = state;
		}

		bool segmentActive(Segment s, int t, Func<Column, Cell, int, bool> state)
		{
			/*  This routine returns true if the number of connected synapses on segment 
			s that are active due to the given state at time t is greater than 
			activationThreshold.  The parameter state can be activeState, or 
			learnState. */
			var activity = 0;
			foreach (var synapse in s.synapses)
				if (synapse.permanence > connectedPerm && state(null, synapse.cell, t) && ++activity > activationThreshold)
					return true;
			return false;
		}
 
		Segment getActiveSegment(Column c, Cell i, int t, Func<Column, Cell, int, bool> state)
		{
			/*  For the given column c cell i, return a segment index such that 
			segmentActive(s, t, state) is true. If multiple segments are active, sequence 
			segments are given preference. Otherwise, segments with most activity 
			are given preference. */
			var best = i.GetState(t).dendriteSegments
				.Select(s => new { segment = s, activity = s.synapses.Count(syn => syn.permanence > connectedPerm && state(null, syn.cell, t)) })
				.Where(s => s.activity > activationThreshold)
				.ToList()
				.OrderByDescending(s => s.segment.sequenceSegment)
				.ThenByDescending(s => s.activity)
				.FirstOrDefault();
			return best == null ? null : best.segment;
		}

		Segment getBestMatchingSegment(Column c, Cell i, int t)
		{
			/*  For the given column c cell i at time t, find the segment with the largest 
			number of active synapses. This routine is aggressive in finding the best 
			match. The permanence value of synapses is allowed to be below 
			connectedPerm. The number of active synapses is allowed to be below 
			activationThreshold, but must be above minThreshold. The routine 
			returns the segment index. If no segments are found, then an index of -1 is 
			returned. */
			var best = i.GetState(t).dendriteSegments
				.Select(s => new { segment = s, activity = s.synapses.Count(syn => activeState(null, syn.cell, t)) })
				.Where(s => s.activity > minThreshold)
				.ToList()
				.OrderByDescending(s => s.activity)
				.FirstOrDefault();
			return best == null ? null : best.segment;
		}
 
		Cell getBestMatchingCell(Column c, int t)
		{
			/* For the given column, return the cell with the best matching segment (as 
			defined above). If no cell has a matching segment, then return the cell with 
			the fewest number of segments. */
			var best =
				c.cells
				.SelectMany(i => i.GetState(t).dendriteSegments.Select(s => new { cell = i, segment = s, activity = s.synapses.Count(syn => activeState(null, syn.cell, t)) }))
				.Where(s => s.activity > minThreshold)
				.ToList()
				.OrderByDescending(s => s.activity)
				.FirstOrDefault();
			
			if (best == null)
			{
				return c.cells
					.OrderBy(i => i.GetState(t).dendriteSegments.Count)
					.FirstOrDefault();
			}
			return best.cell;
		}

		SegmentUpdate getSegmentActiveSynapses(Column c, Cell i, Segment s, int t, bool newSynapses = false) 
		{
			/*  Return a segmentUpdate data structure containing a list of proposed 
			changes to segment s. Let activeSynapses be the list of active synapses 
			where the originating cells have their activeState output = 1 at time step t.  
			(This list is empty if s = -1 since the segment doesn't exist.) newSynapses 
			is an optional argument that defaults to false. If newSynapses is true, then 
			newSynapseCount - count(activeSynapses) synapses are added to 
			activeSynapses. These synapses are randomly chosen from the set of cells 
			that have learnState output = 1 at time step t.*/

			var active = s == null ? new List<Synapse>() :
					s.synapses.Where(syn => syn.permanence > connectedPerm && syn.cell.GetState(t).active).ToList();

			var newItems = new List<Synapse>();

			if (newSynapses)
			{
				// TODO: Limit to learning radius
				var learnCells = this.columns.SelectMany(c2 => c2.cells)
					.Where(i2 => i2 != i && i2.GetState(t).learn)
					.ToList();
				if (learnCells.Count > 0)
					while (active.Count + newItems.Count < newSynapseCount)
						newItems.Add(new Synapse {
							permanence = initialPerm,
							cell = learnCells[rnd.Next(learnCells.Count)]
						});
			}

			return new SegmentUpdate
			{
				column = c,
				cell = i,
				segment = s,
				activeSynapses = active,
				newSynapses = newItems
			};
		}
 
		void adaptSegments(IEnumerable<SegmentUpdate> segmentList, bool positiveReinforcement)
		{
			/*  This function iterates through a list of segmentUpdate's and reinforces 
			each segment. For each segmentUpdate element, the following changes are 
			performed. If positiveReinforcement is true then synapses on the active 
			list get their permanence counts incremented by permanenceInc. All other 
			synapses get their permanence counts decremented by permanenceDec. If 
			positiveReinforcement is false, then synapses on the active list get their 
			permanence counts decremented by permanenceDec.   After this step, any 
			synapses in segmentUpdate that do yet exist get added with a permanence 
			count of initialPerm. */
			foreach (var update in segmentList)
			{
				var segment = update.segment;
				if (segment == null)
				{
					segment = new Segment();
					update.cell.currentState.dendriteSegments.Add(segment);
				}
				if (positiveReinforcement)
				{
					foreach (var synapse in segment.synapses)
						if (update.activeSynapses.Contains(synapse))
							synapse.permanence += permanenceInc;
						else
							synapse.permanence -= permanenceDec;
				}
				else
				{
					foreach (var synapse in update.activeSynapses)
						synapse.permanence -= permanenceDec;
				}
				foreach (var synapse in update.activeSynapses)
					if (!segment.synapses.Contains(synapse))
					{
						synapse.permanence = initialPerm;
						segment.synapses.Add(synapse);
					}
				segment.synapses.AddRange(update.newSynapses);
			}
		}

		class Column
		{
			public Column(int cellsPerColumn)
			{
				cells = new Cell[cellsPerColumn];
				for (var i = 0; i < cellsPerColumn; i++)
					cells[i] = new Cell();
			}

			public Cell[] cells;
		}

		class Cell
		{
			public Cell()
			{
				previousState.dendriteSegments = new List<Segment>();
				currentState.dendriteSegments = new List<Segment>();
			}

			public CellState previousState;
			public CellState currentState;

			public CellState GetState(int t)
			{
				if (t == 0) return currentState;
				if (t == -1) return previousState;
				throw new Exception("Unexpected value of t.");
			}

			public void Tick()
			{
				var list = previousState.dendriteSegments ?? new List<Segment>();
				previousState = currentState;
				currentState.active = false;
				currentState.learn = false;
				currentState.predictive = false;
				currentState.dendriteSegments = list;
				list.Clear();
				foreach (var segment in previousState.dendriteSegments)
					list.Add(segment.Clone());
			}
		}

		struct CellState
		{
			public bool predictive;
			public bool active;
			public bool learn;
			public List<Segment> dendriteSegments;
		}

		class Segment
		{
			public bool sequenceSegment;
			public List<Synapse> synapses = new List<Synapse>();
			
			public Segment Clone()
			{
				return new Segment
				{
					sequenceSegment = sequenceSegment,
					synapses = synapses.Select(s => s.Clone()).ToList()
				};
			}
		}

		class Synapse
		{
			public float permanence;
			public Cell cell;

			public Synapse Clone()
			{
				return new Synapse {
					permanence = permanence,
					cell = cell
				};
			}
		}

		class SegmentUpdateList : Collection<SegmentUpdate>
		{
			public IEnumerable<SegmentUpdate> this[Column c, Cell i]
			{
				get
				{
					foreach (var s in this)
						if (s.column == c && s.cell == i)
							yield return s;
				}
			}

			public void add(SegmentUpdate item)
			{
				this.Add(item);
			}

			public void delete(Column c, Cell i)
			{
				foreach (var item in this[c, i].ToList())
					this.Remove(item);
			}
		}

		class SegmentUpdate
		{
			public Column column;
			public Cell cell;

			// Data structure holding three pieces of information required to update a given segment:
			// a) segment index (-1 if it's a new  segment),
			public Segment segment;
			// b) a list of existing active synapses, and
			public List<Synapse> activeSynapses;
			// c) a flag  indicating whether this segment should be marked as a  sequence segment (defaults to false). 
			public bool sequenceSegment;

			public List<Synapse> newSynapses;

		}

		public IEnumerator<bool> GetEnumerator()
		{
			for (var i = 0; i < columns.Count; i++)
				for (var ii = 0; ii < cellsPerColumn; ii++)
					yield return this[i, ii];
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

	}
}
