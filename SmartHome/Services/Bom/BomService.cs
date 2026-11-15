using SmartHome.Models.Bom;
using SmartHome.Bom;
using SmartHome.DTOs.Bom;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace SmartHome.Services.Bom;

public sealed class BomService(AppDbContext db)
{
    public async Task Lock() => await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(734021,1)");
    private async Task<T> Scalar<T>(string sql, params NpgsqlParameter[] parameters)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await db.Database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = sql;
        foreach (var parameter in parameters) command.Parameters.Add(parameter);
        return (T)(await command.ExecuteScalarAsync() ?? throw new ArgumentException("Исторический документ не найден."));
    }
    private async Task<RevisionDto> Hydrate(BomRevision r) => new(r.Id, r.ProductId, r.UnitId, r.PreviousId, r.BaseRevisionId, r.Reason, r.CreatedAt,
        await Scalar<string>("SELECT bom_read_document(@id)::text", new NpgsqlParameter("id", r.Id)));
    public async Task<RevisionDto> Revision(int id) => await Hydrate(await db.Set<BomRevision>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && !x.IsConfigured && x.IsFinalized)
        ?? throw new ArgumentException("Версия шаблона не найдена."));
    public async Task<RevisionDto?> Latest(int productId)
    {
        var r = await db.Set<BomRevision>().AsNoTracking().Where(x => x.ProductId == productId && !x.IsConfigured && x.IsFinalized).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        return r == null ? null : await Hydrate(r);
    }
    public async Task<SpecDocument> Effective(RevisionDto r, int depth = 0)
    {
        if (depth > 32) throw new ArgumentException("Слишком длинная цепочка модификаций.");
        var own = BomJson.Decode<SpecDocument>(r.Document);
        if (r.BaseRevisionId is not int b) return own;
        var basis = await Effective(await Revision(b), depth + 1);
        foreach (var p in basis.Positions) p.Inherited = true;
        var changed = own.Positions.Select(x => x.Id).ToHashSet();
        own.Positions = basis.Positions.Where(x => !changed.Contains(x.Id)).Concat(own.Positions).Where(x => !x.Excluded).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToList();
        return own;
    }
    public async Task<List<SpecParameter>> Parameters(int productId)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == productId) ?? throw new ArgumentException("Изделие не найдено.");
        var ps = await db.ClassParameters.AsNoTracking().Include(x => x.ParameterDefinition).ThenInclude(x => x.EnumClass).ThenInclude(x => x!.Values)
            .Where(x => x.ClassNodeId == product.ClassNodeId).OrderBy(x => x.SortOrder).ToListAsync();
        var vs = await db.ProductParameterValues.AsNoTracking().Where(x => x.ProductId == productId).ToListAsync();
        return ps.Select(p =>
        {
            var d = p.ParameterDefinition; var v = vs.SingleOrDefault(x => x.ClassParameterId == p.Id);
            object? val = d.ValueType switch
            {
                ParameterValueType.Integer => v?.IntegerValue,
                ParameterValueType.Number => v?.NumberValue,
                ParameterValueType.String => v?.StringValue,
                ParameterValueType.DateTime => v?.DateTimeValue,
                ParameterValueType.Enum => v?.EnumValueId,
                _ => null
            };
            return new SpecParameter($"p{d.Id}", d.Id, d.Name, (int)d.ValueType, p.IsRequired, p.MinNumberValue, p.MaxNumberValue,
                d.EnumClass?.Values.Select(e => e.Id).ToArray() ?? [], val == null ? null : JsonSerializer.SerializeToElement(val, BomJson.Options))
            { EnumLabels = d.EnumClass?.Values.ToDictionary(e => e.Id, e => e.DisplayName ?? e.StringValue ?? e.NumberValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? $"#{e.Id}") ?? [] };
        }).ToList();
    }
    private async Task<int[]> Classes(int id)
    {
        var all = await db.ClassNodes.AsNoTracking().ToDictionaryAsync(x => x.Id);
        List<int> result = [];
        for (int? at = id; at != null; at = all[at.Value].ParentId) result.Add(at.Value);
        return result.ToArray();
    }
    public async Task<RevisionDto> Save(int productId, SpecWrite input, int? baseId = null)
    {
        var current = await Latest(productId);
        if (current?.Id != input.ExpectedRevisionId) throw new InvalidOperationException("Спецификация уже изменена. Обновите страницу.");
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 512) throw new ArgumentException("Укажите причину изменения (до 512 символов).");
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == productId) ?? throw new ArgumentException("Изделие не найдено.");
        var unit = await db.MeasureUnits.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.UnitId) ?? throw new ArgumentException("Выберите единицу количества изделия.");
        baseId ??= current?.BaseRevisionId;
        var basis = baseId == null ? null : await Effective(await Revision(baseId.Value));
        var catalogParameters = await Parameters(productId);
        var existingParameters = basis?.Parameters ?? (current == null ? null : (await Effective(current)).Parameters);
        var parameters = input.Parameters ?? (existingParameters == null ? catalogParameters : existingParameters.Concat(catalogParameters.Where(p => !existingParameters.Any(e => e.DefinitionId == p.DefinitionId))).ToList());
        var doc = new SpecDocument
        {
            ProductName = product.Name,
            UnitName = unit.ShortName,
            Classes = await Classes(product.ClassNodeId),
            Positions = input.Positions,
            Rules = input.Rules,
            Calculations = input.Calculations,
            Parameters = parameters
        };
        foreach (var p in doc.Parameters)
        {
            if (p.Key != $"p{p.DefinitionId}" || p.Min > p.Max || p.EnumIds.Distinct().Count() != p.EnumIds.Length)
                throw new ArgumentException("Неверный ключ, диапазон или перечисление параметра.");
            if (p.Default is { } value && value.ValueKind != JsonValueKind.Null)
            {
                var typed = Typed(p, value); ValidateType(p, typed);
                if (typed is decimal n && (p.Min != null && n < p.Min || p.Max != null && n > p.Max || p.Type == 5 && !p.EnumIds.Contains(checked((int)n))))
                    throw new ArgumentException($"Значение по умолчанию параметра {p.Name} вне допустимых значений.");
            }
        }
        if (doc.Positions.Count > 500 || doc.Rules.Count > 100 || doc.Calculations.Count > 100) throw new ArgumentException("Слишком много позиций или правил.");
        foreach (var p in doc.Positions.Where(x => !x.Excluded))
        {
            if (p.Amount <= 0 || p.Amount > 1_000_000_000 || p.ForQuantity <= 0 || p.ForQuantity > 1_000_000_000 || p.Amount / p.ForQuantity > 1_000_000_000)
                throw new ArgumentException("Норма и знаменатель должны быть положительными и ограниченными.");
            if (p.RevisionId == 0) p.RevisionId = (await Latest(p.ComponentId))?.Id ?? throw new ArgumentException("Сначала задайте единицу и сохраните спецификацию компонента.");
            var child = await Revision(p.RevisionId);
            if (child.ProductId != p.ComponentId || child.UnitId != p.UnitId) throw new ArgumentException("Единица или версия не соответствует компоненту.");
            if (p.ChildVariantId is int cv)
            {
                var v = await Variant(cv);
                if (v.RevisionId != p.RevisionId) throw new ArgumentException("Вложенный вариант относится к другой версии компонента.");
                if (p.Bindings.Count != 0) throw new ArgumentException("Выберите вариант или передачу параметров, а не оба способа.");
            }
            var childDoc = await Effective(child);
            foreach (var binding in p.Bindings)
            {
                var target = childDoc.Parameters.SingleOrDefault(x => x.Key == binding.Key) ?? throw new ArgumentException("Неизвестный параметр вложенной сборки.");
                var value = Eval(binding.Value, Sample(doc.Parameters), null, true);
                ValidateType(target, value);
            }
        }
        var effectivePositions = (basis?.Positions ?? []).Where(x => !doc.Positions.Any(o => o.Id == x.Id)).Concat(doc.Positions).Where(x => !x.Excluded).ToList();
        CheckExpressions(doc, effectivePositions);
        var revisionId = await Scalar<int>("SELECT register_change(@product,@unit,@previous,@base,@document,@reason)",
            new NpgsqlParameter("product", productId), new NpgsqlParameter("unit", input.UnitId),
            new NpgsqlParameter("previous", NpgsqlDbType.Integer) { Value = (object?)current?.Id ?? DBNull.Value },
            new NpgsqlParameter("base", NpgsqlDbType.Integer) { Value = (object?)baseId ?? DBNull.Value },
            new NpgsqlParameter("document", NpgsqlDbType.Jsonb) { Value = BomJson.Encode(doc) }, new NpgsqlParameter("reason", input.Reason.Trim()));
        return await Revision(revisionId);
    }
    private static Dictionary<string, object> Sample(List<SpecParameter> ps) => ps.ToDictionary(x => x.Key, x => x.Type switch { 1 or 2 or 5 => (object)1m, 4 => DateTime.UnixEpoch, _ => "sample" });
    private static object Eval(string text, Dictionary<string, object> values, HashSet<int>? included = null, bool typeOnly = false) =>
        new Expressions(text, key =>
        {
            if (key.StartsWith("has_") && int.TryParse(key[4..], out var id) && included != null) { if (typeOnly && !included.Contains(id)) throw new ArgumentException($"Неизвестный компонент {id} в ограничении."); return included.Contains(id); }
            return values.TryGetValue(key, out var v) ? v : throw new ArgumentException($"Неизвестный или незаполненный параметр {key}.");
        }, typeOnly).Run();
    private static void CheckExpressions(SpecDocument doc, List<SpecPosition> positions)
    {
        var sample = Sample(doc.Parameters); var included = positions.Select(x => x.ComponentId).ToHashSet();
        foreach (var p in positions)
        {
            if (!string.IsNullOrWhiteSpace(p.Condition)) Expressions.Boolean(Eval(p.Condition, sample, null, true));
            if (!string.IsNullOrWhiteSpace(p.Formula)) Expressions.Number(Eval(p.Formula, sample, null, true));
        }
        foreach (var rule in doc.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Message) || rule.Message.Length > 512) throw new ArgumentException("Задайте сообщение ограничения.");
            Expressions.Boolean(Eval(rule.Expression, sample, included, true));
        }
        foreach (var c in doc.Calculations)
        {
            var parameter = doc.Parameters.SingleOrDefault(x => x.Key == c.Key) ?? throw new ArgumentException("Неизвестный вычисляемый параметр.");
            ValidateType(parameter, Eval(c.Value, sample, null, true));
        }
        var done = doc.Parameters.Select(x => x.Key).Where(x => !doc.Calculations.ContainsKey(x)).ToHashSet();
        var pending = doc.Calculations.ToDictionary();
        while (pending.Count > 0)
        {
            var ready = pending.Where(x => Expressions.References(x.Value).All(done.Contains)).Select(x => x.Key).ToList();
            if (ready.Count == 0) throw new ArgumentException("Циклическая зависимость вычисляемых параметров.");
            foreach (var key in ready) { done.Add(key); pending.Remove(key); }
        }
    }
    private static void ValidateType(SpecParameter p, object v)
    {
        var valid = p.Type switch
        {
            1 => v is decimal n && n == decimal.Truncate(n) && n >= int.MinValue && n <= int.MaxValue,
            2 => v is decimal,
            3 => v is string s && s.Length <= 1024 && (!p.Required || !string.IsNullOrWhiteSpace(s)),
            4 => v is DateTime,
            5 => v is decimal e && e == decimal.Truncate(e),
            _ => false
        };
        if (!valid) throw new ArgumentException($"Неверный тип параметра {p.Name} ({p.Key}).");
    }
    private static object Typed(SpecParameter p, JsonElement value)
    {
        if (p.Type == 4)
        {
            var text = value.ValueKind == JsonValueKind.String ? value.GetString()! : "";
            if (!System.Text.RegularExpressions.Regex.IsMatch(text, @"(Z|[+-]\d{2}:\d{2})$") || !DateTimeOffset.TryParse(text, out var d)) throw new ArgumentException("Дата параметра должна содержать часовой пояс.");
            return d.UtcDateTime;
        }
        return Expressions.Value(value);
    }
    public async Task<ResolvedNode> Resolve(int revisionId, Dictionary<string, JsonElement> supplied, HashSet<int>? path = null, int depth = 0)
    {
        var r = await Revision(revisionId); var doc = await Effective(r);
        path = path == null ? [] : new HashSet<int>(path);
        if (depth > 32 || !path.Add(r.ProductId)) throw new ArgumentException("Цикл или глубина состава превышает 32.");
        var values = new Dictionary<string, object>();
        foreach (var key in supplied.Keys) if (!doc.Parameters.Any(p => p.Key == key) || doc.Calculations.ContainsKey(key)) throw new ArgumentException($"Неизвестный или вычисляемый параметр {key}.");
        foreach (var p in doc.Parameters.Where(x => !doc.Calculations.ContainsKey(x.Key)))
        {
            JsonElement? v = supplied.TryGetValue(p.Key, out var x) ? x : p.Default;
            if (v != null && v.Value.ValueKind != JsonValueKind.Null) values[p.Key] = Typed(p, v.Value);
        }
        var pending = doc.Calculations.ToDictionary();
        while (pending.Count > 0)
        {
            var ready = pending.Where(x => Expressions.References(x.Value).All(values.ContainsKey)).ToList();
            if (ready.Count == 0) throw new ArgumentException("Цикл вычислений или не заполнены исходные параметры.");
            foreach (var c in ready) { values[c.Key] = Eval(c.Value, values); pending.Remove(c.Key); }
        }
        foreach (var p in doc.Parameters)
        {
            if (!values.TryGetValue(p.Key, out var v)) { if (p.Required) throw new ArgumentException($"Заполните обязательный параметр {p.Name} ({p.Key})."); continue; }
            ValidateType(p, v);
            if (v is decimal n && (p.Min != null && n < p.Min || p.Max != null && n > p.Max || p.Type == 5 && !p.EnumIds.Contains((int)n))) throw new ArgumentException($"Значение параметра {p.Name} вне допустимого диапазона или перечисления.");
        }
        var active = doc.Positions.Where(x => !x.Excluded && (string.IsNullOrWhiteSpace(x.Condition) || Expressions.Boolean(Eval(x.Condition, values)))).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToList();
        var included = active.Select(x => x.ComponentId).ToHashSet();
        foreach (var rule in doc.Rules) if (!Expressions.Boolean(Eval(rule.Expression, values, included))) throw new ArgumentException(rule.Message);
        var result = new ResolvedNode
        {
            ProductId = r.ProductId,
            RevisionId = r.Id,
            Name = doc.ProductName,
            UnitId = r.UnitId,
            Unit = doc.UnitName,
            Classes = doc.Classes,
            Source = doc,
            Values = values.ToDictionary(x => x.Key, x => JsonSerializer.SerializeToElement(x.Value, BomJson.Options))
        };
        foreach (var pos in active)
        {
            var amount = string.IsNullOrWhiteSpace(pos.Formula) ? pos.Amount : Expressions.Number(Eval(pos.Formula, values));
            if (amount <= 0 || amount > 1_000_000_000 || pos.ForQuantity <= 0) throw new ArgumentException("Норма должна быть положительной и не больше 1000000000.");
            ResolvedNode child;
            if (pos.ChildVariantId is int cv)
            {
                child = BomJson.Decode<ResolvedNode>((await Variant(cv)).Snapshot);
                child.ConfiguredRevisionId = cv;
                CheckTree(child, path, depth + 1);
            }
            else
            {
                var bindings = pos.Bindings.ToDictionary(x => x.Key, x => JsonSerializer.SerializeToElement(Eval(x.Value, values), BomJson.Options));
                child = await Resolve(pos.RevisionId, bindings, path, depth + 1);
            }
            child.Amount = amount; child.ForQuantity = pos.ForQuantity;
            child.PositionId = pos.Id; child.Quantity = checked(amount / pos.ForQuantity);
            if (child.Quantity <= 0 || child.Quantity > 1_000_000_000) throw new ArgumentException("Относительная норма вне диапазона (0; 1000000000].");
            result.Children.Add(child);
            if (Count(result) > 10000) throw new ArgumentException("Состав превышает 10000 узлов.");
        }
        if (Count(result) > 10000) throw new ArgumentException("Состав превышает 10000 узлов.");
        return result;
    }
    private static int Count(ResolvedNode n) => 1 + n.Children.Sum(Count);
    private static void CheckTree(ResolvedNode n, HashSet<int> path, int depth)
    {
        var copy = new HashSet<int>(path);
        if (depth > 32 || !copy.Add(n.ProductId)) throw new ArgumentException("Цикл во вложенном варианте.");
        foreach (var c in n.Children) CheckTree(c, copy, depth + 1);
    }
    public async Task<VariantDto> Variant(int id)
    {
        var v = await db.Set<BomRevision>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsConfigured && x.IsFinalized) ?? throw new ArgumentException("Исполнение не найдено.");
        return new(v.Id, v.SourceRevisionId!.Value, v.Name ?? "Вложенное исполнение", v.CreatedAt,
            await Scalar<string>("SELECT bom_read_variant(@id)::text", new NpgsqlParameter("id", id)));
    }
    public async Task<VariantDto> CreateVariant(VariantWrite input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 256) throw new ArgumentException("Укажите имя варианта до 256 символов.");
        var tree = await Resolve(input.RevisionId, input.Values);
        var id = await Scalar<int>("SELECT bom_create_variant(@revision,@name,@snapshot)",
            new NpgsqlParameter("revision", input.RevisionId), new NpgsqlParameter("name", input.Name.Trim()),
            new NpgsqlParameter("snapshot", NpgsqlDbType.Jsonb) { Value = BomJson.Encode(tree) });
        return await Variant(id);
    }
    public Task<object> Tree(int id, decimal quantity) => Query("find_bom", ("id", id, NpgsqlDbType.Integer), ("q", quantity, NpgsqlDbType.Numeric));
    public Task<object> Totals(int id, decimal quantity, int? classId) => Query("calc_bom", ("id", id, NpgsqlDbType.Integer), ("q", quantity, NpgsqlDbType.Numeric), ("c", classId, NpgsqlDbType.Integer));
    public Task<object> OrderTotals(int id, int? classId) => Query("calc_order_bom", ("id", id, NpgsqlDbType.Integer), ("c", classId, NpgsqlDbType.Integer));

    private async Task<object> Query(string function, params (string name, object? value, NpgsqlDbType type)[] args)
    {
        await db.Database.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand($"SELECT coalesce(jsonb_agg(t),'[]'::jsonb)::text FROM {function}({string.Join(',', args.Select(x => "@" + x.name))}) t", (NpgsqlConnection)db.Database.GetDbConnection());
        foreach (var a in args) cmd.Parameters.AddWithValue(a.name, a.type, a.value ?? DBNull.Value);
        using var document = JsonDocument.Parse((string)(await cmd.ExecuteScalarAsync())!);
        return document.RootElement.Clone();
    }
    public async Task<object> CreateModification(ModificationWrite input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.ShortName) || input.Name.Length > 256 || input.ShortName.Length > 128) throw new ArgumentException("Укажите имя и обозначение модификации.");
        var r = await Revision(input.BaseRevisionId);
        var basis = await db.Products.AsNoTracking().SingleAsync(x => x.Id == r.ProductId);
        var p = new Product { Name = input.Name.Trim(), ShortName = input.ShortName.Trim(), ClassNodeId = basis.ClassNodeId }; db.Add(p); await db.SaveChangesAsync();
        var doc = await Effective(r);
        var created = await Save(p.Id, new SpecWrite { UnitId = r.UnitId, Reason = input.Reason, Rules = doc.Rules, Calculations = doc.Calculations }, r.Id);
        return new { productId = p.Id, revision = created };
    }
}
