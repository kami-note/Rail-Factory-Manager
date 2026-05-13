import React, { useEffect, useState } from 'react';
import {
  Box,
  Typography,
  Paper,
  Stack,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  CircularProgress,
  Alert,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Autocomplete,
  Avatar
} from '@mui/material';
import { Users, UserPlus, Shield, X, Trash2, Mail, Save } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';

interface Role {
  id: string;
  name: string;
}

interface TenantUser {
  email: string;
  name?: string;
  roles: { roleId: string; roleName: string }[];
}

interface UsersManagementPageProps {
  tenantCode: string;
}

export function UsersManagementPage({ tenantCode }: UsersManagementPageProps) {
  const [users, setUsers] = useState<TenantUser[]>([]);
  const [availableRoles, setAvailableRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [usersData, rolesData] = await Promise.all([
        fetchJsonOrThrow<TenantUser[]>('/api/iam/users', { headers: buildTenantHeaders(tenantCode), credentials: 'include' }, 'Erro ao carregar usuários'),
        fetchJsonOrThrow<Role[]>('/api/iam/roles', { headers: buildTenantHeaders(tenantCode), credentials: 'include' }, 'Erro ao carregar perfis')
      ]);
      setUsers(usersData);
      setAvailableRoles(rolesData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao carregar usuários.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, [tenantCode]);

  const handleAssignRole = async () => {
    if (!inviteEmail || !selectedRoleId) return;
    setIsSubmitting(true);
    try {
      await fetchJsonOrThrow(`/api/iam/users/${inviteEmail}/roles`, {
        method: 'POST',
        headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ roleId: selectedRoleId })
      }, 'Erro ao atribuir perfil');
      setIsInviteModalOpen(false);
      setInviteEmail('');
      setSelectedRoleId(null);
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao atribuir papel.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleRemoveRole = async (email: string, roleId: string) => {
    if (!window.confirm(`Remover este papel do usuário ${email}?`)) return;
    try {
      await fetchJsonOrThrow(`/api/iam/users/${email}/roles/${roleId}`, {
        method: 'DELETE',
        headers: buildTenantHeaders(tenantCode),
        credentials: 'include'
      }, 'Erro ao remover perfil');
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao remover papel.');
    }
  };

  return (
    <Box sx={{ p: 4 }}>
      <ModuleHeader 
        label="USUÁRIOS E PERMISSÕES" 
        icon={<Users size={20} />}
        action={
          <Button 
            variant="contained" 
            startIcon={<UserPlus size={18} />} 
            onClick={() => setIsInviteModalOpen(true)}
            sx={{ fontWeight: 800 }}
          >
            Vincular Usuário
          </Button>
        }
      />

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}><CircularProgress /></Box>
      ) : error ? (
        <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>
      ) : (
        <TableContainer component={Paper} variant="outlined" sx={{ borderRadius: 2 }}>
          <Table>
            <TableHead>
              <TableRow sx={{ bgcolor: 'grey.50' }}>
                <TableCell sx={{ fontWeight: 800 }}>USUÁRIO</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>PAPEIS ATRIBUÍDOS</TableCell>
                <TableCell align="right" sx={{ fontWeight: 800 }}>AÇÕES</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {users.map((user) => (
                <TableRow key={user.email} hover>
                  <TableCell sx={{ width: '40%' }}>
                    <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
                      <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.light', fontSize: '0.8rem' }}>
                        {user.email.charAt(0).toUpperCase()}
                      </Avatar>
                      <Box>
                        <Typography variant="body2" sx={{ fontWeight: 700 }}>{user.name || 'Usuário Google'}</Typography>
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <Mail size={12} /> {user.email}
                        </Typography>
                      </Box>
                    </Stack>
                  </TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
                      {user.roles.map(r => (
                        <Chip 
                          key={r.roleId} 
                          label={r.roleName} 
                          size="small" 
                          onDelete={() => handleRemoveRole(user.email, r.roleId)}
                          icon={<Shield size={12} />}
                          sx={{ fontWeight: 600 }}
                        />
                      ))}
                      {user.roles.length === 0 && (
                        <Typography variant="caption" color="text.disabled">Sem papéis ativos</Typography>
                      )}
                    </Stack>
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title="Atribuir novo papel">
                      <IconButton size="small" onClick={() => {
                        setInviteEmail(user.email);
                        setIsInviteModalOpen(true);
                      }}>
                        <UserPlus size={18} />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
              {users.length === 0 && (
                <TableRow>
                  <TableCell colSpan={3} align="center" sx={{ py: 6, color: 'text.disabled' }}>
                    Nenhum usuário vinculado a este tenant ainda.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Dialog open={isInviteModalOpen} onClose={() => setIsInviteModalOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle sx={{ fontWeight: 900 }}>Vincular Papel a Usuário</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={3} sx={{ mt: 1 }}>
            <TextField 
              label="E-mail do Usuário" 
              fullWidth 
              required 
              value={inviteEmail}
              onChange={e => setInviteEmail(e.target.value)}
              placeholder="exemplo@email.com"
              helperText="O usuário deve possuir uma conta Google."
            />
            
            <Autocomplete
              options={availableRoles}
              getOptionLabel={(option) => option.name}
              onChange={(_, newValue) => setSelectedRoleId(newValue?.id || null)}
              renderInput={(params) => (
                <TextField {...params} variant="outlined" label="Selecionar Papel" placeholder="Escolha um perfil..." />
              )}
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setIsInviteModalOpen(false)} color="inherit" startIcon={<X size={18} />}>Cancelar</Button>
          <Button 
            variant="contained" 
            onClick={handleAssignRole} 
            disabled={!inviteEmail || !selectedRoleId || isSubmitting}
            startIcon={isSubmitting ? <CircularProgress size={18} color="inherit" /> : <Save size={18} />}
            sx={{ fontWeight: 800 }}
          >
            Confirmar Vínculo
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
