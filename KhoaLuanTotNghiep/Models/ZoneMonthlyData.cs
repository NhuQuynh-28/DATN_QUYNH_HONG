namespace KhoaLuanTotNghiep.Models
{
    public class ZoneMonthlyData
    {
        public int Id { get; set; }

        public int ZoneId { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        public int? OrdersForecast { get; set; }
        public int? CustomersForecast { get; set; }

        public int? OrdersReal { get; set; }
        public int? CustomersReal { get; set; }
    }
}