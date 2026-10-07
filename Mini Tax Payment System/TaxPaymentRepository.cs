using Dapper;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mini_Tax_Payment_System.Models;

namespace Mini_Tax_Payment_System;

internal class TaxPaymentRepository
{
    private readonly string _connectionString;

    public TaxPaymentRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private MySqlConnection Open() => new MySqlConnection(_connectionString);

    public async Task AddSaleAsync(decimal amount)
    {
        decimal tax = amount * 0.10m;

        await using var conn = Open();

        const string sql = @"
                            INSERT INTO Sales (Amount, TaxAmount)
                            VALUES (@amount, @tax)";

        await conn.ExecuteAsync(sql, new {amount, tax});
    }

    public async Task<decimal> GetUnpaidTaxSum()
    {
        await using var conn = Open();

        const string sql = @"
                            SELECT SUM(TaxAmount)
                            FROM Sales
                            WHERE IsTaxPaid = 0";

        return await conn.ExecuteScalarAsync<decimal>(sql);
    }

    public async Task<decimal> PayTaxAsync()
    {
        await using var conn = Open();

        await conn.OpenAsync();

        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // 1. Get and lock unpaid sales
            var unpaidSales = await conn.QueryAsync<Sales>
                (
                    @"
                    SELECT Id, Amount, TaxAmount, IsTaxPaid
                    FROM Sales
                    WHERE IsTaxPaid = 0
                    FOR UPDATE
                    ",
                    transaction: transaction
                );

            // 2. Calculate aggregate tax
            var totalTax = unpaidSales.Sum(x => x.TaxAmount);

            // Nothing to pay
            if (totalTax <= 0)
            {
                await transaction.RollbackAsync();
                return 0;
                // the catch block will do it's magic
                //throw new InvalidOperationException("There are nothing to pay!");
            }

            // 3. Create one aggregate payment
            await conn.ExecuteAsync
                (
                    """
                    INSERT INTO TaxPayments (Amount)
                    VALUES (@Amount)
                    """,
                    new { Amount = totalTax },
                    transaction: transaction
                );

            // 4. Mark the sales as paid
            await conn.ExecuteAsync
                (
                    """
                    UPDATE Sales
                    SET IsTaxPaid = 1
                    WHERE IsTaxPaid = 0
                    """,
                    transaction: transaction
                );

            await transaction.CommitAsync();
            return totalTax;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<TaxPayments>> GetTaxPaymentsAsync()
    {
        await using var conn = Open();

        const string sql = @"
                            SELECT *
                            FROM TaxPayments";

        var reader = await conn.ExecuteReaderAsync(sql);

        var taxPaymentsList = new List<TaxPayments>();

        while (reader.Read())
        {
            taxPaymentsList.Add
                (
                    new TaxPayments()
                    { 
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        PaidAt = reader.GetDateTime(reader.GetOrdinal("PaidAt"))                    
                    }
                );
        }

        return taxPaymentsList;
    }
}

