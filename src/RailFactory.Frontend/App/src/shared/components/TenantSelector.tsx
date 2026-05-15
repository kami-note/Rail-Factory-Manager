import React, { useEffect, useState } from 'react';
import { Box, CircularProgress, FormControl, InputLabel, MenuItem, Select, Typography } from '@mui/material';
import { InlineError } from './common/InlineError';
import { fetchJsonOrThrow, toUiErrorMessage } from '../lib/http';

interface Tenant {
  code: string;
  displayName: string;
}

interface TenantSelectorProps {
  onTenantSelected: (tenantCode: string) => void;
  selectedTenantCode?: string;
}

/**
 * Component to allow the user to select the organization (tenant).
 * Fetches the list of active tenants from the catalog.
 */
export const TenantSelector: React.FC<TenantSelectorProps> = ({ onTenantSelected, selectedTenantCode }) => {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadTenants = async () => {
      try {
        const data = await fetchJsonOrThrow<Tenant[]>(
          '/api/tenancy/tenants',
          {},
          'Não foi possível carregar as organizações'
        );
        setTenants(data);
      } catch (requestError) {
        setError(toUiErrorMessage(requestError, 'Não foi possível carregar as organizações.'));
      } finally {
        setLoading(false);
      }
    };

    void loadTenants();
  }, []);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
        <CircularProgress size={24} />
      </Box>
    );
  }

  if (error) {
    return <InlineError message={error} marginBottom={2} />;
  }

  return (
    <Box sx={{ mt: 2 }}>
      <FormControl fullWidth sx={{ mb: 3 }}>
        <InputLabel id="tenant-select-label">Organization</InputLabel>
        <Select
          labelId="tenant-select-label"
          id="tenant-select"
          value={selectedTenantCode || ''}
          label="Organization"
          onChange={(e) => onTenantSelected(e.target.value)}
        >
          {tenants.map((t) => (
            <MenuItem key={t.code} value={t.code}>
              {t.displayName} ({t.code})
            </MenuItem>
          ))}
        </Select>
      </FormControl>
      {!selectedTenantCode && (
        <Typography variant="body2" color="text.secondary">
          Selecione uma organização para continuar.
        </Typography>
      )}
    </Box>
  );
};
