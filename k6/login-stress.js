import http from 'k6/http';
import { check } from 'k6';

// ---------------------------------------------------------------------------
// FitnessApp — login STRESS test: koliko logina/sek app izdrži
//
// Postupno diže broj zahtjeva u sekundi (RPS) i gleda kad krenu greške /
// kad latencija probije prag. Zadnji "zeleni" stupanj = praktični max.
//
// Pokretanje (lokalno, preporuka):
//   k6 run C:\k6\login-stress.js
//
// Override targeta (RPS na vrhu) i trajanja stupnja:
//   k6 run -e PEAK=300 C:\k6\login-stress.js
//
// PROD (OPREZ — shared hosting, lako ga srušiš / dobiješ ban):
//   k6 run -e BASE_URL=https://fitness-application.com -e PEAK=50 C:\k6\login-stress.js
// ---------------------------------------------------------------------------

const BASE_URL = __ENV.BASE_URL || 'https://fitness-application.com';
const EMAIL    = __ENV.EMAIL    || 'klijent.demo@fitness.local';
const PASSWORD = __ENV.PASSWORD || 'demo12';
const PEAK     = Number(__ENV.PEAK) || 200;   // ciljani vrh RPS-a

export const options = {
  scenarios: {
    ramp_logins: {
      executor: 'ramping-arrival-rate',
      startRate: 5,                 // kreni s 5 logina/sek
      timeUnit: '1s',
      preAllocatedVUs: 50,
      maxVUs: 2000,                 // gornja granica VU-ova koje smije rezervirati
      stages: [
        { target: Math.round(PEAK * 0.25), duration: '20s' },
        { target: Math.round(PEAK * 0.50), duration: '20s' },
        { target: Math.round(PEAK * 0.75), duration: '20s' },
        { target: PEAK,                     duration: '20s' },
        { target: PEAK,                     duration: '20s' }, // drži vrh
      ],
    },
  },
  thresholds: {
    // Test "pukne" (abortira) čim pređe ove granice — tu je tvoj plafon.
    http_req_failed:   [{ threshold: 'rate<0.01', abortOnFail: true }],
    http_req_duration: [{ threshold: 'p(95)<1000', abortOnFail: true }],
  },
};

export default function () {
  const res = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email: EMAIL, password: PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } }
  );
  check(res, { 'status 200': (r) => r.status === 200 });
}
