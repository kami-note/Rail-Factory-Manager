import React, { useEffect, useState } from 'react';
import { Routes, Route, useNavigate, useLocation, Navigate } from 'react-router-dom';
import { AuthSessionProvider, buildLoginHref, logout, useAuthSession } from './features/auth';
import { ProtectedDashboardLayout } from './shared/layouts/ProtectedDashboardLayout';
import { OverviewPanel, Status } from './features/dashboard';
import { ReceiptsWorkspace, AssociationWorkbenchPage } from './features/supply-chain';
import { InventoryStocksPage, MaterialDetailsPage } from './features/inventory';
import { RolesManagementPage, UsersManagementPage } from './features/iam';
import { WorkCentersPage, BomsPage, ProductionOrdersPage } from './features/production';
import { TenantSelector } from './shared/components/TenantSelector';
import { Box, Button, Card, CircularProgress, Container, Typography, Link } from '@mui/material';
import { buildTenantHeaders, fetchJsonOrThrow, toUiErrorMessage } from './shared/lib/http';

const TENANT_STORAGE_KEY = 'rail_factory_tenant_code';

/**
 * Root Application component handling routing, session validation, and tenant selection.
 * Implementation follows the "Elite Prevention Protocol" for localized and secure UI.
 */
export function App() {
  const [tenantCode, setTenantCode] = useState<string>(() => {
    const queryTenantCode = new URLSearchParams(window.location.search).get('tenantCode');
    if (queryTenantCode) {
      localStorage.setItem(TENANT_STORAGE_KEY, queryTenantCode);
      return queryTenantCode;
    }

    return localStorage.getItem(TENANT_STORAGE_KEY) || '';
  });

  const handleTenantSelected = (code: string) => {
    setTenantCode(code);
    localStorage.setItem(TENANT_STORAGE_KEY, code);
  };

  return (
    <AuthSessionProvider tenantCode={tenantCode}>
      <AppContent tenantCode={tenantCode} onTenantSelected={handleTenantSelected} />
    </AuthSessionProvider>
  );
}

type AppContentProps = {
  tenantCode: string;
  onTenantSelected: (code: string) => void;
};

