namespace TradingSystem.UI.Models
{
    using System.Globalization;
    using System.Windows.Media;
    using System.Xml.Serialization;

    public class TradeModel : BaseModel
    {
        public TradeModel()
        {
            AskBybit = 0.00m;
            BidBybit = 0.00m;
            AskBinance = 0.00m;
            BidBinance = 0.00m;

            Tick = 0;
            Vol = 0.00m;
            Delta = 0.00m;
            Time = DateTime.MinValue.ToString();
        }

        private decimal _askBybit;
        public decimal AskBybit
        {
            get { return _askBybit; }
            set { _askBybit = value; OnPropertyChanged(); }
        }

        private decimal _bidBybit;
        public decimal BidBybit
        {
            get { return _bidBybit; }
            set { _bidBybit = value; OnPropertyChanged(); }
        }

        private decimal _askBinance;
        [XmlIgnore]
        public decimal AskBinance
        {
            get { return _askBinance; }
            set { if (_askBinance != value) { _askBinance = value; OnPropertyChanged(); } }
        }

        private decimal _bidBinance;
        [XmlIgnore]
        public decimal BidBinance
        {
            get { return _bidBinance; }
            set { if (_bidBinance != value) { _bidBinance = value; OnPropertyChanged(); } }
        }

        private int _tick;
        public int Tick
        {
            get { return _tick; }
            set { _tick = value; OnPropertyChanged(); }
        }

        private decimal _vol;
        public decimal Vol
        {
            get { return _vol; }
            set { _vol = value; OnPropertyChanged(); }
        }

        private decimal _delta;
        public decimal Delta
        {
            get { return _delta; }
            set { _delta = value; OnPropertyChanged(); }
        }

        private string _time;
        public string Time
        {
            get { return _time; }
            set { _time = value; OnPropertyChanged(); }
        }

        private string _LastLog;
        [XmlIgnore]
        public string LastLog
        {
            get { return _LastLog; }
            set { if (_LastLog != value) { _LastLog = value; OnPropertyChanged(); } }
        }
        private Brush _LastLogBrush;
        [XmlIgnore]
        public Brush LastLogBrush
        {
            get { return _LastLogBrush; }
            set { if (_LastLogBrush != value) { _LastLogBrush = value; OnPropertyChanged(); } }
        }

        [XmlIgnore]
        public Action<string> LogOrderSuccess;
        [XmlIgnore]
        public Action<string> LogInfo;
        [XmlIgnore]
        public Action<string> LogError;
        [XmlIgnore]
        public Action<string> LogWarning;
        [XmlIgnore]
        public Action LogClear;


        private bool _Log;
        public bool Log
        {
            get { return _Log; }
            set { if (_Log != value) { _Log = value; OnPropertyChanged(); } }
        }

        private string _PriceFormat;
        [XmlIgnore]
        public string PriceFormat
        {
            get { return _PriceFormat; }
            set { if (_PriceFormat != value) { _PriceFormat = value; OnPropertyChanged(); } }
        }

        public string FormatPrice(decimal price)
        {
            return PriceFormat.Length > 0 ? price.ToString(PriceFormat, CultureInfo.InvariantCulture) : price.ToString(CultureInfo.InvariantCulture);
        }
    }
}
