export enum UserRole {
  Admin = 0,
  Engineer = 1
}

export function parseUserRole(role: string | number): UserRole {
  if (typeof role === 'number') {
    return role as UserRole;
  }

  return role === 'Admin' ? UserRole.Admin : UserRole.Engineer;
}
