namespace TradingSystem.ConsoleApp.Connection
{
    using System.Data.Entity;

    public class DataContext : DbContext
    {
        public DataContext(string connectionString) : base(connectionString)
        {
        }
       
         public virtual DbSet<Models.HistoricalBar> HistoricalBars { get; set; }  
    }
}
