import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { apiDelete, apiGet, apiPost } from "../api/client";
import type {
  ClassNodeDto,
  ClassParameterDto,
  CreateClassParameterDto,
  CreateEnumClassDto,
  CreateMeasureUnitDto,
  CreateParameterDefinitionDto,
  CreateParameterGroupDto,
  EnumClassDto,
  MeasureUnitDto,
  ParameterDefinitionDto,
  ParameterGroupDto,
} from "../types/api";
import { ParameterValueTypes } from "../types/api";

const valueTypeLabels: { v: number; label: string }[] = [
  { v: 1, label: "Целое число" },
  { v: 2, label: "Вещественное число" },
  { v: 3, label: "Строка" },
  { v: 4, label: "Дата и время" },
  { v: 5, label: "Перечисление" },
];

const enumClassValueLabels: { v: 1 | 2 | 3; label: string }[] = [
  { v: 1, label: "Строковые значения" },
  { v: 2, label: "Числовые значения" },
  { v: 3, label: "Иконки" },
];

function sectionTitle(text: string) {
  return (
    <h2 style={{ margin: "0 0 0.75rem", fontSize: "1.15rem", fontWeight: 700, color: "var(--warm)" }}>{text}</h2>
  );
}

export function SchemaPage() {
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [groups, setGroups] = useState<ParameterGroupDto[] | null>(null);
  const [definitions, setDefinitions] = useState<ParameterDefinitionDto[] | null>(null);
  const [measureUnits, setMeasureUnits] = useState<MeasureUnitDto[] | null>(null);
  const [enumClasses, setEnumClasses] = useState<EnumClassDto[] | null>(null);
  const [classNodes, setClassNodes] = useState<ClassNodeDto[] | null>(null);
  const [classParams, setClassParams] = useState<ClassParameterDto[] | null>(null);

  const [classNodeId, setClassNodeId] = useState<number | "">("");

  const refreshAll = useCallback(async () => {
    const [g, d, m, e, c] = await Promise.all([
      apiGet<ParameterGroupDto[]>("/api/parameter-groups"),
      apiGet<ParameterDefinitionDto[]>("/api/parameter-definitions"),
      apiGet<MeasureUnitDto[]>("/api/measure-units").catch(() => [] as MeasureUnitDto[]),
      apiGet<EnumClassDto[]>("/api/enum-classes"),
      apiGet<ClassNodeDto[]>("/api/classnodes"),
    ]);
    setGroups(g);
    setDefinitions(d);
    setMeasureUnits(m);
    setEnumClasses(e);
    setClassNodes(c);
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        await refreshAll();
      } catch (e) {
        if (!cancelled) setErr(e instanceof Error ? e.message : "Ошибка загрузки справочников");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [refreshAll]);

  useEffect(() => {
    if (classNodeId === "") {
      setClassParams(null);
      return;
    }
    let cancelled = false;
    (async () => {
      try {
        const p = await apiGet<ClassParameterDto[]>(`/api/classnodes/${classNodeId}/parameters`);
        if (!cancelled) setClassParams(p);
      } catch (e) {
        if (!cancelled) setErr(e instanceof Error ? e.message : "Ошибка параметров класса");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [classNodeId]);

  const definitionsAvailableForClass = useMemo(() => {
    if (!definitions || !classParams) return definitions ?? [];
    const used = new Set(classParams.map((x) => x.parameterDefinitionId));
    return definitions.filter((d) => !used.has(d.id));
  }, [definitions, classParams]);

  async function run(
    fn: () => Promise<void>,
  ) {
    setBusy(true);
    setErr(null);
    try {
      await fn();
      await refreshAll();
      if (classNodeId !== "") {
        setClassParams(await apiGet<ClassParameterDto[]>(`/api/classnodes/${classNodeId}/parameters`));
      }
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Ошибка");
    } finally {
      setBusy(false);
    }
  }

  const [gName, setGName] = useState("");
  const [gShort, setGShort] = useState("");
  const [gSort, setGSort] = useState(1);

  const [muName, setMuName] = useState("");
  const [muShort, setMuShort] = useState("");

  const [ecName, setEcName] = useState("");
  const [ecShort, setEcShort] = useState("");
  const [ecVt, setEcVt] = useState<1 | 2 | 3>(1);
  const [ecSort, setEcSort] = useState(1);
  const [ecMu, setEcMu] = useState<number | "">("");

  const [pdName, setPdName] = useState("");
  const [pdShort, setPdShort] = useState("");
  const [pdVt, setPdVt] = useState<number>(2);
  const [pdMu, setPdMu] = useState<number | "">("");
  const [pdEc, setPdEc] = useState<number | "">("");

  const [cpDef, setCpDef] = useState<number | "">("");
  const [cpGroup, setCpGroup] = useState<number | "">("");
  const [cpSort, setCpSort] = useState(1);
  const [cpReq, setCpReq] = useState(false);
  const [cpMin, setCpMin] = useState<number | "">("");
  const [cpMax, setCpMax] = useState<number | "">("");

  const selectedDef = useMemo(() => {
    if (cpDef === "" || !definitions) return null;
    return definitions.find((x) => x.id === cpDef) ?? null;
  }, [cpDef, definitions]);

  async function submitGroup(e: FormEvent) {
    e.preventDefault();
    const body: CreateParameterGroupDto = {
      name: gName.trim(),
      shortName: gShort.trim(),
      sortOrder: gSort,
    };
    await run(async () => {
      await apiPost<ParameterGroupDto, CreateParameterGroupDto>("/api/parameter-groups", body);
      setGName("");
      setGShort("");
      setGSort(1);
    });
  }

  async function submitMeasure(e: FormEvent) {
    e.preventDefault();
    const body: CreateMeasureUnitDto = { name: muName.trim(), shortName: muShort.trim() };
    await run(async () => {
      await apiPost<MeasureUnitDto, CreateMeasureUnitDto>("/api/measure-units", body);
      setMuName("");
      setMuShort("");
    });
  }

  async function submitEnumClass(e: FormEvent) {
    e.preventDefault();
    if (ecVt === 2 && ecMu === "") {
      setErr("Для числового перечисления выберите единицу измерения.");
      return;
    }
    const body: CreateEnumClassDto = {
      name: ecName.trim(),
      shortName: ecShort.trim(),
      valueType: ecVt,
      sortOrder: ecSort,
      measureUnitId: ecVt === 2 && ecMu !== "" ? Number(ecMu) : null,
    };
    await run(async () => {
      await apiPost<EnumClassDto, CreateEnumClassDto>("/api/enum-classes", body);
      setEcName("");
      setEcShort("");
      setEcVt(1);
      setEcSort(1);
      setEcMu("");
    });
  }

  async function submitDefinition(e: FormEvent) {
    e.preventDefault();
    const vt = pdVt as 1 | 2 | 3 | 4 | 5;
    const body: CreateParameterDefinitionDto = {
      name: pdName.trim(),
      shortName: pdShort.trim(),
      valueType: vt,
      measureUnitId:
        (vt === ParameterValueTypes.Integer || vt === ParameterValueTypes.Number) && pdMu !== ""
          ? Number(pdMu)
          : null,
      enumClassId: vt === ParameterValueTypes.Enum && pdEc !== "" ? Number(pdEc) : null,
    };
    await run(async () => {
      await apiPost<ParameterDefinitionDto, CreateParameterDefinitionDto>("/api/parameter-definitions", body);
      setPdName("");
      setPdShort("");
      setPdVt(2);
      setPdMu("");
      setPdEc("");
    });
  }

  async function submitClassParameter(e: FormEvent) {
    e.preventDefault();
    if (classNodeId === "" || cpDef === "") return;
    const body: CreateClassParameterDto = {
      parameterDefinitionId: Number(cpDef),
      parameterGroupId: cpGroup === "" ? null : Number(cpGroup),
      sortOrder: cpSort,
      isRequired: cpReq,
      minNumberValue:
        selectedDef &&
        (selectedDef.valueType === ParameterValueTypes.Integer ||
          selectedDef.valueType === ParameterValueTypes.Number) &&
        cpMin !== ""
          ? Number(cpMin)
          : null,
      maxNumberValue:
        selectedDef &&
        (selectedDef.valueType === ParameterValueTypes.Integer ||
          selectedDef.valueType === ParameterValueTypes.Number) &&
        cpMax !== ""
          ? Number(cpMax)
          : null,
    };
    await run(async () => {
      await apiPost<ClassParameterDto, CreateClassParameterDto>(
        `/api/classnodes/${classNodeId}/parameters`,
        body,
      );
      setCpDef("");
      setCpGroup("");
      setCpSort(1);
      setCpReq(false);
      setCpMin("");
      setCpMax("");
    });
  }

  async function removeClassParameter(row: ClassParameterDto) {
    if (row.isInherited) {
      setErr("Унаследованный параметр удаляется только у родительского класса.");
      return;
    }
    if (!confirm(`Убрать параметр «${row.parameterName}» у этого класса?`)) return;
    await run(async () => {
      await apiDelete(`/api/classnodes/${row.classNodeId}/parameters/${row.id}`);
    });
  }

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1.5rem", maxWidth: "900px" }}>
      <header>
        <h1 style={{ margin: 0, fontSize: "1.5rem" }}>Справочники параметров</h1>
        <p className="hint" style={{ marginTop: "0.35rem" }}>
          Группы (как «Подключение»), глобальные свойства вроде «Дальность действия», единицы измерения и привязка свойств к классу изделия.
        </p>
      </header>
      {err && <div className="error-banner">{err}</div>}

      <div className="card" style={{ padding: "1.15rem" }}>
        {sectionTitle("Единицы измерения")}
        <p className="hint" style={{ marginBottom: "0.75rem" }}>
          Например метр для дальности — добавьте при необходимости и выберите её в описании числового параметра.
        </p>
        <div className="table-wrap" style={{ marginBottom: "1rem", maxHeight: "200px" }}>
          <table className="data">
            <thead>
              <tr>
                <th>Название</th>
                <th>Кратко</th>
              </tr>
            </thead>
            <tbody>
              {measureUnits?.map((u) => (
                <tr key={u.id}>
                  <td>{u.name}</td>
                  <td className="mono">{u.shortName}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <form
          style={{ display: "grid", gap: "0.5rem", maxWidth: "420px" }}
          onSubmit={(e) => void submitMeasure(e)}
        >
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.5rem" }}>
            <div>
              <label>Название</label>
              <input className="input" required value={muName} onChange={(e) => setMuName(e.target.value)} />
            </div>
            <div>
              <label>Кратко</label>
              <input className="input" required value={muShort} onChange={(e) => setMuShort(e.target.value)} />
            </div>
          </div>
          <button type="submit" className="btn-ghost" disabled={busy}>
            Добавить единицу измерения
          </button>
        </form>
      </div>

      <div className="card" style={{ padding: "1.15rem" }}>
        {sectionTitle("Группы параметров")}
        <p className="hint" style={{ marginBottom: "0.75rem" }}>
          Логические блоки на карточке изделия (например «Подключение», «Радиоканал»).
        </p>
        <div className="table-wrap" style={{ marginBottom: "1rem", maxHeight: "220px" }}>
          <table className="data">
            <thead>
              <tr>
                <th>Название</th>
                <th>Кратко</th>
                <th>Порядок</th>
              </tr>
            </thead>
            <tbody>
              {groups?.map((x) => (
                <tr key={x.id}>
                  <td>{x.name}</td>
                  <td className="mono">{x.shortName}</td>
                  <td>{x.sortOrder}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <form style={{ display: "grid", gap: "0.5rem", maxWidth: "480px" }} onSubmit={(e) => void submitGroup(e)}>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.5rem" }}>
            <div>
              <label>Название группы</label>
              <input className="input" required value={gName} onChange={(e) => setGName(e.target.value)} />
            </div>
            <div>
              <label>Краткое имя (лат.)</label>
              <input className="input" required value={gShort} onChange={(e) => setGShort(e.target.value)} />
            </div>
          </div>
          <div style={{ maxWidth: "120px" }}>
            <label>Порядок сортировки</label>
            <input
              className="input mono"
              type="number"
              value={gSort}
              onChange={(e) => setGSort(Number.parseInt(e.target.value, 10) || 0)}
            />
          </div>
          <button type="submit" className="btn-primary" disabled={busy}>
            Создать группу
          </button>
        </form>
      </div>

      <div className="card" style={{ padding: "1.15rem" }}>
        {sectionTitle("Типы перечислений")}
        <p className="hint" style={{ marginBottom: "0.75rem" }}>
          Нужны для параметров типа «Перечисление». Значения списка задаются в API отдельно (например через Swagger) или уже есть в базе.
        </p>
        <div className="table-wrap" style={{ marginBottom: "1rem", maxHeight: "180px" }}>
          <table className="data">
            <thead>
              <tr>
                <th>Название</th>
                <th>Тип значений</th>
              </tr>
            </thead>
            <tbody>
              {enumClasses?.map((x) => (
                <tr key={x.id}>
                  <td>{x.name}</td>
                  <td>{enumClassValueLabels.find((t) => t.v === x.valueType)?.label ?? x.valueType}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <form style={{ display: "grid", gap: "0.5rem", maxWidth: "520px" }} onSubmit={(e) => void submitEnumClass(e)}>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.5rem" }}>
            <div>
              <label>Название</label>
              <input className="input" required value={ecName} onChange={(e) => setEcName(e.target.value)} />
            </div>
            <div>
              <label>Краткое имя</label>
              <input className="input" required value={ecShort} onChange={(e) => setEcShort(e.target.value)} />
            </div>
          </div>
          <div>
            <label>Тип значений в списке</label>
            <select className="input" value={ecVt} onChange={(e) => setEcVt(Number(e.target.value) as 1 | 2 | 3)}>
              {enumClassValueLabels.map((o) => (
                <option key={o.v} value={o.v}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>
          {ecVt === 2 && (
            <div>
              <label>Единица измерения (обязательно для числового списка)</label>
              <select
                className="input"
                value={ecMu === "" ? "" : String(ecMu)}
                onChange={(e) => setEcMu(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
              >
                <option value="">— выберите —</option>
                {measureUnits?.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.name} ({u.shortName})
                  </option>
                ))}
              </select>
            </div>
          )}
          <div style={{ maxWidth: "140px" }}>
            <label>Порядок</label>
            <input
              className="input mono"
              type="number"
              value={ecSort}
              onChange={(e) => setEcSort(Number.parseInt(e.target.value, 10) || 0)}
            />
          </div>
          <button type="submit" className="btn-ghost" disabled={busy}>
            Создать перечисление
          </button>
        </form>
      </div>

      <div className="card" style={{ padding: "1.15rem" }}>
        {sectionTitle("Описания параметров (глобальные свойства)")}
        <p className="hint" style={{ marginBottom: "0.75rem" }}>
          Здесь создаётся само свойство, например «Дальность действия» с типом «вещественное число» и единицей «м».
        </p>
        <div className="table-wrap" style={{ marginBottom: "1rem", maxHeight: "280px" }}>
          <table className="data">
            <thead>
              <tr>
                <th>Название</th>
                <th>Тип</th>
                <th>Ед. изм. / перечисление</th>
              </tr>
            </thead>
            <tbody>
              {definitions?.map((x) => (
                <tr key={x.id}>
                  <td>
                    <div style={{ fontWeight: 600 }}>{x.name}</div>
                    <div className="hint mono" style={{ fontSize: "0.8rem" }}>
                      {x.shortName}
                    </div>
                  </td>
                  <td>{valueTypeLabels.find((t) => t.v === x.valueType)?.label ?? x.valueType}</td>
                  <td>
                    {x.enumClassName ?? (x.measureUnitShortName ? `ЕИ: ${x.measureUnitShortName}` : "—")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <form style={{ display: "grid", gap: "0.65rem", maxWidth: "520px" }} onSubmit={(e) => void submitDefinition(e)}>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.5rem" }}>
            <div>
              <label>Название свойства</label>
              <input className="input" required value={pdName} onChange={(e) => setPdName(e.target.value)} />
            </div>
            <div>
              <label>Краткое имя</label>
              <input className="input" required value={pdShort} onChange={(e) => setPdShort(e.target.value)} />
            </div>
          </div>
          <div>
            <label>Тип значения</label>
            <select className="input" value={pdVt} onChange={(e) => setPdVt(Number(e.target.value))}>
              {valueTypeLabels.map((o) => (
                <option key={o.v} value={o.v}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>
          {(pdVt === ParameterValueTypes.Integer || pdVt === ParameterValueTypes.Number) && (
            <div>
              <label>Единица измерения (необязательно)</label>
              <select
                className="input"
                value={pdMu === "" ? "" : String(pdMu)}
                onChange={(e) => setPdMu(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
              >
                <option value="">— без единицы —</option>
                {measureUnits?.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.name} ({u.shortName})
                  </option>
                ))}
              </select>
            </div>
          )}
          {pdVt === ParameterValueTypes.Enum && (
            <div>
              <label>Тип перечисления</label>
              <select
                className="input"
                required
                value={pdEc === "" ? "" : String(pdEc)}
                onChange={(e) => setPdEc(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
              >
                <option value="">— выберите —</option>
                {enumClasses?.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>
          )}
          <button type="submit" className="btn-warm" disabled={busy}>
            Создать описание параметра
          </button>
        </form>
      </div>

      <div className="card" style={{ padding: "1.15rem" }}>
        {sectionTitle("Привязка к классу изделия")}
        <p className="hint" style={{ marginBottom: "0.75rem" }}>
          Выберите класс в иерархии и добавьте к нему свойство (из уже созданных описаний), укажите группу — ту же, что «Подключение», или новую.
        </p>
        <div style={{ marginBottom: "1rem" }}>
          <label>Класс изделия</label>
          <select
            className="input"
            style={{ maxWidth: "100%" }}
            value={classNodeId === "" ? "" : String(classNodeId)}
            onChange={(e) => setClassNodeId(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
          >
            <option value="">— выберите класс —</option>
            {classNodes
              ?.slice()
              .sort((a, b) => a.name.localeCompare(b.name, "ru"))
              .map((n) => (
                <option key={n.id} value={n.id}>
                  {n.name} ({n.shortName}){n.isTerminal ? " · конечный" : ""}
                </option>
              ))}
          </select>
        </div>

        {classNodeId !== "" && (
          <>
            <div className="table-wrap" style={{ marginBottom: "1rem", maxHeight: "320px" }}>
              <table className="data">
                <thead>
                  <tr>
                    <th>Параметр</th>
                    <th>Группа</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {classParams?.map((row) => (
                    <tr key={row.id}>
                      <td>
                        <div style={{ fontWeight: 600 }}>{row.parameterName}</div>
                        <div className="hint" style={{ fontSize: "0.8rem" }}>
                          {row.isInherited ? "унаследован" : "собственный"}
                        </div>
                      </td>
                      <td>{row.parameterGroupName ?? "—"}</td>
                      <td>
                        {!row.isInherited && (
                          <button
                            type="button"
                            className="btn-danger"
                            disabled={busy}
                            onClick={() => void removeClassParameter(row)}
                          >
                            Удалить
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <form
              style={{ display: "grid", gap: "0.65rem", maxWidth: "520px" }}
              onSubmit={(e) => void submitClassParameter(e)}
            >
              <div>
                <label>Описание параметра</label>
                <select
                  className="input"
                  required
                  value={cpDef === "" ? "" : String(cpDef)}
                  onChange={(e) => setCpDef(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
                >
                  <option value="">— выберите свойство —</option>
                  {definitionsAvailableForClass.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.name} ({d.shortName})
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label>Группа на карточке (необязательно)</label>
                <select
                  className="input"
                  value={cpGroup === "" ? "" : String(cpGroup)}
                  onChange={(e) => setCpGroup(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
                >
                  <option value="">— без группы —</option>
                  {groups?.map((g) => (
                    <option key={g.id} value={g.id}>
                      {g.name}
                    </option>
                  ))}
                </select>
              </div>
              <div style={{ maxWidth: "140px" }}>
                <label>Порядок в группе</label>
                <input
                  className="input mono"
                  type="number"
                  value={cpSort}
                  onChange={(e) => setCpSort(Number.parseInt(e.target.value, 10) || 0)}
                />
              </div>
              <label style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
                <input type="checkbox" checked={cpReq} onChange={(e) => setCpReq(e.target.checked)} />
                Обязательный параметр
              </label>
              {selectedDef &&
                (selectedDef.valueType === ParameterValueTypes.Integer ||
                  selectedDef.valueType === ParameterValueTypes.Number) && (
                  <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.5rem" }}>
                    <div>
                      <label>Мин. значение</label>
                      <input
                        className="input mono"
                        type="number"
                        step="any"
                        value={cpMin === "" ? "" : cpMin}
                        onChange={(e) => setCpMin(e.target.value === "" ? "" : Number.parseFloat(e.target.value))}
                      />
                    </div>
                    <div>
                      <label>Макс. значение</label>
                      <input
                        className="input mono"
                        type="number"
                        step="any"
                        value={cpMax === "" ? "" : cpMax}
                        onChange={(e) => setCpMax(e.target.value === "" ? "" : Number.parseFloat(e.target.value))}
                      />
                    </div>
                  </div>
                )}
              <button type="submit" className="btn-primary" disabled={busy || definitionsAvailableForClass.length === 0}>
                Добавить параметр к классу
              </button>
              {definitionsAvailableForClass.length === 0 && (
                <div className="hint">Все описания параметров уже привязаны к этом классу — создайте новое описание выше.</div>
              )}
            </form>
          </>
        )}
      </div>
    </div>
  );
}
