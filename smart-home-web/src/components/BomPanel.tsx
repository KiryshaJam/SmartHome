import { ExpressionField, StringParameterInput } from "./FriendlyExpression";
import { describeExpression, defaultCondition, writeExpression, expressionReferences, humanizeBomError } from "../utils/bomExpressions";
import { CompatibilityBuilder, ConfigurationResult, SpecParameters, parameterValue, type ResolvedNode } from "./BomEditors";
import { useUnsavedChanges } from "./UnsavedChanges";
import { useEffect, useRef, useState } from "react";
import { apiGet, apiPost, apiPut } from "../api/client";
import type { ClassNodeDto, MeasureUnitDto, ProductDto, ParameterDefinitionDto, EnumValueDto, ProductWithParametersDto } from "../types/api";

export interface SpecParam {
  key: string;
  definitionId: number;
  name: string;
  type: number;
  required: boolean;
  min: number | null;
  max: number | null;
  enumIds: number[];
  enumLabels?: Record<string, string>;
  default: string | number | null;
}
interface Position {
  id: string;
  componentId: number;
  revisionId: number;
  amount: number;
  forQuantity: number;
  unitId: number;
  sortOrder: number;
  condition: string | null;
  formula: string | null;
  childVariantId: number | null;
  bindings: Record<string, string>;
  excluded: boolean;
  inherited: boolean;
}
interface Rule {
  expression: string;
  message: string;
}
interface Doc {
  positions: Position[];
  parameters: SpecParam[];
  rules: Rule[];
  calculations: Record<string, string>;
}
interface Revision {
  id: number;
  unitId: number;
  previousId: number | null;
  baseRevisionId: number | null;
  document: string;
  reason: string;
  createdAt: string;
}
export interface Spec {
  baseEffective: Doc | null;
  revision: Revision | null;
  effective: Doc | null;
  parameters: SpecParam[];
}
export interface Variant {
  id: number;
  name: string;
  productId: number;
  revisionId: number;
}
export interface Total {
  product_id: number;
  name: string;
  unit: string;
  unit_id: number;
  required_quantity: number;
}
interface Tree extends Total {
  norm: number;
  for_quantity: number;
  path: string;
  depth: number;
  revision_id: number;
  leaf: boolean;
}
const message = (e: unknown) => (e instanceof Error ? e.message : "Ошибка");

export function TotalsTable({ rows }: { rows: Total[] }) {
  return (
    <div className="table-scroll">
      <table className="data data-table">
        <thead>
          <tr>
            <th>Материал / компонент</th>
            <th>Количество</th>
            <th>Единица</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={`${r.product_id}-${r.unit_id}`}>
              <td>{r.name}</td>
              <td>{r.required_quantity}</td>
              <td>{r.unit}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {!rows.length && (
        <p className="hint">Нет материалов выбранного класса.</p>
      )}
    </div>
  );
}

