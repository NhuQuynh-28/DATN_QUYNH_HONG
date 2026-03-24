namespace KhoaLuanTotNghiep.Models
{
    public class ZoneDistance
    {

        public int Id { get; set; }

        public int ZoneId1 { get; set; }

        public int ZoneId2 { get; set; }

        public double Distance { get; set; }

        public bool IsAdjacent { get; set; }
    
}
}
