import React, { useState } from 'react';
import { 
  Box, 
  Drawer, 
  AppBar, 
  Toolbar, 
  Typography, 
  List, 
  ListItemButton, 
  ListItemIcon, 
  ListItemText, 
  Chip, 
  Divider, 
  Stack,
  alpha,
  useTheme,
  useMediaQuery,
  Avatar,
  IconButton
} from '@mui/material';
import { LayoutDashboard, ReceiptText, Boxes, LogOut, Settings, Bell, Menu, Link2, ShieldCheck, Users, Factory, BookOpen, ClipboardList, Truck, UserCheck, PackageCheck, ScrollText, Plug, SendHorizonal, Building2, FileCheck2 } from 'lucide-react';
import type { ReactNode } from 'react';
import { usePermissions } from '../../features/auth';
import { SystemPermissions } from '../types/permissions';

const drawerWidth = 220;

type NavItem = {
  href: string;
  label: string;
  icon: React.ReactNode;
  permission?: string;
};

type NavGroup = {
  title: string;
  items: NavItem[];
};

const navGroups: NavGroup[] = [
  {
    title: 'GERAL',
    items: [
      { href: '/app', label: 'INÍCIO', icon: <LayoutDashboard size={18} /> },
    ]
  },
  {
    title: 'SUPRIMENTOS',
    items: [
      { href: '/app/receipts', label: 'RECEBIMENTOS', icon: <ReceiptText size={18} />, permission: SystemPermissions.SupplyChain.Read },
      { href: '/app/supply-chain/association', label: 'ASSOCIAÇÃO DE SKUs', icon: <Link2 size={18} />, permission: SystemPermissions.SupplyChain.Read },
      { href: '/app/inventory', label: 'ESTOQUE', icon: <Boxes size={18} />, permission: SystemPermissions.Inventory.Read },
    ]
  },
  {
    title: 'PRODUÇÃO',
    items: [
      { href: '/app/production/work-centers', label: 'CENTROS DE TRABALHO', icon: <Factory size={18} />, permission: SystemPermissions.Production.Read },
      { href: '/app/production/boms', label: 'ESTRUTURA DE PRODUTOS', icon: <BookOpen size={18} />, permission: SystemPermissions.Production.Read },
      { href: '/app/production/orders', label: 'ORDENS DE PRODUÇÃO', icon: <ClipboardList size={18} />, permission: SystemPermissions.Production.Read },
    ]
  },
  {
    title: 'LOGÍSTICA',
    items: [
      { href: '/app/logistics/carriers', label: 'TRANSPORTADORAS', icon: <Truck size={18} />, permission: SystemPermissions.Logistics.Read },
      { href: '/app/logistics/shipment-orders', label: 'EXPEDIÇÃO', icon: <PackageCheck size={18} />, permission: SystemPermissions.Logistics.Read },
      { href: '/app/logistics/dispatches', label: 'DESPACHOS', icon: <SendHorizonal size={18} />, permission: SystemPermissions.Logistics.Read },
      { href: '/app/logistics/nfe-monitor', label: 'MONITOR NF-e', icon: <FileCheck2 size={18} />, permission: SystemPermissions.Logistics.Read },
      { href: '/app/logistics/fiscal-settings', label: 'CONFIG. FISCAL', icon: <Settings size={18} />, permission: SystemPermissions.Logistics.Fiscal },
    ]
  },
  {
    title: 'FROTA',
    items: [
      { href: '/app/fleet', label: 'FROTA', icon: <Truck size={18} />, permission: SystemPermissions.Fleet.Read },
    ]
  },
  {
    title: 'EQUIPE',
    items: [
      { href: '/app/hr/people', label: 'FUNCIONÁRIOS', icon: <UserCheck size={18} />, permission: SystemPermissions.Hr.Read },
    ]
  },
  {
    title: 'ACESSO E SEGURANÇA',
    items: [
      { href: '/app/iam/users', label: 'USUÁRIOS', icon: <Users size={18} />, permission: SystemPermissions.Iam.RolesManage },
      { href: '/app/iam/roles', label: 'NÍVEIS DE ACESSO', icon: <ShieldCheck size={18} />, permission: SystemPermissions.Iam.RolesManage },
      { href: '/app/iam/audit', label: 'AUDITORIA', icon: <ScrollText size={18} />, permission: SystemPermissions.Iam.Read },
    ]
  },
  {
    title: 'CONFIGURAÇÕES',
    items: [
      { href: '/app/settings/integrations', label: 'INTEGRAÇÕES', icon: <Plug size={18} />, permission: SystemPermissions.Tenancy.Admin },
      { href: '/app/settings/tenants', label: 'EMPRESAS', icon: <Building2 size={18} />, permission: SystemPermissions.Tenancy.Admin },
    ]
  }
];

