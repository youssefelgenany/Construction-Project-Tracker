export interface Engineer {
  id: number;
  fullName: string;
  email: string;
  phoneNumber: string;
  position: string;
  hireDate: string;
  isActive: boolean;
}

export interface EngineerDetails extends Engineer {
  assignedProjectsCount: number;
  assignedTasksCount: number;
}

export interface CreateEngineer {
  fullName: string;
  email: string;
  password: string;
  phoneNumber: string;
  position: string;
  hireDate: string;
}

export interface UpdateEngineer {
  fullName: string;
  email: string;
  phoneNumber: string;
  position: string;
  hireDate: string;
  isActive: boolean;
}
