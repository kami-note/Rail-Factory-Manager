import React, { useEffect, useState } from 'react';
import { Box, CircularProgress, FormControl, InputLabel, MenuItem, Select, Typography, Alert } from '@mui/material';
import { fetchJsonOrThrow } from '../lib/http';

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
          `/api/tenancy/tenants?t=${Date.now()}`,
          {},
          'Failed to load organizations'
        );
        setTenants(data);
      } catch (requestError) {
        setError(requestError instanceof Error ? requestError.message : 'Failed to load organizations.');
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
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        {error}
      </Alert>
    );
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
          Please select an organization to continue.
        </Typography>
      )}
    </Box>
  );
};
