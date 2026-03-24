namespace KhoaLuanTotNghiep.Models
{
    public class Driver
    {
        public int Id { get; set; }

        public string DriverName { get; set; }

        public string Phone { get; set; }

        public string Status { get; set; }
        // DangLam | NghiLam | RoiBo

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

