-- Thêm cột ActualCustomers vào bảng Assignments
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Assignments' 
               AND COLUMN_NAME = 'ActualCustomers')
BEGIN
    ALTER TABLE Assignments ADD ActualCustomers INT NOT NULL DEFAULT 0;
    PRINT 'Đã thêm cột ActualCustomers vào bảng Assignments';
END
ELSE
BEGIN
    PRINT 'Cột ActualCustomers đã tồn tại';
END