export function VariantFields({
  parameters,
  calculations,
  values,
  onChange,
}: {
  parameters: SpecParam[];
  calculations: Record<string, string>;
  values: Record<string, string | number>;
  onChange: (v: Record<string, string | number>) => void;
}) {
  return (
    <div className="bom-grid">
      {parameters
        .filter((p) => !calculations[p.key])
        .map((p) => (
          <label key={p.key}>
            {p.name} {p.required ? "*" : ""}
            {p.type === 5 ? (
              <select
                className="input"
                value={values[p.key] ?? ""}
                onChange={(e) => {
                  const next = { ...values };
                  if (e.target.value === "") delete next[p.key];
                  else next[p.key] = Number(e.target.value);
                  onChange(next);
                }}
              >
                <option value="">— значение по умолчанию —</option>
                {p.enumIds.map((id) => (
                  <option key={id} value={id}>
                    {p.enumLabels?.[id] ?? `Значение #${id}`}
                  </option>
                ))}
              </select>
            ) : p.type === 3 ? <StringParameterInput name={p.name} value={String(values[p.key] ?? "")} placeholder={p.default == null ? "Выберите значение" : `По умолчанию: ${parameterValue(p, p.default)}`}
              onChange={value => { const next = { ...values }; if (value === "") delete next[p.key]; else next[p.key] = value; onChange(next); }} /> : (
              <input
                className="input"
                type={
                  p.type === 1 || p.type === 2
                    ? "number"
                    : p.type === 4
                      ? "datetime-local"
                      : "text"
                }
                step={p.type === 1 ? "1" : "any"}
                min={p.min ?? undefined}
                max={p.max ?? undefined}
                value={values[p.key] ?? ""}
                placeholder={
                  p.default == null ? "Не задано" : String(p.default)
                }
                onChange={(e) => {
                  const next = { ...values };
                  if (e.target.value === "") delete next[p.key];
                  else
                    next[p.key] =
                      p.type === 1 || p.type === 2
                        ? Number(e.target.value)
                        : e.target.value;
                  onChange(next);
                }}
              />
            )}
            <span className="hint">{p.min != null ? `от ${p.min} ` : ""}{p.max != null ? `до ${p.max} · ` : ""}По умолчанию: {parameterValue(p, p.default)}</span>
          </label>
        ))}
    </div>
  );
}
export function variantValues(
  parameters: SpecParam[],
  values: Record<string, string | number>,
) {
  return Object.fromEntries(
    Object.entries(values).map(([k, v]) => [
      k,
      parameters.find((p) => p.key === k)?.type === 4
        ? new Date(String(v)).toISOString()
        : v,
    ]),
  );
}
async function describeConfiguration(node: ResolvedNode) {
  const descriptions = new Map<number, Promise<Spec>>();
  async function describe(n: ResolvedNode): Promise<void> {
    if (!n.source?.parameters.length && Object.keys(n.values).length > 0) {
      if (!descriptions.has(n.revisionId)) descriptions.set(n.revisionId, apiGet<Spec>(`/api/bom/products/${n.productId}?revisionId=${n.revisionId}`));
      const spec = await descriptions.get(n.revisionId)!;
      n.source = { parameters: spec.parameters };
    }
    await Promise.all(n.children.map(describe));
  }
  await describe(node);
  return node;
}
export function BomPanel({
  productId,
  onCreated,
}: {
  productId: number;
  onCreated: () => Promise<void>;
}) {
  const [tab, setTab] = useState("rules");
  const [parameters, setParameters] = useState<SpecParam[]>([]);
  const [catalogChoices, setCatalogChoices] = useState<SpecParam[] | null>(null);
  const [selectedParameterKeys, setSelectedParameterKeys] = useState<string[]>([]);
  const [parameterNames, setParameterNames] = useState<Record<string, string>>({});
  const [enumOptions, setEnumOptions] = useState<Record<string, Record<string, string>>>({});
  const [baseline, setBaseline] = useState("");
  const [prepareAsMaterial, setPrepareAsMaterial] = useState(false);
  const [newComponentUnitId, setNewComponentUnitId] = useState(0);
  const [previewNode, setPreviewNode] = useState<ResolvedNode | null>(null);
  const [savedNode, setSavedNode] = useState<ResolvedNode | null>(null);
  const [spec, setSpec] = useState<Spec | null>(null),
    [products, setProducts] = useState<ProductDto[]>([]),
    [units, setUnits] = useState<MeasureUnitDto[]>([]),
    [classes, setClasses] = useState<ClassNodeDto[]>([]);
  const [positions, setPositions] = useState<Position[]>([]),
    [rules, setRules] = useState<Rule[]>([]),
    [calculations, setCalculations] = useState<Record<string, string>>({}),
    [unitId, setUnitId] = useState(0),
    [reason, setReason] = useState("");
  const [error, setError] = useState(""),
    [busy, setBusy] = useState(false),
    [notice, setNotice] = useState("");
  const [history, setHistory] = useState<Revision[]>([]),
    [historical, setHistorical] = useState<Spec | null>(null),
    [editor, setEditor] = useState<Position | null>(null),
    [child, setChild] = useState<Spec | null>(null),
    [childVariants, setChildVariants] = useState<Variant[]>([]);
  const [variants, setVariants] = useState<Variant[]>([]),
    [values, setValues] = useState<Record<string, string | number>>({}),
    [variantName, setVariantName] = useState(""),
    [variantId, setVariantId] = useState(0),
    [quantity, setQuantity] = useState(1),
    [classId, setClassId] = useState("");
  const [tree, setTree] = useState<Tree[]>([]),
    [totals, setTotals] = useState<Total[]>([]),
    [preview, setPreview] = useState<string>("");
  const calculationGeneration = useRef(0);
  useEffect(() => {
    calculationGeneration.current += 1;
    setTree([]);
    setTotals([]);
    return () => {
      calculationGeneration.current += 1;
    };
  }, [productId, variantId, quantity, classId]);
  const [modName, setModName] = useState(""),
    [modShort, setModShort] = useState("");
  const loadGeneration = useRef(0);
  async function load() {
    const generation = ++loadGeneration.current;
    const [s, p, u, c, h, v] = await Promise.all([
      apiGet<Spec>(`/api/bom/products/${productId}`),
      apiGet<ProductDto[]>("/api/products"),
      apiGet<MeasureUnitDto[]>("/api/measure-units"),
      apiGet<ClassNodeDto[]>("/api/classnodes"),
      apiGet<Revision[]>(`/api/bom/products/${productId}/history`),
      apiGet<Variant[]>(`/api/bom/variants?productId=${productId}`),
    ]);
    const definitions = await apiGet<ParameterDefinitionDto[]>("/api/parameter-definitions");
    const enums = await Promise.all(s.parameters.filter(p => p.type === 5).map(async p => {
      const enumClassId = definitions.find(d => d.id === p.definitionId)?.enumClassId;
      const choices = enumClassId ? await apiGet<EnumValueDto[]>(`/api/enum-classes/${enumClassId}/values`) : [];
      return [p.key, Object.fromEntries(choices.map(e => [e.id, e.displayName ?? e.stringValue ?? String(e.numberValue ?? e.id)]))] as const;
    }));
    if (generation !== loadGeneration.current) return;
    if (baseline === "" && !s.parameters.length) setTab("composition");
    setSpec(s);
    setProducts(p);
    setUnits(u);
    setClasses(c);
    setHistory(h);
    setVariants(v);
    setCatalogChoices(null);
    setSelectedParameterKeys([]);
    setParameters(s.parameters);
    setParameterNames(Object.fromEntries(definitions.map(p => [`p${p.id}`, p.name])));
    setValues(current => Object.fromEntries(Object.entries(current).filter(([key]) => s.parameters.some(p => p.key === key) && !s.effective?.calculations[key])));
    setEnumOptions(Object.fromEntries(enums));
    const d = s.revision ? (JSON.parse(s.revision.document) as Doc) : null;
    setPositions(d?.positions ?? []);
    setRules(d?.rules ?? []);
    setCalculations(d?.calculations ?? {});
    const product = p.find(p => p.id === productId);
    const classUnit = c.find(c => c.id === product?.classNodeId)?.measureUnitId;
    const unit = s.revision?.unitId ?? classUnit ?? u.find(u => u.shortName === "шт.")?.id ?? u[0]?.id ?? 0;
    setUnitId(unit);
    setBaseline(JSON.stringify([d?.positions ?? [], d?.rules ?? [], d?.calculations ?? {}, s.parameters, unit]));
  }
  async function importCatalogParameters() {
    const card = await apiGet<ProductWithParametersDto>(`/api/products/${productId}/parameters/card`);
    const imported = await Promise.all(card.parameters.filter(p => !parameters.some(x => x.definitionId === p.parameterDefinitionId)).map(async p => {
      const choices = p.enumClassId ? await apiGet<EnumValueDto[]>(`/api/enum-classes/${p.enumClassId}/values`) : [];
      const labels = Object.fromEntries(choices.map(e => [e.id, e.displayName ?? e.stringValue ?? String(e.numberValue ?? e.id)]));
      return { key: `p${p.parameterDefinitionId}`, definitionId: p.parameterDefinitionId, name: p.parameterName, type: p.valueType,
        required: p.isRequired, min: p.minNumberValue, max: p.maxNumberValue, enumIds: choices.map(e => e.id), enumLabels: labels,
        default: p.valueType === 1 ? p.integerValue : p.valueType === 2 ? p.numberValue : p.valueType === 3 ? p.stringValue : p.valueType === 4 ? p.dateTimeValue : p.enumValueId };
    }));
    setCatalogChoices(imported);
    setSelectedParameterKeys([]);
    setNotice(imported.length ? "Выберите ниже, какие параметры подключить." : "Все параметры справочника уже подключены.");
  }
  useEffect(() => {
    let active = true;
    void load().catch((e) => {
      if (active) setError(message(e));
    });
    return () => {
      active = false;
      loadGeneration.current += 1;
    };
  }, [productId]);
  useEffect(() => {
    let active = true;
    setChild(null);
    setChildVariants([]);
    if (editor?.componentId)
      void Promise.all([
        apiGet<Spec>(
          `/api/bom/products/${editor.componentId}${editor.revisionId ? `?revisionId=${editor.revisionId}` : ""}`,
        ),
        apiGet<Variant[]>(`/api/bom/variants?productId=${editor.componentId}`),
      ])
        .then(([s, v]) => {
          if (active) {
            setChild(s);
            setChildVariants(v);
          }
        })
        .catch((e) => {
          if (active) setError(message(e));
        });
    return () => {
      active = false;
    };
  }, [editor?.componentId, editor?.revisionId]);
  async function run(action: () => Promise<void>) {
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await action();
    } catch (e) {
      setError(message(e));
    } finally {
      setBusy(false);
    }
  }
  function edit(p: Position) {
    setPrepareAsMaterial(false);
    setEditor({ ...p, bindings: { ...p.bindings } });
  }
  function exclude(p: Position) {
    setPositions([
      ...positions.filter((x) => x.id !== p.id),
      { ...p, excluded: true, inherited: false },
    ]);
  }
  function remove(p: Position) {
    setPositions(positions.filter((x) => x.id !== p.id));
  }
  const dirty = baseline !== "" && (baseline !== JSON.stringify([positions, rules, calculations, parameters, unitId]) || !!reason.trim());
  useUnsavedChanges(dirty || !!editor);
  const effective = (spec?.baseEffective?.positions ?? [])
    .filter((p) => !positions.some((x) => x.id === p.id))
    .map((p) => ({ ...p, inherited: true }))
    .concat(positions)
    .filter((p) => !p.excluded)
    .sort((a, b) => a.sortOrder - b.sortOrder);
  const previewGeneration = useRef(0);
  useEffect(() => {
    previewGeneration.current += 1;
    setPreview("");
    setPreviewNode(null);
    return () => {
      previewGeneration.current += 1;
    };
  }, [productId, historical?.revision?.id, spec?.revision?.id, values, variantName, dirty]);
  useEffect(() => { setSavedNode(null); }, [variantId]);
  const usedParameterKeys = [
    ...rules.map(r => r.expression), ...Object.values(calculations),
    ...effective.flatMap(p => [p.condition ?? "", p.formula ?? "", ...Object.values(p.bindings)]),
  ].map(expressionReferences);
  const canRemoveParameter = (p: SpecParam) => !calculations[p.key] && usedParameterKeys.every(keys => keys !== null && !keys.has(p.key));
  const shown = historical?.effective?.positions ?? effective;
  const configuration = historical ?? spec;
  const name = (id: number) => products.find(p => p.id === id)?.name ?? "Компонент из прежней версии";
  const friendlyMessage = (text: string) => humanizeBomError(text, parameterNames, Object.fromEntries(products.map(p => [String(p.id), p.name])));
  const expressionContext = { parameters: configuration?.parameters ?? parameters,
    components: Array.from(new Map(shown.map(p => [p.componentId, { id: p.componentId, name: name(p.componentId) }])).values()) };
  return (
    <section className="bom-panel">
      <h3>Настройка изделия</h3>
      <p className="hint">Настройте изделие по трём шагам. Для готового изделия можно сразу перейти к шагу 3 и выбрать исполнение.</p>
      <div className="bom-tabs" role="group" aria-label="Разделы спецификации">
        {[["rules", "1. Параметры изделия"], ["composition", "2. Компоненты и расход"], ["variants", "3. Проверка и исполнение"], ["history", "История и модификации"]].map(([id, label]) =>
          <button key={id} disabled={busy || !spec} className={tab === id ? "btn-primary" : "btn-secondary"} aria-pressed={tab === id} onClick={() => setTab(id)}>{label}</button>)}
      </div>
      {dirty && <p className="warning-banner" role="status">Вы изменили настройки. Сохраните этот шаг перед проверкой изделия.</p>}
      {editor && tab !== "composition" && <p className="hint">Открыто редактирование компонента на шаге 2. Добавьте его в состав или отмените изменения.</p>}
      {!spec && <p role="status">Загрузка настройки изделия…</p>}
      <fieldset className="bom-form" hidden={!spec} disabled={busy || !spec}>
      {error && (
        <div className="error-banner" role="alert">
          {friendlyMessage(error)}
        </div>
      )}
      {notice && <p className="success-banner" role="status">{notice}</p>}
      {historical && (
        <div className="warning-banner">
          <strong>История: версия {historical.revision?.id}</strong>
          <button className="btn-ghost" onClick={() => { setHistorical(null); setValues({}); }}>
            Вернуться к редактированию
          </button>
        </div>
      )}
      <div hidden={tab !== "composition"} className="bom-form">
      <div hidden={!!editor} className="bom-form">
      <label>
        В чём измеряется готовое изделие
        <select
          className="input"
          value={historical?.revision?.unitId ?? unitId}
          disabled={!!spec?.revision}
          onChange={(e) => setUnitId(Number(e.target.value))}
        >
          {units.map((u) => (
            <option key={u.id} value={u.id}>
              {u.name} ({u.shortName})
            </option>
          ))}
        </select>
      </label>
      {spec?.revision && (
        <p className="hint">
          Версия {configuration?.revision?.id}
          {configuration?.revision?.baseRevisionId
            ? ` · модификация версии ${configuration.revision.baseRevisionId}`
            : ""}
          . Сохранённые исполнения сохраняют свой состав при дальнейших изменениях.
        </p>
      )}
      <div className="table-scroll">
        <table className="data data-table">
          <thead>
            <tr>
              <th>Компонент</th>
              <th>Количество / расчёт</th>
              <th>Когда использовать</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            {shown.map((p) => (
              <tr key={p.id}>
                <td>
                  {name(p.componentId)}
                  <div className="hint">
                    Версия {p.revisionId}
                    {p.inherited ? " · унаследована" : spec?.baseEffective?.positions.some(x => x.id === p.id) ? " · заменена в модификации" : " · собственная"}
                    {p.childVariantId ? " · готовое исполнение компонента" : ""}
                    {Object.keys(p.bindings).length > 0 && <div>Значения передаются из параметров изделия</div>}
                  </div>
                </td>
                <td>
                  {p.formula ? describeExpression(p.formula, expressionContext) : <>{p.amount} {units.find(u => u.id === p.unitId)?.shortName}</>}
                  <div className="hint">{p.forQuantity === 1 ? "на одно готовое изделие" : `на ${p.forQuantity} ед. готового изделия`}</div>
                </td>
                <td>
                  {describeExpression(p.condition ?? "", expressionContext, "Включать всегда")}

                </td>
                <td>
                  {!historical && (
                    <>
                      <button className="btn-ghost" onClick={() => edit(p)}>
                        Настроить компонент
                      </button>
                      {!p.inherited && spec?.baseEffective?.positions.some(x => x.id === p.id) && <button className="btn-ghost" onClick={() => remove(p)}>Вернуть позицию базы</button>}
                      {spec?.revision?.baseRevisionId &&
                      spec.baseEffective?.positions.some(
                        (x) => x.id === p.id,
                      ) ? (
                        <button
                          className="btn-ghost"
                          onClick={() => exclude(p)}
                        >
                          Исключить
                        </button>
                      ) : (
                        <button className="btn-ghost" onClick={() => remove(p)}>
                          Удалить позицию
                        </button>
                      )}
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {!shown.length && <p className="hint">Компонентов пока нет. Добавьте их кнопкой ниже. Если это материал, достаточно выбрать единицу и сохранить этот шаг.</p>}
      {!historical && positions.some((p) => p.excluded) && (
        <p className="hint">
          Исключены:{" "}
          {positions
            .filter((p) => p.excluded)
            .map((p) => (
              <span key={p.id}>
                {name(p.componentId)}{" "}
                <button className="btn-ghost" onClick={() => remove(p)}>
                  Вернуть из базы
                </button>
              </span>
            ))}
        </p>
      )}
      {!historical && (
        <button
          className="btn-secondary"
          onClick={() =>
            setEditor({
              id: crypto.randomUUID(),
              componentId: 0,
              revisionId: 0,
              amount: 1,
              forQuantity: 1,
              unitId: 0,
              sortOrder: effective.length * 10,
              condition: null,
              formula: null,
              childVariantId: null,
              bindings: {},
              excluded: false,
              inherited: false,
            })
          }
        >
          Добавить компонент
        </button>
      )}
      </div>
      {editor && !historical && (
        <form
          className="card-nested bom-form"
          onSubmit={(e) => {
            e.preventDefault();
            void run(async () => {
              if (!child || !editor.componentId) throw new Error("Сначала выберите компонент.");
              let revision = child.revision;
              if (!revision) {
                if (!prepareAsMaterial) throw new Error("У компонента ещё нет состава. Отметьте использование без вложенных компонентов или сначала настройте это изделие отдельно.");
                const prepared = await apiPut<Revision>(`/api/bom/products/${editor.componentId}`, {
                  expectedRevisionId: null,
                  unitId: newComponentUnitId || units.find(u => u.shortName === "шт.")?.id || units[0]?.id,
                  reason: "Подготовлен компонент без вложенного состава", positions: [], rules: [], calculations: {},
                });
                revision = prepared;
              }
              const p = { ...editor, inherited: false, unitId: revision.unitId, revisionId: editor.revisionId || revision.id };
              setPositions([...positions.filter(x => x.id !== p.id), p]);
              setEditor(null);
              setPrepareAsMaterial(false);
              setNotice("Компонент добавлен в состав. Сохраните шаг 2, чтобы применить изменения.");
            });
          }}
        >
          <h4>Настройка компонента</h4>
          <p className="hint">Выберите компонент, его количество и условия использования. Кнопка «Готово» вернёт к списку компонентов.</p>
          <label>
            Компонент
            <select
              className="input"
              required
              value={editor.componentId}
              onChange={(e) => {
                setPrepareAsMaterial(false);
                setEditor({
                  ...editor,
                  componentId: Number(e.target.value),
                  revisionId: 0,
                  childVariantId: null,
                  bindings: {},
                });
              }}
            >
              <option value="0">— выберите изделие —</option>
              {products
                .filter((p) => p.id !== productId)
                .map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
            </select>
          </label>
          {editor.componentId > 0 && child && !child.revision && <div className="card-nested bom-form">
            <p>Этот компонент ещё не настроен.</p>
            <label className="checkbox-label"><input type="checkbox" checked={prepareAsMaterial} onChange={e => setPrepareAsMaterial(e.target.checked)} />Использовать как материал без вложенных компонентов</label>
            {prepareAsMaterial && <label>Единица расхода нового компонента<select className="input" aria-label="Единица расхода нового компонента" value={newComponentUnitId || units.find(u => u.shortName === "шт.")?.id || units[0]?.id || 0} onChange={e => setNewComponentUnitId(Number(e.target.value))}>
              {units.map(u => <option key={u.id} value={u.id}>{u.name} ({u.shortName})</option>)}
            </select></label>}
          </div>}
          <div className="bom-grid">
            <label>
              Расход
              <input
                className="input"
                type="number"
                min="0.000000001"
                step="any"
                required
                value={editor.amount}
                onChange={(e) =>
                  setEditor({ ...editor, amount: Number(e.target.value) })
                }
              />
            </label>


          </div>
          <p className="hint">Единица расхода: {units.find(u => u.id === (child?.revision?.unitId ?? (prepareAsMaterial ? (newComponentUnitId || units.find(u => u.shortName === "шт.")?.id) : undefined)))?.name ?? "будет определена после выбора компонента"}. Количество указано на одно готовое изделие; норму на партию можно задать в дополнительных настройках.</p>
          <ExpressionField label="Условие включения" value={editor.condition ?? ""} parameters={parameters}
            onChange={v => setEditor({ ...editor, condition: v || null })} />
          <ExpressionField kind="formula" emptyLabel="Постоянный расход из поля выше" label="Формула расхода" value={editor.formula ?? ""} parameters={parameters}
            onChange={v => setEditor({ ...editor, formula: v || null })} />
          <p className="hint">Выберите, когда нужен компонент и как считать его расход. Например: «Число датчиков умножить на 2». Названия и значения выбираются из списков.</p>
          <details className="card-nested"><summary>Вложенный состав и дополнительные настройки</summary>
            <label>
              Норма указана на сколько изделий
              <input
                className="input"
                type="number"
                min="0.000000001"
                step="any"
                required
                value={editor.forQuantity}
                onChange={(e) =>
                  setEditor({ ...editor, forQuantity: Number(e.target.value) })
                }
              />
            </label>
            <label>
              Порядок
              <input
                className="input"
                type="number"
                value={editor.sortOrder}
                onChange={(e) =>
                  setEditor({ ...editor, sortOrder: Number(e.target.value) })
                }
              />
            </label>          <p className="hint">
            Единица расхода:{" "}
            {units.find((u) => u.id === child?.revision?.unitId)?.name ??
              "Задайте единицу в карточке компонента"}
            . Версия: {editor.revisionId || child?.revision?.id || "не задана"}
          </p>
          <button
            type="button"
            className="btn-ghost"
            onClick={() =>
              setEditor({ ...editor, revisionId: 0, childVariantId: null })
            }
          >
            Текущая версия компонента
          </button>
          <label>
            Исполнение вложенного компонента
            <select
              className="input"
              value={editor.childVariantId ?? ""}
              onChange={(e) => {
                const v = childVariants.find(
                  (x) => x.id === Number(e.target.value),
                );
                setEditor({
                  ...editor,
                  childVariantId: v?.id ?? null,
                  revisionId: v?.revisionId ?? 0,
                  bindings: {},
                });
              }}
            >
              <option value="">Настроить параметры этого компонента</option>
              {childVariants.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name}
                </option>
              ))}
            </select>
          </label>
          {!editor.childVariantId &&
            child?.parameters.map((p) => (
              <ExpressionField kind="binding" resultParameter={p} key={p.key} label={`Значение внутри компонента: ${p.name}`} value={editor.bindings[p.key] ?? ""} parameters={parameters}
                emptyLabel={`Оставить исходное значение: ${parameterValue(p, p.default)}`}
                onChange={v => { const bindings = { ...editor.bindings }; if (v) bindings[p.key] = v; else delete bindings[p.key]; setEditor({ ...editor, bindings }); }} />
            ))}
          </details>
          <button className="btn-secondary" type="submit">
            Готово: добавить компонент в состав
          </button>
          <button
            className="btn-ghost"
            type="button"
            onClick={() => setEditor(null)}
          >
            Отмена
          </button>
        </form>
      )}
      </div>
      <div hidden={tab !== "rules"} className="bom-form">
      <h4>Параметры и допустимые значения</h4>
      {!historical && <button className="btn-secondary" disabled={busy} onClick={() => void run(importCatalogParameters)}>Подключить параметры из справочника</button>}
      {!historical && catalogChoices && catalogChoices.length > 0 && <fieldset className="card-nested bom-form"><legend>Какие параметры нужны этому изделию?</legend>
        <p className="hint">Отметьте только те параметры, которые пользователь будет выбирать при подборе изделия.</p>
        {catalogChoices.map(p => <label className="checkbox-label" key={p.key}><input type="checkbox" checked={selectedParameterKeys.includes(p.key)} onChange={e => setSelectedParameterKeys(current => e.target.checked ? [...current, p.key] : current.filter(key => key !== p.key))} />{p.name}</label>)}
        <button type="button" className="btn-secondary" disabled={!selectedParameterKeys.length} onClick={() => {
          const chosen = catalogChoices.filter(p => selectedParameterKeys.includes(p.key) && !parameters.some(existing => existing.key === p.key));
          setParameters([...parameters, ...chosen]);
          setEnumOptions(current => ({ ...current, ...Object.fromEntries(chosen.map(p => [p.key, p.enumLabels ?? {}])) }));
          setCatalogChoices(null);
          setNotice(`Подключены: ${chosen.map(p => p.name).join(", ")}. Сохраните шаг 1.`);
        }}>Подключить выбранные параметры</button>
        <button type="button" className="btn-ghost" onClick={() => setCatalogChoices(null)}>Отмена выбора параметров</button>
      </fieldset>}
      <SpecParameters parameters={historical?.parameters ?? parameters} options={enumOptions} onChange={setParameters} canRemove={canRemoveParameter} readOnly={!!historical} />
      {historical && (
        <div className="card-nested">
          <strong>Правила исторической версии</strong>
          {historical.effective?.rules.map((r, i) => (
            <p key={i}>
              {describeExpression(r.expression, expressionContext)} — {friendlyMessage(r.message)}
            </p>
          ))}
          {Object.entries(historical.effective?.calculations ?? {}).map(
            ([k, v]) => (
              <p key={k}>
                {configuration?.parameters.find(p => p.key === k)?.name ?? "Вычисляемое значение"}: {describeExpression(v, expressionContext)}
              </p>
            ),
          )}
        </div>
      )}
      {!historical && (
        <section className="bom-form">
          <details><summary>Совместимость и ограничения ({rules.length})</summary>
          <p className="hint">Ограничения проверяются при создании исполнения. Выбирайте названия компонентов и параметров; сообщение объяснит пользователю причину отказа.</p>
          <CompatibilityBuilder components={Array.from(new Map(effective.map(p => [p.componentId, { id: p.componentId, name: name(p.componentId) }])).values())} onAdd={r => setRules([...rules, r])} />
          {rules.map((r, i) => (
            <div className="bom-grid" key={i}>
              <ExpressionField label={`Ограничение ${i + 1}`} value={r.expression} parameters={parameters}
                components={Array.from(new Map(effective.map(p => [p.componentId, { id: p.componentId, name: name(p.componentId) }])).values())}
                onChange={v => setRules(rules.map((x, j) => j === i ? { ...x, expression: v } : x))} />
              <label>
                Причина отказа
                <input
                  className="input"
                  value={friendlyMessage(r.message)}
                  onChange={(e) =>
                    setRules(
                      rules.map((x, j) =>
                        j === i ? { ...x, message: e.target.value } : x,
                      ),
                    )
                  }
                />
              </label>
              <button
                className="btn-ghost"
                onClick={() => setRules(rules.filter((_, j) => j !== i))}
              >
                Удалить правило
              </button>
            </div>
          ))}
          <button
            className="btn-ghost"
            onClick={() =>
              setRules([...rules, { expression: writeExpression(defaultCondition(parameters)), message: "Выбранное сочетание параметров недопустимо." }])
            }
          >
            Добавить ограничение
          </button>
          <p className="hint">Для сложных случаев можно объединять условия словами «все» / «хотя бы одно» и запрещать выбранное сочетание.</p>
          </details>
          <details><summary>Вычисляемые параметры ({Object.keys(calculations).length})</summary>
          <p className="hint">Здесь можно получать значение автоматически, например длину линии по числу датчиков.</p>
          {parameters.map(p => <ExpressionField kind="formula" resultParameter={p} key={p.key} label={`Как получить «${p.name}»`} value={calculations[p.key] ?? ""} parameters={parameters}
            emptyLabel="Пользователь выбирает значение сам" onChange={v => { const c = { ...calculations }; if (v) c[p.key] = v; else delete c[p.key]; setCalculations(c); }} />)}
          </details>
        </section>
      )}
      </div>
      {(tab === "composition" || tab === "rules") && !historical && !editor && <div className="bom-form">
      <details><summary>Комментарий к изменению (необязательно)</summary>
      <label>
        Что изменилось
        <input
          className="input"
          maxLength={512}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
        />
      </label>
      </details>
      <button
        className="btn-primary"
        disabled={busy || !!editor || !spec}
        onClick={() =>
          void run(async () => {
            await apiPut(`/api/bom/products/${productId}`, {
              expectedRevisionId: spec?.revision?.id ?? null,
              unitId,
              positions,
              parameters,
              rules,
              calculations,
              reason: reason.trim() || (tab === "rules" ? "Изменены параметры изделия" : "Изменён состав изделия"),
            });
            await load();
            setReason("");
            setTab(tab === "rules" ? "composition" : "variants");
            setNotice("Шаг сохранён. Можно продолжать настройку.");
          })
        }
      >
        {tab === "rules" ? "Сохранить параметры → к компонентам" : "Сохранить состав → к проверке"}
      </button>
      </div>}
      {spec?.revision && (
        <>
          <div hidden={tab !== "history"} className="bom-form">
          <details open>
            <summary>Создать модификацию</summary>
            <p className="hint">Основа: версия {configuration?.revision?.id}. Наследуется состав этой версии, а собственные изменения сохраняются отдельно.</p>
            <label>
              Имя
              <input
                className="input"
                value={modName}
                onChange={(e) => setModName(e.target.value)}
              />
            </label>
            <label>
              Обозначение
              <input
                className="input"
                value={modShort}
                onChange={(e) => setModShort(e.target.value)}
              />
            </label>
            <button
              className="btn-secondary"
              disabled={busy || dirty || !!editor || !modName.trim() || !modShort.trim()}
              onClick={() =>
                void run(async () => {
                  await apiPost("/api/bom/modifications", {
                    baseRevisionId: configuration!.revision!.id,
                    name: modName,
                    shortName: modShort,
                    reason: "Создана модификация",
                  });
                  await onCreated();
                  setNotice(
                    "Модификация создана. Выберите её в каталоге изделий.",
                  );
                  setModName("");
                  setModShort("");
                })
              }
            >
              Создать из сохранённой версии
            </button>
          </details>
          <details open>
            <summary>История спецификации</summary>
            {history.map((h) => (
              <p key={h.id}>
                {new Date(h.createdAt).toLocaleString()} · {h.reason}{" "}
                <button
                  className="btn-ghost"
                  disabled={busy || !!editor}
                  onClick={() =>
                    void run(async () => {
                      setValues({});
                      setTab("composition");
                      setHistorical(
                        await apiGet<Spec>(
                          `/api/bom/products/${productId}?revisionId=${h.id}`,
                        ),
                      );
                    })
                  }
                >
                  Показать версию {h.id}
                </button>
              </p>
            ))}
          </details>
          </div>
          <div hidden={tab !== "variants"} className="bom-form">
          {historical && <p className="warning-banner">Конфигурируется историческая версия {historical.revision?.id}. <button onClick={() => { setHistorical(null); setValues({}); }}>Использовать текущую версию</button></p>}
          <h4>Выберите параметры готового изделия</h4>
          <p className="hint">
            Выберите нужные значения и нажмите «Проверить». Появится полный состав и расход. После проверки можно сохранить исполнение для заказа.
          </p>
          <label>
            Название исполнения (необязательно)
            <input
              className="input"
              value={variantName}
              onChange={(e) => setVariantName(e.target.value)}
            />
          </label>
          <VariantFields
            parameters={configuration!.parameters}
            calculations={configuration!.effective?.calculations ?? {}}
            values={values}
            onChange={setValues}
          />
          <div className="bom-grid">
            <button
              className="btn-secondary"
              disabled={busy || dirty || !!editor}
              onClick={() =>
                void run(async () => {
                  const generation = ++previewGeneration.current;
                  setPreview("");
                  setPreviewNode(null);
                  const v = await apiPost<ResolvedNode>("/api/bom/preview", {
                    revisionId: configuration!.revision!.id,
                    name: variantName || "Проверка",
                    values: variantValues(configuration!.parameters, values),
                  });
                  await describeConfiguration(v);
                  if (generation !== previewGeneration.current) return;
                  setPreviewNode(v);
                  setPreview(
                    `Конфигурация допустима. ${v.name}: ${v.children.length} позиций верхнего уровня.`,
                  );
                })
              }
            >
              Проверить изделие и показать состав
            </button>
            <button
              className="btn-primary"
              disabled={busy || dirty || !!editor}
              onClick={() =>
                void run(async () => {
                  const generation = ++previewGeneration.current;
                  setPreview("");
                  const v = await apiPost<Variant>("/api/bom/variants", {
                    revisionId: configuration!.revision!.id,
                    name: variantName.trim() || (`${name(productId)} — ` + configuration!.parameters.filter(p => !configuration!.effective?.calculations[p.key]).map(p => `${p.name}: ${parameterValue(p, values[p.key] ?? p.default)}`).join(", ")).slice(0, 256),
                    values: variantValues(configuration!.parameters, values),
                  });
                  setVariants(await apiGet<Variant[]>(`/api/bom/variants?productId=${productId}`));
                  setVariantId(v.id);
                  setNotice("Исполнение сохранено. Оно доступно для выбора в заказе.");
                  if (generation === previewGeneration.current) setPreview("Вариант сохранён.");
                })
              }
            >
              Сохранить исполнение для заказа
            </button>
          </div>
          {preview && <p className="success-banner" role="status">{preview}</p>}
          {previewNode && <ConfigurationResult node={previewNode} />}
          <details><summary>Ранее сохранённые исполнения и общий расход материалов</summary>
          <p className="hint">Выберите исполнение, укажите нужное количество изделий и рассчитайте материалы.</p>
          <label>
            Ранее сохранённое исполнение
            <select
              className="input"
              value={variantId}
              onChange={(e) => {
                setVariantId(Number(e.target.value));
                setTree([]);
                setTotals([]);
              }}
            >
              <option value="0">— выберите исполнение —</option>
              {variants.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name} · версия {v.revisionId}
                </option>
              ))}
            </select>
          </label>
          <button className="btn-ghost" disabled={busy || !variantId} onClick={() => void run(async () => {
            const selected = variantId;
            const generation = calculationGeneration.current;
            setSavedNode(null);
            const v = await apiGet<{ snapshot: string }>(`/api/bom/variants/${selected}`);
            const node = JSON.parse(v.snapshot) as ResolvedNode;
            await describeConfiguration(node);
            if (generation === calculationGeneration.current) { setSavedNode(node); setNotice("Параметры готового исполнения загружены."); }
          })}>Показать параметры готового исполнения</button>
          {savedNode && <ConfigurationResult node={savedNode} />}
          <div className="bom-grid">
            <label>
              Количество изделий
              <input
                className="input"
                type="number"
                min="0.000000001"
                step="any"
                value={quantity}
                onChange={(e) => setQuantity(Number(e.target.value))}
              />
            </label>
            <label>
              Класс материалов
              <select
                className="input"
                value={classId}
                onChange={(e) => setClassId(e.target.value)}
              >
                <option value="">Все конечные компоненты</option>
                {classes.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name} с подклассами
                  </option>
                ))}
              </select>
            </label>
          </div>
          <button
            className="btn-secondary"
            disabled={!variantId || busy || !Number.isFinite(quantity) || quantity <= 0}
            onClick={() =>
              void run(async () => {
                const generation = ++calculationGeneration.current;
                setTree([]);
                setTotals([]);
                const [t, n] = await Promise.all([
                  apiGet<Tree[]>(
                    `/api/bom/variants/${variantId}/tree?quantity=${quantity}`,
                  ),
                  apiGet<Total[]>(
                    `/api/bom/variants/${variantId}/totals?quantity=${quantity}${classId ? `&classId=${classId}` : ""}`,
                  ),
                ]);
                if (generation !== calculationGeneration.current) return;
                setTree(t);
                setTotals(n);
              })
            }
          >
            Показать состав и рассчитать
          </button>
          {tree.length > 0 && (
            <>
              <div className="table-scroll">
                <table className="data data-table">
                  <thead>
                    <tr>
                      <th>Уровень</th>
                      <th>Изделие</th>
                      <th>Норма / для количества</th>
                      <th>Количество</th>
                      <th>Единица</th>
                    </tr>
                  </thead>
                  <tbody>
                    {tree.map((t) => (
                      <tr key={t.path}>
                        <td>{t.depth}</td>
                        <td style={{ paddingLeft: `${.8 + t.depth * 1.2}rem` }}>
                          {t.depth > 0 ? "↳ " : ""}{t.name} · v{t.revision_id}
                        </td>
                        <td>
                          {t.norm} / {t.for_quantity}
                        </td>
                        <td>{t.required_quantity}</td>
                        <td>{t.unit}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <TotalsTable rows={totals} />
            </>
          )}
          </details>
          </div>
        </>
      )}
      <details><summary>Отменить изменения / обновить данные</summary>
      <button className="btn-ghost" disabled={busy} onClick={() => {
        if ((dirty || editor) && !window.confirm("Отменить несохранённые правки спецификации?")) return;
        void run(async () => { await load(); setEditor(null); setReason(""); setHistorical(null); });
      }}>Загрузить сохранённую спецификацию</button>
      </details>
      {!spec?.revision && tab === "variants" && <p className="hint">Сначала пройдите шаги 1 и 2 и сохраните настройки изделия.</p>}
      </fieldset>
    </section>
  );
}
