-- ==========================================
-- Crochet Craft Store Management System
-- Complete Database Restore Script (Missing Tables and Stored Procedures)
-- Enforces C# WPF Front-End Requirements
-- Target Database: db_crochet_craft3
-- ==========================================

USE db_crochet_craft3;
GO

-- 1. DROP EXISTING TABLE-VALUED TYPES & TABLES IF THEY INTERFERE (Clean Slate Setup)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[delivery]') AND type in (N'U'))
    DROP TABLE [dbo].[delivery];
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[order_details]') AND type in (N'U'))
    DROP TABLE [dbo].[order_details];
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[orders]') AND type in (N'U'))
    DROP TABLE [dbo].[orders];
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[employee]') AND type in (N'U'))
    DROP TABLE [dbo].[employee];
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[customer]') AND type in (N'U'))
    DROP TABLE [dbo].[customer];
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND type in (N'U'))
    DROP TABLE [dbo].[products];
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[admin_tbl]') AND type in (N'U'))
    DROP TABLE [dbo].[admin_tbl];

-- Drop OrderProducts table type if it already exists
IF EXISTS (SELECT * FROM sys.types st JOIN sys.schemas ss ON st.schema_id = ss.schema_id WHERE st.name = N'orderproducts' AND ss.name = N'dbo')
    DROP TYPE [dbo].[orderproducts];
GO

-- ==========================================
-- 2. CREATE MISSING TABLES
-- ==========================================

-- Admin Credentials Table
CREATE TABLE [dbo].[admin_tbl] (
    [username] NVARCHAR(50) PRIMARY KEY,
    [password] NVARCHAR(50) NOT NULL
);
GO

-- Insert Default Admin Credentials
INSERT INTO [dbo].[admin_tbl] ([username], [password]) 
VALUES (N'admin', N'admin123');
GO

-- Customer Table
CREATE TABLE [dbo].[customer] (
    [c_id] INT IDENTITY(1,1) PRIMARY KEY,
    [c_name] NVARCHAR(100) NOT NULL,
    [c_contact] VARCHAR(15) NOT NULL,
    [c_email] VARCHAR(100) NOT NULL,
    [c_address] NVARCHAR(255) NOT NULL
);
GO

-- Employee Table
CREATE TABLE [dbo].[employee] (
    [emp_id] INT IDENTITY(1,1) PRIMARY KEY,
    [emp_name] NVARCHAR(100) NOT NULL,
    [emp_contact] VARCHAR(15) NOT NULL,
    [emp_email] VARCHAR(100) NOT NULL,
    [emp_role] NVARCHAR(50) NOT NULL,
    [emp_hiredate] DATE NOT NULL
);
GO

-- Products Table
CREATE TABLE [dbo].[products] (
    [p_id] INT IDENTITY(1,1) PRIMARY KEY,
    [p_name] NVARCHAR(100) NOT NULL,
    [p_description] NVARCHAR(255) NULL,
    [p_category] NVARCHAR(50) NOT NULL,
    [p_price] DECIMAL(10,2) NOT NULL CHECK ([p_price] >= 0),
    [p_stockqty] INT NOT NULL CHECK ([p_stockqty] >= 0)
);
GO

-- Orders Table
CREATE TABLE [dbo].[orders] (
    [o_id] INT IDENTITY(1,1) PRIMARY KEY,
    [c_id] INT NOT NULL,
    [o_date] DATE NOT NULL,
    [o_status] VARCHAR(20) NOT NULL DEFAULT 'pending',
    CONSTRAINT FK_Orders_Customer FOREIGN KEY ([c_id]) REFERENCES [dbo].[customer]([c_id]) ON DELETE CASCADE
);
GO

-- Order Details (Junction Table for Orders and Products)
CREATE TABLE [dbo].[order_details] (
    [o_id] INT NOT NULL,
    [p_id] INT NOT NULL,
    [qty] INT NOT NULL CHECK ([qty] > 0),
    CONSTRAINT PK_OrderDetails PRIMARY KEY CLUSTERED ([o_id], [p_id]),
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY ([o_id]) REFERENCES [dbo].[orders]([o_id]) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY ([p_id]) REFERENCES [dbo].[products]([p_id]) ON DELETE CASCADE
);
GO

