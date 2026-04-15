using System;

namespace KhoaLuanTotNghiep.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        public int DriverId { get; set; }
        public Driver Driver { get; set; }

        public int DispatcherId { get; set; }
        public User Dispatcher { get; set; }

        public int AreaId { get; set; }
        public Area Area { get; set; }

        public DateTime Date { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
 
        public int PlannedOrders { get; set; }  // dự đoán
        public int PlannedCustomers { get; set; }
        public int ActualOrders { get; set; }
        public int ActualCustomers { get; set; }
        
        public string? ZoneIds { get; set; }   // Danh sách ID vùng (JSON: [1,2,3...])
        public double TotalDistance { get; set; } // Tổng kc tới tâm
    }
}
