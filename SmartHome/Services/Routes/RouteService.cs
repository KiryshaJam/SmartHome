using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs.Routes;
using SmartHome.Models.Routes;
namespace SmartHome.Services.Routes;

public class RouteService(AppDbContext db)
{
    private async Task<JsonElement> Query(FormattableString sql)
    {
        using var document = JsonDocument.Parse(await db.Database.SqlQuery<string>(sql).SingleAsync());
        return document.RootElement.Clone();
    }

    private async Task<List<int>> Editable(int root) => await db.Database.SqlQuery<int>($"""
        WITH RECURSIVE owned(sid) AS (
          SELECT {root}
          UNION SELECT i."SourceId" FROM owned o JOIN tech_input i ON i."StepId"=o.sid
          WHERE NOT EXISTS(SELECT 1 FROM tech_route_setting r WHERE r."FinalStepId"=i."SourceId" AND r."FinalStepId"<>{root})
        ) SELECT sid AS "Value" FROM owned
        """).ToListAsync();

    public async Task<object> Read(int id)
    {
        var r = await db.Set<TechRouteSetting>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new ArgumentException("Маршрут не найден.");
        var ids = await db.Database.SqlQuery<int>($"SELECT DISTINCT step_id AS \"Value\" FROM find_tm({r.ProductId},1::numeric,{id})").ToListAsync();
        return new { setting = r, editableStepIds = r.Kind == "application" ? new List<int>() : await Editable(r.FinalStepId), steps = await db.Set<TechStep>().AsNoTracking().Where(x => ids.Contains(x.Id)).OrderBy(x => x.Number).ToListAsync(), inputs = await db.Set<TechInput>().AsNoTracking().Where(x => ids.Contains(x.StepId)).ToListAsync() };
    }

    public async Task<object> Save(int? id, RouteWrite input)
    {
        if (input.Name.Length > 256 || input.Steps.Count > 500 || input.Inputs.Count > 2000)
            throw new ArgumentException("Укажите название и маршрут не более 500 позиций и 2000 входов.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(734021,1)");
        var r = id.HasValue ? await db.Set<TechRouteSetting>().SingleOrDefaultAsync(x => x.Id == id) : new TechRouteSetting();
        if (r == null) throw new ArgumentException("Маршрут не найден.");
        if (r.Kind == "application")
        {
            // Applications own only the external parameter value. Structure stays shared.
            r.ParameterValue = input.ParameterValue;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(input.Name)) throw new ArgumentException("Укажите название маршрута.");
            if (input.Kind is not ("standalone" or "template")) throw new ArgumentException("Выберите самостоятельный или типовой маршрут.");
            var oldIds = id.HasValue ? await Editable(r.FinalStepId) : [];
            if (input.Steps.Select(x => x.Id).Distinct().Count() != input.Steps.Count) throw new ArgumentException("Идентификаторы позиций повторяются.");
            var map = new Dictionary<int, int>();
            foreach (var s in input.Steps)
            {
                var key = s.Id;
                if (key > 0)
                {
                    if (!oldIds.Contains(key)) throw new ArgumentException("Нельзя изменять чужую позицию. Подключите её как вход.");
                    var entity = await db.Set<TechStep>().FindAsync(key);
                    db.Entry(entity!).CurrentValues.SetValues(s);
                }
                else { s.Id = 0; db.Add(s); }
                await db.SaveChangesAsync(); map[key] = s.Id;
            }
            int Resolve(int key) => map.TryGetValue(key, out var v) ? v : key > 0 ? key : throw new ArgumentException("Не найдена позиция входа.");
            // Only submitted positions have their inputs replaced; external nested nodes are read only.
            var editedIds = map.Values.ToList();
            var externalSources = input.Inputs.Where(x => !map.ContainsKey(x.SourceId)).Select(x => x.SourceId).Distinct().ToList();
            var availableSources = await db.Set<TechRouteSetting>().Where(x => externalSources.Contains(x.FinalStepId)).Select(x => x.FinalStepId).Distinct().ToListAsync();
            if (externalSources.Except(availableSources).Any()) throw new ArgumentException("Вложенный узел должен быть итоговой позицией сохранённого маршрута.");
            if (!map.ContainsKey(input.FinalStepId)) throw new ArgumentException("Выберите итоговую операцию редактируемого маршрута.");
            await db.Set<TechInput>().Where(x => editedIds.Contains(x.StepId)).ExecuteDeleteAsync();
            foreach (var i in input.Inputs)
            {
                if (!map.ContainsKey(i.StepId)) throw new ArgumentException("Вход задан для неизвестной операции.");
                db.Add(new TechInput { StepId = Resolve(i.StepId), SourceId = Resolve(i.SourceId), Amount = i.Amount, ForQuantity = i.ForQuantity, ParameterId = i.ParameterId });
            }
            if (id.HasValue && r.ProductId != input.ProductId) throw new ArgumentException("Нельзя менять изделие сохранённого маршрута.");
            if (r.Kind == "template")
            {
                var applications = db.Set<TechRouteSetting>().Where(x => x.Kind == "application" && x.FinalStepId == r.FinalStepId);
                if ((input.Kind != "template" || input.ParameterId != r.ParameterId) && await applications.AnyAsync())
                    throw new ArgumentException("Применяемый шаблон должен сохранить вид и определение параметра. Создайте новый типовой процесс.");
                await applications.ExecuteUpdateAsync(x => x.SetProperty(a => a.FinalStepId, Resolve(input.FinalStepId)));
            }
            r.Name = input.Name.Trim(); r.ProductId = input.ProductId; r.RevisionId = input.RevisionId;
            r.Kind = input.Kind; r.ParameterId = input.ParameterId; r.ParameterValue = input.ParameterValue;
            r.FinalStepId = Resolve(input.FinalStepId);
            if (!id.HasValue) db.Add(r);
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return await Read(r.Id);
    }

