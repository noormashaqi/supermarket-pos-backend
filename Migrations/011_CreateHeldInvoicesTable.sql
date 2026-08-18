CREATE TABLE IF NOT EXISTS HeldInvoices (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    EmployeeId BIGINT NOT NULL,
    ReferenceTag VARCHAR(100) NOT NULL,
    CustomerName VARCHAR(150) NULL,
    DiscountPercentage VARCHAR(10) NULL,
    CartState TEXT NOT NULL, -- Holds JSON serialized cart array
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_HeldInvoices_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES Employees(Id)
        ON DELETE CASCADE,

    INDEX IX_HeldInvoices_EmployeeId (EmployeeId)
) ENGINE = InnoDB
  DEFAULT CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
