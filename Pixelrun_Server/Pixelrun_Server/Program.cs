using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pixelrun_Server;
using Pixelrun_Server.Services;
using System.Text;

try
{

    var builder = WebApplication.CreateBuilder(args);


    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "PixelRun API",
            Version = "v1"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Type JWT token"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    });

    
    builder.Services.AddDbContext<GameDbContext>(opt =>
    {
        opt.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"));
    });

    builder.Services.AddScoped<PlayerService>();
    builder.Services.AddScoped<TokenService>();
    builder.Services.AddScoped<RecordService>();
    builder.Services.AddScoped<ShopService>();
    builder.Services.AddScoped<QuestService>();

    // MultiplayerHub is Singleton because it holds the static player map
    builder.Services.AddSingleton<MultiplayerHub>();

    builder.Services.AddCors(opt =>
        opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    var app = builder.Build();


    var retries = 20;
    while (retries > 0)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            db.Database.Migrate();
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine("DB not ready yet...");
            retries--;
            Thread.Sleep(3000);
        }
    }

    //using (var scope = app.Services.CreateScope())
    //{
    //    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    //    db.Database.EnsureCreated();

    //    // Safely add any missing columns (SQLite doesn't support auto-migrations)
    //    var alterCmds = new[]
    //    {
    //        "ALTER TABLE Players ADD COLUMN IsAdmin INTEGER NOT NULL DEFAULT 0",
    //        "ALTER TABLE Players ADD COLUMN EquippedPlayerSkin TEXT NOT NULL DEFAULT 'default'",
    //        "ALTER TABLE Players ADD COLUMN EquippedBarSkin TEXT NOT NULL DEFAULT 'bar_default'",
    //        "ALTER TABLE Players ADD COLUMN EquippedSlashSkin TEXT NOT NULL DEFAULT 'slash_default'",
    //    };
    //    foreach (var cmd in alterCmds)
    //    {
    //        try { db.Database.ExecuteSqlRaw(cmd); }
    //        catch { /* column already exists */ }
    //    }
    //}

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "PixelRun API v1"));
    }

    app.UseCors("AllowAll");

    // ── WebSocket middleware ──────────────────────────────────────────────────
    //app.UseWebSockets(new WebSocketOptions
    //{
    //    KeepAliveInterval = TimeSpan.FromSeconds(30)
    //});

    //app.Map("/ws/game", async (HttpContext ctx, MultiplayerHub hub) =>
    //{
    //    if (!ctx.WebSockets.IsWebSocketRequest)
    //    {
    //        ctx.Response.StatusCode = 400;
    //        return;
    //    }
    //    var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    //    await hub.HandleAsync(ctx, ws);
    //});
    // ─────────────────────────────────────────────────────────────────────────

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    var logPath = Path.Combine(AppContext.BaseDirectory, "startup_error.log");
    File.WriteAllText(logPath, $"[{DateTime.Now}] FATAL:\n{ex}\n\nInner: {ex.InnerException}");
    Console.WriteLine($"FATAL: {ex}");
    
    throw;
}