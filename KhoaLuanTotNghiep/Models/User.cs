using System.ComponentModel.DataAnnotations.Schema;

namespace KhoaLuanTotNghiep.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }

        [NotMapped]
        public string ConfirmPassword { get; set; }

        public string? Email { get; set; }

        public string Role { get; set; } = "Driver";

        public string? ResetCode { get; set; }

        // 🔥 THÊM MẤY CÁI NÀY
        public bool IsApproved { get; set; } = false;   // duyệt tài khoản
        public bool IsActive { get; set; } = true;      // bật/tắt

        public int? AreaId { get; set; }                // gán khu vực
        public Area? Area { get; set; }
    }
}