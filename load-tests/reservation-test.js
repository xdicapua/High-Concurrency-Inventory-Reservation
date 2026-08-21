import http from 'k6/http';
import { check } from 'k6';
import { Counter } from 'k6/metrics';

// Contadores personalizados para validar consistencia
const successfulReservations = new Counter('successful_reservations');
const outOfStockResponses = new Counter('out_of_stock_responses');

export const options = {
    scenarios: {
        flash_sale_spike: {
            executor: 'constant-vus',
            vus: 50,              // 50 usuarios concurrentes golpeando la API
            duration: '10s',       // durante 10 segundos continuos
        },
    },
    thresholds: {
        http_req_duration: ['p(95)<50', 'p(99)<100'], // Criterio de aceptación de latencia
    },
};

function generateUUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = (Math.random() * 16) | 0;
        const v = c === 'x' ? r : (r & 0x3) | 0x8;
        return v.toString(16);
    });
}

export default function () {
    const url = 'http://localhost:5233/api/v1/reservations';
    const payload = JSON.stringify({
        sku: 'PROD-001',
        userId: generateUUID(),
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.post(url, payload, params);

    if (res.status === 200) {
        successfulReservations.add(1);
        check(res, {
            'status is 200': (r) => r.status === 200,
        });
    } else if (res.status === 409) {
        outOfStockResponses.add(1);
        check(res, {
            'status is 409 out of stock': (r) => r.status === 409,
        });
    } else {
        check(res, {
            'unexpected status': (r) => false,
        });
    }
}