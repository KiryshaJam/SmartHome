import { createContext, useContext, useEffect, useId, useRef, type ReactNode } from "react";

const ChangesContext = createContext<{
  entries: Map<string, boolean>;
  confirmLeave: () => boolean;
} | null>(null);

export function UnsavedChangesProvider({ children }: { children: ReactNode }) {
  const entries = useRef(new Map<string, boolean>()).current;
  const value = useRef({
    entries,
    confirmLeave: () => ![...entries.values()].some(Boolean) ||
      window.confirm("Есть несохранённые изменения. Перейти и потерять их?"),
  }).current;
  useEffect(() => {
    function beforeUnload(e: BeforeUnloadEvent) {
      if ([...entries.values()].some(Boolean)) { e.preventDefault(); e.returnValue = ""; }
    }
    window.addEventListener("beforeunload", beforeUnload);
    return () => window.removeEventListener("beforeunload", beforeUnload);
  }, [entries]);
  return <ChangesContext.Provider value={value}>{children}</ChangesContext.Provider>;
}
export function useConfirmLeave() {
  const context = useContext(ChangesContext);
  if (!context) throw new Error("UnsavedChangesProvider required");
  return context.confirmLeave;
}
export function useUnsavedChanges(dirty: boolean) {
  const context = useContext(ChangesContext);
  const id = useId();
  useEffect(() => {
    context?.entries.set(id, dirty);
    return () => { context?.entries.delete(id); };
  }, [context, id, dirty]);
}
