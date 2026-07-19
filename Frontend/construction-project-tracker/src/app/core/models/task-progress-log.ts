export interface TaskProgressLog {
  id: number;
  taskId: number;
  engineerId: number;
  engineerName: string;
  previousProgress: number;
  newProgress: number;
  description: string;
  createdAt: string;
}

export interface CreateTaskProgressLog {
  newProgress: number;
  description: string;
}
