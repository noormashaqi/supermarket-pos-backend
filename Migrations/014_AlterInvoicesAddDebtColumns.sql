-- Add debt-related columns to the Invoices table.
-- Uses a procedure to safely add columns only if they don't already exist.

DELIMITER $$

DROP PROCEDURE IF EXISTS AddDebtColumnsToInvoices$$

CREATE PROCEDURE AddDebtColumnsToInvoices()
BEGIN
    -- Add CustomerId column
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Invoices'
          AND COLUMN_NAME = 'CustomerId'
    ) THEN
        ALTER TABLE Invoices ADD COLUMN CustomerId BIGINT NULL AFTER HasReturn;
        ALTER TABLE Invoices ADD CONSTRAINT FK_Invoices_Customer
            FOREIGN KEY (CustomerId) REFERENCES Customers(Id);
    END IF;

    -- Add PaymentMethod column
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Invoices'
          AND COLUMN_NAME = 'PaymentMethod'
    ) THEN
        ALTER TABLE Invoices ADD COLUMN PaymentMethod VARCHAR(10) NOT NULL DEFAULT 'Cash' AFTER CustomerId;
    END IF;

    -- Add PaymentStatus column
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Invoices'
          AND COLUMN_NAME = 'PaymentStatus'
    ) THEN
        ALTER TABLE Invoices ADD COLUMN PaymentStatus VARCHAR(10) NOT NULL DEFAULT 'Paid' AFTER PaymentMethod;
    END IF;
END$$

DELIMITER ;

CALL AddDebtColumnsToInvoices();
DROP PROCEDURE IF EXISTS AddDebtColumnsToInvoices;
