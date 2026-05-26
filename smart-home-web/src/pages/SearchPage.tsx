import { useEffect, useMemo, useState, type FormEvent } from "react";
import { apiGet, apiPost } from "../api/client";
import type {
  ClassNodeDto,
  ClassParameterDto,
  EnumValueDto,
  ProductFilterDto,
  ProductWithParametersDto,
} from "../types/api";
import { ParameterValueTypes } from "../types/api";
import { formatDateTimeLocal } from "../utils/datetime";

export function SearchPage() {
  const [leaves, setLeaves] = useState<ClassNodeDto[] | null>(null);
  const [classId, setClassId] = useState<number | "">("");
  const [withDesc, setWithDesc] = useState(true);
  const [params, setParams] = useState<ClassParameterDto[] | null>(null);
  const [paramId, setParamId] = useState<number | "">("");
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [results, setResults] = useState<ProductWithParametersDto[] | null>(null);

  const [intV, setIntV] = useState<number | "">("");
  const [numFrom, setNumFrom] = useState<number | "">("");
  const [numTo, setNumTo] = useState<number | "">("");
  const [strContains, setStrContains] = useState("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [enumId, setEnumId] = useState<number | "">("");
  const [enumOptions, setEnumOptions] = useState<EnumValueDto[]>([]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const l = await apiGet<ClassNodeDto[]>("/api/classnodes/leaves");
        if (!cancelled) {
          setLeaves(l);
          if (l.length && classId === "") setClassId(l[0]!.id);
        }
      } catch (e) {
        if (!cancelled) setErr(e instanceof Error ? e.message : "Ошибка");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const classIdNum = classId === "" ? null : classId;

  useEffect(() => {
    if (classIdNum == null) {
      setParams(null);
      return;
    }
    let cancelled = false;
    (async () => {
      try {
        const p = await apiGet<ClassParameterDto[]>(`/api/classnodes/${classIdNum}/parameters`);
        if (!cancelled) {
          setParams(p);
          setParamId("");
        }
      } catch (e) {
        if (!cancelled) setErr(e instanceof Error ? e.message : "Ошибка параметров");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [classIdNum]);

  const selectedParam = useMemo(() => {
    if (paramId === "" || !params) return null;
    return params.find((x) => x.id === paramId) ?? null;
  }, [paramId, params]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      if (!selectedParam?.enumClassId) {
        setEnumOptions([]);
        return;
      }
      try {
        const v = await apiGet<EnumValueDto[]>(`/api/enum-classes/${selectedParam.enumClassId}/values`);
        if (!cancelled) setEnumOptions(v);
      } catch {
        if (!cancelled) setEnumOptions([]);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [selectedParam?.enumClassId]);

  async function runSearch(e: FormEvent) {
    e.preventDefault();
    if (classIdNum == null) return;
    setLoading(true);
    setErr(null);
    const body: ProductFilterDto = { classNodeId: classIdNum };
    if (paramId !== "") {
      body.classParameterId = Number(paramId);
      if (selectedParam) {
        if (selectedParam.valueType === ParameterValueTypes.Integer && intV !== "") {
          body.integerValue = Number(intV);
        }
        if (selectedParam.valueType === ParameterValueTypes.Number) {
          if (numFrom !== "") body.numberFrom = Number(numFrom);
          if (numTo !== "") body.numberTo = Number(numTo);
        }
        if (selectedParam.valueType === ParameterValueTypes.String && strContains.trim()) {
          body.stringContains = strContains.trim();
        }
        if (selectedParam.valueType === ParameterValueTypes.DateTime) {
          const df = formatDateTimeLocal(dateFrom);
          const dt = formatDateTimeLocal(dateTo);
          if (df) body.dateFrom = df;
          if (dt) body.dateTo = dt;
        }
        if (selectedParam.valueType === ParameterValueTypes.Enum && enumId !== "") {
          body.enumValueId = Number(enumId);
        }
      }
    }

    const path = withDesc ? "/api/product-search/filter-with-descendants" : "/api/product-search/filter";
    try {
      const data = await apiPost<ProductWithParametersDto[], ProductFilterDto>(path, body);
      setResults(data);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Ошибка поиска");
      setResults(null);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
      <header>
        <h1 className="page-title">Поиск</h1>
        <p className="hint" style={{ marginTop: "0.35rem" }}>
          Выберите класс изделия и при необходимости уточните фильтр по одному из параметров этого класса.
        </p>
      </header>
      {err && <div className="error-banner">{err}</div>}

      <form className="card" style={{ display: "grid", gap: "0.85rem", maxWidth: "560px" }} onSubmit={(e) => void runSearch(e)}>
        <div>
          <label htmlFor="sc">Класс изделия</label>
          <select
            id="sc"
            className="input"
            value={classId === "" ? "" : String(classId)}
            onChange={(e) => setClassId(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
          >
            {leaves?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </div>
        <label style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
          <input type="checkbox" checked={withDesc} onChange={(e) => setWithDesc(e.target.checked)} />
          Учитывать подклассы (потомков)
        </label>
        <div>
          <label htmlFor="sp">Параметр для фильтра (необязательно)</label>
          <select
            id="sp"
            className="input"
            value={paramId === "" ? "" : String(paramId)}
            onChange={(e) => setParamId(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
          >
            <option value="">— все изделия класса —</option>
            {params?.map((p) => (
              <option key={p.id} value={p.id}>
                {p.parameterName} ({p.parameterShortName})
              </option>
            ))}
          </select>
        </div>

        {selectedParam && (
          <div
            className="card-nested" style={{ display: "grid", gap: "0.65rem" }}
          >
            <div className="hint">
              Тип:{" "}
              {selectedParam.valueType === ParameterValueTypes.Integer && "целое"}
              {selectedParam.valueType === ParameterValueTypes.Number && "число"}
              {selectedParam.valueType === ParameterValueTypes.String && "строка"}
              {selectedParam.valueType === ParameterValueTypes.DateTime && "дата и время"}
              {selectedParam.valueType === ParameterValueTypes.Enum && "перечисление"}
            </div>
            {selectedParam.valueType === ParameterValueTypes.Integer && (
              <div>
                <label>Значение</label>
                <input
                  className="input mono"
                  type="number"
                  step={1}
                  value={intV === "" ? "" : intV}
                  onChange={(e) => setIntV(e.target.value === "" ? "" : Number.parseInt(e.target.value, 10))}
                />
              </div>
            )}
            {selectedParam.valueType === ParameterValueTypes.Number && (
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.65rem" }}>
                <div>
                  <label>От</label>
                  <input
                    className="input mono"
                    type="number"
                    step="any"
                    value={numFrom === "" ? "" : numFrom}
                    onChange={(e) => setNumFrom(e.target.value === "" ? "" : Number.parseFloat(e.target.value))}
                  />
                </div>
                <div>
                  <label>До</label>
                  <input
                    className="input mono"
                    type="number"
                    step="any"
                    value={numTo === "" ? "" : numTo}
                    onChange={(e) => setNumTo(e.target.value === "" ? "" : Number.parseFloat(e.target.value))}
                  />
                </div>
              </div>
            )}
            {selectedParam.valueType === ParameterValueTypes.String && (
              <div>
                <label>Содержит текст</label>
                <input className="input" value={strContains} onChange={(e) => setStrContains(e.target.value)} />
              </div>
            )}
            {selectedParam.valueType === ParameterValueTypes.DateTime && (
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.65rem" }}>
                <div>
                  <label>С даты</label>
                  <input
                    className="input mono"
                    type="datetime-local"
                    value={dateFrom}
                    onChange={(e) => setDateFrom(e.target.value)}
                  />
                </div>
                <div>
                  <label>По дату</label>
                  <input
                    className="input mono"
                    type="datetime-local"
                    value={dateTo}
                    onChange={(e) => setDateTo(e.target.value)}
                  />
                </div>
                <div className="hint" style={{ gridColumn: "1 / -1" }}>
                  Можно указать только одну границу интервала — как в API.
                </div>
              </div>
            )}
            {selectedParam.valueType === ParameterValueTypes.Enum && (
              <div>
                <label>Значение списка</label>
                <select
                  className="input"
                  value={enumId === "" ? "" : String(enumId)}
                  onChange={(e) => setEnumId(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
                >
                  <option value="">— выберите —</option>
                  {enumOptions.map((v) => (
                    <option key={v.id} value={v.id}>
                      {v.displayName ?? v.stringValue ?? `#${v.id}`}
                    </option>
                  ))}
                </select>
              </div>
            )}
          </div>
        )}

        <button type="submit" className="btn-primary" disabled={loading || classIdNum == null}>
          {loading ? "Поиск…" : "Найти"}
        </button>
      </form>

      {results && (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ padding: "0.85rem 1rem", borderBottom: "1px solid var(--border)" }}>
            <span style={{ fontWeight: 600 }}>Результаты</span>
            <span className="hint" style={{ marginLeft: "0.5rem" }}>
              {results.length} шт.
            </span>
          </div>
          <div className="table-wrap" style={{ border: "none" }}>
            <table className="data">
              <thead>
                <tr>
                  <th>Название</th>
                  <th>Класс</th>
                  <th>Кратко</th>
                </tr>
              </thead>
              <tbody>
                {results.map((r) => (
                  <tr key={r.id}>
                    <td style={{ fontWeight: 600 }}>{r.name}</td>
                    <td>{r.classNodeName}</td>
                    <td className="mono">{r.shortName}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
