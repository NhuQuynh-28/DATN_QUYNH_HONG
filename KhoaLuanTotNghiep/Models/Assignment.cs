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

        public int PlannedOrders { get; set; }  // dự đoán
        public int ActualOrders { get; set; }

    }
}