    public async Task<object> Apply(int templateId, ApplyRoute input)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(734021,1)");
        var t = await db.Set<TechRouteSetting>().SingleOrDefaultAsync(x => x.Id == templateId && x.Kind == "template")
            ?? throw new ArgumentException("Типовой процесс не найден.");
        if (string.IsNullOrWhiteSpace(input.Name)) throw new ArgumentException("Укажите название.");
        var r = new TechRouteSetting { Name = input.Name.Trim(), ProductId = input.ProductId, RevisionId = input.RevisionId, FinalStepId = t.FinalStepId, Kind = "application", ParameterId = t.ParameterId, ParameterValue = input.ParameterValue };
        db.Add(r); await db.SaveChangesAsync(); await tx.CommitAsync(); return await Read(r.Id);
    }

    public async Task<object> Calculate(int id, decimal quantity, int? operationId)
    {
        // One repeatable-read snapshot keeps all four functions on the same version of the graph and prices.
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var r = await db.Set<TechRouteSetting>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id) ?? throw new ArgumentException("Маршрут не найден.");
        var tree = await Query($"SELECT coalesce(jsonb_agg(to_jsonb(f)), '[]')::text AS \"Value\" FROM find_tm({r.ProductId},{quantity},{id}) f");
        var materials = await Query($"SELECT coalesce(jsonb_agg(to_jsonb(f)), '[]')::text AS \"Value\" FROM sum_mr({r.ProductId},{quantity},{id}) f");
        var labor = await Query($"SELECT coalesce(jsonb_agg(to_jsonb(f)), '[]')::text AS \"Value\" FROM tr_to({r.ProductId},{quantity},{id}) f");
        var op = operationId ?? r.FinalStepId;
        var inputs = await Query($"SELECT coalesce(jsonb_agg(to_jsonb(f)), '[]')::text AS \"Value\" FROM mr_q({op},{quantity},{id}) f");
        var missing = materials.EnumerateArray().Where(x => x.GetProperty("price").ValueKind == JsonValueKind.Null).Select(x => x.GetProperty("name").GetString()).ToArray();
        decimal? materialCost = missing.Length > 0 ? null : materials.EnumerateArray().Sum(x => x.GetProperty("cost").GetDecimal());
        var laborCost = labor.EnumerateArray().Sum(x => x.GetProperty("cost").GetDecimal());
        var minutes = labor.EnumerateArray().Sum(x => x.GetProperty("minutes").GetDecimal());
        var total = materialCost + laborCost;
        await tx.CommitAsync();
        return new { tree, materials, labor, inputs, quantity, operationId = op, materialCost, laborCost, minutes, total, unitCost = total / quantity, missingPrices = missing };
    }
}
