-- 016_SeedAllInitialData.sql
-- Seed comprehensive initial/mock data for Supermarket POS system.

-- =============================================
-- 1. EMPLOYEES
-- =============================================
INSERT IGNORE INTO Employees (Id, FullName, Username, PasswordHash, Role, IsActive, CreatedAt)
VALUES
(1, 'Admin User', 'admin', '$2a$11$9x3BfWpL.5Yy7vZq8XmN9eJ2v5y8z1a2b3c4d5e6f7g8h9i0j1k2l', 'Admin', 1, '2026-01-01 08:00:00'),
(2, 'Ahmad Cashier', 'cashier1', '$2a$11$9x3BfWpL.5Yy7vZq8XmN9eJ2v5y8z1a2b3c4d5e6f7g8h9i0j1k2l', 'Cashier', 1, '2026-01-01 08:30:00'),
(3, 'Sami Inventory', 'inventory1', '$2a$11$9x3BfWpL.5Yy7vZq8XmN9eJ2v5y8z1a2b3c4d5e6f7g8h9i0j1k2l', 'InventoryEmployee', 1, '2026-01-01 09:00:00');

-- =============================================
-- 2. EMPLOYEE PERMISSIONS
-- =============================================
-- Admin (EmployeeId = 1): Full System Permissions
INSERT IGNORE INTO EmployeePermissions (EmployeeId, PermissionKey) VALUES
(1, 'employees.view'),
(1, 'employees.create'),
(1, 'employees.update'),
(1, 'employees.deactivate'),
(1, 'employees.manage_permissions'),
(1, 'attendance.view'),
(1, 'attendance.view_employee'),
(1, 'sales.create'),
(1, 'sales.view'),
(1, 'categories.view'),
(1, 'categories.create'),
(1, 'products.view'),
(1, 'products.create'),
(1, 'products.update'),
(1, 'products.deactivate'),
(1, 'products.stock_add'),
(1, 'invoices.create'),
(1, 'invoices.view'),
(1, 'invoices.return'),
(1, 'invoices.exchange'),
(1, 'invoices.override_price'),
(1, 'invoices.debt_sale'),
(1, 'returns.exchange'),
(1, 'customers.view'),
(1, 'customers.create'),
(1, 'customers.record_payment'),
(1, 'reports.view'),
(1, 'dashboard.view');

-- Cashier (EmployeeId = 2): Basic POS Permissions
INSERT IGNORE INTO EmployeePermissions (EmployeeId, PermissionKey) VALUES
(2, 'invoices.create'),
(2, 'invoices.view'),
(2, 'invoices.return'),
(2, 'invoices.exchange'),
(2, 'returns.exchange'),
(2, 'sales.create'),
(2, 'sales.view'),
(2, 'categories.view'),
(2, 'products.view'),
(2, 'customers.view'),
(2, 'customers.create'),
(2, 'customers.record_payment'),
(2, 'attendance.view_employee'),
(2, 'dashboard.view');

-- Inventory Staff (EmployeeId = 3): Product & Category Management Permissions
INSERT IGNORE INTO EmployeePermissions (EmployeeId, PermissionKey) VALUES
(3, 'products.view'),
(3, 'products.create'),
(3, 'products.update'),
(3, 'products.deactivate'),
(3, 'products.stock_add'),
(3, 'categories.view'),
(3, 'categories.create'),
(3, 'attendance.view_employee'),
(3, 'dashboard.view');

-- =============================================
-- 3. ATTENDANCE LOGS
-- =============================================
INSERT IGNORE INTO AttendanceLogs (Id, EmployeeId, LoginTime, LogoutTime) VALUES
(1, 1, '2026-08-18 08:00:00', '2026-08-18 16:30:00'),
(2, 2, '2026-08-18 08:15:00', '2026-08-18 17:00:00'),
(3, 1, '2026-08-19 08:00:00', '2026-08-19 16:00:00'),
(4, 2, '2026-08-19 08:20:00', '2026-08-19 16:45:00'),
(5, 1, '2026-08-20 08:00:00', NULL);

