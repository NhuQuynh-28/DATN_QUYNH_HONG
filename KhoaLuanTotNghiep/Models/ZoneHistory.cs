namespace KhoaLuanTotNghiep.Models
{
    public class ZoneHistory
    {
        public int Id { get; set; }

        public int ZoneId { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        public int? OrdersReal { get; set; }      // thực tế
        public int? CustomersReal { get; set; }

        public int OrdersForecast { get; set; }  // dự kiến
        public int CustomersForecast { get; set; }

        public Zone Zone { get; set; }
    }
}
