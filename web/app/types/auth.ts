/** Application roles. */
export type Role = 'Admin' | 'User'

/** The signed-in user (GET /auth/me, POST /auth/login). */
export interface CurrentUser {
  id: number
  userName: string
  email: string | null
  role: Role
  mustChangePassword: boolean
}

/** Body for POST /auth/login. */
export interface LoginRequest {
  userName: string
  password: string
  rememberMe: boolean
}

/** Body for POST /auth/me/password. */
export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}
