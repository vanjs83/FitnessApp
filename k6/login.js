import http from 'k6/http';
import { check, sleep } from 'k6';

// ---------------------------------------------------------------------------
// FitnessApp — login load/smoke test
//
// Pokretanje (PowerShell):
//   k6 run C:\k6\login.js
//
// Sve se može override-ati preko env varijabli, npr:
//   k6 run -e BASE_URL=https://fitness-application.com -e EMAIL=admin@x.com -e PASSWORD=Tajna1! C:\k6\login.js
//   k6 run -e VUS=10 -e DURATION=30s C:\k6\login.js
// ---------------------------------------------------------------------------

const BASE_URL = __ENV.BASE_URL || 'https://fitness-application.com';
const EMAIL    = __ENV.EMAIL    || 'tihomir.vanjurek@gmail.com';
const PASSWORD = __ENV.PASSWORD || 'demo12';
const FULLNAME = __ENV.FULLNAME || 'tihomirvanjurek'
const ROLE     = __ENV.ROLE || 'Trainer'

export const options = {
  vus:      Number(__ENV.VUS)      || 1000,
  duration: __ENV.DURATION         || '60s',
  thresholds: {
    http_req_failed:   ['rate<0.01'],   // <1% grešaka
    http_req_duration: ['p(95)<800'],   // 95% zahtjeva ispod 800ms
  },
};

export default function () {
  const id = `${__VU}-${__ITER}-${Date.now()}`;

  const res = http.post(
    `${BASE_URL}/api/v1/auth/register`,
    JSON.stringify({
      fullname: `user_${id}`,
      email: `user_${id}@test.com`,
      password: PASSWORD,
      role: ROLE
    }),
    { headers: { 'Content-Type': 'application/json' } }
  );

 check(res, {
  'status OK': (r) => r.status === 200 || r.status === 201,
  'ima id korisnika': (r) => {
    try {
      return !!r.json('id') || !!r.json('userId');
    } catch {
      return false;
    }
  },
});
 // sleep(1);
}
