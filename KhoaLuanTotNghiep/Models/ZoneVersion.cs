namespace KhoaLuanTotNghiep.Models
{
    public class ZoneVersion
    {
        public int Id { get; set; }

        public int? AreaId { get; set; }

        public string VersionName { get; set; }

        public DateTime? CreatedDate { get; set; }
        
        public DateTime? UpdatedDate { get; set; }   // ngày sửa
        public int? SoPolygon { get; set; }           // số polygon
        public double? DienTichBaoPhu { get; set; }

        public bool? IsActive { get; set; }

        public Area Area { get; set; }

        public List<Zone> Zones { get; set; }
    }
}
