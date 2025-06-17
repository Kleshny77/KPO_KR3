import { HubConnectionBuilder } from '@microsoft/signalr';

const SIGNALR_URL = 'http://localhost:8080/order-status';

export function connectOrderStatus(userId, onStatusChanged) {
  const connection = new HubConnectionBuilder()
    .withUrl(SIGNALR_URL)
    .withAutomaticReconnect()
    .build();

  connection.on('OrderStatusChanged', onStatusChanged);

  connection.start().then(() => {
    connection.invoke('JoinUserGroup', userId);
  });

  return connection;
} 