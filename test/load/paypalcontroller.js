import http from "k6/http";
import { check, fail } from "k6";

const REGIONS = __ENV.REGIONS.split(",") || ["US", "EU"];
const BILLING_URL = __ENV.BILLING_URL;
const BILLING_PAYPAL_WEBHOOK_KEY = __ENV.BILLING_PAYPAL_WEBHOOK_KEY;

export const options = {
  scenarios: {
    single_user: {
      executor: "constant-vus",
      vus: 1,
      duration: "30s",
      tags: { test_type: "single_user" },
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<350"],
  },
};

function generateFormData() {
  const accountCredit = "1";
  const organizationId = crypto.randomUUID();
  const region = REGIONS[Math.floor(Math.random() * REGIONS.length)];
  return {
    custom: `organization_id:${organizationId},account_credit:${accountCredit},region:${region}`,
  };
}

export default function () {
  const formData = generateFormData();
  const params = {
    headers: {
      "Content-Type": "application/x-www-form-urlencoded",
    },
  };
  const res = http.post(
    `${BILLING_URL}/paypal/ipn/?key=${BILLING_PAYPAL_WEBHOOK_KEY}`,
    formData,
    params,
  );
  if (
    !check(res, {
      "status is 200": (r) => r.status === 200,
    })
  ) {
    console.error(
      `PayPal IPN request failed with status ${res.status}: ${res.body}`,
    );
    fail("PayPal IPN status was *not* 200");
  }
}
