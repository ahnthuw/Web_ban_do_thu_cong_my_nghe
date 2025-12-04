using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_ban_do_thu_cong_my_nghe.Migrations
{
    /// <inheritdoc />
    public partial class EnsureDiscountCodesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('discount_codes', 'U') IS NULL
BEGIN
    CREATE TABLE discount_codes
    (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        code NVARCHAR(64) NOT NULL,
        description NVARCHAR(255) NULL,
        percent_off DECIMAL(5,2) NULL,
        amount_off DECIMAL(18,2) NULL,
        min_order_value DECIMAL(18,2) NOT NULL CONSTRAINT DF_discount_codes_min_order DEFAULT(0),
        start_date DATETIME2 NOT NULL DEFAULT(GETUTCDATE()),
        end_date DATETIME2 NULL,
        usage_limit INT NOT NULL DEFAULT(0),
        times_used INT NOT NULL DEFAULT(0),
        is_active BIT NOT NULL DEFAULT(1),
        created_at DATETIME2 NOT NULL DEFAULT(GETUTCDATE())
    );
END
ELSE
BEGIN
    IF COL_LENGTH('discount_codes','percent_off') IS NULL
        ALTER TABLE discount_codes ADD percent_off DECIMAL(5,2) NULL;

    IF COL_LENGTH('discount_codes','amount_off') IS NULL
        ALTER TABLE discount_codes ADD amount_off DECIMAL(18,2) NULL;

    IF COL_LENGTH('discount_codes','min_order_value') IS NULL
        ALTER TABLE discount_codes ADD min_order_value DECIMAL(18,2) NOT NULL CONSTRAINT DF_discount_codes_min_order DEFAULT(0);

    IF COL_LENGTH('discount_codes','start_date') IS NULL
        ALTER TABLE discount_codes ADD start_date DATETIME2 NOT NULL DEFAULT(GETUTCDATE());

    IF COL_LENGTH('discount_codes','end_date') IS NULL
        ALTER TABLE discount_codes ADD end_date DATETIME2 NULL;

    IF COL_LENGTH('discount_codes','usage_limit') IS NULL
        ALTER TABLE discount_codes ADD usage_limit INT NOT NULL DEFAULT(0);

    IF COL_LENGTH('discount_codes','times_used') IS NULL
        ALTER TABLE discount_codes ADD times_used INT NOT NULL DEFAULT(0);

    IF COL_LENGTH('discount_codes','is_active') IS NULL
        ALTER TABLE discount_codes ADD is_active BIT NOT NULL DEFAULT(1);

    IF COL_LENGTH('discount_codes','created_at') IS NULL
        ALTER TABLE discount_codes ADD created_at DATETIME2 NOT NULL DEFAULT(GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_discount_codes_code' AND object_id = OBJECT_ID('discount_codes'))
BEGIN
    CREATE UNIQUE INDEX IX_discount_codes_code ON discount_codes(code);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('discount_codes', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('discount_codes','created_at') IS NOT NULL
        ALTER TABLE discount_codes DROP COLUMN created_at;
    IF COL_LENGTH('discount_codes','is_active') IS NOT NULL
        ALTER TABLE discount_codes DROP COLUMN is_active;
    IF COL_LENGTH('discount_codes','times_used') IS NOT NULL
        ALTER TABLE discount_codes DROP COLUMN times_used;
    IF COL_LENGTH('discount_codes','usage_limit') IS NOT NULL
        ALTER TABLE discount_codes DROP COLUMN usage_limit;
    IF COL_LENGTH('discount_codes','end_date') IS NOT NULL
        ALTER TABLE discount_codes DROP COLUMN end_date;
    IF COL_LENGTH('discount_codes','start_date') IS NOT NULL
        ALTER TABLE discount_codes DROP COLUMN start_date;
    IF COL_LENGTH('discount_codes','min_order_value') IS NOT NULL
        ALTER TABLE discount_codes DROP COLUMN min_order_value;
    IF COL_LENGTH('discount_codes','amount_off') IS NOT NULL
        ALTER TABLE discount_codes DROP COLUMN amount_off;
    IF COL_LENGTH('discount_codes','percent_off') IS NOT NULL
        ALTER TABLE discount_codes DROP COLUMN percent_off;
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_discount_codes_code' AND object_id = OBJECT_ID('discount_codes'))
BEGIN
    DROP INDEX IX_discount_codes_code ON discount_codes;
END
");
        }
    }
}
