/**
 * Utility for formatting dates in the pt-BR locale.
 */
export const RelativeDateFormatter = {
  /**
   * Formats a date string or Date object to pt-BR.
   * @param date - The date to format.
   * @param includeTime - Whether to include the time.
   * @returns Formatted string.
   */
  format: (date: string | Date | null | undefined, includeTime = true): string => {
    if (!date) return '-'
    const d = typeof date === 'string' ? new Date(date) : date

    return new Intl.DateTimeFormat('pt-BR', {
      dateStyle: 'short',
      timeStyle: includeTime ? 'short' : undefined
    }).format(d)
  }
}

/**
 * Alias for RelativeDateFormatter.format for backward compatibility or direct use.
 */
export const formatRelativeDate = RelativeDateFormatter.format;

/**
 * Utility for formatting technical IDs (UUIDs) and business keys.
...
 */
export const TechnicalIdFormatter = {
  /**
   * Truncates a UUID to its first 8 characters.
   * @param id - The UUID to truncate.
   * @returns Truncated ID string.
   */
  truncate: (id: string): string => {
    if (!id || id.length < 8) return id
    return id.substring(0, 8).toUpperCase()
  },

  /**
   * Formats a business document reference (e.g., NF-e).
   * @param prefix - The type of document (e.g., NF-e).
   * @param value - The document number.
   * @returns Formatted business key.
   */
  formatBusinessKey: (prefix: string, value: string): string => {
    if (!value) return '-'
    return `${prefix} ${value}`
  },

  /**
   * Copies a value to the clipboard.
   * @param value - The string to copy.
   */
  copyToClipboard: async (value: string): Promise<void> => {
    try {
      await navigator.clipboard.writeText(value)
    } catch (err) {
      console.error('Failed to copy text: ', err)
    }
  }
}

/**
 * Utility for formatting monetary values to BRL.
 */
export const CurrencyFormatter = {
  /**
   * Formats a number to BRL currency string (e.g. R$ 1.500,00)
   * @param value - The monetary value to format.
   * @returns Formatted currency string or '-' if null/undefined.
   */
  format: (value: number | string | null | undefined): string => {
    if (value === null || value === undefined || value === '') return '-';
    const num = typeof value === 'string' ? parseFloat(value) : value;
    if (isNaN(num)) return '-';
    
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(num);
  }
}
