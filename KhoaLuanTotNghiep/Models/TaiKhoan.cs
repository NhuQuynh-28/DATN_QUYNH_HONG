using System.ComponentModel.DataAnnotations;

namespace KhoaLuanTotNghiep.Models
{
    public class TaiKhoan
    {
        [Key]
        public string TenDangNhap { get; set; }

        public string MatKhau { get; set; }

        public string VaiTro { get; set; }
    }
}
