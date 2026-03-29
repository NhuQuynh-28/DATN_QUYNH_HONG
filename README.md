# Khóa Luận Tốt Nghiệp - PHÁT TRIỂN HỆ THỐNG PHÂN CHIA TỐI ƯU VÙNG HOẠT ĐỘNG CỦA BƯU TÁ GIAO NHẬN HÀNG THƯƠNG MẠI ĐIỆN TỬ

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


#  Quy trình 3‑giai đoạn **Greedy → Local Search → ALNS**

## 1️⃣ Mục tiêu chung
- Tìm lịch phân công (driver ↔ zone) tối ưu cho các ràng buộc (số đơn, số khách, giới hạn khu vực).
- Cân bằng **độ chất lượng** và **thời gian tính toán**.

| Giai đoạn | Vai trò | Đặc điểm |
|-----------|---------|----------|
| **Greedy** | Khởi tạo nhanh một nghiệm khả thi | Chọn quyết định "tốt nhất" tại mỗi bước dựa trên tiêu chí đơn giản (ví dụ: lợi nhuận / khoảng cách). |
| **Local Search** | Cải thiện nghiệm Greedy bằng các phép đổi chỗ / di chuyển cục bộ | Khám phá không gian lân cận, giảm "độ lệch" so với optimum. |
| **ALNS** | Tối ưu sâu hơn bằng cấu trúc lớn và điều chỉnh thích nghi | Thay đổi một phần lớn của nghiệm để thoát khỏi các "cục bộ tối ưu". |

---

## 2️⃣ Bước 1 – Greedy Initialization
1. **Input**: danh sách `drivers` và `zones`.
2. **Tiến trình**:
   - Lặp qua tài xế (hoặc zone) theo thứ tự đã sắp xếp.
   - Với mỗi tài xế, chọn zone có lợi nhuận cao nhất mà vẫn thoả mãn ràng buộc.
   - Gán tài xế vào zone, cập nhật trạng thái.
3. **Kết quả**: `AssignmentSolution GreedyInit` chứa `Objective` và `Phase = "Greedy"`.
4. **Mục đích**: nhanh (<1 s), cung cấp cơ sở cho các bước tiếp theo.

> **Mã liên quan**: `HeuristicAssignmentService.GreedyInitialize`, UI hiển thị `aaGreedyObj`.

---

## 3️⃣ Bước 2 – Local Search (Tìm kiếm cục bộ)
- **Phương pháp**: Swap, Relocate, 2‑opt/3‑opt, `RepairGreedy`, `RepairRegret2`.
- **Quy trình**:
  1. Clone nghiệm Greedy (`var localSol = CloneSolution(greedy)`).
  2. Lặp qua các candidate và áp dụng các phép sửa.
  3. Đánh giá mỗi biến đổi bằng `EvaluateSolution`; chấp nhận nếu cải thiện.
  4. Dừng khi không còn cải thiện hoặc đạt giới hạn thời gian.
- **Mục đích**: nâng cao chất lượng (tăng 5‑15 % objective) trong thời gian vài giây.

> **Mã liên quan**: `RepairGreedy`, `RepairRegret2`, UI hiển thị "Greedy → Local Search → ALNS đang chạy...".

---

## 4️⃣ Bước 3 – ALNS (Adaptive Large‑Neighbourhood Search)
1. **Adaptive selection** – duy trì trọng số cho các destroy/repair operators.
2. **Destroy phase** – bỏ một phần lớn các gán (20‑40 % tài xế) bằng các chiến lược: random, worst‑profit, regret‑based, …
3. **Repair phase** – tái gán các tài xế đã bỏ bằng các heuristic (cũng có thể là Greedy, Regret, …).
4. **Acceptance criterion** – chấp nhận nghiệm mới nếu objective tốt hơn hoặc theo Simulated Annealing.
5. **Weight update** – cập nhật trọng số dựa trên thành công.
6. **Lặp** nhiều vòng (50‑200) tới khi thời gian giới hạn hoặc không còn cải thiện.
- **Mục đích**: khám phá không gian rộng, đạt cải thiện 20‑30 % so với Greedy, ổn định nhất.

> **Mã liên quan**: `AutoAssign` (gọi Greedy, Local Search, ALNS), UI thông báo cải thiện %.

---

## 5️⃣ Tại sao ba giai đoạn cần thiết?
| Yếu tố | Greedy | Local Search | ALNS |
|--------|--------|--------------|------|
| **Tốc độ** | Rất nhanh | Nhanh‑trung bình | Chậm‑trung bình → chậm |
| **Chất lượng** | Thấp | Trung bình | Cao |
| **Độ ổn định** | Độ lệch lớn | Ổn định hơn | Ổn định nhất |
| **Chi phí tính toán** | Thấp | Vừa | Cao (điều chỉnh thời gian) |
| **Ứng dụng** | Demo, kiểm thử nhanh | Tối ưu nhanh cho batch vừa | Tối ưu cuối cùng, chất lượng cao |

Kết hợp ba thuật cho phép **khởi tạo nhanh → cải thiện nhanh → tối ưu sâu**.

## Liên hệ

- **Giảng viên hướng dẫn**: Nguyễn Văn A (email@example.com)
- **Sinh viên thực hiện**: Quỳnh Hồng (email@student.university.edu)


