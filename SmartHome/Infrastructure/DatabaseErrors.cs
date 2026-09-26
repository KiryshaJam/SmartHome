using Npgsql;

namespace SmartHome.Infrastructure;

public static class DatabaseErrors
{
    public static async Task Handle(HttpContext context, RequestDelegate next)
    {
        try { await next(context); }
        catch (Exception exception) when (FindPostgres(exception) is { } pg &&
            pg.SqlState is "23503" or "23505" or "23514" or "23502" or "22001" or "22003" or "P0001" or "40001" or "40P01")
        {
            var error = FindPostgres(exception)!;
            context.Response.StatusCode = error.SqlState is "23503" or "23505" or "40001" or "40P01" ? 409 : 400;
            var message = error.SqlState switch
            {
                "23503" => "Запись используется или связанный объект уже удалён. Обновите страницу.",
                "23505" => "Запись с таким именем, значением или порядком уже существует.",
                "22001" => "Превышена допустимая длина текста.",
                "22003" => "Число выходит за допустимый диапазон.",
                "40001" or "40P01" => "Данные изменились одновременно. Повторите действие.",
                "P0001" => error.MessageText,
                _ => "Значение не соответствует ограничениям данных."
            };
            await context.Response.WriteAsJsonAsync(new { message });
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (Exception exception)
        {
            var unavailable = IsConnectionFailure(exception);
            context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("DatabaseErrors").LogError(exception, "Request failed");
            context.Response.StatusCode = unavailable ? 503 : 500;
            await context.Response.WriteAsJsonAsync(new {
                message = unavailable ? "База данных временно недоступна. Повторите попытку позже." : "Не удалось выполнить действие. Повторите попытку или обратитесь к администратору."
            });
        }
    }

    private static bool IsConnectionFailure(Exception? error) =>
        error is NpgsqlException and not PostgresException ||
        (error?.InnerException is { } inner && IsConnectionFailure(inner));

    private static PostgresException? FindPostgres(Exception? error) =>
        error is PostgresException postgres ? postgres : error?.InnerException is { } inner ? FindPostgres(inner) : null;
}