-- Delivery Table (Employee Assignments)
CREATE TABLE [dbo].[delivery] (
    [del_id] INT IDENTITY(1,1) PRIMARY KEY,
    [o_id] INT NOT NULL,
    [emp_id] INT NOT NULL,
    [del_date] DATE NOT NULL,
    [del_status] VARCHAR(20) NOT NULL DEFAULT 'pending',
    CONSTRAINT FK_Delivery_Orders FOREIGN KEY ([o_id]) REFERENCES [dbo].[orders]([o_id]) ON DELETE CASCADE,
    CONSTRAINT FK_Delivery_Employee FOREIGN KEY ([emp_id]) REFERENCES [dbo].[employee]([emp_id]) ON DELETE CASCADE
);
GO

-- ==========================================
-- 3. CREATE TABLE-VALUED TYPES FOR ORDERS
-- ==========================================
CREATE TYPE [dbo].[orderproducts] AS TABLE (
    [p_id] INT,
    [qty] INT
);
GO

-- ==========================================
-- 4. CREATE STORED PROCEDURES
-- ==========================================

-- Admin Login and Auth
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[admin_login_check]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[admin_login_check];
GO
CREATE PROCEDURE [dbo].[admin_login_check]
    @username NVARCHAR(50),
    @password NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[admin_tbl] WHERE [username] = @username AND [password] = @password)
    BEGIN
        RAISERROR('Authentication Failed: Invalid username or password.', 16, 1);
        RETURN;
    END
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[admin_change_password]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[admin_change_password];
GO
CREATE PROCEDURE [dbo].[admin_change_password]
    @username NVARCHAR(50),
    @old_password NVARCHAR(50),
    @new_password NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[admin_tbl] WHERE [username] = @username AND [password] = @old_password)
    BEGIN
        RAISERROR('Password Change Failed: Current password does not match.', 16, 1);
        RETURN;
    END
    UPDATE [dbo].[admin_tbl]
    SET [password] = @new_password
    WHERE [username] = @username;
END;
GO

-- Customer Stored Procedures
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[add_customer]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[add_customer];
GO
CREATE PROCEDURE [dbo].[add_customer]
    @c_name NVARCHAR(100),
    @c_contact VARCHAR(15),
    @c_email VARCHAR(100),
    @c_address NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validations matching frontend expectation
    IF LEN(@c_contact) <> 11 OR @c_contact LIKE '%[^0-9]%'
    BEGIN
        RAISERROR('invalid contact: Contact number must be exactly 11 digits.', 16, 1);
        RETURN;
    END
    
    IF @c_email NOT LIKE '%@gmail.com'
    BEGIN
        RAISERROR('invalid email: Email must end with @gmail.com', 16, 1);
        RETURN;
    END

    INSERT INTO [dbo].[customer] ([c_name], [c_contact], [c_email], [c_address])
    VALUES (@c_name, @c_contact, @c_email, @c_address);
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[update_customer]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[update_customer];
GO
CREATE PROCEDURE [dbo].[update_customer]
    @c_id INT,
    @c_contact VARCHAR(15) = NULL,
    @c_email VARCHAR(100) = NULL,
    @c_address NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM [dbo].[customer] WHERE [c_id] = @c_id)
    BEGIN
        RAISERROR('customer not found: Customer with this ID does not exist.', 16, 1);
        RETURN;
    END

    IF @c_contact IS NOT NULL AND (LEN(@c_contact) <> 11 OR @c_contact LIKE '%[^0-9]%')
    BEGIN
        RAISERROR('invalid contact: Contact number must be exactly 11 digits.', 16, 1);
        RETURN;
    END
    
    IF @c_email IS NOT NULL AND @c_email NOT LIKE '%@gmail.com'
    BEGIN
        RAISERROR('invalid email: Email must end with @gmail.com', 16, 1);
        RETURN;
    END

    UPDATE [dbo].[customer]
    SET [c_contact] = ISNULL(@c_contact, [c_contact]),
        [c_email] = ISNULL(@c_email, [c_email]),
        [c_address] = ISNULL(@c_address, [c_address])
    WHERE [c_id] = @c_id;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[delete_customer]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[delete_customer];
