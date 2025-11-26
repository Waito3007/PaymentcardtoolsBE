using Paymentcardtools.Extension.Interface;
using Paymentcardtools.Service;
using Paymentcardtools.Service.Interface;
using Paymentcardtools.Extension;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IInputSource, Inputtxt>();
builder.Services.AddScoped<IKeyQualityService, KeyQualityService>();
var corsPolicyName = "AllowPaymentcardtoolsClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5156",
                "https://localhost:7069",
                "https://paymentcardtools-client.vercel.app",
                "https://paymentcardtools-api-latest.onrender.com")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(corsPolicyName);

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("Health");

app.Run();
