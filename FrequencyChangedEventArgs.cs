using System;

namespace HamLibSharp
{

	public class FrequencyChangedEventArgs : EventArgs
	{
		public FrequencyChangedEventArgs(int vfo, double frequency)
		{
			Vfo = vfo;
			Frequency = frequency;
		}
		public int Vfo { get; set; }
		public double Frequency { get; set; }
	}
}
