CREATE TABLE IF NOT EXISTS CustomerPayments (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    CustomerId BIGINT NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    EmployeeId BIGINT NOT NULL,
    PaidAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Notes VARCHAR(255) NULL,

    CONSTRAINT FK_CustomerPayments_Customer
        FOREIGN KEY (CustomerId)
        REFERENCES Customers(Id),

    CONSTRAINT FK_CustomerPayments_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES Employees(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
