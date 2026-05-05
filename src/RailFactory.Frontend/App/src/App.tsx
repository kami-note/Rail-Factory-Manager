import React, { useEffect, useState } from 'react';
import { Routes, Route, useNavigate, useLocation, Navigate } from 'react-router-dom';
import { buildLoginHref, logout, useAuthSession } from './auth';
import { ProtectedDashboardLayout } from './components/ProtectedDashboardLayout';
import { OverviewPanel } from './features/dashboard/OverviewPanel';
import { ReceiptsWorkspace } from './features/dashboard/ReceiptsWorkspace';
import { InventoryStocksPage } from './features/dashboard/InventoryStocksPage';
import type { Status } from './features/dashboard/types';
import { Box, Button, Card, CircularProgress, Container, Typography, Link } from '@mui/material';

export function App() {
  const navigate = useNavigate();
  const location = useLocation();
  const [status, setStatus] = useState<Status | null>(null);
  const [statusError, setStatusError] = useState<string | null>(null);
  const [logoutError, setLogoutError] = useState<string | null>(null);
  const tenantCode = import.meta.env.VITE_TENANT_CODE ?? 'dev';
  
  const auth = useAuthSession(tenantCode);
  const loginHref = buildLoginHref(tenantCode, '/app');

  useEffect(() => {
    fetch('/api/status', {
      credentials: 'include',
      headers: {
        'X-Tenant-Code': tenantCode
      }
    })
      .then(async response => {
        if (!response.ok) {
          throw new Error(`Status request failed: ${response.status}`);
        }
        return response.json() as Promise<Status>;
      })
      .then(setStatus)
      .catch((requestError: Error) => setStatusError(requestError.message));
  }, [tenantCode]);

  const handleLogout = async () => {
    try {
      setLogoutError(null);
      await logout(tenantCode);
      navigate('/');
    } catch (requestError) {
      setLogoutError(requestError instanceof Error ? requestError.message : 'Logout failed.');
    }
  };

  if (location.pathname.startsWith('/app') && auth.status === 'loading') {
    return (
      <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100vh' }}>
        <CircularProgress size={40} sx={{ mb: 2 }} />
        <Typography variant="h1">Validating session</Typography>
        <Typography variant="body1" color="text.secondary">Checking protected access for tenant {tenantCode}.</Typography>
      </Box>
    );
  }

  if (location.pathname.startsWith('/app') && auth.status !== 'authenticated') {
    return (
      <Container maxWidth="sm" sx={{ py: 8 }}>
        <Card sx={{ p: 4, textAlign: 'center' }}>
          <Typography variant="h1" gutterBottom>Session expired</Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
            This area is protected and requires an active server session.
          </Typography>
          <Button 
            variant="contained" 
            href={loginHref} 
            fullWidth 
            sx={{ mb: 2 }}
            startIcon={<img src="/google-g-logo.png" alt="" style={{ width: 18, height: 18 }} />}
          >
            Sign in with Google
          </Button>
          <Link href="/" color="primary" underline="hover">Back to login</Link>
        </Card>
      </Container>
    );
  }

  return (
    <Routes>
      <Route path="/" element={
        <Container maxWidth="sm" sx={{ py: 8 }}>
          <Card sx={{ p: 4, textAlign: 'center' }}>
            <Typography variant="h1" gutterBottom>Sign In</Typography>
            <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
              Access the protected operations dashboard.
            </Typography>
            <Button 
              variant="contained" 
              href={loginHref} 
              fullWidth 
              startIcon={<img src="/google-g-logo.png" alt="" style={{ width: 18, height: 18 }} />}
            >
              Sign in with Google
            </Button>
            {auth.oauthError && <Typography color="error" sx={{ mt: 2 }}>{auth.oauthError}</Typography>}
            {auth.status === 'error' && auth.error && <Typography color="error" sx={{ mt: 2 }}>{auth.error}</Typography>}
          </Card>
        </Container>
      } />
      
      <Route path="/app/*" element={
        <ProtectedDashboardLayout
          tenantCode={tenantCode}
          userLabel={auth.session?.user?.email ?? auth.session?.user?.name ?? 'authenticated'}
          environmentLabel={status?.environment ?? 'loading'}
          serviceLabel={status?.service ?? 'loading'}
          currentPath={location.pathname}
          onNavigate={(path) => navigate(path)}
          onLogout={handleLogout}
          logoutError={logoutError}
        >
          <Routes>
            <Route index element={<OverviewPanel status={status} statusError={statusError} onNavigate={(path) => navigate(path)} />} />
            <Route path="receipts" element={<ReceiptsWorkspace tenantCode={tenantCode} />} />
            <Route path="import-xml" element={<ReceiptsWorkspace tenantCode={tenantCode} requestedDrawer="xml" />} />
            <Route path="inventory" element={<InventoryStocksPage tenantCode={tenantCode} />} />
            <Route path="*" element={<Navigate to="/app" replace />} />
          </Routes>
        </ProtectedDashboardLayout>
      } />
    </Routes>
  );
}
