using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "URL Shortener API", Version = "v1" });
});
builder.Services.AddSingleton<UrlStore>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// POST /shorten — cria uma URL encurtada
app.MapPost("/shorten", (ShortenRequest request, UrlStore store, HttpContext ctx) =>
{
    if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        return Results.BadRequest("URL inválida. Informe uma URL absoluta válida.");

    var code = store.Add(request.Url, request.CustomAlias);
    if (code is null)
        return Results.BadRequest("Alias já está em uso. Escolha outro.");

    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    return Results.Ok(new ShortenResponse(code, $"{baseUrl}/{code}", request.Url));
})
.WithName("ShortenUrl");

// GET /{code} — redireciona para a URL original
app.MapGet("/{code}", (string code, UrlStore store) =>
{
    var url = store.Get(code);
    return url is not null
        ? Results.Redirect(url)
        : Results.NotFound($"Código '{code}' não encontrado.");
})
.WithName("RedirectUrl");

// GET /api/urls — lista todas as URLs cadastradas
app.MapGet("/api/urls", (UrlStore store) => store.GetAll())
.WithName("ListUrls");

// DELETE /api/urls/{code} — remove uma URL encurtada
app.MapDelete("/api/urls/{code}", (string code, UrlStore store) =>
{
    return store.Remove(code)
        ? Results.Ok($"Código '{code}' removido com sucesso.")
        : Results.NotFound($"Código '{code}' não encontrado.");
})
.WithName("DeleteUrl");

// GET /api/stats/{code} — retorna estatísticas de cliques
app.MapGet("/api/stats/{code}", (string code, UrlStore store) =>
{
    var entry = store.GetEntry(code);
    return entry is not null
        ? Results.Ok(entry)
        : Results.NotFound($"Código '{code}' não encontrado.");
})
.WithName("GetStats");

app.Run();

// ─── Models ───────────────────────────────────────────────────────────────────

record ShortenRequest(string Url, string? CustomAlias = null);
record ShortenResponse(string Code, string ShortUrl, string OriginalUrl);

record UrlEntry(string Code, string OriginalUrl, DateTime CreatedAt, int Clicks);

// ─── Store (in-memory com thread safety) ─────────────────────────────────────

class UrlStore
{
    private readonly ConcurrentDictionary<string, (string Url, DateTime Created, int Clicks)> _map = new();

    public string? Add(string url, string? alias = null)
    {
        var code = alias ?? GenerateCode();
        if (alias is not null && _map.ContainsKey(alias))
            return null;

        _map[code] = (url, DateTime.UtcNow, 0);
        return code;
    }

    public string? Get(string code)
    {
        if (_map.TryGetValue(code, out var entry))
        {
            _map[code] = entry with { Clicks = entry.Clicks + 1 };
            return entry.Url;
        }
        return null;
    }

    public bool Remove(string code) => _map.TryRemove(code, out _);

    public UrlEntry? GetEntry(string code) =>
        _map.TryGetValue(code, out var e)
            ? new UrlEntry(code, e.Url, e.Created, e.Clicks)
            : null;

    public IEnumerable<UrlEntry> GetAll() =>
        _map.Select(kv => new UrlEntry(kv.Key, kv.Value.Url, kv.Value.Created, kv.Value.Clicks));

    private static string GenerateCode()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }
}