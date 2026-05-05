import React, { useEffect, useState } from 'react';
import { Box, Button, Card, CircularProgress, FormControl, InputLabel, MenuItem, Select, Typography, Alert } from '@mui/material';

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
    fetch(`/api/tenancy/tenants?t=${Date.now()}`)
      .then(async response => {
        if (!response.ok) {
          throw new Error(`Failed to load organizations: ${response.status}`);
        }
        const data = await response.json() as Tenant[];
        console.log('Loaded tenants:', data);
        return data;
      })
      .then(data => {
        setTenants(data);
        setLoading(false);
      })
      .catch((err: Error) => {
        setError(err.message);
        setLoading(false);
      });
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
