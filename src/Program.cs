using BankingTransactions.Api.Configuration;
using BankingTransactions.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBankingApi(builder.Configuration);
var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
//app.UseAuthentication();
//app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
