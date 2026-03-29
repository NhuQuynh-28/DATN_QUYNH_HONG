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

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? Cccd { get; set; }

        // URL to avatar image (stored in wwwroot/uploads)
        public string? AvatarUrl { get; set; }

        public string Role { get; set; } = "Driver";

        public string? ResetCode { get; set; }

        public bool IsApproved { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public int? AreaId { get; set; }
        public Area? Area { get; set; }

        // Ngày tạo tài khoản (ngày đăng ký)
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Ngày được approve (ngày cập nhật trạng thái)
        public DateTime? UpdatedAt { get; set; }
    }
}