GO
CREATE PROCEDURE [dbo].[delete_customer]
    @c_id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[customer] WHERE [c_id] = @c_id)
    BEGIN
        RAISERROR('customer not found: Customer not found.', 16, 1);
        RETURN;
    END
    DELETE FROM [dbo].[customer] WHERE [c_id] = @c_id;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_all_customers]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_all_customers];
GO
CREATE PROCEDURE [dbo].[view_all_customers]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [c_id], [c_name], [c_contact], [c_email], [c_address] 
    FROM [dbo].[customer]
    ORDER BY [c_id] DESC;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_top_customers]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_top_customers];
GO
CREATE PROCEDURE [dbo].[view_top_customers]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        c.[c_id] AS [Customer ID], 
        c.[c_name] AS [Customer Name], 
        COUNT(DISTINCT o.[o_id]) AS [Total Orders], 
        ISNULL(SUM(od.[qty] * p.[p_price]), 0) AS [Total Spent]
    FROM [dbo].[customer] c
    LEFT JOIN [dbo].[orders] o ON c.[c_id] = o.[c_id]
    LEFT JOIN [dbo].[order_details] od ON o.[o_id] = od.[o_id]
    LEFT JOIN [dbo].[products] p ON od.[p_id] = p.[p_id]
    GROUP BY c.[c_id], c.[c_name]
    HAVING COUNT(DISTINCT o.[o_id]) > 0
    ORDER BY [Total Spent] DESC;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_customer_summary]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_customer_summary];
GO
CREATE PROCEDURE [dbo].[view_customer_summary]
    @c_id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[customer] WHERE [c_id] = @c_id)
    BEGIN
        RAISERROR('Customer not found: The customer with ID %d does not exist.', 16, 1, @c_id);
        RETURN;
    END

    SELECT 
        c.[c_name], 
        COUNT(DISTINCT o.[o_id]) AS [total_orders], 
        ISNULL(SUM(od.[qty] * p.[p_price]), 0) AS [total_spent]
    FROM [dbo].[customer] c
    LEFT JOIN [dbo].[orders] o ON c.[c_id] = o.[c_id]
    LEFT JOIN [dbo].[order_details] od ON o.[o_id] = od.[o_id]
    LEFT JOIN [dbo].[products] p ON od.[p_id] = p.[p_id]
    WHERE c.[c_id] = @c_id
    GROUP BY c.[c_id], c.[c_name];
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_customer_order_history]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_customer_order_history];
GO
CREATE PROCEDURE [dbo].[view_customer_order_history]
    @c_id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[customer] WHERE [c_id] = @c_id)
    BEGIN
        RAISERROR('Customer not found: The customer with ID %d does not exist.', 16, 1, @c_id);
        RETURN;
    END

    SELECT 
        o.[o_id] AS [Order ID], 
        o.[o_date] AS [Order Date], 
        ISNULL(SUM(od.[qty] * p.[p_price]), 0) AS [Total Amount], 
        o.[o_status] AS [Order Status]
    FROM [dbo].[orders] o
    LEFT JOIN [dbo].[order_details] od ON o.[o_id] = od.[o_id]
    LEFT JOIN [dbo].[products] p ON od.[p_id] = p.[p_id]
    WHERE o.[c_id] = @c_id
    GROUP BY o.[o_id], o.[o_date], o.[o_status]
    ORDER BY o.[o_date] DESC;
END;
GO


-- Employee Stored Procedures
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[add_employee]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[add_employee];
GO
CREATE PROCEDURE [dbo].[add_employee]
    @emp_name NVARCHAR(100),
    @emp_contact VARCHAR(15),
    @emp_email VARCHAR(100),
    @emp_role NVARCHAR(50),
    @emp_hiredate DATE
