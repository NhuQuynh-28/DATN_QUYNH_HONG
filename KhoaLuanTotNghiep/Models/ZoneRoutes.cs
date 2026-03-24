namespace KhoaLuanTotNghiep.Models
{
    public class ZoneRoute
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
