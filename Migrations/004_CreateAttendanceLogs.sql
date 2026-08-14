CREATE TABLE IF NOT EXISTS AttendanceLogs
(
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    EmployeeId BIGINT NOT NULL,
    LoginTime DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    LogoutTime DATETIME NULL,

    CONSTRAINT FK_AttendanceLogs_Employees
        FOREIGN KEY (EmployeeId)
        REFERENCES Employees(Id)
);

CREATE INDEX IF NOT EXISTS IX_AttendanceLogs_EmployeeId ON AttendanceLogs(EmployeeId);
CREATE INDEX IF NOT EXISTS IX_AttendanceLogs_LoginTime ON AttendanceLogs(LoginTime);