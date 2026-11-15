import { apiGet, apiPost, apiPut } from "./client";
import type {
  RouteDocument,
  RouteSetting,
  RouteCalculation,
} from "../types/routes";
export const readRoute = (id: number) =>
  apiGet<RouteDocument>(`/api/routes/${id}`);
export const saveRoute = (document: RouteDocument) => {
  const { setting, steps, inputs } = document;
  const editable = (id: number) =>
    id <= 0 ||
    !document.editableStepIds ||
    document.editableStepIds.includes(id);
  const payload = {
    ...setting,
    steps: steps.filter((s) => editable(s.id)),
    inputs: inputs.filter((i) => editable(i.stepId)),
  };
  return setting.id
    ? apiPut<RouteDocument>(`/api/routes/${setting.id}`, payload)
    : apiPost<RouteDocument>("/api/routes", payload);
};
export const applyRoute = (id: number, setting: RouteSetting) =>
  apiPost<RouteDocument>(`/api/routes/${id}/apply`, setting);
export const calculateRoute = (id: number, q: number, operationId?: number) =>
  apiGet<RouteCalculation>(
    `/api/routes/${id}/calculate?quantity=${q}${operationId ? `&operationId=${operationId}` : ""}`,
  );

export const createDepartment = (name: string, typeId: number) =>
  apiPost<{ id: number; name: string; typeId: number }>(
    "/api/routes/departments",
    { name, typeId },
  );
export const createWorkCenter = (
  name: string,
  typeId: number,
  departmentId: number,
) =>
  apiPost<{ id: number; name: string; typeId: number; departmentId: number }>(
    "/api/routes/work-centers",
    { name, typeId, departmentId },
  );