type ProtectedDashboardLayoutProps = {
  tenantCode: string;
  userLabel: string;
  currentPath: string;
  onNavigate: (path: string) => void;
  onLogout: () => Promise<void>;
  children: ReactNode;
};

/**
 * Main application layout for authenticated sessions.
 * @remarks
 * Localization: All navigation labels are in Portuguese (Brazil).
 * Standard: Uses lucide-react for iconography.
 */
export function ProtectedDashboardLayout({
  tenantCode,
  userLabel,
  currentPath,
  onNavigate,
  onLogout,
  children
}: ProtectedDashboardLayoutProps) {
  const theme = useTheme();
  const { hasPermission } = usePermissions(tenantCode);
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [mobileOpen, setMobileOpen] = useState(false);

  const handleDrawerToggle = () => {
    setMobileOpen((open) => !open);
  };

  const navigateTo = (path: string) => {
    onNavigate(path);
    if (isMobile) setMobileOpen(false);
  };

  const sidebar = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', bgcolor: '#ffffff' }}>
      <List disablePadding sx={{ flexGrow: 1, pt: 2, pb: 2 }}>
        {navGroups.map((group, groupIndex) => {
          const visibleItems = group.items.filter(item => !item.permission || hasPermission(item.permission));
          if (visibleItems.length === 0) return null;

          return (
            <React.Fragment key={group.title}>
              <Box sx={{ px: 4, py: 1, mt: groupIndex > 0 ? 1 : 0 }}>
                <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', letterSpacing: '0.05em', opacity: 0.7 }}>
                  {group.title}
                </Typography>
              </Box>
              {visibleItems.map(item => {
                const active = item.href === '/app' ? currentPath === '/app' : currentPath.startsWith(item.href);
                return (
                  <ListItemButton
                    key={item.href}
                    onClick={() => navigateTo(item.href)}
                    selected={active}
                    sx={{
                      py: 1.5,
                      px: 4,
                      mx: 1,
                      borderRadius: 1,
                      mb: 0.5,
                      '&.Mui-selected': { 
                        bgcolor: alpha(theme.palette.primary.main, 0.08), 
                        color: 'primary.main',
                        '& .MuiListItemIcon-root': { color: 'primary.main' },
                        '&:hover': { bgcolor: alpha(theme.palette.primary.main, 0.12) }
                      },
                    }}
                  >
                    <ListItemIcon sx={{ minWidth: 32 }}>{item.icon}</ListItemIcon>
                    <ListItemText 
                      primary={item.label} 
                      slotProps={{ primary: { variant: 'caption', sx: { fontWeight: active ? 800 : 600 } } }}
                    />
                  </ListItemButton>
                );
              })}
            </React.Fragment>
          );
        })}
      </List>

      <Box sx={{ p: 2 }}>
        <Box sx={{ px: 2, mb: 2 }}>
          <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, display: 'block', mb: 1, opacity: 0.6 }}>
            ORGANIZAÇÃO
          </Typography>
          <Chip 
            label={tenantCode.toUpperCase()} 
            size="small" 
            sx={{ 
              borderRadius: 0.5, 
              bgcolor: 'primary.main', 
              color: 'white', 
              fontWeight: 800, 
              fontSize: '0.65rem',
              width: '100%'
            }} 
          />
        </Box>
        <Divider sx={{ mb: 2, opacity: 0.5 }} />
        <ListItemButton onClick={onLogout} sx={{ px: 2, borderRadius: 1, color: 'text.secondary' }}>
          <ListItemIcon sx={{ minWidth: 32 }}><LogOut size={16} /></ListItemIcon>
          <ListItemText primary="SAIR DO SISTEMA" slotProps={{ primary: { variant: 'caption', sx: { fontWeight: 700 } } }} />
        </ListItemButton>
      </Box>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: '#f3f2f1' }}>
      <AppBar 
        position="fixed" 
        elevation={0} 
        sx={{ 
          bgcolor: 'primary.main', 
          borderBottom: '1px solid', 
          borderColor: 'primary.dark',
          zIndex: (theme) => theme.zIndex.drawer + 1
        }}
      >
        <Toolbar variant="dense" sx={{ minHeight: 48, justifyContent: 'space-between', alignItems: 'center', px: { xs: 2, md: 4 } }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            {isMobile && (
              <IconButton
                color="inherit"
                aria-label="open drawer"
                edge="start"
                onClick={handleDrawerToggle}
                sx={{ mr: 1, color: 'white' }}
              >
                <Menu size={20} />
              </IconButton>
            )}
            <Stack spacing={0} sx={{ justifyContent: 'center' }}>
              <Typography variant="h6" sx={{ fontSize: '0.85rem', fontWeight: 900, color: 'white', letterSpacing: '0.1em', lineHeight: 1.15 }}>
                RAIL FACTORY
              </Typography>
              <Typography variant="caption" sx={{ color: 'white', fontWeight: 700, opacity: 0.8, lineHeight: 1.15 }}>
                SISTEMA DE CONTROLE
              </Typography>
            </Stack>
          </Stack>

          <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
            <Box
              sx={{
                height: 24,
                display: { xs: 'none', sm: 'flex' },
                alignItems: 'center'
              }}
            >
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <Typography
                  variant="caption"
                  sx={{
                    color: 'white',
                    fontWeight: 700,
                    opacity: 0.9,
                    display: 'flex',
                    alignItems: 'center',
                    height: 24
                  }}
                >
                  {userLabel}
                </Typography>
                <Avatar sx={{ width: 24, height: 24, fontSize: '0.65rem', bgcolor: 'white', color: 'primary.main', fontWeight: 800 }}>
                  {userLabel.charAt(0).toUpperCase()}
                </Avatar>
              </Stack>
            </Box>

            <Divider orientation="vertical" flexItem sx={{ bgcolor: 'rgba(255,255,255,0.2)', my: 1, display: { xs: 'none', sm: 'block' } }} />

            <Box sx={{ height: 24, display: 'flex', alignItems: 'center', gap: 1.5 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: 16, height: 16, color: 'white', opacity: 0.8, cursor: 'pointer' }}>
                <Bell size={16} />
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: 16, height: 16, color: 'white', opacity: 0.8, cursor: 'pointer' }}>
                <Settings size={16} />
              </Box>
            </Box>
          </Stack>
        </Toolbar>
      </AppBar>

      {isMobile ? (
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{ keepMounted: true }}
          sx={{
            '& .MuiDrawer-paper': { width: drawerWidth, borderRight: '1px solid #edebe9' }
          }}
        >
          {sidebar}
        </Drawer>
      ) : (
        <Drawer 
          variant="permanent" 
          sx={{ 
            width: drawerWidth, 
            flexShrink: 0,
            '& .MuiDrawer-paper': { 
              width: drawerWidth, 
              borderRight: '1px solid #edebe9',
              boxShadow: 'none',
              boxSizing: 'border-box',
              pt: '48px' // AppBar height
            } 
          }}
        >
          {sidebar}
        </Drawer>
      )}
      
      <Box sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', pt: '48px' }}>
        <Box sx={{ flexGrow: 1, overflow: 'auto' }}>
          {children}
        </Box>
      </Box>
    </Box>
  );
}
