import React, { useState, useEffect } from 'react';
import { createOrder, topUp, getBalance, getOrders } from './api';
import { connectOrderStatus } from './OrderStatusSignalR';
import './App.css';

function App() {
  const [userId, setUserId] = useState('user1');
  const [amount, setAmount] = useState(100);
  const [description, setDescription] = useState('Товар');
  const [balance, setBalance] = useState(null);
  const [orders, setOrders] = useState([]);
  const [statusUpdates, setStatusUpdates] = useState([]);
  const [connection, setConnection] = useState(null);

  useEffect(() => {
    if (!userId) return;
    getBalance(userId).then(b => setBalance(b.balance));
    getOrders(userId).then(setOrders);
    if (connection) connection.stop();
    const conn = connectOrderStatus(userId, (msg) => {
      setStatusUpdates(upds => [msg, ...upds]);
      // Автообновление заказов и баланса
      getOrders(userId).then(setOrders);
      getBalance(userId).then(b => setBalance(b.balance));
    });
    setConnection(conn);
    return () => conn && conn.stop();
    // eslint-disable-next-line
  }, [userId]);

  return (
    <div style={{ maxWidth: 500, margin: '40px auto', fontFamily: 'sans-serif' }}>
      <h2>Магазин (микросервисы, SignalR)</h2>
      <div>
        <label>User ID: <input value={userId} onChange={e => setUserId(e.target.value)} /></label>
      </div>
      <div style={{ margin: '10px 0' }}>
        <button onClick={async () => {
          await topUp(userId, amount);
          getBalance(userId).then(b => setBalance(b.balance));
        }}>Пополнить счет на {amount}</button>
        <input type="number" value={amount} onChange={e => setAmount(Number(e.target.value))} style={{ width: 80, marginLeft: 8 }} />
      </div>
      <div>Баланс: <b>{balance !== null ? balance : '...'}</b></div>
      <div style={{ margin: '10px 0' }}>
        <input value={description} onChange={e => setDescription(e.target.value)} placeholder="Описание заказа" />
        <button onClick={async () => {
          await createOrder(userId, amount, description);
          getOrders(userId).then(setOrders);
        }}>Создать заказ на {amount}</button>
      </div>
      <h4>Мои заказы</h4>
      <ul>
        {orders.map(o => (
          <li key={o.id}>{o.description} — {o.amount} — <b>{o.status}</b></li>
        ))}
      </ul>
      <h4>Push-уведомления</h4>
      <ul>
        {statusUpdates.map((u, i) => (
          <li key={i}>Заказ {u.orderId}: <b>{u.status}</b></li>
        ))}
      </ul>
    </div>
  );
}

export default App;