AS
BEGIN
    SET NOCOUNT ON;
    IF LEN(@emp_contact) <> 11 OR @emp_contact LIKE '%[^0-9]%'
    BEGIN
        RAISERROR('Contact number must be exactly 11 digits.', 16, 1);
        RETURN;
    END
    INSERT INTO [dbo].[employee] ([emp_name], [emp_contact], [emp_email], [emp_role], [emp_hiredate])
    VALUES (@emp_name, @emp_contact, @emp_email, @emp_role, @emp_hiredate);
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[update_employee]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[update_employee];
GO
CREATE PROCEDURE [dbo].[update_employee]
    @emp_id INT,
    @emp_contact VARCHAR(15) = NULL,
    @emp_role NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[employee] WHERE [emp_id] = @emp_id)
    BEGIN
        RAISERROR('Employee not found.', 16, 1);
        RETURN;
    END
    IF @emp_contact IS NOT NULL AND (LEN(@emp_contact) <> 11 OR @emp_contact LIKE '%[^0-9]%')
    BEGIN
        RAISERROR('Contact number must be exactly 11 digits.', 16, 1);
        RETURN;
    END
    
    UPDATE [dbo].[employee]
    SET [emp_contact] = ISNULL(@emp_contact, [emp_contact]),
        [emp_role] = ISNULL(@emp_role, [emp_role])
    WHERE [emp_id] = @emp_id;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[delete_employee]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[delete_employee];
GO
CREATE PROCEDURE [dbo].[delete_employee]
    @emp_id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[employee] WHERE [emp_id] = @emp_id)
    BEGIN
        RAISERROR('Employee not found.', 16, 1);
        RETURN;
    END
    DELETE FROM [dbo].[employee] WHERE [emp_id] = @emp_id;
END;
GO

-- Product Stored Procedures
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[add_product]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[add_product];
GO
CREATE PROCEDURE [dbo].[add_product]
    @p_name NVARCHAR(100),
    @p_description NVARCHAR(255),
    @p_category NVARCHAR(50),
    @p_price DECIMAL(10,2),
    @p_stockqty INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[products] ([p_name], [p_description], [p_category], [p_price], [p_stockqty])
    VALUES (@p_name, @p_description, @p_category, @p_price, @p_stockqty);
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[update_product]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[update_product];
GO
CREATE PROCEDURE [dbo].[update_product]
    @p_id INT,
    @p_price DECIMAL(10,2),
    @p_stockqty INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[products] WHERE [p_id] = @p_id)
    BEGIN
        RAISERROR('Product not found.', 16, 1);
        RETURN;
    END
    UPDATE [dbo].[products]
    SET [p_price] = @p_price,
        [p_stockqty] = @p_stockqty
    WHERE [p_id] = @p_id;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[remove_product]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[remove_product];
GO
CREATE PROCEDURE [dbo].[remove_product]
    @p_id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[products] WHERE [p_id] = @p_id)
    BEGIN
        RAISERROR('Product not found.', 16, 1);
        RETURN;
    END
    DELETE FROM [dbo].[products] WHERE [p_id] = @p_id;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_all_products]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_all_products];
GO
CREATE PROCEDURE [dbo].[view_all_products]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [p_id], [p_name], [p_description], [p_category], [p_price], [p_stockqty]
    FROM [dbo].[products]
    ORDER BY [p_name] ASC;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_low_stock_products]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_low_stock_products];
GO
CREATE PROCEDURE [dbo].[view_low_stock_products]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [p_id], [p_name], [p_description], [p_category], [p_price], [p_stockqty]
    FROM [dbo].[products]
    WHERE [p_stockqty] < 10
    ORDER BY [p_stockqty] ASC;
END;
GO


