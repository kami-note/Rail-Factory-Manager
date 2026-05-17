import React, { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Paper,
  Tabs,
  Tab,
  CircularProgress,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Stack,
  alpha,
  useTheme,
  Divider,
  Alert,
  AlertTitle,
  Grid
} from '@mui/material'
import { ArrowLeft as BackIcon, AlertTriangle, User, Calendar, GitMerge as MergeIcon } from 'lucide-react'
import { buildTenantHeaders, fetchJsonOrThrow, toUiErrorMessage } from '../../../shared/lib/http'
import { PageError } from '../../../shared/components/common/PageError'
import { StatusChip } from '../../../shared/components/common/StatusChip'
import type { DisplayStatus } from '../../../shared/lib/utils/status-mapping'
import { MergeMaterialModal } from './MergeMaterialModal'

interface MaterialDetailsPageProps {
  tenantCode: string
}

type ProcurementType = 'Make' | 'Buy' | 'MakeAndBuy'
type MaterialStatus = 'Draft' | 'Verified' | 'Obsolete' | string

type MaterialData = {
  code: string
  officialName: string
  description: string
  unitOfMeasure: string
  procurementType: ProcurementType
  status: DisplayStatus
  category: DisplayStatus
  gtin?: string | null
  ncm?: string | null
  imageUrl?: string | null
  createdBy: string
  lastModifiedBy: string
  replacedBy?: string | null
  supplierMappings: Array<{
    id: string
    supplierName: string
    supplierCode: string
    conversionFactor: number
    lastPrice: number
  }>
}

type MaterialApiResponse = {
  materialCode: string
  officialName: string
  description: string
  unitOfMeasure: string
  procurementType: ProcurementType
  category: string
  status: MaterialStatus
  gtin?: string | null
  ncm?: string | null
  imageUrl?: string | null
  createdBy: string
  lastModifiedBy: string
  replacedBy?: string | null
  supplierMappings: MaterialData['supplierMappings']
}

// Local mapping for Material Category (Backend-driven UI not fully implemented for categories yet)
const categoryMap: Record<string, DisplayStatus> = {
  RawMaterial: { key: 'RawMaterial', label: 'Matéria-Prima', color: 'default' },
  FinishedGood: { key: 'FinishedGood', label: 'Produto Acabado', color: 'default' },
  Packaging: { key: 'Packaging', label: 'Embalagem', color: 'default' },
  Consumable: { key: 'Consumable', label: 'Consumível', color: 'default' }
}

const procurementMap: Record<string, string> = {
  Buy: 'Compra',
  Make: 'Fabricação',
  MakeAndBuy: 'Misto'
}

const statusMap: Record<string, DisplayStatus> = {
    Draft: { key: 'Draft', label: 'Rascunho', color: 'warning' },
    Verified: { key: 'Verified', label: 'Verificado', color: 'success' },
    Obsolete: { key: 'Obsolete', label: 'Obsoleto', color: 'error' }
};

function mapMaterialResponse(data: MaterialApiResponse): MaterialData {
  return {
    code: data.materialCode,
    officialName: data.officialName,
    description: data.description,
    unitOfMeasure: data.unitOfMeasure,
    procurementType: data.procurementType,
    category: categoryMap[data.category] ?? { key: data.category, label: data.category, color: 'default' },
    status: statusMap[data.status] ?? { key: data.status, label: data.status, color: 'default' },
    gtin: data.gtin,
    ncm: data.ncm,
    imageUrl: data.imageUrl,
    createdBy: data.createdBy,
    lastModifiedBy: data.lastModifiedBy,
    replacedBy: data.replacedBy,
    supplierMappings: data.supplierMappings ?? []
  }
}

/**
 * Dynamic Product Details Screen (Inventory)
 * @param props - Component properties.
 */
