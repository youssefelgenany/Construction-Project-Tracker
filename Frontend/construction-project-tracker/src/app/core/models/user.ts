import { UserRole } from '../enums/user-role';

export interface User {
  id: number;
  fullName: string;
  email: string;
  role: UserRole | string;
}
