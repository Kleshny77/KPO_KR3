import React, { useState, useEffect, useRef } from 'react';
import {
  createOrder,
  topUp,
  getBalance,
  getOrders
} from './api';
import { connectOrderStatus } from './OrderStatusSignalR';
import './App.css';

function generateGuid() {
  let guid = ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c =>
    (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
  );
  return guid;
}

function isGuid(str) {
  return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-4[0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$/.test(str);
}

const StatusBadge = ({ status }) => (
  <span className={
    status === 'FINISHED' ? 'badge badge-success' :
    status === 'CANCELLED' ? 'badge badge-fail' :
    'badge badge-pending'
  }>{status}</span>
);

const Loader = () => <span className="loader" />;

const Toast = ({ message, type, onClose }) => (
  <div className={`toast ${type}`}>{message}<button className="toast-close" onClick={onClose}>×</button></div>
);

function App() {
  const [userId, setUserId] = useState('');
  const [amount, setAmount] = useState(100);
  const [description, setDescription] = useState('Товар');
  const [balance, setBalance] = useState(null);
  const [orders, setOrders] = useState([]);
  const [statusUpdates, setStatusUpdates] = useState([]);
  const [connection, setConnection] = useState(null);
  const [loading, setLoading] = useState(false);
  const [btnLoading, setBtnLoading] = useState({});
  const [toast, setToast] = useState(null);
  const userIdRef = useRef();

  useEffect(() => { userIdRef.current && userIdRef.current.focus(); }, []);

  useEffect(() => {
    if (!isGuid(userId)) {
      const guid = generateGuid();
      setUserId(guid);
    }
  }, []);

  const refresh = async (uid = userId) => {
    setLoading(true);
    if (!isGuid(uid)) {
      setToast({ message: 'Некорректный userId! Попробуйте создать счёт заново.', type: 'info' });
      setLoading(false);
      return;
    }
    console.log('Refresh userId:', uid);
    try {
      const bRes = await fetch(`http://localhost:8080/payments/api/Accounts/balance/${uid}`);
      if (bRes.status === 404) {
        setBalance(null);
        setOrders([]);
        setToast({ message: 'Счёт ещё не создан, нажмите \'Создать счёт\'', type: 'info' });
        setLoading(false);
        return;
      }
      if (!bRes.ok) throw new Error('Ошибка при получении баланса');
      const b = await bRes.json();
      setBalance(b.balance);
      const oRes = await fetch(`http://localhost:8080/orders/api/Orders?userId=${uid}`);
      if (oRes.status === 404) {
        setOrders([]);
        setLoading(false);
        return;
      }
      if (!oRes.ok) throw new Error('Ошибка при получении заказов');
      setOrders(await oRes.json());
    } catch (e) {
      setToast({ message: e.message || 'Ошибка при загрузке данных', type: 'error' });
    }
    setLoading(false);
  };

  useEffect(() => {
    if (!userId) return;
    refresh(userId);
    if (connection) connection.stop();
    const conn = connectOrderStatus(userId, (msg) => {
      setStatusUpdates(upds => [msg, ...upds]);
      refresh(userId);
    });
    setConnection(conn);
    return () => conn && conn.stop();
  }, [userId]);

  useEffect(() => {
    if (toast) {
      const t = setTimeout(() => setToast(null), 3000);
      return () => clearTimeout(t);
    }
  }, [toast]);

  const handleCopyUserId = () => {
    navigator.clipboard.writeText(userId);
    setToast({ message: 'User ID скопирован!', type: 'success' });
  };

  const handleCreateAccount = async () => {
    setBtnLoading(l => ({ ...l, create: true }));
    const newGuid = generateGuid();
    setUserId(newGuid);
    if (!isGuid(newGuid)) {
      setToast({ message: 'Ошибка генерации userId! Попробуйте ещё раз.', type: 'error' });
      setBtnLoading(l => ({ ...l, create: false }));
      return;
    }
    console.log('CreateAccount userId:', newGuid);
    try {
      const res = await fetch('http://localhost:8080/payments/api/Accounts/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: newGuid })
      });
      if (res.ok) {
        setToast({ message: 'Счёт успешно создан!', type: 'success' });
        refresh(newGuid);
      } else {
        let msg = 'Ошибка при создании счёта';
        try {
          const data = await res.json();
          msg = data?.errors?.[0] || data?.message || msg;
        } catch (e) {
          const text = await res.text();
          if (text) msg = text;
        }
        setToast({ message: msg, type: 'error' });
      }
    } catch (e) {
      setToast({ message: e.message || 'Ошибка при создании счёта', type: 'error' });
    }
    setBtnLoading(l => ({ ...l, create: false }));
  };

  const handleTopUp = async () => {
    setBtnLoading(l => ({ ...l, topup: true }));
    try {
      await topUp(userId, amount);
      setToast({ message: 'Баланс успешно пополнен!', type: 'success' });
      refresh();
    } catch (e) {
      setToast({ message: 'Ошибка при пополнении', type: 'error' });
    }
    setBtnLoading(l => ({ ...l, topup: false }));
  };

  const handleCreateOrder = async () => {
    setBtnLoading(l => ({ ...l, order: true }));
    try {
      await createOrder(userId, amount, description);
      setToast({ message: 'Заказ создан!', type: 'success' });
      refresh();
    } catch (e) {
      setToast({ message: 'Ошибка при создании заказа', type: 'error' });
    }
    setBtnLoading(l => ({ ...l, order: false }));
  };

  return (
    <div className="main-container modern-ui">
      <h2>Магазин <span className="subtitle">(микросервисы, SignalR)</span></h2>
      <div className="block">
        <label htmlFor="userId">User ID:</label>
        <div className="user-id-row">
          <input ref={userIdRef} id="userId" value={userId} readOnly className="input" autoComplete="off" />
          <button className="btn icon-btn" title="Скопировать" onClick={handleCopyUserId}>
            <span role="img" aria-label="copy">📋</span>
          </button>
        </div>
        <div className="input-hint">Ваш уникальный идентификатор пользователя (GUID).<br/>Пример: <span style={{fontFamily:'monospace'}}>b1a7e2c3-4d5f-6789-abcd-1234567890ef</span></div>
        <button onClick={handleCreateAccount} disabled={btnLoading.create} className="btn primary">
          {btnLoading.create ? <Loader /> : <><span role="img" aria-label="plus">➕</span> Создать счёт (новый User ID)</>}
        </button>
      </div>
      <div className="block">
        <div className="balance-row">
          <span>Баланс: <b>{balance !== null ? balance : <Loader />}</b></span>
          <button onClick={() => refresh()} disabled={loading} className="btn icon-btn" title="Обновить">
            <span role="img" aria-label="refresh">🔄</span>
          </button>
        </div>
      </div>
      <div className="block">
        <label>Сумма:
          <input type="number" value={amount} min={1} max={1000000} onChange={e => setAmount(Number(e.target.value))} className="input" placeholder="Сумма" />
        </label>
        <button onClick={handleTopUp} disabled={btnLoading.topup} className="btn primary">
          {btnLoading.topup ? <Loader /> : <><span role="img" aria-label="plus">💸</span> Пополнить</>}
        </button>
      </div>
      <div className="block">
        <label>Описание заказа:
          <input value={description} onChange={e => setDescription(e.target.value)} placeholder="Описание заказа" className="input" />
        </label>
        <button onClick={handleCreateOrder} disabled={btnLoading.order} className="btn primary">
          {btnLoading.order ? <Loader /> : <><span role="img" aria-label="cart">🛒</span> Создать заказ</>}
        </button>
      </div>
      <div className="block">
        <h4>Мои заказы</h4>
        <button onClick={() => refresh()} disabled={loading} className="btn icon-btn" title="Обновить заказы">
          <span role="img" aria-label="refresh">🔄</span>
        </button>
        <table className="orders-table">
          <thead>
            <tr><th>Описание</th><th>Сумма</th><th>Статус</th></tr>
          </thead>
          <tbody>
            {orders.length === 0 ? (
              <tr><td colSpan={3} style={{ textAlign: 'center', color: '#b0b0b0' }}>Нет заказов</td></tr>
            ) : orders.map(o => (
              <tr key={o.id}>
                <td>{o.description}</td>
                <td>{o.amount}</td>
                <td><StatusBadge status={o.status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="block">
        <h4>Push-уведомления</h4>
        <ul className="push-list">
          {statusUpdates.length === 0 ? (
            <li style={{ color: '#b0b0b0' }}>Нет уведомлений</li>
          ) : statusUpdates.map((u, i) => (
            <li key={i}><b>Заказ {u.orderId}:</b> <StatusBadge status={u.status} /></li>
          ))}
        </ul>
      </div>
      {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
      {loading && <div className="loading">Загрузка...</div>}
    </div>
  );
}

export default App;