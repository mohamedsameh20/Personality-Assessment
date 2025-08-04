using PersonalityAssessment.Api.Services;
using PersonalityAssessment.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace PersonalityAssessment.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });
            
            // Add database context
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            
            // Add CORS for local development
            builder.Services.AddCors();
            
            // Add health checks
            builder.Services.AddHealthChecks();
            
            // Add response compression
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });
            
            // Add memory cache
            builder.Services.AddMemoryCache();
            builder.Services.AddResponseCaching();
            
            // Register our services
            builder.Services.AddScoped<IAssessmentService, AssessmentService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IQuestionService, QuestionService>();
            builder.Services.AddScoped<IPersonalityScorer, PersonalityScorer>();
            builder.Services.AddScoped<ICompatibilityService, CompatibilityService>();
            
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            // Use response compression
            app.UseResponseCompression();
            
            // Use response caching
            app.UseResponseCaching();

            // Enable CORS for local development
            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            // Enable default files (index.html)
            app.UseDefaultFiles();
            
            // Enable static files with caching
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    // Cache static files for 1 day
                    if (app.Environment.IsProduction())
                    {
                        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=86400");
                    }
                }
            });

            app.UseHttpsRedirection();

            app.UseAuthorization();

            // Add health check endpoint
            app.MapHealthChecks("/health");

            app.MapControllers();
            
            // Auto-migrate database in production
            if (app.Environment.IsProduction())
            {
                using (var scope = app.Services.CreateScope())
                {
                    try
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        context.Database.Migrate();
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "An error occurred while migrating the database.");
                    }
                }
            }

            app.Run();
        }
    }
}
