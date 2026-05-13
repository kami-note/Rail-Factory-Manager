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
  Divider,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormGroup,
  FormControlLabel,
  Checkbox,
  Autocomplete,
  alpha,
  useTheme
} from '@mui/material';
import { ShieldCheck, Plus, Save, X, Layers } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';

interface Role {
  id: string;
  name: string;
  description: string;
  permissions: string[];
  childRoleIds: string[];
}

interface RolesManagementPageProps {
  tenantCode: string;
}

/**
 * Page for managing tenant-specific hierarchical roles.
 * Allows administrators to compose roles from atomic permissions and other roles.
 */
export function RolesManagementPage({ tenantCode }: RolesManagementPageProps) {
  const theme = useTheme();
  const [roles, setRoles] = useState<Role[]>([]);
  const [availablePermissions, setAvailablePermissions] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [newRole, setNewRole] = useState({
    name: '',
    description: '',
    permissions: [] as string[],
    childRoleIds: [] as string[]
  });

  const loadData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [rolesData, permsData] = await Promise.all([
        fetchJsonOrThrow<Role[]>('/api/iam/roles', { headers: buildTenantHeaders(tenantCode), credentials: 'include' }, 'Erro ao carregar perfis'),
        fetchJsonOrThrow<string[]>('/api/iam/permissions', { headers: buildTenantHeaders(tenantCode), credentials: 'include' }, 'Erro ao carregar permissões')
      ]);
      setRoles(rolesData);
      setAvailablePermissions(permsData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao carregar dados de acesso.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, [tenantCode]);

  const handleCreateRole = async () => {
    setIsSubmitting(true);
    try {
      await fetchJsonOrThrow('/api/iam/roles', {
        method: 'POST',
        headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(newRole)
      }, 'Erro ao criar papel');
      setIsModalOpen(false);
      setNewRole({ name: '', description: '', permissions: [], childRoleIds: [] });
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao criar papel.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const togglePermission = (perm: string) => {
    setNewRole(prev => ({
      ...prev,
      permissions: prev.permissions.includes(perm)
        ? prev.permissions.filter(p => p !== perm)
        : [...prev.permissions, perm]
    }));
  };

  return (
    <Box sx={{ p: 4 }}>
      <ModuleHeader 
        label="GESTÃO DE ACESSO E PERFIS" 
        icon={<ShieldCheck size={20} />}
        action={
          <Button 
            variant="contained" 
            startIcon={<Plus size={18} />} 
            onClick={() => setIsModalOpen(true)}
            sx={{ fontWeight: 800 }}
          >
            Novo Papel
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
                <TableCell sx={{ fontWeight: 800 }}>NOME DO PAPEL</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>COMPOSIÇÃO / HERANÇA</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>PERMISSÕES ATÔMICAS</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {roles.map((role) => (
                <TableRow key={role.id} hover>
                  <TableCell sx={{ width: '25%' }}>
                    <Typography variant="body2" sx={{ fontWeight: 700, color: 'primary.main' }}>{role.name}</Typography>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>{role.description || 'Sem descrição'}</Typography>
                  </TableCell>
                  <TableCell sx={{ width: '25%' }}>
                    {role.childRoleIds.length > 0 ? (
                      <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5 }}>
                        {role.childRoleIds.map(childId => {
                          const child = roles.find(r => r.id === childId);
                          return (
                            <Chip 
                              key={childId} 
                              icon={<Layers size={10} />}
                              label={child?.name || 'Role desconhecida'} 
                              size="small" 
                              sx={{ fontSize: '0.6rem', fontWeight: 700, bgcolor: alpha(theme.palette.secondary.main, 0.1) }} 
                            />
                          );
                        })}
                      </Stack>
                    ) : (
                      <Typography variant="caption" color="text.disabled">Papel Base (Simples)</Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5 }}>
                      {role.permissions.map(p => (
                        <Chip key={p} label={p} size="small" variant="outlined" sx={{ fontSize: '0.65rem', fontWeight: 600 }} />
                      ))}
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
              {roles.length === 0 && (
                <TableRow>
                  <TableCell colSpan={3} align="center" sx={{ py: 4, color: 'text.disabled' }}>
                    Nenhum papel personalizado criado para este tenant.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Dialog open={isModalOpen} onClose={() => setIsModalOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 900 }}>Criar Novo Papel / Perfil</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={3} sx={{ mt: 1 }}>
            <TextField 
              label="Nome do Papel" 
              fullWidth 
              required 
              value={newRole.name}
              onChange={e => setNewRole({ ...newRole, name: e.target.value })}
              placeholder="Ex: Operador de Almoxarifado"
            />
            <TextField 
              label="Descrição" 
              fullWidth 
              multiline 
              rows={2}
              value={newRole.description}
              onChange={e => setNewRole({ ...newRole, description: e.target.value })}
            />

            <Box>
              <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 1 }}>Papéis Base (Composição)</Typography>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 2 }}>
                O novo papel herdará todas as permissões dos papéis selecionados abaixo.
              </Typography>
              <Autocomplete
                multiple
                options={roles}
                getOptionLabel={(option) => option.name}
                value={roles.filter(r => newRole.childRoleIds.includes(r.id))}
                onChange={(_, newValue) => {
                  setNewRole({ ...newRole, childRoleIds: newValue.map(v => v.id) });
                }}
                renderInput={(params) => (
                  <TextField {...params} variant="outlined" label="Selecionar Papéis" placeholder="Perfis existentes..." />
                )}
                sx={{ mb: 1 }}
              />
            </Box>
            
            <Box>
              <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 1 }}>Permissões Atômicas Adicionais</Typography>
              <Divider sx={{ mb: 2 }} />
              <FormGroup sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1 }}>
                {availablePermissions.map(perm => (
                  <FormControlLabel 
                    key={perm}
                    control={
                      <Checkbox 
                        size="small" 
                        checked={newRole.permissions.includes(perm)}
                        onChange={() => togglePermission(perm)}
                      />
                    } 
                    label={<Typography variant="caption" sx={{ fontWeight: 600 }}>{perm}</Typography>} 
                  />
                ))}
              </FormGroup>
            </Box>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setIsModalOpen(false)} color="inherit" startIcon={<X size={18} />}>Cancelar</Button>
          <Button 
            variant="contained" 
            onClick={handleCreateRole} 
            disabled={!newRole.name || (newRole.permissions.length === 0 && newRole.childRoleIds.length === 0) || isSubmitting}
            startIcon={isSubmitting ? <CircularProgress size={18} color="inherit" /> : <Save size={18} />}
            sx={{ fontWeight: 800 }}
          >
            Salvar Papel
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
