# PixelRun Server 

## Требования

Перед запуском убедитесь, что у вас установлены:

* Docker
* Docker Compose

Проверить можно командами:

```bash
docker --version
docker compose version
```

---

## Запуск проекта

В корне проекта выполните:

```bash
docker compose up --build
```

---

## База данных

* PostgreSQL поднимается автоматически
* Миграции применяются при старте сервера
* Данные сохраняются в volume (`db-data`)

---

## Доступ к API

После запуска:

* API: http://localhost:8080
* Swagger UI: http://localhost:8080/swagger
* Frontend: http://localhost:3000

