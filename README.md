Client (Postman/Frontend)
       |
       ↓
[Order API (.NET Core Web API)]
       |
       ↓
[RabbitMQ Kuyruğu (order-queue)]
       |
       ↓
[Worker Service - Order Processor]
       |
       ↓
[Veritabanı - Orders Tablosu]


RabbitMQ çalışıyor olmalı (docker run -d --hostname my-rabbit --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management)

Postman veya Swagger ile aşağıdaki body’yi POST /api/order endpoint’ine gönder:
{
  "customerName": "Ali Veli",
  "product": "Telefon",
  "quantity": 2
}
