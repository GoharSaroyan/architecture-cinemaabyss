using System.Text.Json.Serialization;
using Npgsql;

// Database connection
NpgsqlDataSource db = null!;

// Initialize database connection
InitDb();

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Set up HTTP routes
app.Map("/health", HealthHandler);
app.Map("/api/users", HandleUsers);
app.Map("/api/movies", HandleMovies);
app.Map("/api/payments", HandlePayments);
app.Map("/api/subscriptions", HandleSubscriptions);

// Start server
var port = Environment.GetEnvironmentVariable("PORT");
if (string.IsNullOrEmpty(port))
{
    port = "8080";
}
Console.WriteLine($"Starting server on port {port}");
try
{
    app.Run($"http://0.0.0.0:{port}");
}
finally
{
    db.Dispose();
}

void InitDb()
{
    var connStr = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
    if (string.IsNullOrEmpty(connStr))
    {
        connStr = "postgres://postgres:postgres@localhost/cinemaabyss?sslmode=disable";
    }

    try
    {
        db = NpgsqlDataSource.Create(ToNpgsqlConnectionString(connStr));
    }
    catch (Exception err)
    {
        Fatal(err);
        return;
    }

    try
    {
        using var conn = db.OpenConnection();
    }
    catch (Exception err)
    {
        Fatal(err);
        return;
    }
    Console.WriteLine("Successfully connected to database");
}

// Npgsql doesn't parse libpq-style "postgres://" URIs the way lib/pq does, so convert
// the same DSN the Go service accepts into Npgsql's keyword=value format.
string ToNpgsqlConnectionString(string dsn)
{
    var uri = new Uri(dsn);
    var csb = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = uri.AbsolutePath.TrimStart('/'),
    };

    var userInfo = uri.UserInfo.Split(':', 2);
    if (userInfo.Length > 0 && userInfo[0].Length > 0)
    {
        csb.Username = Uri.UnescapeDataString(userInfo[0]);
    }
    if (userInfo.Length > 1)
    {
        csb.Password = Uri.UnescapeDataString(userInfo[1]);
    }

    foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        var kv = pair.Split('=', 2);
        var key = Uri.UnescapeDataString(kv[0]);
        var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
        if (key == "sslmode")
        {
            csb.SslMode = value.ToLowerInvariant() switch
            {
                "disable" => SslMode.Disable,
                "require" => SslMode.Require,
                "verify-ca" => SslMode.VerifyCA,
                "verify-full" => SslMode.VerifyFull,
                _ => SslMode.Prefer,
            };
        }
    }

    return csb.ConnectionString;
}

void Fatal(Exception err)
{
    Console.Error.WriteLine(err.Message);
    Environment.Exit(1);
}

async Task HealthHandler(HttpContext context)
{
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new Dictionary<string, bool> { ["status"] = true });
}

// User handlers
async Task HandleUsers(HttpContext context)
{
    switch (context.Request.Method)
    {
        case "GET":
            if (!string.IsNullOrEmpty(context.Request.Query["id"]))
            {
                await GetUserById(context);
            }
            else
            {
                await GetAllUsers(context);
            }
            break;
        case "POST":
            await CreateUser(context);
            break;
        default:
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            await context.Response.WriteAsync("Method not allowed");
            break;
    }
}

async Task GetAllUsers(HttpContext context)
{
    var users = new List<User>();
    try
    {
        await using var cmd = db.CreateCommand("SELECT id, username, email FROM users");
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
            });
        }
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(users);
}

async Task GetUserById(HttpContext context)
{
    var id = context.Request.Query["id"].ToString();
    var u = new User();
    try
    {
        await using var cmd = db.CreateCommand("SELECT id, username, email FROM users WHERE id = $1");
        cmd.Parameters.Add(new NpgsqlParameter { Value = int.Parse(id) });
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new Exception("sql: no rows in result set");
        }
        u.Id = reader.GetInt32(0);
        u.Username = reader.GetString(1);
        u.Email = reader.GetString(2);
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(u);
}

