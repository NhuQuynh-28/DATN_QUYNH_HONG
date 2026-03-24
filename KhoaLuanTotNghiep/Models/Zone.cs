namespace KhoaLuanTotNghiep.Models
{
    public class Zone
    {
        public int Id { get; set; }

        public string? ZoneName { get; set; }

        public string? AreaName { get; set; }

        public string? Points { get; set; }
        public double CenterLat { get; set; }
        public double CenterLng { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Dispatcher nhap
        public int Order { get; set; }
        public int Customers {  get; set; }
        public int AvgPerDay { get; set; }

        public int AvgPerWeek { get; set; } 
        // cum buu ta
        public int ClusterId {  get; set; }
        public int? VersionId { get; set; }

        public ZoneVersion Version { get; set; }
        public double Area {  get; set; }
    }
}