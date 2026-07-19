export interface Assignment {
  id: number;
  projectId: number;
  projectName: string;
  engineerId: number;
  engineerName: string;
  assignedDate: string;
}

export interface AssignEngineer {
  projectId: number;
  engineerId: number;
}
