export const API_ENDPOINTS = {
  auth: '/auth',
  projects: '/projects',
  engineers: '/engineers',
  assignments: '/project-assignments',
  tasks: '/tasks',
  documents: '/documents',
  dashboard: '/dashboard'
} as const;

export const STORAGE_KEYS = {
  token: 'cpt_token',
  user: 'cpt_user'
} as const;
