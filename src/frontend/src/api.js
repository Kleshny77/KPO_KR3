const API_URL = 'http://localhost:8080';

export async function createOrder(userId, amount, description) {
  const res = await fetch(`${API_URL}/orders/create`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId, amount, description })
  });
  return res.json();
}

export async function topUp(userId, amount) {
  const res = await fetch(`${API_URL}/payments/create`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(userId)
  });
  if (res.ok) {
    // Счет создан, теперь пополняем
    const res2 = await fetch(`${API_URL}/payments/topup`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ userId, amount })
    });
    return res2.json();
  } else {
    // Счет уже есть, просто пополняем
    const res2 = await fetch(`${API_URL}/payments/topup`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ userId, amount })
    });
    return res2.json();
  }
}

export async function getBalance(userId) {
  const res = await fetch(`${API_URL}/payments/balance/${userId}`);
  return res.json();
}

export async function getOrders(userId) {
  const res = await fetch(`${API_URL}/orders?userId=${userId}`);
  return res.json();
}

export async function getOrderStatus(orderId) {
  const res = await fetch(`${API_URL}/orders/${orderId}`);
  return res.json();
} 