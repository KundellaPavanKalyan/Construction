export interface Employee {
  employeeId: number;
  employeeName: string;
  qualification: string | null;
  role: string | null;
  dailyWage: number;
}

export interface PayrollRow {
  payrollId: number;
  employeeId: number;
  employeeName: string;
  qualification: string | null;
  role: string | null;
  dailyWage: number;
  daysPresent: number;
  monthlySalary: number;
  otHours: number;
  otAmount: number;
  advanceAmount: number;
  totalAmount: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateEmployeeRequest {
  employeeName: string;
  qualification?: string | null;
  role?: string | null;
  dailyWage: number;
}

export interface CreatePayrollMonthRequest {
  month: number;
  year: number;
}

export interface UpdatePayrollRequest {
  daysPresent: number;
  otHours: number;
  advanceAmount: number;
}

export interface AttendanceEmployeeRow {
  employeeId: number;
  employeeName: string;
  role: string | null;
  dailyWage: number;
  payrollId: number;
  advanceAmount: number;
  presentByDayJson: string;
  otByDayJson: string;
}

export interface SaveAttendanceRequest {
  month: number;
  year: number;
  rows: { employeeId: number; presentByDayJson: string; otByDayJson: string }[];
}

export interface InvoiceListItem {
  invoiceId: number;
  originalFileName: string;
  invoiceNumber: string | null;
  invoiceDate: string | null;
  vendorName: string | null;
  sgstAmount: number | null;
  cgstAmount: number | null;
  igstAmount: number | null;
  transportCharges: number | null;
  basicTotal: number | null;
  totalAmount: number | null;
  projectName: string | null;
  extractionStatus: string;
}

export interface InvoiceDetail extends InvoiceListItem {
  extractionNotes: string | null;
  extractedText: string | null;
}

export interface ImpressRow {
  employeeId: number;
  employeeName: string;
  payrollId: number;
  week1: number;
  week2: number;
  week3: number;
  week4: number;
  total: number;
}

export interface SaveImpressRequest {
  month: number;
  year: number;
  rows: { employeeId: number; week1: number; week2: number; week3: number; week4: number }[];
}

export interface MonthlyTrackingRow {
  monthlyTrackingId: number;
  month: number;
  year: number;
  projectSiteName: string;
  workDescription: string | null;
  status: string;
  remarks: string | null;
  recordedAtUtc: string;
}

export interface SaveMonthlyTrackingRequest {
  month: number;
  year: number;
  projectSiteName: string;
  workDescription?: string | null;
  status?: string;
  remarks?: string | null;
}

export interface MaterialTrackingRow {
  materialTrackingId: number;
  month: number;
  year: number;
  materialName: string;
  quantity: number;
  unit: string;
  unitRate: number;
  totalAmount: number;
  supplierName: string | null;
  receivedDate: string | null;
  remarks: string | null;
  recordedAtUtc: string;
}

export interface SaveMaterialTrackingRequest {
  month: number;
  year: number;
  materialName: string;
  quantity: number;
  unit?: string;
  unitRate: number;
  supplierName?: string | null;
  receivedDate?: string | null;
  remarks?: string | null;
}