-- Order Stored Procedures
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[add_order]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[add_order];
GO
CREATE PROCEDURE [dbo].[add_order]
    @c_id INT,
    @o_date DATE,
    @products [dbo].[orderproducts] READONLY
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Validate Customer
        IF NOT EXISTS (SELECT 1 FROM [dbo].[customer] WHERE [c_id] = @c_id)
        BEGIN
            RAISERROR('Customer ID not found. Order aborted.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Validate Stock
        IF EXISTS (
            SELECT 1 
            FROM @products p_req
            JOIN [dbo].[products] p ON p_req.[p_id] = p.[p_id]
            WHERE p.[p_stockqty] < p_req.[qty]
        )
        BEGIN
            RAISERROR('Insufficient stock for one or more products. Order aborted.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Create Order Header
        DECLARE @o_id INT;
        INSERT INTO [dbo].[orders] ([c_id], [o_date], [o_status])
        VALUES (@c_id, @o_date, 'pending');
        SET @o_id = SCOPE_IDENTITY();

        -- Insert Order Lines
        INSERT INTO [dbo].[order_details] ([o_id], [p_id], [qty])
        SELECT @o_id, [p_id], [qty] FROM @products;

        -- Deduct Stock Quantity
        UPDATE p
        SET p.[p_stockqty] = p.[p_stockqty] - p_req.[qty]
        FROM [dbo].[products] p
        JOIN @products p_req ON p.[p_id] = p_req.[p_id];

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[remove_order]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[remove_order];
GO
CREATE PROCEDURE [dbo].[remove_order]
    @odr_id INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM [dbo].[orders] WHERE [o_id] = @odr_id)
        BEGIN
            RAISERROR('Order ID not found.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Restore stock quantity when order is deleted
        UPDATE p
        SET p.[p_stockqty] = p.[p_stockqty] + od.[qty]
        FROM [dbo].[products] p
        JOIN [dbo].[order_details] od ON p.[p_id] = od.[p_id]
        WHERE od.[o_id] = @odr_id;

        -- Delete delivery record first (if assigned)
        DELETE FROM [dbo].[delivery] WHERE [o_id] = @odr_id;
        -- Delete details and header
        DELETE FROM [dbo].[order_details] WHERE [o_id] = @odr_id;
        DELETE FROM [dbo].[orders] WHERE [o_id] = @odr_id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_all_orders]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_all_orders];
GO
CREATE PROCEDURE [dbo].[view_all_orders]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        o.[o_id] AS [Order ID], 
        o.[o_date] AS [Order Date], 
        c.[c_name] AS [Customer Name], 
        ISNULL(SUM(od.[qty] * p.[p_price]), 0) AS [Total Amount], 
        o.[o_status] AS [Order Status]
    FROM [dbo].[orders] o
    JOIN [dbo].[customer] c ON o.[c_id] = c.[c_id]
    LEFT JOIN [dbo].[order_details] od ON o.[o_id] = od.[o_id]
    LEFT JOIN [dbo].[products] p ON od.[p_id] = p.[p_id]
    GROUP BY o.[o_id], o.[o_date], c.[c_name], o.[o_status]
    ORDER BY o.[o_id] DESC;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_pending_orders]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_pending_orders];
GO
CREATE PROCEDURE [dbo].[view_pending_orders]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        o.[o_id] AS [Order ID], 
        o.[o_date] AS [Order Date], 
        c.[c_name] AS [Customer Name], 
        ISNULL(SUM(od.[qty] * p.[p_price]), 0) AS [Total Amount], 
        o.[o_status] AS [Order Status]
    FROM [dbo].[orders] o
    JOIN [dbo].[customer] c ON o.[c_id] = c.[c_id]
    LEFT JOIN [dbo].[order_details] od ON o.[o_id] = od.[o_id]
    LEFT JOIN [dbo].[products] p ON od.[p_id] = p.[p_id]
    WHERE o.[o_status] = 'pending'
    GROUP BY o.[o_id], o.[o_date], c.[c_name], o.[o_status]
    ORDER BY o.[o_id] DESC;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_orders_by_status]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_orders_by_status];
GO
CREATE PROCEDURE [dbo].[view_orders_by_status]
    @status NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        o.[o_id] AS [Order ID], 
        o.[o_date] AS [Order Date], 
        c.[c_name] AS [Customer Name], 
        ISNULL(SUM(od.[qty] * p.[p_price]), 0) AS [Total Amount], 
        o.[o_status] AS [Order Status]
    FROM [dbo].[orders] o
    JOIN [dbo].[customer] c ON o.[c_id] = c.[c_id]
    LEFT JOIN [dbo].[order_details] od ON o.[o_id] = od.[o_id]
    LEFT JOIN [dbo].[products] p ON od.[p_id] = p.[p_id]
    WHERE o.[o_status] = @status
    GROUP BY o.[o_id], o.[o_date], c.[c_name], o.[o_status]
    ORDER BY o.[o_id] DESC;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_order_details]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_order_details];