-- =============================================
-- 4. CATEGORY
-- =============================================
INSERT IGNORE INTO Category (Id, Name) VALUES
(1, 'Beverages (المشروبات)'),
(2, 'Dairy & Cheese (الألبان والأجبان)'),
(3, 'Snacks & Sweets (المفرحات والحلويات)'),
(4, 'Cleaning Supplies (منظفات ومستلزمات)'),
(5, 'Canned Goods (المعلبات والغذائيات)');

-- =============================================
-- 5. PRODUCT
-- =============================================
INSERT IGNORE INTO Product (Id, Name, CategoryId, SellingPrice, CostPrice, Quantity, Unit, IsActive, CreatedAt) VALUES
(1, 'Coca Cola 1.5L', 1, 4.50, 3.20, 120, 'Piece', 1, '2026-01-01 10:00:00'),
(2, 'Pepsi Can 355ml', 1, 2.50, 1.75, 200, 'Piece', 1, '2026-01-01 10:05:00'),
(3, 'Fresh Milk 1L', 2, 6.00, 4.50, 45, 'Piece', 1, '2026-01-01 10:10:00'),
(4, 'Turkish White Cheese 500g', 2, 14.00, 10.50, 30, 'Piece', 1, '2026-01-01 10:15:00'),
(5, 'Lays Potato Chips Salt 150g', 3, 3.50, 2.20, 85, 'Piece', 1, '2026-01-01 10:20:00'),
(6, 'KitKat 4 Finger Chocolate Pack', 3, 12.00, 8.80, 50, 'Package', 1, '2026-01-01 10:25:00'),
(7, 'Fairy Dishwashing Liquid 800ml', 4, 11.50, 8.00, 40, 'Piece', 1, '2026-01-01 10:30:00'),
(8, 'Ariel Laundry Detergent Powder 2.5kg', 4, 38.00, 29.00, 25, 'Piece', 1, '2026-01-01 10:35:00'),
(9, 'Tomato Paste 400g Pack', 5, 8.50, 6.00, 60, 'Package', 1, '2026-01-01 10:40:00'),
(10, 'Tuna Chunks in Sunflower Oil 180g', 5, 5.00, 3.50, 90, 'Piece', 1, '2026-01-01 10:45:00'),
(11, 'Mineral Water 500ml Pack (12 bottles)', 1, 15.00, 10.00, 40, 'Package', 1, '2026-01-01 10:50:00'),
(12, 'Greek Yogurt 170g', 2, 4.00, 2.80, 35, 'Piece', 1, '2026-01-01 10:55:00');

-- =============================================
-- 6. STOCK HISTORY
-- =============================================
INSERT IGNORE INTO StockHistory (Id, ProductId, QuantityAdded, EmployeeId, Date) VALUES
(1, 1, 150, 1, '2026-01-01 11:00:00'),
(2, 2, 250, 1, '2026-01-01 11:05:00'),
(3, 3, 50, 3, '2026-01-01 11:10:00'),
(4, 4, 35, 3, '2026-01-01 11:15:00'),
(5, 5, 100, 3, '2026-01-01 11:20:00'),
(6, 6, 60, 1, '2026-01-01 11:25:00'),
(7, 7, 45, 3, '2026-01-01 11:30:00'),
(8, 8, 30, 1, '2026-01-01 11:35:00'),
(9, 9, 70, 3, '2026-01-01 11:40:00'),
(10, 10, 100, 3, '2026-01-01 11:45:00'),
(11, 11, 50, 1, '2026-01-01 11:50:00'),
(12, 12, 40, 3, '2026-01-01 11:55:00');

-- =============================================
-- 7. CUSTOMERS
-- =============================================
INSERT IGNORE INTO Customers (Id, FullName, Nickname, PhoneNumber, CurrentBalance, CreatedAt) VALUES
(1, 'Abu Khaled Grocery', 'أبو خالد', '0599123456', 45.00, '2026-01-10 12:00:00'),
(2, 'Mahmoud Al-Ali', 'أبو صبحي', '0598765432', 0.00, '2026-01-15 14:30:00'),
(3, 'Tariq Hassan', 'طارق الأستاذ', '0597112233', 120.00, '2026-02-01 09:15:00');