function AppContent({ tenantCode, onTenantSelected }: AppContentProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const isProtectedRoute = location.pathname.startsWith('/app');
  const [status, setStatus] = useState<Status | null>(null);
  const [statusError, setStatusError] = useState<string | null>(null);
  const auth = useAuthSession(tenantCode);
  const loginHref = tenantCode ? buildLoginHref(tenantCode, '/app') : '#';
  const navigateTo = (path: string) => navigate(path);

  useEffect(() => {
    const queryTenantCode = new URLSearchParams(location.search).get('tenantCode');
    if (!queryTenantCode || queryTenantCode === tenantCode) {
      return;
    }

    onTenantSelected(queryTenantCode);
  }, [location.search, onTenantSelected, tenantCode]);

  useEffect(() => {
    if (!tenantCode || !isProtectedRoute || auth.status !== 'authenticated') {
      setStatus(null);
      setStatusError(null);
      return;
    }

    const loadStatus = async () => {
      try {
        setStatusError(null);
        const response = await fetchJsonOrThrow<Status>(
          '/api/status',
          {
            credentials: 'include',
            headers: buildTenantHeaders(tenantCode)
          },
          'Falha ao carregar status do sistema'
        );
        setStatus(response);
      } catch (requestError) {
        setStatus(null);
        setStatusError(toUiErrorMessage(requestError, 'Não foi possível carregar o status do sistema.'));
      }
    };

    void loadStatus();
  }, [tenantCode, isProtectedRoute, auth.status]);

  useEffect(() => {
    if (!isProtectedRoute || !tenantCode) {
      return;
    }

    void auth.refreshSession();

    const handleFocus = () => {
      void auth.refreshSession();
    };

    window.addEventListener('focus', handleFocus);
    return () => {
      window.removeEventListener('focus', handleFocus);
    };
  }, [auth.refreshSession, isProtectedRoute, tenantCode]);

  const handleLogout = async () => {
    try {
      await logout(tenantCode);
      auth.clearSession();
      setStatus(null);
      setStatusError(null);
      navigateTo('/');
    } catch (requestError) {
      console.error(toUiErrorMessage(requestError, 'Logout falhou.'));
    }
  };

  if (isProtectedRoute && !tenantCode) {
    return <Navigate to="/" replace />;
  }

  if (isProtectedRoute && auth.status === 'loading') {
    return (
      <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100vh' }}>
        <CircularProgress size={40} sx={{ mb: 2 }} />
        <Typography variant="h5" sx={{ fontWeight: 800 }}>Validando sessão</Typography>
        <Typography variant="body1" color="text.secondary">Verificando acesso protegido para a organização <strong>{tenantCode}</strong>.</Typography>
      </Box>
    );
  }

  if (isProtectedRoute && auth.status !== 'authenticated') {
    return (
      <Container maxWidth="sm" sx={{ py: 8 }}>
        <Card sx={{ p: 4, textAlign: 'center', borderRadius: 2, border: 1, borderColor: 'divider' }}>
          <Typography variant="h4" sx={{ fontWeight: 900, mb: 2 }} gutterBottom>Sessão expirada</Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
            Esta área é protegida e exige uma sessão ativa para a organização <strong>{tenantCode}</strong>.
          </Typography>
          <Button 
            variant="contained" 
            href={loginHref} 
            fullWidth 
            size="large"
            sx={{ mb: 2, py: 1.5, fontWeight: 800, borderRadius: 2 }}
            disabled={!tenantCode}
            startIcon={<img src="/google-g-logo.png" alt="" style={{ width: 18, height: 18 }} />}
          >
            Entrar com Google
          </Button>
          <Link 
            href="/" 
            color="primary" 
            underline="hover" 
            sx={{ fontWeight: 700, cursor: 'pointer' }}
          >
            Voltar para o início
          </Link>
        </Card>
      </Container>
    );
  }

  return (
    <Routes>
      <Route path="/" element={
        <Container maxWidth="sm" sx={{ py: 8 }}>
          <Card sx={{ p: 5, textAlign: 'center', borderRadius: 3, border: 1, borderColor: 'divider', boxShadow: 0 }}>
            <Typography variant="h4" sx={{ fontWeight: 950, letterSpacing: '-0.05em', mb: 1 }}>
              RAIL FACTORY
            </Typography>
            <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
              Selecione sua organização para acessar o painel de operações.
            </Typography>

            <TenantSelector 
              onTenantSelected={onTenantSelected}
              selectedTenantCode={tenantCode} 
            />

            <Button 
              variant="contained" 
              href={loginHref} 
              fullWidth 
              size="large"
              disabled={!tenantCode}
              sx={{ mt: 4, py: 2, fontWeight: 900, borderRadius: 2 }}
              startIcon={<img src="/google-g-logo.png" alt="" style={{ width: 18, height: 18 }} />}
            >
              Entrar com Google
            </Button>
            {auth.oauthError && <Typography color="error" sx={{ mt: 2, fontWeight: 600 }}>{auth.oauthError}</Typography>}
            {auth.status === 'error' && auth.error && <Typography color="error" sx={{ mt: 2, fontWeight: 600 }}>{auth.error}</Typography>}
          </Card>
        </Container>
      } />
      
      <Route path="/app/*" element={
        <ProtectedDashboardLayout
          tenantCode={tenantCode}
          userLabel={auth.session?.user?.email ?? auth.session?.user?.name ?? 'authenticated'}
          currentPath={location.pathname}
          onNavigate={navigateTo}
          onLogout={handleLogout}
        >
          <Routes>
            <Route index element={<OverviewPanel status={status} statusError={statusError} tenantCode={tenantCode} onNavigate={navigateTo} />} />
            <Route path="receipts" element={<ReceiptsWorkspace tenantCode={tenantCode} />} />
            <Route path="supply-chain/association" element={<AssociationWorkbenchPage tenantCode={tenantCode} />} />
            <Route path="import-xml" element={<ReceiptsWorkspace tenantCode={tenantCode} requestedDrawer="xml" />} />
            <Route path="inventory" element={<InventoryStocksPage tenantCode={tenantCode} />} />
            <Route path="inventory/materials/:materialCode" element={<MaterialDetailsPage tenantCode={tenantCode} />} />
            <Route path="iam/users" element={<UsersManagementPage tenantCode={tenantCode} />} />
            <Route path="iam/roles" element={<RolesManagementPage tenantCode={tenantCode} />} />
            <Route path="production/work-centers" element={<WorkCentersPage tenantCode={tenantCode} />} />
            <Route path="production/boms" element={<BomsPage tenantCode={tenantCode} />} />
            <Route path="production/orders" element={<ProductionOrdersPage tenantCode={tenantCode} />} />
            <Route path="*" element={<Navigate to="/app" replace />} />
          </Routes>
        </ProtectedDashboardLayout>
      } />
    </Routes>
  );
}
