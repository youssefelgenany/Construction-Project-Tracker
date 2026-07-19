import { TaskDependency, TimelineTask } from './task-dependency.model';

export interface ProjectTimeline {
  projectId: number;
  projectName: string;
  projectStartDate: string;
  projectEndDate: string;
  tasks: TimelineTask[];
  dependencies: TaskDependency[];
}