GO
CREATE PROCEDURE [dbo].[view_order_details]
    @odr_id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        od.[o_id] AS [Order ID], 
        p.[p_name] AS [Product Name], 
        p.[p_category] AS [Category], 
        od.[qty] AS [Quantity], 
        p.[p_price] AS [Price], 
        (od.[qty] * p.[p_price]) AS [Subtotal]
    FROM [dbo].[order_details] od
    JOIN [dbo].[products] p ON od.[p_id] = p.[p_id]
    WHERE od.[o_id] = @odr_id;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_monthly_revenue]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_monthly_revenue];
GO
CREATE PROCEDURE [dbo].[view_monthly_revenue]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        YEAR(o.[o_date]) AS [Year], 
        DATENAME(MONTH, o.[o_date]) AS [Month], 
        SUM(od.[qty] * p.[p_price]) AS [TotalRevenue]
    FROM [dbo].[orders] o
    JOIN [dbo].[order_details] od ON o.[o_id] = od.[o_id]
    JOIN [dbo].[products] p ON od.[p_id] = p.[p_id]
    GROUP BY YEAR(o.[o_date]), MONTH(o.[o_date]), DATENAME(MONTH, o.[o_date])
    ORDER BY YEAR(o.[o_date]) DESC, MONTH(o.[o_date]) DESC;
END;
GO


-- Delivery Stored Procedures
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[add_delivery]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[add_delivery];
GO
CREATE PROCEDURE [dbo].[add_delivery]
    @o_id INT,
    @emp_id INT,
    @del_date DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM [dbo].[orders] WHERE [o_id] = @o_id)
    BEGIN
        RAISERROR('Order ID not found in database.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[employee] WHERE [emp_id] = @emp_id)
    BEGIN
        RAISERROR('Employee ID not found in database.', 16, 1);
        RETURN;
    END

    INSERT INTO [dbo].[delivery] ([o_id], [emp_id], [del_date], [del_status])
    VALUES (@o_id, @emp_id, @del_date, 'pending');

    -- Auto-shipped order
    UPDATE [dbo].[orders] SET [o_status] = 'shipped' WHERE [o_id] = @o_id;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[update_delivery]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[update_delivery];
GO
CREATE PROCEDURE [dbo].[update_delivery]
    @del_id INT,
    @del_status VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM [dbo].[delivery] WHERE [del_id] = @del_id)
    BEGIN
        RAISERROR('Delivery ID not found.', 16, 1);
        RETURN;
    END

    DECLARE @curr_status VARCHAR(20);
    DECLARE @o_id INT;
    SELECT @curr_status = [del_status], @o_id = [o_id] FROM [dbo].[delivery] WHERE [del_id] = @del_id;

    IF @curr_status <> 'pending'
    BEGIN
        RAISERROR('Only pending deliveries can be updated.', 16, 1);
        RETURN;
    END

    UPDATE [dbo].[delivery]
    SET [del_status] = @del_status
    WHERE [del_id] = @del_id;

    -- If completed, mark order completed
    IF @del_status = 'completed'
    BEGIN
        UPDATE [dbo].[orders] SET [o_status] = 'completed' WHERE [o_id] = @o_id;
    END
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_all_deliveries]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_all_deliveries];
GO
CREATE PROCEDURE [dbo].[view_all_deliveries]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        d.[del_id], 
        d.[o_id], 
        e.[emp_name], 
        d.[del_date], 
        d.[del_status]
    FROM [dbo].[delivery] d
    JOIN [dbo].[employee] e ON d.[emp_id] = e.[emp_id]
    ORDER BY d.[del_date] ASC;
END;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[view_deliveries_by_employee]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[view_deliveries_by_employee];
GO
CREATE PROCEDURE [dbo].[view_deliveries_by_employee]
    @emp_id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[employee] WHERE [emp_id] = @emp_id)
    BEGIN
        RAISERROR('Employee not found in database.', 16, 1);
        RETURN;
    END

    SELECT 
        d.[del_id], 
        d.[o_id], 
        d.[del_date], 
        d.[del_status]
    FROM [dbo].[delivery] d
    WHERE d.[emp_id] = @emp_id
    ORDER BY d.[del_date] ASC;
END;
GO


-- ==========================================
-- 5. POPULATE TEST DUMMY DATA FOR VERIFICATION
-- ==========================================

