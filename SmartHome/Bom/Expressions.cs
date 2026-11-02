using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartHome.Bom;

// A small typed expression grammar. No reflection, dynamic code, SQL or script execution.
public sealed class Expressions
{
    private readonly List<string> tokens = [];
    private int cursor;
    private readonly Func<string, object> lookup;
    private readonly bool typeOnly;
    public static HashSet<string> References(string text) => Regex.Matches(Regex.Replace(text, "\"(?:[^\"\\\\]|\\\\.)*\"", ""), @"\bp\d+\b")
        .Select(m => m.Value).ToHashSet();
    public Expressions(string text, Func<string, object> lookup, bool typeOnly = false)
    {
        if (text.Length > 1024) throw new ArgumentException("Выражение длиннее 1024 символов.");
        this.lookup = lookup; this.typeOnly = typeOnly;
        var pattern = "\\s*(p[0-9]+|has_[A-Za-z0-9_]+|[0-9]+(?:\\.[0-9]+)?|\"(?:[^\"\\\\]|\\\\.)*\"|==|!=|<=|>=|&&|\\|\\||[()+*/%<>!\\-]|AND\\b|OR\\b|NOT\\b|true\\b|false\\b)";
        var at = 0;
        while (at < text.Length && !string.IsNullOrWhiteSpace(text[at..]))
        {
            var m = Regex.Match(text[at..], pattern);
            if (!m.Success || m.Index != 0) throw new ArgumentException($"Неизвестный символ выражения около: {text[at..]}");
            tokens.Add(m.Groups[1].Value); at += m.Length;
            if (tokens.Count > 256) throw new ArgumentException("Слишком сложное выражение.");
        }
    }
    public object Run() { var result = Parse(0, 0); if (cursor != tokens.Count) throw new ArgumentException("Лишние символы выражения."); return result; }
    private static int Priority(string op) => op switch
    {
        "OR" or "||" => 1,
        "AND" or "&&" => 2,
        "==" or "!=" => 3,
        "<" or ">" or "<=" or ">=" => 4,
        "+" or "-" => 5,
        "*" or "/" or "%" => 6,
        _ => -1
    };
    private object Parse(int min, int depth)
    {
        if (depth > 32 || cursor >= tokens.Count) throw new ArgumentException("Незаконченное или слишком глубокое выражение.");
        var t = tokens[cursor++]; object left;
        if (t == "(") { left = Parse(0, depth + 1); if (cursor >= tokens.Count || tokens[cursor++] != ")") throw new ArgumentException("Не закрыта скобка."); }
        else if (t is "!" or "NOT") left = !Boolean(Parse(7, depth + 1));
        else if (t == "-") left = -Number(Parse(7, depth + 1));
        else if (t is "true" or "false") left = t == "true";
        else if (t.StartsWith('"'))
        {
            try { left = JsonSerializer.Deserialize<string>(t)!; }
            catch (JsonException) { throw new ArgumentException("Неверная строковая константа в выражении."); }
        }
        else if (decimal.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var n)) left = n;
        else left = lookup(t);
        while (cursor < tokens.Count && Priority(tokens[cursor]) >= min)
        {
            var op = tokens[cursor++]; var right = Parse(Priority(op) + 1, depth + 1);
            left = Apply(op, left, right);
        }
        return left;
    }
    private object Apply(string op, object l, object r)
    {
        checked
        {
            return op switch
            {
                "AND" or "&&" => Boolean(l) & Boolean(r),
                "OR" or "||" => Boolean(l) | Boolean(r),
                "==" or "!=" => CompareEquality(op, l, r),
                "<" => Compare(l, r) < 0,
                ">" => Compare(l, r) > 0,
                "<=" => Compare(l, r) <= 0,
                ">=" => Compare(l, r) >= 0,
                "+" => Number(l) + Number(r),
                "-" => Number(l) - Number(r),
                "*" => typeOnly ? CheckNumbers(l, r) : Number(l) * Number(r),
                "/" or "%" => Divide(op, Number(l), Number(r)),
                _ => throw new ArgumentException("Неизвестная операция.")
            };
        }
    }
    private decimal Divide(string op, decimal l, decimal r)
    {
        if (typeOnly) return 1;
        if (r == 0) throw new ArgumentException("Деление на ноль в формуле.");
        return op == "/" ? l / r : l % r;
    }
    private static decimal CheckNumbers(object l, object r) { Number(l); Number(r); return 1; }
    private static bool CompareEquality(string op, object l, object r)
    {
        if (l.GetType() != r.GetType()) throw new ArgumentException("Сравниваются несовместимые типы.");
        return op == "==" ? l.Equals(r) : !l.Equals(r);
    }
    private static int Compare(object l, object r) => l is decimal a && r is decimal b ? a.CompareTo(b)
        : l is DateTime d && r is DateTime e ? d.CompareTo(e) : throw new ArgumentException("Для сравнения нужны числа или даты одного типа.");
    public static decimal Number(object x) => x is decimal n ? n : throw new ArgumentException("Ожидалось число.");
    public static bool Boolean(object x) => x is bool b ? b : throw new ArgumentException("Ожидалось логическое условие.");
    public static object Value(JsonElement x) => x.ValueKind switch
    {
        JsonValueKind.Number => x.TryGetDecimal(out var number) ? number : throw new ArgumentException("Число параметра выходит за диапазон decimal."),
        JsonValueKind.String => x.GetString()!,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw new ArgumentException("Недопустимое значение параметра.")
    };
}
