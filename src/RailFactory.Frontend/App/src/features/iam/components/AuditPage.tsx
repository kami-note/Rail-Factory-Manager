import React, { useCallback, useEffect, useState } from 'react';
import {
  Alert, Box, Chip, CircularProgress, Paper,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TablePagination, Typography, Stack, ToggleButton, ToggleButtonGroup,
} from '@mui/material';
import { ScrollText } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';

type AuditEntry = {
  id: string;
  action: string;
  actorEmail: string;
  affectedEmail?: string;
  ipAddress?: string;
  correlationId?: string;
  metadataJson: string;
  occurredAt: string;
};

type AuditResponse = {
  total: number;
  page: number;
  pageSize: number;
  items: AuditEntry[];
};

const ACTION_LABELS: Record<string, { label: string; color: 'default' | 'info' | 'warning' | 'success' | 'error' }> = {
  role_assigned: { label: 'Role atribuída', color: 'success' },
  role_revoked: { label: 'Role revogada', color: 'error' },
  session_created: { label: 'Login', color: 'info' },
};

type Props = { tenantCode: string };

export function AuditPage({ tenantCode }: Props) {
  const [entries, setEntries] = useState<AuditEntry[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [pageSize] = useState(50);
  const [actionFilter, setActionFilter] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams({ page: String(page + 1), pageSize: String(pageSize) });
      if (actionFilter) params.set('action', actionFilter);
      const result = await fetchJsonOrThrow<AuditResponse>(
        `/api/iam/admin/audit?${params.toString()}`,
        { credentials: 'include', headers: buildTenantHeaders(tenantCode) },
        'Erro ao carregar trilha de auditoria'
      );
      setEntries(result.items);
      setTotal(result.total);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar auditoria.');
    } finally {
      setLoading(false);
    }
  }, [tenantCode, page, pageSize, actionFilter]);

  useEffect(() => { void load(); }, [load]);

  const handleActionFilter = (_: React.MouseEvent, value: string | null) => {
    setActionFilter(value ?? '');
    setPage(0);
  };

  if (error) return <PageError message={error} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader label="Trilha de Auditoria" icon={<ScrollText size={20} />} />

      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap', gap: 1 }}>
        <ToggleButtonGroup value={actionFilter} exclusive onChange={handleActionFilter} size="small">
          <ToggleButton value="">Todas</ToggleButton>
          <ToggleButton value="role_assigned">Role atribuída</ToggleButton>
          <ToggleButton value="role_revoked">Role revogada</ToggleButton>
          <ToggleButton value="session_created">Login</ToggleButton>
        </ToggleButtonGroup>
      </Stack>

      {loading && <Box sx={{ py: 4, textAlign: 'center' }}><CircularProgress /></Box>}

      {!loading && (
        <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Data/Hora</TableCell>
                <TableCell>Ação</TableCell>
                <TableCell>Ator</TableCell>
                <TableCell>Afetado</TableCell>
                <TableCell>IP</TableCell>
                <TableCell>Detalhes</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {entries.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center">Nenhum registro de auditoria encontrado.</TableCell>
                </TableRow>
              )}
              {entries.map(entry => {
                const actionMeta = ACTION_LABELS[entry.action] ?? { label: entry.action, color: 'default' as const };
                let metaParsed: Record<string, unknown> = {};
                try { metaParsed = JSON.parse(entry.metadataJson) ?? {}; } catch { /* ignore */ }

                return (
                  <TableRow key={entry.id} hover>
                    <TableCell sx={{ fontFamily: 'monospace', fontSize: 12, whiteSpace: 'nowrap' }}>
                      {new Date(entry.occurredAt).toLocaleString('pt-BR')}
                    </TableCell>
                    <TableCell>
                      <Chip label={actionMeta.label} color={actionMeta.color} size="small" />
                    </TableCell>
                    <TableCell sx={{ fontSize: 13 }}>{entry.actorEmail}</TableCell>
                    <TableCell sx={{ fontSize: 13, color: 'text.secondary' }}>{entry.affectedEmail ?? '—'}</TableCell>
                    <TableCell sx={{ fontFamily: 'monospace', fontSize: 12 }}>{entry.ipAddress ?? '—'}</TableCell>
                    <TableCell sx={{ fontSize: 12, color: 'text.secondary' }}>
                      {Object.keys(metaParsed).length > 0 ? (
                        <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
                          {(metaParsed.roleName as string) ?? JSON.stringify(metaParsed)}
                        </Typography>
                      ) : '—'}
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <TablePagination
        component="div"
        count={total}
        page={page}
        rowsPerPage={pageSize}
        rowsPerPageOptions={[50]}
        onPageChange={(_, p) => setPage(p)}
        labelDisplayedRows={({ from, to, count }) => `${from}–${to} de ${count}`}
      />
    </Box>
  );
}
