export interface Choice {
  id: number;
  name: string;
}
export interface RouteProduct extends Choice {
  unitId: number | null;
}
export interface RouteSetting {
  id: number;
  name: string;
  productId: number;
  revisionId: number | null;
  kind: string;
  finalStepId: number;
  parameterId: number | null;
  parameterValue: number | null;
}
export interface RouteRow extends RouteSetting {
  productName: string;
  revisionName: string | null;
}
export interface TechStep {
  id: number;
  kind: string;
  productId: number;
  resultName: string;
  unitId: number;
  number: number;
  operationId: number | null;
  workCenterId: number | null;
  professionId: number | null;
  qualificationId: number | null;
  tooling: string;
  setupMinutes: number;
  unitMinutes: number;
  hourlyRate: number;
}
export interface TechInput {
  id?: number;
  stepId: number;
  sourceId: number;
  amount: number;
  forQuantity: number;
  parameterId: number | null;
}
export interface RouteDocument {
  editableStepIds?: number[];
  setting: RouteSetting;
  steps: TechStep[];
  inputs: TechInput[];
}
export interface RouteLookups {
  products: RouteProduct[];
  units: (Choice & { shortName: string })[];
  operations: Choice[];
  professions: Choice[];
  qualifications: Choice[];
  departmentTypes: Choice[];
  centerTypes: Choice[];
  departments: (Choice & { typeId: number })[];
  centers: (Choice & {
    department: string;
    departmentId: number;
    typeId: number;
  })[];
  parameters: RouteProduct[];
  variants: (Choice & { productId: number })[];
  sources: {
    id: number;
    resultName: string;
    productId: number;
    unitId: number;
  }[];
}
export interface RouteCalculation {
  quantity: number;
  operationId: number;
  materialCost: number | null;
  laborCost: number;
  minutes: number;
  total: number | null;
  unitCost: number | null;
  missingPrices: string[];
  tree: {
    path: number[];
    depth: number;
    step_id: number;
    parent_id: number | null;
    required_quantity: number;
    kind: string;
    number: number;
    result_name: string;
    unit: string;
  }[];
  inputs: {
    source_id: number;
    name: string;
    kind: string;
    unit: string;
    amount: number;
    for_quantity: number;
    required_quantity: number;
  }[];
  materials: {
    product_id: number;
    name: string;
    unit: string;
    required_quantity: number;
    price: number | null;
    cost: number | null;
  }[];
  labor: {
    step_id: number;
    number: number;
    name: string;
    profession: string;
    required_quantity: number;
    setup_minutes: number;
    unit_minutes: number;
    minutes: number;
    hourly_rate: number;
    cost: number;
  }[];
}
