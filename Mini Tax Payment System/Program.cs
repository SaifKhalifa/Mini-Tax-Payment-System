using System.Threading.Tasks;

namespace Mini_Tax_Payment_System;

internal class Program
{
    // 1. Insert initial unpaid sales AddSale(100); 
    // Tax: $10 AddSale(200); 
    // Tax: $20 AddSale(200); 
    // Tax: $20 -> Total Unpaid Tax: $50
    // Console.WriteLine($"Unpaid Tax before payment: {GetUnpaidTaxSum()}"); 
    
    // 2. Trigger concurrent PayTax calls simultaneously
    // var task1 = Task.Run(() => PayTax());
    // var task2 = Task.Run(() => PayTax());
    // await Task.WhenAll(task1, task2); 
    
    // 3. Verify execution results 
    // Output should confirm ONLY 1 payment record of $50 created in TaxPayments!
    
    //DisplayTaxPayments();
    static async Task Main(string[] args)
    {
        var repo = new TaxPaymentRepository("Server=localhost;Port=3306;Database=TaxPaymentDB;User=root;Password=saif8808;");

        await repo.AddSaleAsync(100m);   // tax 10
        await repo.AddSaleAsync(120m);   // tax 12
        await repo.AddSaleAsync(150m);   // tax 15

        Console.WriteLine($"Unpaid Tax before payment: {await repo.GetUnpaidTaxSum()}");

        var task1 = Task.Run(() => repo.PayTaxAsync());
        var task2 = Task.Run(() => repo.PayTaxAsync());
        var paid = await Task.WhenAll(task1, task2);

        Console.WriteLine($"Call 1 paid: {paid[0]}, Call 2 paid: {paid[1]}");
        Console.WriteLine($"Unpaid Tax after payment: {await repo.GetUnpaidTaxSum()}");

        var payments = await repo.GetTaxPaymentsAsync();

        Console.WriteLine($"TaxPayments records: {payments.Count}");

        foreach (var p in payments)
        {
            Console.WriteLine($"  Id={p.Id}  Amount={p.Amount}  PaidAt={p.PaidAt}");
        }
    }
}
