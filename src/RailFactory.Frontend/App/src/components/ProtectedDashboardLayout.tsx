import React from 'react';
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
  Avatar
} from '@mui/material';
import { LayoutDashboard, ReceiptText, LogOut, Settings, Bell } from 'lucide-react';
import type { ReactNode } from 'react';

const drawerWidth = 220;
const navItems = [
  { href: '/app', label: 'OVERVIEW', icon: <LayoutDashboard size={18} /> },
  { href: '/app/receipts', label: 'RECEIPTS', icon: <ReceiptText size={18} /> },
];

export function ProtectedDashboardLayout({
  tenantCode,
  userLabel,
  currentPath,
  onNavigate,
  onLogout,
  children
}: {
  tenantCode: string;
  userLabel: string;
  currentPath: string;
  onNavigate: (path: string) => void;
  onLogout: () => Promise<void>;
  children: ReactNode;
}) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const sidebar = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', bgcolor: '#ffffff' }}>
      <Box sx={{ p: 4, mb: 2 }}>
        <Typography variant="h6" color="primary" sx={{ letterSpacing: '0.1em', fontWeight: 900, fontSize: '0.85rem' }}>
          RAIL FACTORY
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, opacity: 0.6 }}>CONTROL SYSTEM</Typography>
      </Box>
      
      <List disablePadding sx={{ flexGrow: 1 }}>
        {navItems.map((item) => {
          const active = item.href === '/app' ? currentPath === '/app' : currentPath.startsWith(item.href);
          return (
            <ListItemButton
              key={item.href}
              onClick={() => onNavigate(item.href)}
              selected={active}
              sx={{
                py: 2,
                px: 4,
                mx: 1,
                borderRadius: 1,
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
                slotProps={{ primary: { variant: 'caption', fontWeight: active ? 800 : 600 } }}
              />
            </ListItemButton>
          );
        })}
      </List>

      <Box sx={{ p: 2 }}>
        <Divider sx={{ mb: 2, opacity: 0.5 }} />
        <ListItemButton onClick={onLogout} sx={{ px: 2, borderRadius: 1, color: 'text.secondary' }}>
          <ListItemIcon sx={{ minWidth: 32 }}><LogOut size={16} /></ListItemIcon>
          <ListItemText primary="SIGN OUT" slotProps={{ primary: { variant: 'caption', fontWeight: 700 } }} />
        </ListItemButton>
      </Box>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: '#f3f2f1' }}>
      {!isMobile && (
        <Drawer 
          variant="permanent" 
          sx={{ 
            width: drawerWidth, 
            '& .MuiDrawer-paper': { 
              width: drawerWidth, 
              borderRight: '1px solid #edebe9',
              boxShadow: 'none'
            } 
          }}
        >
          {sidebar}
        </Drawer>
      )}
      
      <Box sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
        <AppBar position="sticky" elevation={0} sx={{ bgcolor: 'primary.main', borderBottom: '1px solid', borderColor: 'primary.dark' }}>
          <Toolbar variant="dense" sx={{ minHeight: 48, justifyContent: 'space-between', px: { xs: 2, md: 4 } }}>
            <Stack direction="row" spacing={2} alignItems="center">
              <Typography variant="h6" sx={{ fontSize: '0.85rem', fontWeight: 800, color: 'white' }}>
                {navItems.find(i => i.href === '/app' ? currentPath === '/app' : currentPath.startsWith(i.href))?.label || 'SYSTEM'}
              </Typography>
              <Divider orientation="vertical" flexItem sx={{ bgcolor: 'rgba(255,255,255,0.2)', my: 1.5 }} />
              <Chip 
                label={tenantCode.toUpperCase()} 
                size="small" 
                sx={{ 
                  borderRadius: 0.5, 
                  bgcolor: 'rgba(255,255,255,0.12)', 
                  color: 'white', 
                  fontWeight: 800, 
                  fontSize: '0.6rem',
                  border: '1px solid rgba(255,255,255,0.2)'
                }} 
              />
            </Stack>

            <Stack direction="row" spacing={3} alignItems="center">
              <Stack direction="row" spacing={1} alignItems="center" sx={{ display: { xs: 'none', sm: 'flex' } }}>
                <Typography variant="caption" sx={{ color: 'white', fontWeight: 700, opacity: 0.9 }}>{userLabel}</Typography>
                <Avatar sx={{ width: 24, height: 24, fontSize: '0.65rem', bgcolor: 'white', color: 'primary.main', fontWeight: 800 }}>
                  {userLabel.charAt(0).toUpperCase()}
                </Avatar>
              </Stack>
              <Divider orientation="vertical" flexItem sx={{ bgcolor: 'rgba(255,255,255,0.2)', my: 1.5 }} />
              <Bell size={16} color="white" style={{ opacity: 0.8, cursor: 'pointer' }} />
              <Settings size={16} color="white" style={{ opacity: 0.8, cursor: 'pointer' }} />
            </Stack>
          </Toolbar>
        </AppBar>
        
        <Box sx={{ flexGrow: 1, overflow: 'auto' }}>
          {children}
        </Box>
      </Box>
    </Box>
  );
}
