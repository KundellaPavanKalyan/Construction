IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513052815_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [EmployeeId] int NOT NULL IDENTITY,
        [EmployeeName] nvarchar(200) NOT NULL,
        [Qualification] nvarchar(150) NULL,
        [Role] nvarchar(150) NULL,
        [DailyWage] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([EmployeeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513052815_InitialCreate'
)
BEGIN
    CREATE TABLE [Payrolls] (
        [PayrollId] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Month] tinyint NOT NULL,
        [Year] int NOT NULL,
        [DaysPresent] int NOT NULL,
        [OtHours] decimal(18,2) NOT NULL,
        [OtAmount] decimal(18,2) NOT NULL,
        [AdvanceAmount] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Payrolls] PRIMARY KEY ([PayrollId]),
        CONSTRAINT [FK_Payrolls_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([EmployeeId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513052815_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_EmployeeName] ON [Employees] ([EmployeeName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513052815_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payrolls_EmployeeId_Month_Year] ON [Payrolls] ([EmployeeId], [Month], [Year]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513052815_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513052815_InitialCreate', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514054155_AddAttendance'
)
BEGIN
    CREATE TABLE [Attendances] (
        [AttendanceId] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Month] tinyint NOT NULL,
        [Year] int NOT NULL,
        [PresentByDayJson] nvarchar(max) NOT NULL,
        [OtByDayJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Attendances] PRIMARY KEY ([AttendanceId]),
        CONSTRAINT [FK_Attendances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([EmployeeId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514054155_AddAttendance'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Attendances_EmployeeId_Month_Year] ON [Attendances] ([EmployeeId], [Month], [Year]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260514054155_AddAttendance'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260514054155_AddAttendance', N'9.0.0');
END;

COMMIT;
GO

