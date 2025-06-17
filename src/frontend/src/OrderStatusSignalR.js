import { HubConnectionBuilder } from '@microsoft/signalr';

const SIGNALR_URL = 'http://localhost:5001/orderStatusHub';

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