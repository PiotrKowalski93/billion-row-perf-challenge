namespace Shared
{
    public class StationStats
    {
        public double Min  { get; set; }
        public double Max { get; set; }
        public double Sum { get; set; }
        public int Count { get; set; }
        public double Avg => Count > 0 ? Sum / Count : 0.0;

        public void Update(double temperature)
        {
            if (temperature < Min) 
                Min = temperature;
            if (temperature > Max) 
                Max = temperature;

            Sum += temperature;
            Count++;
        }

        public override string ToString()
        {
            return $"{Min:F1}/{Avg:F1}/{Max:F1}";
        }
    }
}