async Task CreateUser(HttpContext context)
{
    User? u;
    try
    {
        u = await context.Request.ReadFromJsonAsync<User>();
        if (u is null)
        {
            throw new Exception("EOF");
        }
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    try
    {
        await using var cmd = db.CreateCommand("INSERT INTO users (username, email) VALUES ($1, $2) RETURNING id");
        cmd.Parameters.Add(new NpgsqlParameter { Value = u.Username });
        cmd.Parameters.Add(new NpgsqlParameter { Value = u.Email });
        u.Id = (int)(await cmd.ExecuteScalarAsync())!;
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    context.Response.StatusCode = StatusCodes.Status201Created;
    await context.Response.WriteAsJsonAsync(u);
}

// Movie handlers
async Task HandleMovies(HttpContext context)
{
    switch (context.Request.Method)
    {
        case "GET":
            if (!string.IsNullOrEmpty(context.Request.Query["id"]))
            {
                await GetMovieById(context);
            }
            else
            {
                await GetAllMovies(context);
            }
            break;
        case "POST":
            await CreateMovie(context);
            break;
        default:
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            await context.Response.WriteAsync("Method not allowed");
            break;
    }
}

async Task GetAllMovies(HttpContext context)
{
    var movies = new List<Movie>();
    try
    {
        await using var cmd = db.CreateCommand("SELECT id, title, description, rating FROM movies");
        Console.WriteLine("get movies from monolith");
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var m = new Movie
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                Rating = reader.GetDouble(3),
            };

            // Get genres for this movie
            var genres = new List<string>();
            await using (var genreCmd = db.CreateCommand("SELECT genre FROM movie_genres WHERE movie_id = $1"))
            {
                genreCmd.Parameters.Add(new NpgsqlParameter { Value = m.Id });
                await using var genreReader = await genreCmd.ExecuteReaderAsync();
                while (await genreReader.ReadAsync())
                {
                    genres.Add(genreReader.GetString(0));
                }
            }
            m.Genres = genres;

            movies.Add(m);
        }
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(movies);
}

async Task GetMovieById(HttpContext context)
{
    var id = context.Request.Query["id"].ToString();
    var m = new Movie();
    try
    {
        await using var cmd = db.CreateCommand("SELECT id, title, description, rating FROM movies WHERE id = $1");
        cmd.Parameters.Add(new NpgsqlParameter { Value = int.Parse(id) });
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new Exception("sql: no rows in result set");
        }
        m.Id = reader.GetInt32(0);
        m.Title = reader.GetString(1);
        m.Description = reader.GetString(2);
        m.Rating = reader.GetDouble(3);
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    // Get genres for this movie
    try
    {
        var genres = new List<string>();
        await using var genreCmd = db.CreateCommand("SELECT genre FROM movie_genres WHERE movie_id = $1");
        genreCmd.Parameters.Add(new NpgsqlParameter { Value = m.Id });
        await using var genreReader = await genreCmd.ExecuteReaderAsync();
        while (await genreReader.ReadAsync())
        {
            genres.Add(genreReader.GetString(0));
        }
        m.Genres = genres;
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(m);
}

async Task CreateMovie(HttpContext context)
{
    Movie? m;
    try
    {
        m = await context.Request.ReadFromJsonAsync<Movie>();
        if (m is null)
        {
            throw new Exception("EOF");
        }
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    await using var conn = await db.OpenConnectionAsync();
    await using var tx = await conn.BeginTransactionAsync();

    try
    {
        await using (var cmd = new NpgsqlCommand(
            "INSERT INTO movies (title, description, rating) VALUES ($1, $2, $3) RETURNING id", conn, tx))
        {
            cmd.Parameters.Add(new NpgsqlParameter { Value = m.Title });
            cmd.Parameters.Add(new NpgsqlParameter { Value = m.Description });
            cmd.Parameters.Add(new NpgsqlParameter { Value = m.Rating });
            m.Id = (int)(await cmd.ExecuteScalarAsync())!;
        }

        foreach (var genre in m.Genres)
        {
            await using var genreCmd = new NpgsqlCommand(
                "INSERT INTO movie_genres (movie_id, genre) VALUES ($1, $2)", conn, tx);
            genreCmd.Parameters.Add(new NpgsqlParameter { Value = m.Id });
            genreCmd.Parameters.Add(new NpgsqlParameter { Value = genre });
            await genreCmd.ExecuteNonQueryAsync();
        }
    }
    catch (Exception err)
    {
        await tx.RollbackAsync();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    try
    {
        await tx.CommitAsync();
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    context.Response.StatusCode = StatusCodes.Status201Created;
    await context.Response.WriteAsJsonAsync(m);
}

// Payment handlers
async Task HandlePayments(HttpContext context)
{
    switch (context.Request.Method)
    {
        case "GET":
            if (!string.IsNullOrEmpty(context.Request.Query["id"]))
            {
                await GetPaymentById(context);
            }
            else if (!string.IsNullOrEmpty(context.Request.Query["user_id"]))
            {
                await GetPaymentsByUserId(context);
            }
            else
            {
                await GetAllPayments(context);
            }
            break;
        case "POST":
            await CreatePayment(context);
            break;
        default:
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            await context.Response.WriteAsync("Method not allowed");
            break;
    }
}

async Task GetAllPayments(HttpContext context)
{
    var payments = new List<Payment>();
    try
    {
        await using var cmd = db.CreateCommand("SELECT id, user_id, amount, timestamp FROM payments");
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            payments.Add(new Payment
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Amount = reader.GetDouble(2),
                Timestamp = reader.GetFieldValue<DateTimeOffset>(3),
            });
        }
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(payments);
}

async Task GetPaymentById(HttpContext context)
{
    var id = context.Request.Query["id"].ToString();
    var p = new Payment();
    try
    {
        await using var cmd = db.CreateCommand("SELECT id, user_id, amount, timestamp FROM payments WHERE id = $1");
        cmd.Parameters.Add(new NpgsqlParameter { Value = int.Parse(id) });
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new Exception("sql: no rows in result set");
        }
        p.Id = reader.GetInt32(0);
        p.UserId = reader.GetInt32(1);
        p.Amount = reader.GetDouble(2);
        p.Timestamp = reader.GetFieldValue<DateTimeOffset>(3);
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(p);
}

async Task GetPaymentsByUserId(HttpContext context)
{
    var userId = context.Request.Query["user_id"].ToString();
    var payments = new List<Payment>();
    try
    {
        await using var cmd = db.CreateCommand("SELECT id, user_id, amount, timestamp FROM payments WHERE user_id = $1");
        cmd.Parameters.Add(new NpgsqlParameter { Value = int.Parse(userId) });
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            payments.Add(new Payment
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Amount = reader.GetDouble(2),
                Timestamp = reader.GetFieldValue<DateTimeOffset>(3),
            });
        }
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(payments);
}

async Task CreatePayment(HttpContext context)
{
    Payment? p;
    try
    {
        p = await context.Request.ReadFromJsonAsync<Payment>();
        if (p is null)
        {
            throw new Exception("EOF");
        }
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    p.Timestamp = DateTimeOffset.Now;
    try
    {
        await using var cmd = db.CreateCommand(
            "INSERT INTO payments (user_id, amount, timestamp) VALUES ($1, $2, $3) RETURNING id");
        cmd.Parameters.Add(new NpgsqlParameter { Value = p.UserId });
        cmd.Parameters.Add(new NpgsqlParameter { Value = p.Amount });
        cmd.Parameters.Add(new NpgsqlParameter { Value = p.Timestamp });
        p.Id = (int)(await cmd.ExecuteScalarAsync())!;
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    context.Response.StatusCode = StatusCodes.Status201Created;
    await context.Response.WriteAsJsonAsync(p);
}

// Subscription handlers
async Task HandleSubscriptions(HttpContext context)
{
    switch (context.Request.Method)
    {
        case "GET":
            if (!string.IsNullOrEmpty(context.Request.Query["id"]))
            {
                await GetSubscriptionById(context);
            }
            else if (!string.IsNullOrEmpty(context.Request.Query["user_id"]))
            {
                await GetSubscriptionsByUserId(context);
            }
            else
            {
                await GetAllSubscriptions(context);
            }
            break;
        case "POST":
            await CreateSubscription(context);
            break;
        default:
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            await context.Response.WriteAsync("Method not allowed");
            break;
    }
}

async Task GetAllSubscriptions(HttpContext context)
{
    var subscriptions = new List<Subscription>();
    try
    {
        await using var cmd = db.CreateCommand("SELECT id, user_id, plan_type, start_date, end_date FROM subscriptions");
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            subscriptions.Add(new Subscription
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                PlanType = reader.GetString(2),
                StartDate = reader.GetFieldValue<DateTimeOffset>(3),
                EndDate = reader.GetFieldValue<DateTimeOffset>(4),
            });
        }
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(subscriptions);
}

async Task GetSubscriptionById(HttpContext context)
{
    var id = context.Request.Query["id"].ToString();
    var s = new Subscription();
    try
    {
        await using var cmd = db.CreateCommand(
            "SELECT id, user_id, plan_type, start_date, end_date FROM subscriptions WHERE id = $1");
        cmd.Parameters.Add(new NpgsqlParameter { Value = int.Parse(id) });
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new Exception("sql: no rows in result set");
        }
        s.Id = reader.GetInt32(0);
        s.UserId = reader.GetInt32(1);
        s.PlanType = reader.GetString(2);
        s.StartDate = reader.GetFieldValue<DateTimeOffset>(3);
        s.EndDate = reader.GetFieldValue<DateTimeOffset>(4);
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(s);
}

async Task GetSubscriptionsByUserId(HttpContext context)
{
    var userId = context.Request.Query["user_id"].ToString();
    var subscriptions = new List<Subscription>();
    try
    {
        await using var cmd = db.CreateCommand(
            "SELECT id, user_id, plan_type, start_date, end_date FROM subscriptions WHERE user_id = $1");
        cmd.Parameters.Add(new NpgsqlParameter { Value = int.Parse(userId) });
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            subscriptions.Add(new Subscription
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                PlanType = reader.GetString(2),
                StartDate = reader.GetFieldValue<DateTimeOffset>(3),
                EndDate = reader.GetFieldValue<DateTimeOffset>(4),
            });
        }
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(subscriptions);
}

async Task CreateSubscription(HttpContext context)
{
    Subscription? s;
    try
    {
        s = await context.Request.ReadFromJsonAsync<Subscription>();
        if (s is null)
        {
            throw new Exception("EOF");
        }
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    try
    {
        await using var cmd = db.CreateCommand(
            "INSERT INTO subscriptions (user_id, plan_type, start_date, end_date) VALUES ($1, $2, $3, $4) RETURNING id");
        cmd.Parameters.Add(new NpgsqlParameter { Value = s.UserId });
        cmd.Parameters.Add(new NpgsqlParameter { Value = s.PlanType });
        cmd.Parameters.Add(new NpgsqlParameter { Value = s.StartDate });
        cmd.Parameters.Add(new NpgsqlParameter { Value = s.EndDate });
        s.Id = (int)(await cmd.ExecuteScalarAsync())!;
    }
    catch (Exception err)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(err.Message);
        return;
    }

    context.Response.ContentType = "application/json";
    context.Response.StatusCode = StatusCodes.Status201Created;
    await context.Response.WriteAsJsonAsync(s);
}

// Models
class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";
}

class Movie
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = new();

    [JsonPropertyName("rating")]
    public double Rating { get; set; }
}

class Payment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}

class Subscription
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("plan_type")]
    public string PlanType { get; set; } = "";

    [JsonPropertyName("start_date")]
    public DateTimeOffset StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTimeOffset EndDate { get; set; }
}