export function MaterialDetailsPage({ tenantCode }: MaterialDetailsPageProps) {
  const theme = useTheme();
  const { materialCode } = useParams<{ materialCode: string }>()
  const navigate = useNavigate()
  const [material, setMaterial] = useState<MaterialData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState('mappings')
  const [isMergeModalOpen, setIsMergeModalOpen] = useState(false)

  useEffect(() => {
    if (!materialCode) return

    const fetchMaterial = async () => {
      setLoading(true)
      setError(null)
      try {
        const data = await fetchJsonOrThrow<MaterialApiResponse>(
          `/api/inventory/materials/${encodeURIComponent(materialCode)}`,
          {
            headers: buildTenantHeaders(tenantCode),
            credentials: 'include'
          },
          'Falha ao buscar detalhes do material'
        )
        setMaterial(mapMaterialResponse(data))
      } catch (err) {
        setError(toUiErrorMessage(err, 'Não foi possível carregar os detalhes do material.'))
        setMaterial(null)
      } finally {
        setLoading(false)
      }
    }

    void fetchMaterial()
  }, [materialCode, tenantCode])

  const handleTabChange = (_event: React.SyntheticEvent, newValue: string) => {
    setActiveTab(newValue)
  }

  const handleMergeSuccess = (officialCode: string) => {
    setIsMergeModalOpen(false)
    navigate(`/app/inventory/materials/${officialCode}`)
  }

  if (loading) return <Box sx={{ p: 4, textAlign: 'center' }}><CircularProgress /></Box>
  if (error) return <PageError message={error} />
  if (!material) return <Box sx={{ p: 4 }}><Typography>Material não encontrado.</Typography></Box>

  const showBuyTabs = material.procurementType === 'Buy' || material.procurementType === 'MakeAndBuy'
  const showMakeTabs = material.procurementType === 'Make' || material.procurementType === 'MakeAndBuy'

  const tabs = [
    { id: 'mappings', label: 'Fornecedores (De-Para)', visible: showBuyTabs },
    { id: 'bom', label: 'Estrutura (BOM)', visible: showMakeTabs },
    { id: 'technical', label: 'Dados Técnicos', visible: true }
  ].filter(t => t.visible)

  const currentTabExists = tabs.find(t => t.id === activeTab)
  const effectiveTab = currentTabExists ? activeTab : (tabs[0]?.id || 'mappings')

  return (
    <Box sx={{ p: { xs: 2, md: 4 }, maxWidth: 1200, margin: '0 auto' }}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Button 
          startIcon={<BackIcon size={16} />} 
          onClick={() => navigate('/app/inventory')} 
          sx={{ fontWeight: 700 }}
        >
          Voltar para o Inventário
        </Button>

        {material.status.key !== 'Obsolete' && (
          <Button
            variant="outlined"
            color="primary"
            startIcon={<MergeIcon size={16} />}
            onClick={() => setIsMergeModalOpen(true)}
            sx={{ fontWeight: 700, borderRadius: 2 }}
          >
            Unificar Material
          </Button>
        )}
      </Stack>

      {material.replacedBy && (
        <Alert severity="warning" sx={{ mb: 3, border: 1, borderColor: 'warning.main', borderRadius: 2 }}>
            <AlertTitle sx={{ fontWeight: 800 }}>MATERIAL SUBSTITUÍDO</AlertTitle>
            Este item está obsoleto. O novo código oficial para este material é: 
            <Button 
                variant="text" 
                size="small" 
                sx={{ fontWeight: 800, ml: 1 }} 
                onClick={() => navigate(`/app/inventory/materials/${material.replacedBy}`)}
            >
                {material.replacedBy}
            </Button>
        </Alert>
      )}

      <Paper variant="outlined" sx={{ p: 3, mb: 3, bgcolor: 'background.paper', borderRadius: 2 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} sx={{ justifyContent: 'space-between', alignItems: 'flex-start' }} spacing={2}>
          <Box>
            <Typography variant="h4" sx={{ fontWeight: 800, color: 'text.primary' }}>{material.officialName}</Typography>
            <Typography variant="body1" color="text.secondary" sx={{ mb: 2 }}>{material.description}</Typography>
            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
              <Chip label={`SKU: ${material.code}`} size="small" sx={{ fontWeight: 700 }} />
              <Chip label={`Unidade: ${material.unitOfMeasure}`} size="small" variant="outlined" />
              <Chip label={`Tipo: ${procurementMap[material.procurementType]}`} size="small" color="primary" variant="outlined" sx={{ fontWeight: 600 }} />
              <StatusChip status={material.category} />
              <StatusChip status={material.status} />
            </Stack>
          </Box>
          {material.imageUrl && (
            <Box 
               component="img" 
               src={material.imageUrl} 
               sx={{ width: 100, height: 100, borderRadius: 2, objectFit: 'cover', border: 1, borderColor: 'divider' }}
               alt={material.officialName}
            />
          )}
        </Stack>
      </Paper>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={effectiveTab} onChange={handleTabChange}>
          {tabs.map((tab) => (
            <Tab key={tab.id} value={tab.id} label={tab.label} sx={{ fontWeight: 700 }} />
          ))}
        </Tabs>
      </Box>

      {/* RENDER ACTIVE TAB CONTENT */}
      {effectiveTab === 'mappings' && (
        <Box>
          <Typography variant="h6" sx={{ mb: 2, fontWeight: 800 }}>Vínculos com Fornecedores</Typography>
          {material.supplierMappings.length === 0 ? (
            <Alert severity="info" variant="outlined">Nenhum fornecedor vinculado a este material ainda.</Alert>
          ) : (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ bgcolor: alpha(theme.palette.primary.main, 0.03) }}>
                    <TableCell sx={{ fontWeight: 800 }}>FORNECEDOR</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>CÓDIGO NO FORNECEDOR</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 800 }}>FATOR CONV.</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 800 }}>ÚLTIMO PREÇO</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {material.supplierMappings.map(mapping => (
                    <TableRow key={mapping.id} hover>
                      <TableCell sx={{ fontWeight: 700, color: 'primary.main' }}>{mapping.supplierName}</TableCell>
                      <TableCell sx={{ fontFamily: 'monospace' }}>{mapping.supplierCode}</TableCell>
                      <TableCell align="right">{mapping.conversionFactor.toFixed(4)}</TableCell>
                      <TableCell align="right">R$ {mapping.lastPrice.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Box>
      )}

      {effectiveTab === 'bom' && (
        <Box>
          <Typography variant="h6" sx={{ mb: 2, fontWeight: 800 }}>Estrutura de Produto (BOM)</Typography>
          <Paper variant="outlined" sx={{ p: 4, textAlign: 'center', bgcolor: alpha(theme.palette.primary.main, 0.02) }}>
             <Typography color="text.secondary">Os dados de BOM ainda não estão disponíveis para este material.</Typography>
          </Paper>
        </Box>
      )}

      {effectiveTab === 'technical' && (
        <Box>
          <Typography variant="h6" sx={{ mb: 2, fontWeight: 800 }}>Dados Técnicos e Fiscais</Typography>
          <Paper variant="outlined" sx={{ p: 3, mb: 3 }}>
            <Stack spacing={2}>
              <Box>
                <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>NCM (NOMENCLATURA COMUM DO MERCOSUL)</Typography>
                <Typography variant="body1" sx={{ fontWeight: 600 }}>{material.ncm || 'Não informado'}</Typography>
              </Box>
              <Divider />
              <Box>
                <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>GTIN / EAN</Typography>
                <Typography variant="body1" sx={{ fontWeight: 600 }}>{material.gtin || 'Não informado'}</Typography>
              </Box>
              <Divider />
              <Box>
                <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>URL DA IMAGEM</Typography>
                <Typography variant="body2" sx={{ fontFamily: 'monospace', wordBreak: 'break-all' }}>{material.imageUrl || 'Não informado'}</Typography>
              </Box>
            </Stack>
          </Paper>

          <Typography variant="h6" sx={{ mb: 2, fontWeight: 800 }}>Trilha de Auditoria</Typography>
          <Paper variant="outlined" sx={{ p: 3, bgcolor: alpha(theme.palette.grey[500], 0.02) }}>
             <Grid container spacing={3}>
                <Grid size={{ xs: 12, sm: 6 }}>
                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <User size={16} color={theme.palette.text.secondary} />
                        <Box>
                            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, display: 'block' }}>CRIADO POR</Typography>
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>{material.createdBy}</Typography>
                        </Box>
                    </Stack>
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <Calendar size={16} color={theme.palette.text.secondary} />
                        <Box>
                            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, display: 'block' }}>ÚLTIMA ALTERAÇÃO POR</Typography>
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>{material.lastModifiedBy}</Typography>
                        </Box>
                    </Stack>
                </Grid>
             </Grid>
          </Paper>
        </Box>
      )}

      <MergeMaterialModal
        open={isMergeModalOpen}
        onClose={() => setIsMergeModalOpen(false)}
        onSuccess={handleMergeSuccess}
        tenantCode={tenantCode}
        obsoleteMaterialCode={material.code}
        obsoleteMaterialName={material.officialName}
      />
    </Box>
  )
}
