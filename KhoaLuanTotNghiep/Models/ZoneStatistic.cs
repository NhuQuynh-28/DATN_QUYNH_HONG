namespace KhoaLuanTotNghiep.Models
{
    public class ZoneStatistic
    {
        public int Id { get; set; }

        public int ZoneId { get; set; }

        public int Orders { get; set; }

        public int Customers { get; set; }

        public DateTime Date { get; set; }

        public int CreatedBy { get; set; }
    }
}
