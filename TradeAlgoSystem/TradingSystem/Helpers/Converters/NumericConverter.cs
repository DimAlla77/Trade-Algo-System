namespace TradingSystem.Helpers.Converters
{
    public class NumericConverter
    {
        public static double GetDouble(string input)
        {
            double result;
            bool isConvert = double.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result);
            return isConvert ? result : 0.0;
        }
    }
}
