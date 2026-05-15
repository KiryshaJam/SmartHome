import { useEffect, useRef, useState, type FormEvent } from "react";
import { apiGet, apiPost } from "../api/client";
import type { ClassNodeDto, CreateClassNodeDto, MeasureUnitDto, TreeNodeDto } from "../types/api";

function TreeBranch({
  node,
  depth,
  selectedId,
  onSelect,
}: {
  node: TreeNodeDto;
  depth: number;
  selectedId: number | null;
  onSelect: (id: number) => void;
}) {
  const [open, setOpen] = useState(depth < 2);
  const hasChildren = node.children.length > 0;
  const rowSel = selectedId === node.id;

  return (
    <div
      style={{
        marginLeft: depth ? "0.85rem" : 0,
        borderLeft: depth ? "1px dashed var(--border)" : "none",
        paddingLeft: depth ? "0.65rem" : 0,
      }}
    >
      <div
        onClick={() => onSelect(node.id)}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            onSelect(node.id);
          }
        }}
        role="button"
        tabIndex={0}
        style={{
          display: "flex",
          alignItems: "center",
          gap: "0.5rem",
          padding: "0.35rem 0.4rem",
          flexWrap: "wrap",
          borderRadius: "8px",
          cursor: "pointer",
          background: rowSel ? "var(--warm-soft)" : undefined,
          outline: "none",
        }}
      >
        {hasChildren ? (
          <button
            type="button"
            className="btn-ghost"
            style={{ padding: "0.2rem 0.45rem", minWidth: "1.75rem" }}
            onClick={(e) => {
              e.stopPropagation();
              setOpen((o) => !o);
            }}
            aria-expanded={open}
          >
            {open ? "▼" : "▶"}
          </button>
        ) : (
          <span style={{ width: "1.75rem", display: "inline-block" }} />
        )}
        <span className="mono" style={{ color: "var(--text-muted)", fontSize: "0.82rem" }}>
          {node.shortName}
        </span>
        <span style={{ fontWeight: 600 }}>{node.name}</span>
        {node.isTerminal ? (
          <span className="badge badge-terminal">конечный</span>
        ) : (
          <span className="badge badge-folder">группа</span>
        )}
      </div>
      {hasChildren && open && (
        <div>
          {node.children.map((ch) => (
            <TreeBranch
              key={ch.id}
              node={ch}
              depth={depth + 1}
              selectedId={selectedId}
              onSelect={onSelect}
            />
          ))}
        </div>
      )}
    </div>
  );
}

