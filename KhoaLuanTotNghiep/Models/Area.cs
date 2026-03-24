namespace KhoaLuanTotNghiep.Models
{
    public class Area
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public List<ZoneVersion> Versions { get; set; }
    }
}