-- =============================================
-- 8. INVOICES & INVOICE ITEMS
-- =============================================
-- Invoice 1: Cash Sale by Cashier (EmployeeId = 2)
INSERT IGNORE INTO Invoices (Id, InvoiceNumber, EmployeeId, Date, TotalBeforeDiscount, DiscountPercentage, TotalAfterDiscount, HasReturn, CustomerId, PaymentMethod, PaymentStatus) VALUES
(1, '20260818-001', 2, '2026-08-18 10:15:00', 32.50, 0.00, 32.50, 1, NULL, 'Cash', 'Paid');

INSERT IGNORE INTO InvoiceItems (Id, InvoiceId, ProductId, ProductNameSnapshot, UnitPriceSnapshot, Quantity, LineTotal) VALUES
(1, 1, 1, 'Coca Cola 1.5L', 4.50, 2, 9.00),
(2, 1, 3, 'Fresh Milk 1L', 6.00, 2, 12.00),
(3, 1, 7, 'Fairy Dishwashing Liquid 800ml', 11.50, 1, 11.50);

-- Invoice 2: Cash Sale with 5% Discount by Admin (EmployeeId = 1)
INSERT IGNORE INTO Invoices (Id, InvoiceNumber, EmployeeId, Date, TotalBeforeDiscount, DiscountPercentage, TotalAfterDiscount, HasReturn, CustomerId, PaymentMethod, PaymentStatus) VALUES
(2, '20260819-001', 1, '2026-08-19 14:00:00', 50.00, 5.00, 47.50, 0, NULL, 'Cash', 'Paid');

INSERT IGNORE INTO InvoiceItems (Id, InvoiceId, ProductId, ProductNameSnapshot, UnitPriceSnapshot, Quantity, LineTotal) VALUES
(4, 2, 6, 'KitKat 4 Finger Chocolate Pack', 12.00, 2, 24.00),
(5, 2, 8, 'Ariel Laundry Detergent Powder 2.5kg', 38.00, 1, 38.00);

-- Invoice 3: Debt Sale for Abu Khaled (CustomerId = 1, EmployeeId = 2)
INSERT IGNORE INTO Invoices (Id, InvoiceNumber, EmployeeId, Date, TotalBeforeDiscount, DiscountPercentage, TotalAfterDiscount, HasReturn, CustomerId, PaymentMethod, PaymentStatus) VALUES
(3, '20260819-002', 2, '2026-08-19 16:30:00', 60.00, 0.00, 60.00, 0, 1, 'Debt', 'Unpaid');

INSERT IGNORE INTO InvoiceItems (Id, InvoiceId, ProductId, ProductNameSnapshot, UnitPriceSnapshot, Quantity, LineTotal) VALUES
(6, 3, 4, 'Turkish White Cheese 500g', 14.00, 2, 28.00),
(7, 3, 9, 'Tomato Paste 400g Pack', 8.50, 2, 17.00),
(8, 3, 11, 'Mineral Water 500ml Pack (12 bottles)', 15.00, 1, 15.00);

-- =============================================
-- 9. CUSTOMER PAYMENTS
-- =============================================
INSERT IGNORE INTO CustomerPayments (Id, CustomerId, Amount, EmployeeId, PaidAt, Notes) VALUES
(1, 1, 15.00, 2, '2026-08-20 09:30:00', 'Partial debt payment (دفعة جزئية عن حساب الجبنة والماء)');

-- =============================================
-- 10. RETURNS
-- =============================================
INSERT IGNORE INTO Returns (Id, OriginalInvoiceId, Type, ProductId, QuantityReturned, NewInvoiceId, EmployeeId, Date, Reason) VALUES
(1, 1, 'PureReturn', 1, 1, NULL, 2, '2026-08-18 11:30:00', 'Damaged bottle cap upon customer receipt');

-- =============================================
-- 11. HELD INVOICES
-- =============================================
INSERT IGNORE INTO HeldInvoices (Id, EmployeeId, ReferenceTag, CustomerName, DiscountPercentage, CartState, CreatedAt) VALUES
(1, 2, 'HOLD-001', 'Walk-in Customer (زبون دكانة)', '0', '[{"id":"1","name":"Coca Cola 1.5L","sellingPrice":4.5,"quantity":3,"unit":"piece"},{"id":"5","name":"Lays Potato Chips Salt 150g","sellingPrice":3.5,"quantity":2,"unit":"piece"}]', '2026-08-20 10:00:00');
