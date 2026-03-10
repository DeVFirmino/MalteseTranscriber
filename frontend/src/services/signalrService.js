import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { BACKEND_URL, HUB_PATH } from '../constants/api';

/**
 * Creates a configured SignalR HubConnection.
 * Centralises connection options so they are not scattered across hooks.
 */
export function buildHubConnection() {
  return new HubConnectionBuilder()
    .withUrl(`${BACKEND_URL}${HUB_PATH}`)
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
}
