import { cleanup, fireEvent, render, screen, act } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { InventoryStocksPage } from '../components/InventoryStocksPage';
import { ThemeProvider, createTheme } from '@mui/material';
import { useInventoryBalances } from '../hooks/useInventoryBalances';
import type { InventoryBalance } from '../types';
import React from 'react';

// Mock dependencies
vi.mock('react-router-dom', () => ({
  useNavigate: () => vi.fn(),
}));

vi.mock('../../auth', () => ({
  Authorized: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

vi.mock('../hooks/useInventoryBalances', () => ({
  useInventoryBalances: vi.fn(),
}));

const theme = createTheme();

function renderWithTheme(component: JSX.Element) {
  return render(
    <ThemeProvider theme={theme}>
      {component}
    </ThemeProvider>
  );
}

const mockBalances: InventoryBalance[] = [
  {
    id: '1',
    materialCode: 'RAW-01',
    materialName: 'Aço Laminado',
    quantity: 120,
    unitOfMeasure: 'kg',
    status: { key: 'Available', label: 'Disponível', color: 'success' },
    sourceReference: 'NFE-001',
    lotNumber: 'LOTE-A',
    sourceType: { key: 'Purchase', label: 'Compra', color: 'primary' },
    supplierName: 'Metalúrgica Gerdau',
    createdAt: '2026-06-01T10:00:00Z',
  },
  {
    id: '2',
    materialCode: 'RAW-02',
    materialName: 'Bobina de Cobre',
    quantity: 0,
    unitOfMeasure: 'un',
    status: { key: 'Blocked', label: 'Bloqueado', color: 'error' },
    sourceReference: 'NFE-002',
    lotNumber: 'LOTE-B',
    sourceType: { key: 'Purchase', label: 'Compra', color: 'primary' },
    supplierName: 'Cobre Cia',
    createdAt: '2026-06-02T10:00:00Z',
  },
  {
    id: '3',
    materialCode: 'RAW-03',
    materialName: 'Componente Especial',
    quantity: 45,
    unitOfMeasure: 'un',
    status: { key: 'Available', label: 'Disponível', color: 'success' },
    sourceReference: 'NFE-003',
    lotNumber: 'LOTE-C',
    sourceType: { key: 'Purchase', label: 'Compra', color: 'primary' },
    supplierName: 'Parceiro Gerdau',
    createdAt: '2026-06-03T10:00:00Z',
  }
];

describe('InventoryStocksPage', () => {
  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it('renders stats cards correctly based on mock balances', () => {
    vi.mocked(useInventoryBalances).mockReturnValue({
      data: mockBalances,
      loading: false,
      error: null,
      reload: vi.fn(),
    });

    renderWithTheme(<InventoryStocksPage tenantCode="dev" />);

    // Lotes em Exibição = 3
    expect(screen.getByText('Lotes em Exibição')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();

    // Quantidade Total = 165 (120 + 0 + 45)
    expect(screen.getByText('Quantidade Total')).toBeInTheDocument();
    expect(screen.getByText('165')).toBeInTheDocument();

    // Lotes Bloqueados = 1 (Bobina de Cobre)
    expect(screen.getByText('Lotes Bloqueados')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
  });

  it('filters data by search query input', async () => {
    vi.mocked(useInventoryBalances).mockReturnValue({
      data: mockBalances,
      loading: false,
      error: null,
      reload: vi.fn(),
    });

    renderWithTheme(<InventoryStocksPage tenantCode="dev" />);

    // Initially all materials should be visible
    expect(screen.getByText('Aço Laminado')).toBeInTheDocument();
    expect(screen.getByText('Bobina de Cobre')).toBeInTheDocument();
    expect(screen.getByText('Componente Especial')).toBeInTheDocument();

    // Search for "Gerdau"
    const searchInput = screen.getByPlaceholderText(/Pesquisar por material, código, lote ou fornecedor/i);
    await act(async () => {
      fireEvent.change(searchInput, { target: { value: 'Gerdau' } });
    });

    // Gerdau is supplier for RAW-01 and RAW-03 (Parceiro Gerdau)
    expect(screen.getByText('Aço Laminado')).toBeInTheDocument();
    expect(screen.queryByText('Bobina de Cobre')).not.toBeInTheDocument();
    expect(screen.getByText('Componente Especial')).toBeInTheDocument();

    // Search for "RAW-01"
    await act(async () => {
      fireEvent.change(searchInput, { target: { value: 'RAW-01' } });
    });

    expect(screen.getByText('Aço Laminado')).toBeInTheDocument();
    expect(screen.queryByText('Bobina de Cobre')).not.toBeInTheDocument();
    expect(screen.queryByText('Componente Especial')).not.toBeInTheDocument();
  });

  it('hides zero stock when checkbox is checked', async () => {
    vi.mocked(useInventoryBalances).mockReturnValue({
      data: mockBalances,
      loading: false,
      error: null,
      reload: vi.fn(),
    });

    renderWithTheme(<InventoryStocksPage tenantCode="dev" />);

    expect(screen.getByText('Bobina de Cobre')).toBeInTheDocument();

    const hideZeroCheckbox = screen.getByLabelText(/Ocultar saldo zerado/i);
    await act(async () => {
      fireEvent.click(hideZeroCheckbox);
    });

    // Bobina de Cobre (quantity 0) should be hidden now
    expect(screen.queryByText('Bobina de Cobre')).not.toBeInTheDocument();
    expect(screen.getByText('Aço Laminado')).toBeInTheDocument();
    expect(screen.getByText('Componente Especial')).toBeInTheDocument();
  });

  it('filters by status chip clicks', async () => {
    vi.mocked(useInventoryBalances).mockReturnValue({
      data: mockBalances,
      loading: false,
      error: null,
      reload: vi.fn(),
    });

    renderWithTheme(<InventoryStocksPage tenantCode="dev" />);

    // Click "Bloqueado" status filter
    const blockedChip = screen.getByRole('button', { name: 'Bloqueado' });
    await act(async () => {
      fireEvent.click(blockedChip);
    });

    expect(screen.queryByText('Aço Laminado')).not.toBeInTheDocument();
    expect(screen.getByText('Bobina de Cobre')).toBeInTheDocument();
    expect(screen.queryByText('Componente Especial')).not.toBeInTheDocument();
  });

  it('disables the Histórico button when balance is synthetic (zero stock catalog init)', () => {
    const syntheticBalances: InventoryBalance[] = [
      ...mockBalances,
      {
        id: '00000000-0000-0000-0000-000000000000',
        materialCode: 'RAW-04',
        materialName: 'Material Sintético',
        quantity: 0,
        unitOfMeasure: 'un',
        status: { key: 'Available', label: 'Disponível', color: 'success' },
        sourceReference: 'catalog-init:RAW-04',
        lotNumber: undefined,
        sourceType: { key: 'Purchase', label: 'Compra', color: 'primary' },
        supplierName: undefined,
        createdAt: '2026-06-04T10:00:00Z',
      }
    ];

    vi.mocked(useInventoryBalances).mockReturnValue({
      data: syntheticBalances,
      loading: false,
      error: null,
      reload: vi.fn(),
    });

    renderWithTheme(<InventoryStocksPage tenantCode="dev" />);

    // Check that Material Sintético is rendered
    expect(screen.getByText('Material Sintético')).toBeInTheDocument();

    // Verify the "Sem Histórico" button is rendered as disabled
    const semHistoricoBtn = screen.getByRole('button', { name: /Sem Histórico/i });
    expect(semHistoricoBtn).toBeInTheDocument();
    expect(semHistoricoBtn).toBeDisabled();
  });
});
