# Контрольная точка: API + PostgreSQL + Docker Compose

Проект представляет собой REST API на .NET 8 для управления задачами, работающий в связке с базой данных PostgreSQL. Вся система запускается через Docker Compose.

## Запуск проекта

1. Убедитесь, что у вас установлен и запущен Docker Desktop.
2. В корневой папке проекта выполните команду:
   ```bash
   docker compose up --build -d