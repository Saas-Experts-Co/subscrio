/**
 * Subscrio configuration interface
 */
export interface SubscrioConfig {
  database: {
    connectionString: string;
    ssl?: boolean;
    poolSize?: number;
  };
  adminPassphrase?: string;
  stripe?: {
    secretKey: string;
  };
  logging?: {
    level: 'debug' | 'info' | 'warn' | 'error';
  };
}

