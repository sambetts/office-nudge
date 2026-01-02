
export interface BaseDTO { id?: string }

export interface ServiceConfiguration {
  storageInfo: StorageInfo;
}

export interface StorageInfo {
  accountURI: string;
  sharedAccessToken: string;
}