-- Populate Products
INSERT INTO [dbo].[products] ([p_name], [p_description], [p_category], [p_price], [p_stockqty])
VALUES 
(N'Crochet Red Rose Flower bouquet', N'Beautifully handmade wool rose flower bouquet', N'Flowers', 1500.00, 30),
(N'Woolen Baby Cap', N'Soft cap for babies 0-6 months', N'Caps', 600.00, 50),
(N'Crochet Shoulder Bag', N'Elegant knitted bag with zipper and lining', N'Bags', 2500.00, 25),
(N'Crochet Coasters Pack of 4', N'Sunflower design tableware coasters', N'Home Decor', 800.00, 40),
(N'Amigurumi Teddy Bear Toy', N'Stuffed cute teddy bear toy 8 inches', N'Toys', 1800.00, 15);
GO

-- Populate Customers
INSERT INTO [dbo].[customer] ([c_name], [c_contact], [c_email], [c_address])
VALUES 
(N'Zainab Fatima', '03215551234', 'zainab.fatima@gmail.com', N'House 123, Street 5, DHA Phase 6, Lahore'),
(N'Muhammad Ali', '03004445678', 'm.ali12@gmail.com', N'Flat 4B, Eden Heights, Gulberg III, Lahore'),
(N'Ayesha Khan', '03456667890', 'ayesha.khan99@gmail.com', N'Sector Y, Phase 3C, DHA, Karachi'),
(N'Hamza Riaz', '03127778899', 'hamza.riaz@gmail.com', N'Street 12, G-11/2, Islamabad');
GO

-- Populate Employees
INSERT INTO [dbo].[employee] ([emp_name], [emp_contact], [emp_email], [emp_role], [emp_hiredate])
VALUES 
(N'Usman Siddiqui', '03332221144', 'usman.sid@gmail.com', N'Delivery Rider', '2025-01-15'),
(N'Bilal Ahmed', '03001112233', 'bilal.ahmed@gmail.com', N'Delivery Rider', '2025-02-10'),
(N'Sana Javed', '03218889900', 'sana.javed@gmail.com', N'Store Manager', '2024-11-01');
GO

-- Populate Orders
-- Let's use standard SQL INSERT for orders and details to bypass TVP for dummy data
DECLARE @cId1 INT, @cId2 INT, @pId1 INT, @pId2 INT, @pId3 INT;
SELECT @cId1 = MIN(c_id), @cId2 = MAX(c_id) FROM [dbo].[customer];
SELECT @pId1 = MIN(p_id), @pId2 = MIN(p_id)+1, @pId3 = MAX(p_id) FROM [dbo].[products];

-- Order 1
DECLARE @oId1 INT;
INSERT INTO [dbo].[orders] ([c_id], [o_date], [o_status]) VALUES (@cId1, '2026-05-10', 'completed');
SET @oId1 = SCOPE_IDENTITY();
INSERT INTO [dbo].[order_details] ([o_id], [p_id], [qty]) VALUES (@oId1, @pId1, 2), (@oId1, @pId2, 1);

-- Order 2
DECLARE @oId2 INT;
INSERT INTO [dbo].[orders] ([c_id], [o_date], [o_status]) VALUES (@cId2, '2026-05-15', 'pending');
SET @oId2 = SCOPE_IDENTITY();
INSERT INTO [dbo].[order_details] ([o_id], [p_id], [qty]) VALUES (@oId2, @pId3, 1);
GO

-- Populate Deliveries
DECLARE @oId1 INT, @oId2 INT, @empId1 INT, @empId2 INT;
SELECT @oId1 = MIN(o_id), @oId2 = MAX(o_id) FROM [dbo].[orders];
SELECT @empId1 = MIN(emp_id), @empId2 = MIN(emp_id)+1 FROM [dbo].[employee];

INSERT INTO [dbo].[delivery] ([o_id], [emp_id], [del_date], [del_status])
VALUES 
(@oId1, @empId1, '2026-05-12', 'completed'),
(@oId2, @empId2, '2026-05-18', 'pending');
GO

-- Done!
PRINT 'Database db_crochet_craft3 has been successfully completed with all missing tables, triggers, and stored procedures!';
