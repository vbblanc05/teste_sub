using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=chamados_victor.db")
);

var app = builder.Build();

// garante que o banco exista (sem migrações obrigatórias)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// POST - cadastrar (mantive aqui para a API ficar completa)
// cria um chamado; garante status "Aberto" sempre
app.MapPost("/api/chamado/cadastrar", async (Chamado input, AppDbContext db) =>
{
    input.Status = "Aberto";
    db.Chamados.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/chamado/{input.Id}", input);
});

// GET - listar todos os chamados
app.MapGet("/api/chamado/listar", async (AppDbContext db) =>
    await db.Chamados.ToListAsync()
);

// PATCH - alterar status conforme fluxo Aberto -> Em atendimento -> Resolvido
app.MapPatch("/api/chamado/alterar", async (AlterarRequest req, AppDbContext db) =>
{
    var chamado = await db.Chamados.FindAsync(req.Id);
    if (chamado == null) return Results.NotFound();

    if (chamado.Status == "Aberto")
        chamado.Status = "Em atendimento";
    else if (chamado.Status == "Em atendimento")
        chamado.Status = "Resolvido";
    // outros status permanecem inalterados

    await db.SaveChangesAsync();
    return Results.Ok(chamado);
});

// GET - chamados não resolvidos (Aberto, Em atendimento)
app.MapGet("/api/chamado/naoresolvido", async (AppDbContext db) =>
    await db.Chamados
        .Where(c => c.Status == "Aberto" || c.Status == "Em atendimento")
        .Select(c => new { c.Id, c.Descricao, c.Status })
        .ToListAsync()
);

// GET - chamados resolvidos (Resolvido)
app.MapGet("/api/chamado/resolvidos", async (AppDbContext db) =>
    await db.Chamados
        .Where(c => c.Status == "Resolvido")
        .Select(c => new { c.Id, c.Descricao, c.Status })
        .ToListAsync()
);

app.Run();


// modelos e DbContext (podem ficar neste arquivo ou em arquivos separados)
public class Chamado
{
    public int Id { get; set; }
    public string? Titulo { get; set; }
    public string? Descricao { get; set; }
    public string Status { get; set; } = "Aberto";
}

public class AlterarRequest
{
    public int Id { get; set; }
}
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Chamado> Chamados { get; set; } = null!;
}
