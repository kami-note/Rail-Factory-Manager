/**
 * Utility functions for formatting/masking inputs in the frontend.
 */
export const Masks = {
  /**
   * Only keeps numeric characters from a string.
   */
  cleanDigits: (v: string): string => {
    return v.replace(/\D/g, '');
  },

  /**
   * Formats a string into CPF (999.999.999-99) or CNPJ (99.999.999/9999-99) based on digit length.
   */
  cpfCnpj: (v: string): string => {
    const digits = v.replace(/\D/g, '');
    if (digits.length <= 11) {
      return Masks.cpf(digits);
    }
    return Masks.cnpj(digits);
  },

  /**
   * Formats a string to CPF: 999.999.999-99
   */
  cpf: (v: string): string => {
    const digits = v.replace(/\D/g, '').slice(0, 11);
    if (digits.length <= 3) return digits;
    if (digits.length <= 6) return `${digits.slice(0, 3)}.${digits.slice(3)}`;
    if (digits.length <= 9) return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6)}`;
    return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9)}`;
  },

  /**
   * Formats a string to CNPJ: 99.999.999/9999-99
   */
  cnpj: (v: string): string => {
    const digits = v.replace(/\D/g, '').slice(0, 14);
    if (digits.length <= 2) return digits;
    if (digits.length <= 5) return `${digits.slice(0, 2)}.${digits.slice(2)}`;
    if (digits.length <= 8) return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5)}`;
    if (digits.length <= 12) return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5, 8)}/${digits.slice(8)}`;
    return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5, 8)}/${digits.slice(8, 12)}-${digits.slice(12)}`;
  },

  /**
   * Formats a string to CEP: 99999-999
   */
  cep: (v: string): string => {
    const digits = v.replace(/\D/g, '').slice(0, 8);
    if (digits.length <= 5) return digits;
    return `${digits.slice(0, 5)}-${digits.slice(5)}`;
  },

  /**
   * Formats a string to Phone: (99) 99999-9999 or (99) 9999-9999
   */
  phone: (v: string): string => {
    const digits = v.replace(/\D/g, '').slice(0, 11);
    if (digits.length <= 2) return digits.length > 0 ? `(${digits}` : '';
    if (digits.length <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
    if (digits.length <= 10) return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
  },

  /**
   * Formats a vehicle license plate: AAA-9999 or AAA-9A99 (Mercosul)
   */
  plate: (v: string): string => {
    const clean = v.replace(/[^A-Za-z0-9]/g, '').toUpperCase().slice(0, 7);
    if (clean.length <= 3) return clean;
    return `${clean.slice(0, 3)}-${clean.slice(3)}`;
  }
};

/**
 * Validation utilities for frontend forms.
 */
export const Validators = {
  /**
   * Validates standard email structure.
   */
  email: (v: string): boolean => {
    if (!v) return false;
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(v.trim());
  },

  /**
   * Validates a CPF number using the verification digit algorithm.
   */
  cpf: (v: string): boolean => {
    const clean = v.replace(/\D/g, '');
    if (clean.length !== 11) return false;
    if (/^(\d)\1{10}$/.test(clean)) return false; // Reject all same digits

    let sum = 0;
    for (let i = 0; i < 9; i++) {
      sum += parseInt(clean[i]) * (10 - i);
    }
    let rev = 11 - (sum % 11);
    if (rev === 10 || rev === 11) rev = 0;
    if (rev !== parseInt(clean[9])) return false;

    sum = 0;
    for (let i = 0; i < 10; i++) {
      sum += parseInt(clean[i]) * (11 - i);
    }
    rev = 11 - (sum % 11);
    if (rev === 10 || rev === 11) rev = 0;
    if (rev !== parseInt(clean[10])) return false;

    return true;
  },

  /**
   * Validates a CNPJ number using the verification digit algorithm.
   */
  cnpj: (v: string): boolean => {
    const clean = v.replace(/\D/g, '');
    if (clean.length !== 14) return false;
    if (/^(\d)\1{13}$/.test(clean)) return false; // Reject all same digits

    let size = clean.length - 2;
    let numbers = clean.substring(0, size);
    const digits = clean.substring(size);
    let sum = 0;
    let pos = size - 7;
    for (let i = size; i >= 1; i--) {
      sum += parseInt(numbers.charAt(size - i)) * pos--;
      if (pos < 2) pos = 9;
    }
    let result = sum % 11 < 2 ? 0 : 11 - (sum % 11);
    if (result !== parseInt(digits.charAt(0))) return false;

    size = size + 1;
    numbers = clean.substring(0, size);
    sum = 0;
    pos = size - 7;
    for (let i = size; i >= 1; i--) {
      sum += parseInt(numbers.charAt(size - i)) * pos--;
      if (pos < 2) pos = 9;
    }
    result = sum % 11 < 2 ? 0 : 11 - (sum % 11);
    if (result !== parseInt(digits.charAt(1))) return false;

    return true;
  },

  /**
   * Validates that the input is a valid CPF or CNPJ.
   */
  cpfCnpj: (v: string): boolean => {
    const clean = v.replace(/\D/g, '');
    if (clean.length === 11) return Validators.cpf(clean);
    if (clean.length === 14) return Validators.cnpj(clean);
    return false;
  },

  /**
   * Validates a CEP is 8 digits.
   */
  cep: (v: string): boolean => {
    const clean = v.replace(/\D/g, '');
    return clean.length === 8;
  },

  /**
   * Validates a phone is 10 or 11 digits.
   */
  phone: (v: string): boolean => {
    const clean = v.replace(/\D/g, '');
    return clean.length === 10 || clean.length === 11;
  },

  /**
   * Validates standard or Mercosul vehicle plate: AAA-9999 or AAA-9A99
   */
  plate: (v: string): boolean => {
    const clean = v.replace(/[^A-Za-z0-9]/g, '');
    if (clean.length !== 7) return false;
    const standardRegex = /^[A-Z]{3}[0-9]{4}$/;
    const mercosulRegex = /^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/;
    return standardRegex.test(clean) || mercosulRegex.test(clean);
  }
};
