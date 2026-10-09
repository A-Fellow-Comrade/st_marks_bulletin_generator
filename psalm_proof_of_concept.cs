//lectionary import
using LiturgyTools;

namespace psalmtest {
    public class psalmtest {
        public void psalmtestmethod() {
            LiturgicalData myData = LiturgicalData.Load();
            var calendar = new LiturgicalCalendar(2026, myData);
            var date = new DateOnly(2026, 10, 11);
            var result = calendar.Lookup(date);

            if (result != null && result.Readings != null)
            {
                Console.WriteLine($"Readings for {result.Name}:");
                Console.WriteLine($"Old Testament: {result.Readings.Ot}");
                Console.WriteLine($"Psalm:         {result.Readings.Ps}");
                Console.WriteLine($"Epistle:       {result.Readings.Ep}");
                Console.WriteLine($"Gospel:        {result.Readings.Go}");
            }
            else
            {
                Console.WriteLine("No readings found for this date.");
            }
        }
    }
}
