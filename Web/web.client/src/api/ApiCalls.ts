import { ServiceConfiguration } from "../apimodels/Models";
import { BaseAxiosApiLoader } from "./AxiosApiLoader";


export const getClientConfig = async (loader: BaseAxiosApiLoader): Promise<ServiceConfiguration> => {
  return loader.loadFromApi('api/AppInfo/GetClientConfig', 'POST');
}
