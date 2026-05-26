import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { apiDelete, apiGet, apiPost, apiPut } from "../api/client";
import { ParameterValueEditor } from "../components/ParameterValueEditor";
import type {
  ClassNodeDto,
  ClassParameterDto,
  CreateClassParameterDto,
  EnumValueDto,
  ParameterDefinitionDto,
  ParameterGroupDto,
  ProductDto,
  ProductParameterValueDto,
  ProductWithParametersDto,
  WriteProductParameterValueDto,
} from "../types/api";
import { ParameterValueTypes } from "../types/api";

function displayParamValue(p: ProductParameterValueDto): string {
  switch (p.valueType) {
    case ParameterValueTypes.Integer:
      return p.integerValue != null ? String(p.integerValue) : "—";
    case ParameterValueTypes.Number:
      return p.numberValue != null ? String(p.numberValue) : "—";
    case ParameterValueTypes.String:
      return p.stringValue?.length ? p.stringValue : "—";
    case ParameterValueTypes.DateTime:
      return p.dateTimeValue
        ? new Date(p.dateTimeValue).toLocaleString("ru-RU")
        : "—";
    case ParameterValueTypes.Enum:
      return p.enumDisplayName ?? p.enumStringValue ?? (p.enumNumberValue != null ? String(p.enumNumberValue) : "—");
    default:
      return "—";
  }
}

