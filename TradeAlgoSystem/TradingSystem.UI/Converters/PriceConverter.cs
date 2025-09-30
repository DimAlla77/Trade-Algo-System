namespace TradingSystem.UI.Converters
{
    using System.Globalization;
    using System.Windows.Data;

    public class PriceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var provider = values[0] as Models.TradeModel;
                decimal price = (decimal)values[1];
                if (price != 0)
                {
                    return provider.FormatPrice(price);
                }
            }
            catch
            {
            }
            return "";
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
       
    }
}
