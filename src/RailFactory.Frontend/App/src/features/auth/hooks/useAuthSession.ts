import { useAuthSessionContext } from '../context/AuthSessionContext';

export function useAuthSession(tenantCode: string) {
  void tenantCode;
  return useAuthSessionContext();
}
