import type { Role } from './auth'

/** A user account as seen by an administrator (GET /users). */
export interface User {
  id: number
  userName: string
  email: string | null
  role: Role
  mustChangePassword: boolean
  isLockedOut: boolean
  createdUtc: string
  lastLoginUtc: string | null
}

/** Body for POST /users. */
export interface CreateUserRequest {
  userName: string
  email: string | null
  password: string
  role: Role
}

/** Body for PUT /users/{id}. */
export interface UpdateUserRequest {
  email: string | null
  role: Role
}

/** Body for POST /users/{id}/password. */
export interface ResetPasswordRequest {
  newPassword: string
}