export function DevicesPage() {
  const [products, setProducts] = useState<ProductDto[] | null>(null);
  const [leaves, setLeaves] = useState<ClassNodeDto[] | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [card, setCard] = useState<ProductWithParametersDto | null>(null);
  const [loadingCard, setLoadingCard] = useState(false);
  const [enumCache, setEnumCache] = useState<Record<number, EnumValueDto[]>>({});
  const [createOpen, setCreateOpen] = useState(false);
  const [newName, setNewName] = useState("");
  const [newShort, setNewShort] = useState("");
  const [newClassId, setNewClassId] = useState<number | "">("");
  const [saving, setSaving] = useState(false);

  const loadProducts = useCallback(async () => {
    const list = await apiGet<ProductDto[]>("/api/products");
    setProducts(list);
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const l = await apiGet<ClassNodeDto[]>("/api/classnodes/leaves");
        if (!cancelled) {
          setLeaves(l);
          if (l.length && newClassId === "") setNewClassId(l[0]!.id);
        }
        await loadProducts();
      } catch (e) {
        if (!cancelled) setErr(e instanceof Error ? e.message : "Ошибка");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [loadProducts]);

  useEffect(() => {
    if (selectedId == null) {
      setCard(null);
      return;
    }
    let cancelled = false;
    (async () => {
      setLoadingCard(true);
      setErr(null);
      try {
        const c = await apiGet<ProductWithParametersDto>(`/api/products/${selectedId}/parameters/card`);
        if (!cancelled) setCard(c);
      } catch (e) {
        if (!cancelled) setErr(e instanceof Error ? e.message : "Ошибка карточки");
      } finally {
        if (!cancelled) setLoadingCard(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [selectedId]);

  const ensureEnum = useCallback(
    async (enumClassId: number) => {
      if (enumCache[enumClassId]) return;
      const values = await apiGet<EnumValueDto[]>(`/api/enum-classes/${enumClassId}/values`);
      setEnumCache((m) => ({ ...m, [enumClassId]: values }));
    },
    [enumCache],
  );

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    if (newClassId === "") return;
    setSaving(true);
    setErr(null);
    try {
      const created = await apiPost<ProductDto, { name: string; shortName: string; classNodeId: number }>(
        "/api/products",
        { name: newName.trim(), shortName: newShort.trim(), classNodeId: newClassId },
      );
      await loadProducts();
      setSelectedId(created.id);
      setCreateOpen(false);
      setNewName("");
      setNewShort("");
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Не удалось создать");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: number) {
    if (!confirm("Удалить устройство из каталога?")) return;
    setErr(null);
    try {
      await apiDelete(`/api/products/${id}`);
      if (selectedId === id) {
        setSelectedId(null);
        setCard(null);
      }
      await loadProducts();
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Ошибка удаления");
    }
  }

  const selectedProduct = useMemo(
    () => products?.find((p) => p.id === selectedId) ?? null,
    [products, selectedId],
  );

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
      <header style={{ display: "flex", flexWrap: "wrap", gap: "0.75rem", alignItems: "flex-end" }}>
        <div style={{ flex: "1 1 240px" }}>
          <h1 className="page-title">Устройства</h1>
          <p className="hint" style={{ marginTop: "0.35rem" }}>
            Каталог изделий и редактирование значений параметров класса.
          </p>
        </div>
        <button type="button" className="btn-warm" onClick={() => setCreateOpen((v) => !v)}>
          {createOpen ? "Закрыть форму" : "Добавить устройство"}
        </button>
      </header>
      {err && <div className="error-banner">{err}</div>}

      {createOpen && (
        <form className="card" style={{ display: "grid", gap: "0.75rem", maxWidth: "480px" }} onSubmit={(e) => void handleCreate(e)}>
          <div style={{ fontWeight: 600 }}>Новое устройство</div>
          <div>
            <label htmlFor="dn">Название</label>
            <input id="dn" className="input" required value={newName} onChange={(e) => setNewName(e.target.value)} />
          </div>
          <div>
            <label htmlFor="ds">Краткое имя</label>
            <input id="ds" className="input" required value={newShort} onChange={(e) => setNewShort(e.target.value)} />
          </div>
          <div>
            <label htmlFor="dc">Класс (конечный)</label>
            <select
              id="dc"
              className="input"
              required
              value={newClassId === "" ? "" : String(newClassId)}
              onChange={(e) => setNewClassId(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
            >
              {leaves?.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} ({c.shortName})
                </option>
              ))}
            </select>
          </div>
          <button type="submit" className="btn-primary" disabled={saving}>
            Сохранить
          </button>
        </form>
      )}

      <div
        style={{
          display: "grid",
          gridTemplateColumns: "minmax(0, 1.1fr) minmax(0, 1.4fr)",
          gap: "1rem",
          alignItems: "start",
        }}
      >
        <div className="card card--flush">
          <div className="card-header">
            <span>Каталог</span>
            <span className="hint" style={{ marginLeft: "0.5rem" }}>
              {products ? `${products.length} шт.` : ""}
            </span>
          </div>
          <div className="table-wrap" style={{ border: "none", maxHeight: "min(70vh, 720px)" }}>
            <table className="data">
              <thead>
                <tr>
                  <th>Название</th>
                  <th>Класс</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {products?.map((p) => (
                  <tr
                    key={p.id}
                    className={selectedId === p.id ? "selected" : undefined}
                    style={{ cursor: "pointer" }}
                    onClick={() => setSelectedId(p.id)}
                  >
                    <td>
                      <div style={{ fontWeight: 600 }}>{p.name}</div>
                      <div className="hint mono" style={{ fontSize: "0.82rem" }}>
                        {p.shortName}
                      </div>
                    </td>
                    <td>{p.classNodeName}</td>
                    <td>
                      <button
                        type="button"
                        className="btn-danger"
                        onClick={(e) => {
                          e.stopPropagation();
                          void handleDelete(p.id);
                        }}
                      >
                        Удалить
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="card" style={{ minHeight: "280px" }}>
          {!selectedId && <div className="hint">Выберите устройство слева.</div>}
          {selectedId && loadingCard && <div className="hint">Загрузка карточки…</div>}
          {selectedId && !loadingCard && card && selectedProduct && (
            <ProductCardView
              product={selectedProduct}
              card={card}
              enumCache={enumCache}
              onEnsureEnum={(id) => void ensureEnum(id)}
              onSaved={async () => {
                const c = await apiGet<ProductWithParametersDto>(`/api/products/${selectedId}/parameters/card`);
                setCard(c);
                await loadProducts();
              }}
            />
          )}
        </div>
      </div>
    </div>
  );
}

function AddParameterToClassSection({
  product,
  card,
  onAdded,
}: {
  product: ProductDto;
  card: ProductWithParametersDto;
  onAdded: () => Promise<void>;
}) {
  const [definitions, setDefinitions] = useState<ParameterDefinitionDto[] | null>(null);
  const [groups, setGroups] = useState<ParameterGroupDto[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [formErr, setFormErr] = useState<string | null>(null);
  const [defId, setDefId] = useState<number | "">("");
  const [groupId, setGroupId] = useState<number | "">("");
  const [sortOrder, setSortOrder] = useState(10);
  const [req, setReq] = useState(false);
  const [minV, setMinV] = useState<number | "">("");
  const [maxV, setMaxV] = useState<number | "">("");

  const usedDefIds = useMemo(
    () => new Set(card.parameters.map((p) => p.parameterDefinitionId)),
    [card.parameters],
  );

  const available = useMemo(() => {
    if (!definitions) return [];
    return definitions
      .filter((d) => !usedDefIds.has(d.id))
      .sort((a, b) => a.name.localeCompare(b.name, "ru"));
  }, [definitions, usedDefIds]);

  const selDef = useMemo(() => {
    if (defId === "" || !definitions) return null;
    return definitions.find((x) => x.id === defId) ?? null;
  }, [defId, definitions]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setFormErr(null);
      try {
        const [d, g] = await Promise.all([
          apiGet<ParameterDefinitionDto[]>("/api/parameter-definitions"),
          apiGet<ParameterGroupDto[]>("/api/parameter-groups"),
        ]);
        if (!cancelled) {
          setDefinitions(d);
          setGroups(g);
        }
      } catch (e) {
        if (!cancelled) setFormErr(e instanceof Error ? e.message : "Ошибка загрузки");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [card.classNodeId]);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (defId === "") return;
    setBusy(true);
    setFormErr(null);
    const body: CreateClassParameterDto = {
      parameterDefinitionId: Number(defId),
      parameterGroupId: groupId === "" ? null : Number(groupId),
      sortOrder,
      isRequired: req,
      minNumberValue:
        selDef &&
        (selDef.valueType === ParameterValueTypes.Integer || selDef.valueType === ParameterValueTypes.Number) &&
        minV !== ""
          ? Number(minV)
          : null,
      maxNumberValue:
        selDef &&
        (selDef.valueType === ParameterValueTypes.Integer || selDef.valueType === ParameterValueTypes.Number) &&
        maxV !== ""
          ? Number(maxV)
          : null,
    };
    try {
      await apiPost<ClassParameterDto, CreateClassParameterDto>(
        `/api/classnodes/${card.classNodeId}/parameters`,
        body,
      );
      setDefId("");
      setGroupId("");
      setSortOrder(10);
      setReq(false);
      setMinV("");
      setMaxV("");
      await onAdded();
    } catch (ex) {
      setFormErr(ex instanceof Error ? ex.message : "Не удалось добавить параметр");
    } finally {
      setBusy(false);
    }
  }

  if (loading) {
    return (
      <div className="hint" style={{ marginBottom: "0.5rem" }}>
        Загрузка списков для добавления параметра…
      </div>
    );
  }

  if (available.length === 0) {
    return (
      <div
        className="card-nested" style={{ marginBottom: "0.5rem" }}
      >
        <div style={{ fontWeight: 600, marginBottom: "0.35rem" }}>Новый параметр у устройства</div>
        <p className="hint" style={{ margin: 0 }}>
          Класс «{product.classNodeName}» уже содержит все доступные описания параметров. Создайте новое описание
          на странице «Справочники», затем вернитесь сюда.
        </p>
      </div>
    );
  }

  return (
    <div
      className="card-nested" style={{ marginBottom: "1rem" }}
    >
      <div style={{ fontWeight: 600, marginBottom: "0.35rem" }}>Добавить параметр к классу этого устройства</div>
      <p className="hint" style={{ margin: "0 0 0.75rem" }}>
        Параметр привязывается к классу «{product.classNodeName}»: строка появится у всех устройств этого класса.
        Сначала список свойств задаётся в справочниках, здесь вы подключаете его к классу и группе на карточке.
      </p>
      {formErr && <div className="error-banner" style={{ marginBottom: "0.5rem" }}>{formErr}</div>}
      <form style={{ display: "grid", gap: "0.55rem", maxWidth: "480px" }} onSubmit={(e) => void submit(e)}>
        <div>
          <label>Описание параметра</label>
          <select
            className="input"
            required
            value={defId === "" ? "" : String(defId)}
            onChange={(e) => setDefId(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
          >
            <option value="">— выберите свойство —</option>
            {available.map((d) => (
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
            value={groupId === "" ? "" : String(groupId)}
            onChange={(e) => setGroupId(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
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
          <label>Порядок</label>
          <input
            className="input mono"
            type="number"
            value={sortOrder}
            onChange={(e) => setSortOrder(Number.parseInt(e.target.value, 10) || 0)}
          />
        </div>
        <label style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
          <input type="checkbox" checked={req} onChange={(e) => setReq(e.target.checked)} />
          Обязательный
        </label>
        {selDef &&
          (selDef.valueType === ParameterValueTypes.Integer || selDef.valueType === ParameterValueTypes.Number) && (
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.5rem" }}>
              <div>
                <label>Мин.</label>
                <input
                  className="input mono"
                  type="number"
                  step="any"
                  value={minV === "" ? "" : minV}
                  onChange={(e) => setMinV(e.target.value === "" ? "" : Number.parseFloat(e.target.value))}
                />
              </div>
              <div>
                <label>Макс.</label>
                <input
                  className="input mono"
                  type="number"
                  step="any"
                  value={maxV === "" ? "" : maxV}
                  onChange={(e) => setMaxV(e.target.value === "" ? "" : Number.parseFloat(e.target.value))}
                />
              </div>
            </div>
          )}
        <button type="submit" className="btn-primary" disabled={busy}>
          {busy ? "Добавление…" : "Добавить параметр к классу"}
        </button>
      </form>
    </div>
  );
}

function ProductCardView({
  product,
  card,
  enumCache,
  onEnsureEnum,
  onSaved,
}: {
  product: ProductDto;
  card: ProductWithParametersDto;
  enumCache: Record<number, EnumValueDto[]>;
  onEnsureEnum: (enumClassId: number) => void;
  onSaved: () => Promise<void>;
}) {
  const grouped = useMemo(() => {
    const map = new Map<string, ProductParameterValueDto[]>();
    for (const p of card.parameters) {
      const key = p.parameterGroupName ?? "Общие";
      const arr = map.get(key) ?? [];
      arr.push(p);
      map.set(key, arr);
    }
    for (const [, arr] of map) {
      arr.sort((a, b) => a.sortOrder - b.sortOrder || a.parameterName.localeCompare(b.parameterName));
    }
    return [...map.entries()].sort(([a], [b]) => a.localeCompare(b));
  }, [card.parameters]);

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
      <div>
        <div style={{ fontSize: "20px", fontWeight: 600, color: "var(--text-primary)" }}>{product.name}</div>
        <div className="hint">
          <span className="mono">{product.shortName}</span>
          {" · "}
          {product.classNodeName}
        </div>
      </div>
      <AddParameterToClassSection product={product} card={card} onAdded={onSaved} />
      {grouped.map(([groupName, params]) => (
        <div key={groupName}>
          <div className="group-title">{groupName}</div>
          <div style={{ display: "flex", flexDirection: "column", gap: "0.65rem" }}>
            {params.map((p) => (
              <ParameterRow
                key={p.classParameterId}
                param={p}
                productId={card.id}
                enumOptions={p.enumClassId ? enumCache[p.enumClassId] ?? [] : []}
                onEnsureEnum={onEnsureEnum}
                onSaved={onSaved}
              />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

function ParameterRow({
  param,
  productId,
  enumOptions,
  onEnsureEnum,
  onSaved,
}: {
  param: ProductParameterValueDto;
  productId: number;
  enumOptions: EnumValueDto[];
  onEnsureEnum: (enumClassId: number) => void;
  onSaved: () => Promise<void>;
}) {
  const [edit, setEdit] = useState(false);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const [intV, setIntV] = useState<number | null>(param.integerValue);
  const [numV, setNumV] = useState<number | null>(param.numberValue);
  const [strV, setStrV] = useState(param.stringValue ?? "");
  const [dateV, setDateV] = useState<string | null>(param.dateTimeValue);
  const [enumV, setEnumV] = useState<number | null>(param.enumValueId);

  useEffect(() => {
    setIntV(param.integerValue);
    setNumV(param.numberValue);
    setStrV(param.stringValue ?? "");
    setDateV(param.dateTimeValue);
    setEnumV(param.enumValueId);
  }, [param]);

  useEffect(() => {
    if (edit && param.valueType === ParameterValueTypes.Enum && param.enumClassId) {
      onEnsureEnum(param.enumClassId);
    }
  }, [edit, param.enumClassId, param.valueType, onEnsureEnum]);

  async function save() {
    setBusy(true);
    setErr(null);
    const dto: WriteProductParameterValueDto = {};
    if (param.valueType === ParameterValueTypes.Integer) dto.integerValue = intV;
    else if (param.valueType === ParameterValueTypes.Number) dto.numberValue = numV;
    else if (param.valueType === ParameterValueTypes.String) dto.stringValue = strV.trim() || null;
    else if (param.valueType === ParameterValueTypes.DateTime) dto.dateTimeValue = dateV;
    else if (param.valueType === ParameterValueTypes.Enum) dto.enumValueId = enumV;

    try {
      await apiPut<ProductParameterValueDto, WriteProductParameterValueDto>(
        `/api/products/${productId}/parameters/${param.classParameterId}`,
        dto,
      );
      await onSaved();
      setEdit(false);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Ошибка сохранения");
    } finally {
      setBusy(false);
    }
  }

  async function clearValue() {
    if (!confirm("Сбросить значение параметра?")) return;
    setBusy(true);
    setErr(null);
    try {
      await apiDelete(`/api/products/${productId}/parameters/${param.classParameterId}`);
      await onSaved();
      setEdit(false);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Ошибка");
    } finally {
      setBusy(false);
    }
  }

  const enumSelectOptions = enumOptions.map((v) => ({
    id: v.id,
    label: v.displayName ?? v.stringValue ?? (v.numberValue != null ? String(v.numberValue) : `#${v.id}`),
  }));

  return (
    <div
      className="card-nested"
    >
      <div style={{ display: "flex", justifyContent: "space-between", gap: "0.75rem", flexWrap: "wrap" }}>
        <div>
          <div style={{ fontWeight: 600 }}>{param.parameterName}</div>
          <div className="hint mono" style={{ fontSize: "0.82rem" }}>
            {param.parameterShortName}
            {param.measureUnitShortName ? ` · ${param.measureUnitShortName}` : ""}
            {param.isInherited && " · унаследован"}
          </div>
        </div>
        {!edit && (
          <div style={{ textAlign: "right" }}>
            <div className="mono" style={{ fontSize: "0.95rem" }}>
              {displayParamValue(param)}
            </div>
            <button type="button" className="btn-ghost" style={{ marginTop: "0.35rem" }} onClick={() => setEdit(true)}>
              Изменить
            </button>
          </div>
        )}
      </div>
      {edit && (
        <div style={{ marginTop: "0.75rem", display: "grid", gap: "0.5rem" }}>
          <ParameterValueEditor
            valueType={param.valueType}
            integerValue={intV}
            numberValue={numV}
            stringValue={strV}
            dateTimeValue={dateV}
            enumValueId={enumV}
            minNumberValue={param.minNumberValue}
            maxNumberValue={param.maxNumberValue}
            enumOptions={enumSelectOptions}
            onIntChange={setIntV}
            onNumChange={setNumV}
            onStrChange={setStrV}
            onDateChange={setDateV}
            onEnumChange={setEnumV}
          />
          {err && <div className="error-banner">{err}</div>}
          <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem" }}>
            <button type="button" className="btn-primary" disabled={busy} onClick={() => void save()}>
              Сохранить
            </button>
            <button type="button" className="btn-ghost" disabled={busy} onClick={() => setEdit(false)}>
              Отмена
            </button>
            <button type="button" className="btn-danger" disabled={busy} onClick={() => void clearValue()}>
              Сбросить
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
