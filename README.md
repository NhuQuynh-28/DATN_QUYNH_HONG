# Khoa Luận Tốt Nghiệp - Dự Án Quản Lý Tài Khoản và Lái Xe

## Tổng quan

Dự án này là một ứng dụng web **ASP.NET MVC** được xây dựng để quản lý tài khoản người dùng, tài xế và các chức năng liên quan trong hệ thống quản lý giao thông. Ứng dụng cung cấp giao diện hiện đại, tối ưu cho **dark mode**, hỗ trợ các vai trò:
- **Admin**: Quản lý người dùng, khu vực, tài xế.
- **Dispatcher**: Điều phối tài xế và đơn hàng.
- **Driver**: Xem và cập nhật hồ sơ cá nhân.

## Công nghệ sử dụng

- **Framework**: ASP.NET MVC (.NET Framework 4.x hoặc .NET Core 3.1+ tùy cấu hình)
- **Ngôn ngữ**: C# cho backend, Razor (cshtml) cho view
- **Cơ sở dữ liệu**: SQL Server (Entity Framework)
- **Front‑end**: HTML5, CSS3 (sử dụng màu sắc hài hòa, dark theme), JavaScript (jQuery cho một số tương tác)
- **Thư viện UI**: Bootstrap 5 (đã tùy biến để phù hợp với theme)

## Cấu trúc dự án

```
KhoaLuanTotNghiep/
├─ Controllers/                # Các controller MVC
│   ├─ AdminController.cs
│   ├─ DispatcherController.cs
│   └─ AccountController.cs
├─ Models/                     # Các model dữ liệu / ViewModel
├─ Views/                      # Các view Razor (*.cshtml)
│   ├─ _ViewStart.cshtml
│   ├─ Account/
│   │   ├─ Login.cshtml
│   │   └─ Profile.cshtml
│   ├─ DriverDashboard/
│   │   └─ Index.cshtml
│   └─ Shared/                # Layout, partials
├─ wwwroot/                    # Tài nguyên tĩnh (css, js, images)
│   ├─ css/
│   ├─ js/
│   └─ images/
├─ App_Start/                  # Cấu hình Route, Filter, Bundle
├─ Global.asax.cs
├─ Web.config                  # Cấu hình ứng dụng
└─ README.md                   # Tài liệu dự án (đây)
```

## Hướng dẫn cài đặt

1. **Clone repository**
   ```bash
   git clone <url-repo>
   cd KhoaLuanTotNghiep
   ```
2. **Cài đặt các gói NuGet** (nếu chưa tự động restore)
   ```bash
   dotnet restore   # hoặc mở solution trong Visual Studio và Build -> Restore NuGet Packages
   ```
3. **Cấu hình chuỗi kết nối**
   - Mở `Web.config` và chỉnh sửa phần `<connectionStrings>` để trỏ tới SQL Server của bạn.
4. **Tạo và cập nhật database**
   - Sử dụng Entity Framework Migrations hoặc chạy script SQL có trong thư mục `Database/` (nếu có).
5. **Chạy ứng dụng**
   - **Visual Studio**: Nhấn `F5` hoặc `Ctrl+F5`.
   - **Command line** (nếu dự án .NET Core):
     ```bash
     dotnet run
     ```
6. **Truy cập**
   - Mở trình duyệt và vào `http://localhost:5000` (hoặc cổng được cấu hình).

## Các chức năng chính

- **Đăng nhập / Đăng ký** cho người dùng và tài xế.
- **Quản lý hồ sơ** (Profile) với khả năng tải lên avatar.
- **Dashboard** cho mỗi vai trò, hiển thị thông tin thống kê và nhanh chóng truy cập các chức năng.
- **Quản lý người dùng, khu vực, tài xế** (Admin) với giao diện modal, xác thực phía server.
- **Điều phối đơn hàng** (Dispatcher) – giao diện hiện đại, hỗ trợ lọc và tìm kiếm.

## Kiểm thử & Debug

- **Unit Tests**: Các test nằm trong thư mục `Tests/` (nếu có). Chạy bằng `dotnet test`.
- **Debug**: Đặt breakpoint trong các controller hoặc service, chạy dưới môi trường Development để xem log chi tiết.

## Đóng góp

1. Fork repository.
2. Tạo branch mới cho tính năng hoặc sửa lỗi:
   ```bash
   git checkout -b feature/ten-tinh-nang
   ```
3. Commit và push lên fork của bạn.
4. Tạo Pull Request mô tả chi tiết thay đổi.

## Liên hệ

- **Giảng viên hướng dẫn**: Nguyễn Văn A (email@example.com)
- **Sinh viên thực hiện**: Quỳnh Hồng (email@student.university.edu)


