#!/bin/bash
# =============================================================================
# RailFactory: Create Test Dispatch Script
# Automates the logistics flow to trigger PlugNotas (fiscal) and Asaas (payment)
# =============================================================================
set -e

BASE_URL="https://unarticulative-unelectrical-shavon.ngrok-free.dev/api"
HEADERS=(-H "X-Dev-User: yurinote666@gmail.com" -H "X-Tenant-Code: dev" -H "Content-Type: application/json")

CARRIER_ID="28dc9872-7b2b-47e9-86f9-353ca1ded188"
VEHICLE_ID="90637960-b17a-4702-a798-75998e972b88"
DRIVER_ID="33333333-3333-3333-3333-333333333333"

echo "--------------------------------------------------------"
echo "🚀 1. Creating Shipment Order..."
echo "--------------------------------------------------------"
ORDER_RES=$(curl -s -X POST "${BASE_URL}/logistics/shipment-orders" \
  "${HEADERS[@]}" \
  -d '{
    "Notes": "Pedido de Teste para Emissão PlugNotas & Boleto Asaas",
    "RecipientCnpj": "08114280956",
    "RecipientName": "MARCOS SILVA TESTE",
    "RecipientEmail": "yurinote666@gmail.com",
    "RecipientStreet": "Avenida Paulista",
    "RecipientNumber": "1000",
    "RecipientDistrict": "Bela Vista",
    "RecipientCity": "São Paulo",
    "RecipientState": "SP",
    "RecipientZipCode": "01310100",
    "NatureOfOperation": "VENDA",
    "RecipientIe": "ISENTO",
    "ModalidadeFrete": 0
  }')

echo "Response: ${ORDER_RES}"
ORDER_ID=$(echo "${ORDER_RES}" | grep -oP '"id":"\K[^"]+')
echo "--> Created Order ID: ${ORDER_ID}"

echo "--------------------------------------------------------"
echo "📦 2. Adding Item to Shipment Order..."
echo "--------------------------------------------------------"
ITEM_RES=$(curl -s -X POST "${BASE_URL}/logistics/shipment-orders/${ORDER_ID}/items" \
  "${HEADERS[@]}" \
  -d '{
    "MaterialCode": "MAT-FISCAL-001",
    "Quantity": 1.0,
    "UnitOfMeasure": "UN",
    "WeightKg": 10.0,
    "VolumeCbm": 0.1,
    "NcmCode": "84713012",
    "CfopCode": "5102",
    "UnitValue": 150.0,
    "TaxBaseIcms": 150.0,
    "IcmsRate": 18.0,
    "IcmsOrigin": 0,
    "IcmsCst": "00",
    "PisCst": "07",
    "CofinsCst": "07",
    "IpiRate": 0,
    "IpiCst": "99"
  }')

echo "Response: ${ITEM_RES}"

echo "--------------------------------------------------------"
echo "🔄 3. Advancing Order Status to ReadyToShip..."
echo "--------------------------------------------------------"
echo "   - Start Picking..."
curl -s -X PUT "${BASE_URL}/logistics/shipment-orders/${ORDER_ID}/start-picking" "${HEADERS[@]}"
echo "   - Start Packing..."
curl -s -X PUT "${BASE_URL}/logistics/shipment-orders/${ORDER_ID}/start-packing" "${HEADERS[@]}"
echo "   - Mark Ready To Ship..."
curl -s -X PUT "${BASE_URL}/logistics/shipment-orders/${ORDER_ID}/ready-to-ship" "${HEADERS[@]}"
echo "--> Order is now ReadyToShip."

echo "--------------------------------------------------------"
echo "🚚 4. Creating Dispatch..."
echo "--------------------------------------------------------"
DISPATCH_RES=$(curl -s -X POST "${BASE_URL}/logistics/dispatches" \
  "${HEADERS[@]}" \
  -d "{
    \"ShipmentOrderId\": \"${ORDER_ID}\",
    \"CarrierId\": \"${CARRIER_ID}\",
    \"VehicleId\": \"${VEHICLE_ID}\",
    \"DriverPersonId\": \"${DRIVER_ID}\",
    \"VehiclePlate\": \"BRA2S19\",
    \"VehicleRntrc\": \"12345678\",
    \"DriverCpf\": \"45678912300\",
    \"DriverName\": \"Marcos Oliveira\"
  }")

echo "Response: ${DISPATCH_RES}"
DISPATCH_ID=$(echo "${DISPATCH_RES}" | grep -oP '"id":"\K[^"]+')
echo "--> Created Dispatch ID: ${DISPATCH_ID}"

echo "--------------------------------------------------------"
echo "📋 5. Dispatching: Conference & Ship (Triggers Plugins)..."
echo "--------------------------------------------------------"
echo "   - Conference Dispatch..."
curl -s -X PUT "${BASE_URL}/logistics/dispatches/${DISPATCH_ID}/conference" "${HEADERS[@]}"
echo "   - Shipping Dispatch (Triggers Outbox)..."
curl -s -X PUT "${BASE_URL}/logistics/dispatches/${DISPATCH_ID}/ship" "${HEADERS[@]}"

echo "--------------------------------------------------------"
echo "✅ Flow Completed! Check background logs/dashboards."
echo "   - Dispatch ID: ${DISPATCH_ID}"
echo "--------------------------------------------------------"