export function ClassifierPage() {
  const [roots, setRoots] = useState<ClassNodeDto[] | null>(null);
  const [treeByRoot, setTreeByRoot] = useState<Record<number, TreeNodeDto>>({});
  const loadedRootsRef = useRef(new Set<number>());
  const [loadingRoots, setLoadingRoots] = useState(true);
  const [err, setErr] = useState<string | null>(null);
  const [loadingTreeId, setLoadingTreeId] = useState<number | null>(null);
  const [selectedNodeId, setSelectedNodeId] = useState<number | null>(null);
  const [parentMode, setParentMode] = useState<"root" | "under">("under");
  const [measureUnits, setMeasureUnits] = useState<MeasureUnitDto[] | null>(null);

  const [nnName, setNnName] = useState("");
  const [nnShort, setNnShort] = useState("");
  const [nnTerminal, setNnTerminal] = useState(false);
  const [nnSort, setNnSort] = useState(1);
  const [nnMu, setNnMu] = useState<number | "">("");
  const [saving, setSaving] = useState(false);

  async function refreshTrees() {
    const r = await apiGet<ClassNodeDto[]>("/api/classnodes/roots");
    setRoots(r);
    const next: Record<number, TreeNodeDto> = {};
    for (const id of loadedRootsRef.current) {
      try {
        next[id] = await apiGet<TreeNodeDto>(`/api/classnodes/${id}/descendants`);
      } catch {
        /* корень мог исчезнуть */
      }
    }
    setTreeByRoot(next);
  }

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [r, m] = await Promise.all([
          apiGet<ClassNodeDto[]>("/api/classnodes/roots"),
          apiGet<MeasureUnitDto[]>("/api/measure-units").catch(() => [] as MeasureUnitDto[]),
        ]);
        if (!cancelled) {
          setRoots(r);
          setMeasureUnits(m);
          if (r.length === 1) {
            const id = r[0]!.id;
            setLoadingTreeId(id);
            loadedRootsRef.current.add(id);
            try {
              const tree = await apiGet<TreeNodeDto>(`/api/classnodes/${id}/descendants`);
              if (!cancelled) setTreeByRoot((prev) => ({ ...prev, [id]: tree }));
            } finally {
              if (!cancelled) setLoadingTreeId(null);
            }
          }
        }
      } catch (e) {
        if (!cancelled) setErr(e instanceof Error ? e.message : "Ошибка");
      } finally {
        if (!cancelled) setLoadingRoots(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  async function loadTree(rootId: number) {
    setLoadingTreeId(rootId);
    setErr(null);
    try {
      const tree = await apiGet<TreeNodeDto>(`/api/classnodes/${rootId}/descendants`);
      loadedRootsRef.current.add(rootId);
      setTreeByRoot((prev) => ({ ...prev, [rootId]: tree }));
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Ошибка");
    } finally {
      setLoadingTreeId(null);
    }
  }

  async function handleCreateNode(e: FormEvent) {
    e.preventDefault();
    let parentId: number | null = null;
    if (parentMode === "under") {
      if (selectedNodeId == null) {
        setErr("Выберите узел в дереве кликом или включите режим «Новый корень».");
        return;
      }
      parentId = selectedNodeId;
    }
    setSaving(true);
    setErr(null);
    const body: CreateClassNodeDto = {
      name: nnName.trim(),
      shortName: nnShort.trim(),
      isTerminal: nnTerminal,
      sortOrder: nnSort,
      parentId,
      measureUnitId: nnTerminal && nnMu !== "" ? Number(nnMu) : null,
    };
    try {
      const created = await apiPost<ClassNodeDto, CreateClassNodeDto>("/api/classnodes", body);
      setNnName("");
      setNnShort("");
      setNnTerminal(false);
      setNnSort(1);
      setNnMu("");
      setSelectedNodeId(created.id);
      await refreshTrees();
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : "Не удалось создать узел");
    } finally {
      setSaving(false);
    }
  }

  const selectedLabel =
    selectedNodeId != null
      ? (() => {
          const flat = (n: TreeNodeDto): TreeNodeDto | undefined => {
            if (n.id === selectedNodeId) return n;
            for (const c of n.children) {
              const f = flat(c);
              if (f) return f;
            }
            return undefined;
          };
          for (const r of roots ?? []) {
            const t = treeByRoot[r.id];
            if (t) {
              const hit = flat(t);
              if (hit) return `${hit.name} (${hit.shortName})`;
            }
          }
          return `id ${selectedNodeId}`;
        })()
      : null;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
      <header>
        <h1 style={{ margin: 0, fontSize: "1.5rem" }}>Классификатор</h1>
        <p className="hint" style={{ marginTop: "0.35rem" }}>
          Кликните по узлу, чтобы выбрать родителя для нового класса. Можно добавить корневой узел или ветвь ниже выбранного.
        </p>
      </header>
      {err && <div className="error-banner">{err}</div>}
      <div className="card" style={{ padding: "1.15rem" }}>
        {loadingRoots && <div className="hint">Загрузка корней…</div>}
        {roots && roots.length === 0 && <div className="hint">Корневые узлы не найдены.</div>}
        {roots && roots.length > 1 && (
          <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem", marginBottom: "1rem" }}>
            {roots.map((r) => (
              <button
                key={r.id}
                type="button"
                className="btn-ghost"
                disabled={loadingTreeId === r.id}
                onClick={() => void loadTree(r.id)}
              >
                Загрузить дерево: {r.name}
              </button>
            ))}
          </div>
        )}
        {roots?.map((r) => {
          const tree = treeByRoot[r.id];
          return (
            <div key={r.id} style={{ marginBottom: "1.25rem" }}>
              <div style={{ display: "flex", alignItems: "center", gap: "0.75rem", flexWrap: "wrap" }}>
                <h2 style={{ margin: 0, fontSize: "1.1rem" }}>{r.name}</h2>
                {!tree && roots.length > 1 && (
                  <button type="button" className="btn-primary" onClick={() => void loadTree(r.id)}>
                    Показать дерево
                  </button>
                )}
                {loadingTreeId === r.id && <span className="hint">Загрузка…</span>}
              </div>
              {tree && (
                <TreeBranch
                  node={tree}
                  depth={0}
                  selectedId={selectedNodeId}
                  onSelect={(id) => setSelectedNodeId(id)}
                />
              )}
            </div>
          );
        })}
      </div>

      <div className="card" style={{ padding: "1.15rem", maxWidth: "560px" }}>
        <h2 style={{ margin: "0 0 0.75rem", fontSize: "1.1rem" }}>Новый узел классификатора</h2>
        <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem", marginBottom: "0.75rem" }}>
          <button
            type="button"
            className={parentMode === "under" ? "btn-primary" : "btn-ghost"}
            onClick={() => setParentMode("under")}
          >
            Под выбранным в дереве
          </button>
          <button
            type="button"
            className={parentMode === "root" ? "btn-primary" : "btn-ghost"}
            onClick={() => setParentMode("root")}
          >
            Новый корневой узел
          </button>
        </div>
        {parentMode === "under" && (
          <p className="hint" style={{ marginBottom: "0.75rem" }}>
            Родитель: {selectedLabel ?? "не выбран — кликните узел в дереве выше"}
          </p>
        )}
        {parentMode === "root" && (
          <p className="hint" style={{ marginBottom: "0.75rem" }}>
            Узел будет создан без родителя (ещё один корень рядом с «Умный дом»).
          </p>
        )}
        <form style={{ display: "grid", gap: "0.65rem" }} onSubmit={(e) => void handleCreateNode(e)}>
          <div>
            <label>Название</label>
            <input className="input" required value={nnName} onChange={(e) => setNnName(e.target.value)} />
          </div>
          <div>
            <label>Краткое имя (уникальное)</label>
            <input className="input" required value={nnShort} onChange={(e) => setNnShort(e.target.value)} />
          </div>
          <div style={{ maxWidth: "160px" }}>
            <label>Порядок сортировки</label>
            <input
              className="input mono"
              type="number"
              value={nnSort}
              onChange={(e) => setNnSort(Number.parseInt(e.target.value, 10) || 0)}
            />
          </div>
          <label style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
            <input type="checkbox" checked={nnTerminal} onChange={(e) => setNnTerminal(e.target.checked)} />
            Конечный класс (категория устройств)
          </label>
          {nnTerminal && (
            <div>
              <label>Единица измерения категории (необязательно)</label>
              <select
                className="input"
                value={nnMu === "" ? "" : String(nnMu)}
                onChange={(e) => setNnMu(e.target.value ? Number.parseInt(e.target.value, 10) : "")}
              >
                <option value="">— не задана —</option>
                {measureUnits?.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.name} ({u.shortName})
                  </option>
                ))}
              </select>
            </div>
          )}
          <button type="submit" className="btn-warm" disabled={saving}>
            {saving ? "Создание…" : "Создать узел"}
          </button>
        </form>
      </div>
    </div>
  );
}
