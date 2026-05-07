export type StatusColor = 'success' | 'warning' | 'error' | 'info' | 'default'

export interface StatusMapping {
  label: string
  color: StatusColor
}

/**
 * Maps system statuses to human-readable labels and semantic colors.
 * Supports statuses from SupplyChain and Inventory modules.
 */
export const HumanizedStatusMapping: Record<string, StatusMapping> = {
  // Supply Chain - Material Receipt
  'Registered': { label: 'Registrado', color: 'info' },
  'InConference': { label: 'Em Conferência', color: 'warning' },
  'Conferred': { label: 'Conferido', color: 'success' },
  'Cancelled': { label: 'Cancelado', color: 'error' },

  // Inventory - Balance
  'Pending': { label: 'Pendente', color: 'warning' },
  'Available': { label: 'Disponível', color: 'success' },
  'Blocked': { label: 'Bloqueado', color: 'error' },
  'Consumed': { label: 'Consumido', color: 'default' },
  
  // Generic / Default
  'Default': { label: 'Desconhecido', color: 'default' }
}

/**
 * Gets the humanized status mapping for a given status string.
 * @param status - The raw status string from the API.
 * @returns The mapping with label and color.
 */
export const getStatusMapping = (status: string): StatusMapping => {
  return HumanizedStatusMapping[status] || HumanizedStatusMapping['Default']
}
